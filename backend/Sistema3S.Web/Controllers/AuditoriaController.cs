using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Data;
using Sistema3S.Web.Data;
using Sistema3S.Web.Services.Seguridad;

namespace Sistema3S.Web.Controllers;

[ApiController]
[Route("api/auditoria")]
public class AuditoriaController(Bd3sContext db) : ControllerBase
{
    // El mismo alcance se aplica al listado, sus detalles y el selector de responsables.
    // Conservamos el historial de empleados desactivados y no borramos registros de la web.
    // Se materializan sólo metadatos. Los JSON de cambios se leen después de paginar.
    // Los índices temporales evitan expandir repetidamente CTE correlacionadas.
    private const string RegistrosInternos = """
        SET NOCOUNT ON;
        SELECT u.IdUsuario,u.Correo INTO #Personal
        FROM dbo.Usuario u INNER JOIN dbo.Rol r ON r.IdRol=u.IdRol
        WHERE LOWER(LTRIM(RTRIM(r.Nombre))) NOT IN ('cliente','cliente web','');
        CREATE UNIQUE CLUSTERED INDEX IX_Personal ON #Personal(IdUsuario);

        SELECT a.IdLog,a.IdUsuario,a.Accion,a.TablaAfectada,a.Fecha,a.Descripcion,
            CASE WHEN NULLIF(LTRIM(RTRIM(a.Solicitud)),'') IS NULL
                THEN CONCAT('R:',a.IdLog) ELSE CONCAT('S:',a.Solicitud) END Grupo,
            CASE WHEN p.IdUsuario IS NOT NULL
                AND (a.Origen=N'Administración' OR a.Descripcion LIKE N'Auth.%') THEN 1 ELSE 0 END Interno,
            CASE WHEN a.Descripcion LIKE N'ClienteWeb.%' OR a.Descripcion LIKE N'ClienteAuth.%'
                OR (a.Origen=N'Web' AND ISNULL(a.Descripcion,'') NOT LIKE N'Auth.%')
                OR (a.IdUsuario IS NOT NULL AND p.IdUsuario IS NULL) THEN 1 ELSE 0 END Publico
        INTO #Registros FROM dbo.AuditoriaLog a LEFT JOIN #Personal p ON p.IdUsuario=a.IdUsuario;
        CREATE CLUSTERED INDEX IX_Registros_Grupo ON #Registros(Grupo,IdLog);

        SELECT Grupo,MAX(Fecha) FechaActividad,
            MAX(CASE WHEN Accion='DENEGADA' THEN 2 WHEN Accion='ERROR' THEN 1 ELSE 0 END) ResultadoOrden
        INTO #Grupos FROM #Registros
        GROUP BY Grupo HAVING MAX(Interno)=1 AND MAX(Publico)=0;
        CREATE UNIQUE CLUSTERED INDEX IX_Grupos ON #Grupos(Grupo);
        """;

    // La correlación reúne el resultado y sus cambios antes de filtrar o paginar.
    private const string Eventos = RegistrosInternos + """
        ;WITH Orden AS (
            SELECT r.*,g.FechaActividad,g.ResultadoOrden,
                ROW_NUMBER() OVER(PARTITION BY r.Grupo ORDER BY CASE WHEN TablaAfectada='API' THEN 0
                WHEN TablaAfectada IN ('Compra','Venta','Cotizacion','Producto','Servicio','Usuario','Caja') THEN 1 ELSE 2 END,IdLog DESC) Orden
            FROM #Registros r INNER JOIN #Grupos g ON g.Grupo=r.Grupo
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
                WHEN Area IN ('Ia','IA_Abastecimiento','IA_Recomendacion','IA_SolicitudCompra') THEN N'Inteligencia Artificial'
                ELSE N'Otros' END Modulo,
                CASE ResultadoOrden WHEN 2 THEN 'denegado' WHEN 1 THEN 'error' ELSE 'completado' END Resultado
            FROM Encabezados
        )
        """;

    [HttpGet("opciones")]
    public async Task<IActionResult> Opciones(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(db.Database.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await ConexionAuditada.PrepararAsync(connection, cancellationToken);
        await using var command = new SqlCommand(RegistrosInternos + """
            SELECT u.IdUsuario, COALESCE(NULLIF(LTRIM(RTRIM(CONCAT(p.Nombres,' ',p.ApellidoPaterno,' ',p.ApellidoMaterno))),''),u.Correo) Nombre, u.Correo
            FROM #Personal u LEFT JOIN dbo.PersonalInterno p ON p.IdUsuario=u.IdUsuario
            WHERE EXISTS(SELECT 1 FROM #Registros a JOIN #Grupos g ON g.Grupo=a.Grupo WHERE a.IdUsuario=u.IdUsuario)
            ORDER BY Nombre,u.IdUsuario
            """,connection);
        var responsables = new List<object>();
        await using var reader=await command.ExecuteReaderAsync(cancellationToken);
        while(await reader.ReadAsync(cancellationToken)) responsables.Add(new { id=reader.GetInt32(0), nombre=reader.GetString(1), correo=reader.GetString(2) });
        return Ok(new { modulos=AuditoriaPresentacion.Modulos, responsables });
    }

    [HttpGet]
    public async Task<IActionResult> Listar(string? modulo, int? idUsuario, DateTime? desde, DateTime? hasta,
        string? resultado, int pagina = 1, CancellationToken cancellationToken = default)
    {
        if (pagina < 1 || pagina > 100000 || (desde.HasValue && hasta.HasValue && desde > hasta) || hasta?.Date == DateTime.MaxValue.Date)
            return BadRequest(new { mensaje = "Revisa la página y el rango de fechas." });
        if (!string.IsNullOrEmpty(modulo) && !AuditoriaPresentacion.Modulos.Contains(modulo)
            || !string.IsNullOrEmpty(resultado) && resultado is not ("completado" or "error" or "denegado"))
            return BadRequest(new { mensaje="Selecciona un módulo y un resultado válidos." });
        await using var connection = new SqlConnection(db.Database.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await ConexionAuditada.PrepararAsync(connection, cancellationToken);
        await using var command = new SqlCommand(Eventos + """
            SELECT Grupo,FechaActividad,IdLog,Modulo INTO #Eventos FROM Eventos e
            WHERE (@modulo IS NULL OR e.Modulo=@modulo)
              AND (@usuario IS NULL OR EXISTS(SELECT 1 FROM #Registros b WHERE b.Grupo=e.Grupo AND b.IdUsuario=@usuario))
              AND (@desde IS NULL OR e.FechaActividad>=@desde) AND (@hasta IS NULL OR e.FechaActividad<DATEADD(day,1,@hasta))
              AND (@resultado IS NULL OR e.Resultado=@resultado);
            SELECT COUNT(*) FROM #Eventos;
            SELECT * INTO #Pagina FROM #Eventos ORDER BY FechaActividad DESC,IdLog DESC OFFSET @offset ROWS FETCH NEXT 20 ROWS ONLY;
            """ + """
            SELECT a.IdLog,a.IdUsuario,COALESCE(NULLIF(LTRIM(RTRIM(CONCAT(p.Nombres,' ',p.ApellidoPaterno,' ',p.ApellidoMaterno))),''),u.Correo,'') Responsable,
                a.Accion,a.TablaAfectada,CONVERT(nvarchar(100),a.IdRegistro),a.Fecha,ISNULL(a.Descripcion,''),a.DatosAntes,a.DatosDespues,e.Grupo,ISNULL(a.Origen,''),e.Modulo
            FROM #Pagina e JOIN #Registros r ON r.Grupo=e.Grupo
            JOIN dbo.AuditoriaLog a ON a.IdLog=r.IdLog
            LEFT JOIN dbo.Usuario u ON u.IdUsuario=a.IdUsuario LEFT JOIN dbo.PersonalInterno p ON p.IdUsuario=u.IdUsuario
            ORDER BY e.FechaActividad DESC,e.IdLog DESC,CASE WHEN a.TablaAfectada='API' THEN 0 ELSE 1 END,a.IdLog;
            """, connection);
        command.Parameters.Add("@modulo",SqlDbType.NVarChar,100).Value = string.IsNullOrEmpty(modulo)?DBNull.Value:modulo;
        command.Parameters.Add("@usuario",SqlDbType.Int).Value = (object?)idUsuario ?? DBNull.Value;
        command.Parameters.Add("@desde",SqlDbType.DateTime).Value = (object?)desde?.Date ?? DBNull.Value;
        command.Parameters.Add("@hasta",SqlDbType.DateTime).Value = (object?)hasta?.Date ?? DBNull.Value;
        command.Parameters.Add("@resultado",SqlDbType.VarChar,16).Value = string.IsNullOrEmpty(resultado)?DBNull.Value:resultado;
        command.Parameters.Add("@offset",SqlDbType.Int).Value = (pagina-1)*20;
        var filas = new List<FilaAuditoria>();
        int total;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken)) {
            await reader.ReadAsync(cancellationToken); total=reader.GetInt32(0); await reader.NextResultAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) filas.Add(new(reader.GetInt32(0),reader.IsDBNull(1)?null:reader.GetInt32(1),reader.GetString(2),reader.GetString(3),reader.GetString(4),
                reader.IsDBNull(5)?null:reader.GetString(5),reader.GetDateTime(6),reader.GetString(7),reader.IsDBNull(8)?null:reader.GetString(8),reader.IsDBNull(9)?null:reader.GetString(9),reader.GetString(10),reader.GetString(11),reader.GetString(12)));
        }
        var referencias=await LeerReferencias(connection,filas,cancellationToken);
        var items=filas.GroupBy(f=>f.Grupo).Select(g=>AuditoriaPresentacion.Crear(g.ToArray(),referencias)).ToArray();
        return Ok(new { items, pagina, total, tamanoPagina=20 });
    }

    private static async Task<Dictionary<string,string>> LeerReferencias(SqlConnection connection, IReadOnlyList<FilaAuditoria> filas, CancellationToken cancellationToken)
    {
        // Los nombres de tablas son constantes internas, nunca texto recibido del navegador.
        string[] tablas=["EstadoCompra","EstadoVenta","EstadoCaja","EstadoCotizacion","EstadoComprobante","Rol","Permiso","Categoria","Marca","UnidadMedida","TipoComprobante","TipoMovimientoCaja","TipoMovimientoStock"];
        var sql=string.Join(" UNION ALL ",tablas.Select(t=>$"SELECT CONCAT('Id{t}:',Id{t}) Clave,Nombre Valor FROM dbo.[{t}]"));
        var refs=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        if(filas.Count==0) return refs;
        await using var command=new SqlCommand(sql,connection);
        await using (var reader=await command.ExecuteReaderAsync(cancellationToken))
            while(await reader.ReadAsync(cancellationToken)) refs[reader.GetString(0)]=reader.GetString(1).Replace('_',' ');
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
        await using (var reader=await nombres.ExecuteReaderAsync(cancellationToken))
            while(await reader.ReadAsync(cancellationToken)) refs[reader.GetString(0)]=reader.GetString(1);
        return refs;
    }
}
