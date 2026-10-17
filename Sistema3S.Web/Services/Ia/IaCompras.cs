using System.Data;
using Microsoft.Data.SqlClient;
using Sistema3S.Web.Services.Bi;
namespace Sistema3S.Web.Services.Ia;

public sealed class IaCompras(BiConexion conexion,IConfiguration config)
{
    public async Task<long> Crear(long recommendation,IaSolicitud input,int user,CancellationToken ct)
    {
        if(!config.GetValue("Ia:HistorialOperativoVerificado",false))throw new ArgumentException("Verifica el historial operativo antes de preparar una compra a partir de estimaciones.");
        if(input.Cantidad<1||input.Cantidad>1000000||input.IdProveedor<=0||string.IsNullOrWhiteSpace(input.Justificacion)||input.Justificacion.Length>500)
            throw new ArgumentException("Selecciona proveedor, cantidad positiva y una justificación de la solicitud.");
        await using var c=await conexion.AbrirAsync(ct);await Sistema3S.Web.Services.Seguridad.ConexionAuditada.PrepararAsync(c,ct);await using var cmd=c.CreateCommand();cmd.CommandText="""
        SET XACT_ABORT ON; BEGIN TRANSACTION;
        DECLARE @product int=(SELECT IdProducto FROM ia.Recomendacion WITH(UPDLOCK,HOLDLOCK) WHERE Id=@r AND Procedencia='operativo' AND Estado='Aceptada' AND Cantidad>0);
        IF @product IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.Proveedor WHERE IdProveedor=@v AND Estado=1)
        OR NOT EXISTS(SELECT 1 FROM ia.Recomendacion r JOIN ia.Ejecucion e ON e.Id=r.IdEjecucion WHERE r.Id=@r AND e.Corte=DATEADD(day,-1,@today) AND e.Estado='Completada')
        OR EXISTS(SELECT 1 FROM ia.Recomendacion r JOIN ia.Ejecucion e ON e.Id=r.IdEjecucion WHERE r.Id=@r AND (JSON_VALUE(e.Resultado,'$.version')='3s-ia-4' OR (JSON_VALUE(e.Resultado,'$.version')='3s-ia-5' AND NOT EXISTS(SELECT 1 FROM OPENJSON(e.Resultado,'$.products') WITH(id int '$.id',accepted bit '$.accepted') p WHERE p.id=@product AND p.accepted=1))))
        OR NOT EXISTS(SELECT 1 FROM ia.Abastecimiento a WHERE a.IdProducto=@product AND a.VerificadaHasta>=@today AND (a.EntradaConfirmada=0 OR a.FechaEntrada>@today))
        OR EXISTS(SELECT 1 FROM ia.Recomendacion r WHERE r.Procedencia='operativo' AND r.IdProducto=@product AND r.Id>@r)
        THROW 51000,'Recomendación no apta para preparar una compra.',1;
        IF EXISTS(SELECT 1 FROM ia.SolicitudCompra WHERE IdRecomendacion=@r)
        BEGIN SELECT Id FROM ia.SolicitudCompra WHERE IdRecomendacion=@r; COMMIT; RETURN; END;
        DECLARE @price decimal(18,4)=(SELECT SUM(v.Total)/NULLIF(SUM(CAST(v.Cantidad AS decimal(28,6))),0)
        FROM bi.FactCompra v JOIN dbo.Producto p ON p.IdElementoCatalogo=v.IdElementoCatalogo
        WHERE p.IdProducto=@product AND v.IdProveedor=@v AND v.Procedencia='operativo' AND v.Estado IN(N'Registrada',N'Confirmada')
        AND v.SinDetalleValido=0 AND v.Cantidad>0 AND v.Total>0 AND v.Fecha>=DATEADD(day,-90,@today) AND v.Fecha<@today);
        INSERT ia.SolicitudCompra(IdRecomendacion,IdProducto,IdProveedor,Cantidad,PrecioConIgv,Justificacion,IdUsuario)
        VALUES(@r,@product,@v,@q,@price,@reason,@u);
        SELECT CAST(SCOPE_IDENTITY() AS bigint); COMMIT;
        """;
        cmd.Parameters.AddWithValue("@r",recommendation);cmd.Parameters.AddWithValue("@v",input.IdProveedor);cmd.Parameters.AddWithValue("@q",input.Cantidad);
        cmd.Parameters.AddWithValue("@reason",input.Justificacion);cmd.Parameters.AddWithValue("@u",user);cmd.Parameters.AddWithValue("@today",IaReglas.Hoy.ToDateTime(TimeOnly.MinValue));
        try{return Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));}catch(SqlException ex)when(ex.Number==51000){throw new ArgumentException("Acepta una recomendación operativa reciente con abastecimiento verificado. La referencia técnica no puede originar compras.");}
    }
    public async Task<object> Obtener(long id,CancellationToken ct)
    {
        await using var c=await conexion.AbrirAsync(ct);await Sistema3S.Web.Services.Seguridad.ConexionAuditada.PrepararAsync(c,ct);await using var cmd=c.CreateCommand();cmd.CommandText="""
        SELECT s.Id,s.IdProducto,s.IdProveedor,s.Cantidad,s.PrecioConIgv,s.Justificacion,s.IdCompra,e.Nombre Producto,e.Unidad,p.CodigoProducto,v.RazonSocial Proveedor
        FROM ia.SolicitudCompra s JOIN bi.DimElemento e ON e.IdProducto=s.IdProducto
        JOIN dbo.Producto p ON p.IdProducto=s.IdProducto JOIN dbo.Proveedor v ON v.IdProveedor=s.IdProveedor
        WHERE s.Id=@id AND e.Estado=1 AND v.Estado=1;
        """;
        cmd.Parameters.AddWithValue("@id",id);
        return (await BiConexion.LeerAsync(cmd,["r"],ct))["r"].SingleOrDefault()??throw new ArgumentException("Solicitud no disponible.");
    }
    public async Task Vincular(long id,int purchase,int user,CancellationToken ct)
    {
        await using var c=await conexion.AbrirAsync(ct);await Sistema3S.Web.Services.Seguridad.ConexionAuditada.PrepararAsync(c,ct);await using var cmd=c.CreateCommand();cmd.CommandText="""
        SET XACT_ABORT ON; BEGIN TRANSACTION;
        IF NOT EXISTS(SELECT 1 FROM ia.SolicitudCompra s WITH(UPDLOCK,HOLDLOCK)
        JOIN dbo.Compra c ON c.IdCompra=@compra AND c.IdProveedor=s.IdProveedor JOIN dbo.EstadoCompra e ON e.IdEstadoCompra=c.IdEstadoCompra
        WHERE s.Id=@id AND (s.IdCompra IS NULL OR s.IdCompra=@compra) AND e.Nombre IN(N'Registrada',N'Confirmada')
        AND c.FechaCompra>=DATEADD(hour,-5,s.Creada)
        AND s.Cantidad=(SELECT SUM(d.Cantidad) FROM dbo.DetalleCompra d WHERE d.IdCompra=@compra AND d.IdProducto=s.IdProducto))
        THROW 51000,'La compra no coincide con la solicitud.',1;
        UPDATE ia.SolicitudCompra SET IdCompra=@compra,Vinculada=SYSUTCDATETIME() WHERE Id=@id;
        UPDATE r SET Estado='Vinculada',Revisada=SYSUTCDATETIME(),IdUsuario=@u FROM ia.Recomendacion r
        JOIN ia.SolicitudCompra s ON s.IdRecomendacion=r.Id WHERE s.Id=@id;
        COMMIT;
        """;
        cmd.Parameters.AddWithValue("@id",id);cmd.Parameters.AddWithValue("@compra",purchase);cmd.Parameters.AddWithValue("@u",user);
        try{await cmd.ExecuteNonQueryAsync(ct);}catch(SqlException ex)when(ex.Number is 51000 or 2601 or 2627){throw new ArgumentException("La compra debe ser posterior a la solicitud, estar vigente y coincidir en proveedor, producto y cantidad. No se modificó la compra.");}
    }
    public async Task<object> Proveedores(CancellationToken ct)
    {
        await using var c=await conexion.AbrirAsync(ct);await Sistema3S.Web.Services.Seguridad.ConexionAuditada.PrepararAsync(c,ct);await using var cmd=c.CreateCommand();
        cmd.CommandText="SELECT TOP(500) IdProveedor,RazonSocial Nombre FROM dbo.Proveedor WHERE Estado=1 ORDER BY RazonSocial,IdProveedor";
        return (await BiConexion.LeerAsync(cmd,["r"],ct))["r"];
    }
}
