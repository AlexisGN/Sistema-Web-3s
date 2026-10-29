using System.Data;
using Sistema3S.Web.Services.Bi;

namespace Sistema3S.Web.Services.Comercial;

public sealed record ComercialDetalle(List<Dictionary<string,object?>> Items,int Pagina,int TamanioPagina,int TotalRegistros);

internal static class ComercialDetalleConsulta
{
    internal static async Task<ComercialDetalle> Consultar(BiConexion conexion,string tipo,string segmento,ComercialFiltro f,int pagina,CancellationToken ct)
    {
        f.Validar();
        if(tipo is not ("stock" or "clientes"))throw new ArgumentException("Selecciona un detalle disponible.");
        string[] segmentos=tipo=="stock"?["todos","sin-stock","bajo-minimo","sobre-minimo","por-revisar"]:["todos","recurrentes","nuevos"];
        if(!segmentos.Contains(segmento))throw new ArgumentException("Selecciona un grupo válido.");
        if(pagina<1||pagina>100000)throw new ArgumentException("Selecciona una página válida.");
        const int tamanio=24;
        await using var c=await conexion.AbrirAsync(ct);await using var cmd=c.CreateCommand();cmd.CommandTimeout=15;
        cmd.CommandText=tipo=="stock"?Stock:ComercialConsultas.Preparar+Clientes;
        cmd.CommandText+="""
        SELECT COUNT(*) Total FROM #detalle;
        SELECT * FROM #detalle ORDER BY Nombre,Id OFFSET @offset ROWS FETCH NEXT @tamanio ROWS ONLY;
        """;
        var b=f.PeriodoB;
        foreach(var (name,value) in new(string,object?)[]{("@categoria",f.Categoria),("@elemento",f.Elemento),("@cliente",f.Cliente)})BiConexion.Parametro(cmd,name,SqlDbType.Int,value);
        foreach(var (name,value) in new[]{("@a",f.Desde),("@z",f.Hasta.AddDays(1)),("@b",b.Desde),("@y",b.Hasta.AddDays(1))})BiConexion.Parametro(cmd,name,SqlDbType.DateTime2,value.ToDateTime(TimeOnly.MinValue));
        BiConexion.Parametro(cmd,"@base",SqlDbType.Bit,false);BiConexion.Parametro(cmd,"@comparar",SqlDbType.Bit,false);
        BiConexion.Parametro(cmd,"@segmento",SqlDbType.VarChar,segmento,20);
        BiConexion.Parametro(cmd,"@offset",SqlDbType.Int,(pagina-1)*tamanio);BiConexion.Parametro(cmd,"@tamanio",SqlDbType.Int,tamanio);
        var datos=await BiConexion.LeerAsync(cmd,["total","items"],ct);
        return new(datos["items"],pagina,tamanio,Convert.ToInt32(datos["total"][0]["Total"]));
    }

    private const string Stock="""
    SET NOCOUNT ON;SET LOCK_TIMEOUT 5000;
    SELECT p.IdProducto Id,e.IdElementoCatalogo,e.Nombre,e.ImagenUrl,u.Abreviatura Unidad,i.StockActual,i.StockMinimo,
      CASE WHEN i.StockActual IS NULL OR i.StockActual<0 THEN N'Saldo por revisar'
       WHEN i.StockActual=0 THEN N'Sin stock' WHEN i.StockActual<=i.StockMinimo THEN N'Bajo mínimo con stock' ELSE N'Sobre el mínimo' END Estado
    INTO #detalle
    FROM dbo.Producto p JOIN dbo.ElementoCatalogo e ON e.IdElementoCatalogo=p.IdElementoCatalogo
    JOIN dbo.UnidadMedida u ON u.IdUnidadMedida=p.IdUnidadMedida LEFT JOIN dbo.Inventario i ON i.IdProducto=p.IdProducto
    WHERE e.Estado=1 AND p.AplicaInventario=1
      AND (@categoria IS NULL OR p.IdCategoria=@categoria) AND (@elemento IS NULL OR e.IdElementoCatalogo=@elemento)
      AND (@segmento='todos' OR (@segmento='sin-stock' AND i.StockActual=0)
       OR (@segmento='bajo-minimo' AND i.StockActual>0 AND i.StockActual<=i.StockMinimo)
       OR (@segmento='sobre-minimo' AND i.StockActual>i.StockMinimo)
       OR (@segmento='por-revisar' AND (i.StockActual IS NULL OR i.StockActual<0)));
    """;

    private const string Clientes="""
    WITH compradores AS(
      SELECT IdCliente,COUNT(*) Operaciones,SUM(Base) Importe,MAX(Fecha) UltimaVenta
      FROM #o WHERE Fecha>=@a AND Fecha<@z GROUP BY IdCliente
    ), contexto AS(
      SELECT c.*,CASE WHEN EXISTS(SELECT 1 FROM dbo.Venta v JOIN dbo.EstadoVenta ev ON ev.IdEstadoVenta=v.IdEstadoVenta
        WHERE v.IdCliente=c.IdCliente AND v.FechaVenta<@a AND ev.Nombre=N'Confirmada') THEN 1 ELSE 0 END Recurrente
      FROM compradores c
    )
    SELECT c.IdCliente Id,d.Nombre,c.Operaciones,CAST(c.Importe AS decimal(18,2)) Importe,c.UltimaVenta,c.Recurrente,CAST(cl.Estado AS int) Activo
    INTO #detalle FROM contexto c JOIN bi.DimCliente d ON d.IdCliente=c.IdCliente AND d.Procedencia='operativo'
    JOIN dbo.Cliente cl ON cl.IdCliente=c.IdCliente
    WHERE @segmento='todos' OR (@segmento='recurrentes' AND c.Recurrente=1) OR (@segmento='nuevos' AND c.Recurrente=0);
    """;
}
