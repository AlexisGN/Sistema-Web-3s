using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Sistema3S.Web.Data;
using Sistema3S.Web.Services.Seguridad;

namespace Sistema3S.Web.Controllers;

[ApiController]
[Route("api/auditoria")]
public class AuditoriaController(Bd3sContext db) : ControllerBase
{
    // El mismo alcance se aplica al listado, sus detalles y el selector de responsables.
    // Conservamos el historial de empleados desactivados y no borramos registros de la web.
    private const string RegistrosInternos = """
        ;WITH Personal AS (
            SELECT u.IdUsuario,u.Correo FROM dbo.Usuario u
            INNER JOIN dbo.Rol r ON r.IdRol=u.IdRol
            WHERE LOWER(LTRIM(RTRIM(r.Nombre))) NOT IN ('cliente','cliente web','')
        ), Registros AS (
            SELECT a.*, CASE WHEN NULLIF(LTRIM(RTRIM(a.Solicitud)),'') IS NULL
                THEN CONCAT('R:',a.IdLog) ELSE CONCAT('S:',a.Solicitud) END Grupo
            FROM dbo.AuditoriaLog a
        ), Base AS (
            SELECT a.* FROM Registros a
            WHERE EXISTS (
                SELECT 1 FROM Registros responsable INNER JOIN Personal p ON p.IdUsuario=responsable.IdUsuario
                WHERE responsable.Grupo=a.Grupo
                  AND (responsable.Origen=N'Administración' OR responsable.Descripcion LIKE N'Auth.%')
            )
            AND NOT EXISTS (
                SELECT 1 FROM Registros web WHERE web.Grupo=a.Grupo AND (
                    web.Descripcion LIKE N'ClienteWeb.%' OR web.Descripcion LIKE N'ClienteAuth.%'
                    OR (web.Origen=N'Web' AND ISNULL(web.Descripcion,'') NOT LIKE N'Auth.%')
                    OR (web.IdUsuario IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Personal p WHERE p.IdUsuario=web.IdUsuario))
                )
            )
        )
        """;

    // La correlación reúne el resultado y sus cambios antes de filtrar o paginar.
    private const string Eventos = RegistrosInternos + """
        , Orden AS (
            SELECT *, ROW_NUMBER() OVER(PARTITION BY Grupo ORDER BY CASE WHEN TablaAfectada='API' THEN 0
                WHEN TablaAfectada IN ('Compra','Venta','Cotizacion','Producto','Servicio','Usuario','Caja') THEN 1 ELSE 2 END,IdLog DESC) Orden,
                MAX(Fecha) OVER(PARTITION BY Grupo) FechaActividad
            FROM Base
        ), Encabezados AS (
            SELECT *, CASE WHEN TablaAfectada='API' THEN LEFT(ISNULL(Descripcion,''),CHARINDEX('.',ISNULL(Descripcion,'')+'.')-1)
                ELSE TablaAfectada END Area FROM Orden WHERE Orden=1
        ), Eventos AS (
            SELECT *, CASE
                WHEN Area IN ('Auth','ClienteAuth') OR (Area='ClienteWeb' AND Descripcion LIKE 'ClienteWeb.Login%') THEN N'Accesos'
                WHEN Area='ClienteWeb' AND Descripcion LIKE 'ClienteWeb.Registrar %' THEN N'Clientes'
                WHEN Area IN ('Compra','DetalleCompra','PagoCompra') THEN N'Compras'
                WHEN Area IN ('Venta','DetalleVenta','PagoVenta','Comprobante','SerieComprobante') THEN N'Ventas'
                WHEN Area IN ('Cotizacion','ClienteWeb','DetalleCotizacion','EnvioCorreo') THEN N'Cotizaciones'
                WHEN Area IN ('Caja','MovimientoCaja') THEN N'Caja'
                WHEN Area IN ('Producto','ElementoCatalogo','ImagenElementoCatalogo','Categoria','Marca','UnidadMedida','Catalogo') THEN N'Productos'
                WHEN Area IN ('Servicio') THEN N'Servicios'
                WHEN Area IN ('Inventario','MovimientoStock','AlertaStock') THEN N'Inventario'
                WHEN Area IN ('Cliente','ClientePersonaNatural','ClienteEmpresa','ContactoCliente') THEN N'Clientes'
                WHEN Area IN ('Proveedor','ContactoProveedor') THEN N'Proveedores'
                WHEN Area IN ('Usuario','PersonalInterno','Rol','RolPermiso','Permiso') THEN N'Usuarios y roles'
                ELSE N'Otros' END Modulo,
                CASE WHEN EXISTS(SELECT 1 FROM Base b WHERE b.Grupo=Encabezados.Grupo AND b.Accion='DENEGADA') THEN 'denegado'
                  WHEN EXISTS(SELECT 1 FROM Base b WHERE b.Grupo=Encabezados.Grupo AND b.Accion='ERROR') THEN 'error' ELSE 'completado' END Resultado
            FROM Encabezados
        )
        """;

    [HttpGet("opciones")]
    public async Task<IActionResult> Opciones()
    {
        await using var connection = new SqlConnection(db.Database.GetConnectionString());
        await connection.AbrirAuditadaAsync();
        await using var command = new SqlCommand(RegistrosInternos + """
            SELECT u.IdUsuario, COALESCE(NULLIF(LTRIM(RTRIM(CONCAT(p.Nombres,' ',p.ApellidoPaterno,' ',p.ApellidoMaterno))),''),u.Correo) Nombre, u.Correo
            FROM Personal u LEFT JOIN dbo.PersonalInterno p ON p.IdUsuario=u.IdUsuario
            WHERE EXISTS(SELECT 1 FROM Base a WHERE a.IdUsuario=u.IdUsuario)
            ORDER BY Nombre,u.IdUsuario
            """,connection);
        var responsables = new List<object>();
        await using var reader=await command.ExecuteReaderAsync();
        while(await reader.ReadAsync()) responsables.Add(new { id=reader.GetInt32(0), nombre=reader.GetString(1), correo=reader.GetString(2) });
        return Ok(new { modulos=AuditoriaPresentacion.Modulos, responsables });
    }

    [HttpGet]
    public async Task<IActionResult> Listar(string? modulo, int? idUsuario, DateTime? desde, DateTime? hasta,
        string? resultado, int pagina = 1)
    {
        if (pagina < 1 || pagina > 100000 || (desde.HasValue && hasta.HasValue && desde > hasta) || hasta?.Date == DateTime.MaxValue.Date)
            return BadRequest(new { mensaje = "Revisa la página y el rango de fechas." });
        if (!string.IsNullOrEmpty(modulo) && !AuditoriaPresentacion.Modulos.Contains(modulo)
            || !string.IsNullOrEmpty(resultado) && resultado is not ("completado" or "error" or "denegado"))
            return BadRequest(new { mensaje="Selecciona un módulo y un resultado válidos." });
        await using var connection = new SqlConnection(db.Database.GetConnectionString());
        await connection.AbrirAuditadaAsync();
        await using var command = new SqlCommand(Eventos + """
            SELECT Grupo,FechaActividad,IdLog,Modulo INTO #Eventos FROM Eventos e
            WHERE (@modulo IS NULL OR e.Modulo=@modulo)
              AND (@usuario IS NULL OR EXISTS(SELECT 1 FROM Base b WHERE b.Grupo=e.Grupo AND b.IdUsuario=@usuario))
              AND (@desde IS NULL OR e.FechaActividad>=@desde) AND (@hasta IS NULL OR e.FechaActividad<DATEADD(day,1,@hasta))
              AND (@resultado IS NULL OR e.Resultado=@resultado);
            SELECT COUNT(*) FROM #Eventos;
            SELECT * INTO #Pagina FROM #Eventos ORDER BY FechaActividad DESC,IdLog DESC OFFSET @offset ROWS FETCH NEXT 20 ROWS ONLY;
            """ + RegistrosInternos + """
            SELECT a.IdLog,a.IdUsuario,COALESCE(NULLIF(LTRIM(RTRIM(CONCAT(p.Nombres,' ',p.ApellidoPaterno,' ',p.ApellidoMaterno))),''),u.Correo,'') Responsable,
                a.Accion,a.TablaAfectada,CONVERT(nvarchar(100),a.IdRegistro),a.Fecha,ISNULL(a.Descripcion,''),a.DatosAntes,a.DatosDespues,e.Grupo,ISNULL(a.Origen,''),e.Modulo
            FROM Base a JOIN #Pagina e ON e.Grupo=a.Grupo
            LEFT JOIN dbo.Usuario u ON u.IdUsuario=a.IdUsuario LEFT JOIN dbo.PersonalInterno p ON p.IdUsuario=u.IdUsuario
            ORDER BY e.FechaActividad DESC,e.IdLog DESC,CASE WHEN a.TablaAfectada='API' THEN 0 ELSE 1 END,a.IdLog;
            """, connection);
        command.Parameters.AddWithValue("@modulo", string.IsNullOrEmpty(modulo)?DBNull.Value:modulo);
        command.Parameters.AddWithValue("@usuario", (object?)idUsuario ?? DBNull.Value);
        command.Parameters.AddWithValue("@desde", (object?)desde?.Date ?? DBNull.Value);
        command.Parameters.AddWithValue("@hasta", (object?)hasta?.Date ?? DBNull.Value);
        command.Parameters.AddWithValue("@resultado", string.IsNullOrEmpty(resultado)?DBNull.Value:resultado);
        command.Parameters.AddWithValue("@offset", (pagina-1)*20);
        var filas = new List<FilaAuditoria>();
        int total;
        await using (var reader = await command.ExecuteReaderAsync()) {
            await reader.ReadAsync(); total=reader.GetInt32(0); await reader.NextResultAsync();
            while (await reader.ReadAsync()) filas.Add(new(reader.GetInt32(0),reader.IsDBNull(1)?null:reader.GetInt32(1),reader.GetString(2),reader.GetString(3),reader.GetString(4),
                reader.IsDBNull(5)?null:reader.GetString(5),reader.GetDateTime(6),reader.GetString(7),reader.IsDBNull(8)?null:reader.GetString(8),reader.IsDBNull(9)?null:reader.GetString(9),reader.GetString(10),reader.GetString(11),reader.GetString(12)));
        }
        var referencias=await LeerReferencias(connection,filas);
        var items=filas.GroupBy(f=>f.Grupo).Select(g=>AuditoriaPresentacion.Crear(g.ToArray(),referencias)).ToArray();
        return Ok(new { items, pagina, total, tamanoPagina=20 });
    }

    private static async Task<Dictionary<string,string>> LeerReferencias(SqlConnection connection, IReadOnlyList<FilaAuditoria> filas)
    {
        // Los nombres de tablas son constantes internas, nunca texto recibido del navegador.
        string[] tablas=["EstadoCompra","EstadoVenta","EstadoCaja","EstadoCotizacion","EstadoComprobante","Rol","Permiso","Categoria","Marca","UnidadMedida","TipoComprobante","TipoMovimientoCaja","TipoMovimientoStock"];
        var sql=string.Join(" UNION ALL ",tablas.Select(t=>$"SELECT CONCAT('Id{t}:',Id{t}) Clave,Nombre Valor FROM dbo.[{t}]"));
        var refs=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        await using var command=new SqlCommand(sql,connection);
        await using (var reader=await command.ExecuteReaderAsync())
            while(await reader.ReadAsync()) refs[reader.GetString(0)]=reader.GetString(1).Replace('_',' ');
        var ids = new HashSet<int>();
        foreach(var fila in filas) {
            if(int.TryParse(fila.Registro,out var registro)) ids.Add(registro);
            foreach(var json in new[]{fila.Antes,fila.Despues}) {
                try {
                    using var doc=JsonDocument.Parse(json ?? "{}");
                    if(doc.RootElement.ValueKind != JsonValueKind.Object) continue;
                    foreach(var propiedad in doc.RootElement.EnumerateObject())
                        if(propiedad.Name.StartsWith("Id") && int.TryParse(propiedad.Value.ToString(),out var id)) ids.Add(id);
                } catch(JsonException) { }
            }
        }
        if(ids.Count==0) return refs;
        await using var nombres=new SqlCommand("""
            WITH Ids AS (SELECT CONVERT(int,value) Id FROM OPENJSON(@ids))
            SELECT CONCAT('IdProducto:',p.IdProducto),e.Nombre FROM Producto p JOIN ElementoCatalogo e ON e.IdElementoCatalogo=p.IdElementoCatalogo WHERE p.IdProducto IN (SELECT Id FROM Ids)
            UNION ALL SELECT CONCAT('IdServicio:',s.IdServicio),e.Nombre FROM Servicio s JOIN ElementoCatalogo e ON e.IdElementoCatalogo=s.IdElementoCatalogo WHERE s.IdServicio IN (SELECT Id FROM Ids)
            UNION ALL SELECT CONCAT('IdElementoCatalogo:',e.IdElementoCatalogo),e.Nombre FROM ElementoCatalogo e WHERE e.IdElementoCatalogo IN (SELECT Id FROM Ids)
            UNION ALL SELECT CONCAT('IdProveedor:',p.IdProveedor),p.RazonSocial FROM Proveedor p WHERE p.IdProveedor IN (SELECT Id FROM Ids)
            UNION ALL SELECT CONCAT('IdCliente:',c.IdCliente),COALESCE(e.RazonSocial,NULLIF(LTRIM(RTRIM(CONCAT(n.Nombres,' ',n.ApellidoPaterno,' ',n.ApellidoMaterno))),''),c.NumeroDocumento)
                FROM Cliente c LEFT JOIN ClienteEmpresa e ON e.IdCliente=c.IdCliente LEFT JOIN ClientePersonaNatural n ON n.IdCliente=c.IdCliente WHERE c.IdCliente IN (SELECT Id FROM Ids)
            """,connection);
        nombres.Parameters.AddWithValue("@ids",JsonSerializer.Serialize(ids));
        await using (var reader=await nombres.ExecuteReaderAsync())
            while(await reader.ReadAsync()) refs[reader.GetString(0)]=reader.GetString(1);
        return refs;
    }
}
