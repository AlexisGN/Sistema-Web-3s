using Microsoft.Data.SqlClient;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Sistema3S.Web.Services.Bi;
using Sistema3S.Web.Services.Ia;

// Herramienta local privada: nunca publicada como endpoint. Sólo lote de referencia autorizado.
var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../../"));
var output=Path.GetFullPath(args.ElementAtOrDefault(1)??Path.Combine(root,"IA/evidencia/demostracion"));Directory.CreateDirectory(output);
const string batch="a9410140-2026-4100-8100-000000000004";
var apply=args.FirstOrDefault()=="ejecutar";var committed=false;
var config=new ConfigurationBuilder().SetBasePath(Path.Combine(root,"Sistema3S.Web")).AddJsonFile("appsettings.json",true).AddJsonFile("appsettings.Development.json",true).AddJsonFile("ia.local.json",true).AddEnvironmentVariables().Build();
var connection=Environment.GetEnvironmentVariable("S3S_IA_CONEXION")??config.GetConnectionString("BD_3S")??throw new Exception("Configura la conexión local de BD_3S.");
config=new ConfigurationBuilder().AddConfiguration(config).AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:BD_3S",connection}}).Build();
try {
if(apply&&!config.GetValue("Ia:ReferenciaTecnica",false))throw new InvalidOperationException("La demostración sólo se incorpora en el desarrollo autorizado.");
await using var c=new SqlConnection(connection);await c.OpenAsync();
if(c.Database!=config.GetValue("Ia:BaseReferencia","BD_3S")||new[]{"master","model","msdb","tempdb"}.Contains(c.Database,StringComparer.OrdinalIgnoreCase))throw new Exception("La conexión no corresponde a la base de desarrollo autorizada en Ia:BaseReferencia (BD_3S por defecto).");
async Task<List<Dictionary<string,object?>>> Rows(string sql,SqlTransaction? tx=null){await using var cmd=new SqlCommand(sql,c,tx){CommandTimeout=120};return (await BiConexion.LeerAsync(cmd,["r"],default))["r"];}
var catalog=await Rows("""
SELECT e.IdElementoCatalogo,e.IdProducto,e.Nombre,e.Unidad,e.Categoria,ec.PrecioReferencial Precio,
 (SELECT SUM(d.Subtotal)/NULLIF(SUM(CAST(d.Cantidad AS decimal(28,6))),0) FROM dbo.DetalleCompra d JOIN dbo.Compra c ON c.IdCompra=d.IdCompra JOIN dbo.EstadoCompra s ON s.IdEstadoCompra=c.IdEstadoCompra WHERE d.IdProducto=e.IdProducto AND s.Nombre IN(N'Registrada',N'Confirmada') AND d.Cantidad>0 AND d.Subtotal>0) Costo,
 COALESCE(i.StockMinimo,0) StockMinimo
FROM bi.DimElemento e JOIN dbo.ElementoCatalogo ec ON ec.IdElementoCatalogo=e.IdElementoCatalogo
LEFT JOIN dbo.Producto p ON p.IdProducto=e.IdProducto LEFT JOIN dbo.Inventario i ON i.IdProducto=e.IdProducto
WHERE e.Estado=1 AND (e.IdProducto IS NULL OR p.AplicaInventario=1) ORDER BY e.IdElementoCatalogo
""");
var customers=await Rows("SELECT c.IdCliente,d.Nombre,c.FechaRegistro FROM dbo.Cliente c JOIN bi.DimCliente d ON d.IdCliente=c.IdCliente AND d.Procedencia='operativo' WHERE c.Estado=1 ORDER BY c.IdCliente");
var suppliers=await Rows("SELECT IdProveedor,RazonSocial FROM dbo.Proveedor WHERE Estado=1 ORDER BY IdProveedor");
var states=await Rows("SELECT 'venta' Tipo,Nombre FROM dbo.EstadoVenta UNION ALL SELECT 'compra',Nombre FROM dbo.EstadoCompra UNION ALL SELECT 'cotizacion',Nombre FROM dbo.EstadoCotizacion");
var operations=await Rows("SELECT Tipo,COUNT(*) Cantidad,MIN(Fecha) Desde,MAX(Fecha) Hasta FROM bi.OperacionReferencia GROUP BY Tipo");
await File.WriteAllTextAsync(Path.Combine(output,"auditoria.json"),JsonSerializer.Serialize(new{fechaUtc=DateTime.UtcNow,catalogo=catalog,clientes=customers,proveedores=suppliers,estados=states,referenciaExistente=operations}));
if(!apply){Console.WriteLine("Auditoría guardada. No hubo escrituras SQL. Usa ejecutar sólo en el desarrollo autorizado.");return;}
if(catalog.Count==0||customers.Count==0||suppliers.Count==0||catalog.Any(p=>p["Precio"]==null||Convert.ToDecimal(p["Precio"])<=0||p["Costo"]!=null&&Convert.ToDecimal(p["Costo"])<=0))throw new Exception("El catálogo necesita precios positivos y terceros activos; no se inventarán entidades.");
foreach(var (type,state) in new[]{("venta","Confirmada"),("compra","Registrada"),("cotizacion","Convertida en venta"),("cotizacion","Pendiente"),("cotizacion","Cancelada")})
 if(!states.Any(s=>Equals(s["Tipo"],type)&&Equals(s["Nombre"],state)))throw new Exception("Un estado requerido no existe; revisa el catálogo antes de incorporar la demostración.");
var backupDir=(string)(await new SqlCommand("SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000))",c).ExecuteScalarAsync())!;
var backup=Path.Combine(backupDir,"BD_3S_PreDemoIA_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")+".bak");
foreach(var sql in new[]{"BACKUP DATABASE [BD_3S] TO DISK=@f WITH COPY_ONLY,CHECKSUM,COMPRESSION","RESTORE VERIFYONLY FROM DISK=@f WITH CHECKSUM"}){await using var cmd=new SqlCommand(sql,c){CommandTimeout=300};cmd.Parameters.AddWithValue("@f",backup);await cmd.ExecuteNonQueryAsync();}
await new SqlCommand("DBCC CHECKDB([BD_3S]) WITH NO_INFOMSGS",c){CommandTimeout=300}.ExecuteNonQueryAsync();
await File.WriteAllTextAsync(Path.Combine(output,"respaldo.json"),JsonSerializer.Serialize(new{ruta=backup,copyOnly=true,checksum=true,verifyOnly=true,checkDB=true,restauracionEnOtraBase=false}));
var schema=await File.ReadAllTextAsync(Path.Combine(root,"Sistema3S.Web/Database/20261014_IA.sql"));
foreach(var part in Regex.Split(schema,@"^GO\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase))if(!string.IsNullOrWhiteSpace(part))await new SqlCommand(part,c){CommandTimeout=120}.ExecuteNonQueryAsync();
await using(var tx=(SqlTransaction)await c.BeginTransactionAsync(System.Data.IsolationLevel.Serializable)){
var before=await Fingerprint(c,tx,batch);
await new SqlCommand("DECLARE @r int;EXEC @r=sys.sp_getapplock @Resource=N'IA-Demo-20261014-4',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=5000;IF @r<0 THROW 51000,N'No se obtuvo el bloqueo.',1;",c,tx).ExecuteNonQueryAsync();
var existing=Convert.ToInt32(await new SqlCommand($"SELECT COUNT(*) FROM bi.LoteReferencia WHERE IdLote='{batch}' AND Estado='Completo'",c,tx).ExecuteScalarAsync())>0;
if(!existing){
await new SqlCommand($"INSERT bi.LoteReferencia(IdLote,Nombre,Inicio,Fin,Generador,Estado) VALUES('{batch}',N'Demostración comercial IA julio 2025 a octubre 2026','20250701','20261013',N'Demo identificada 20261014 v4','Preparado')",c,tx).ExecuteNonQueryAsync();
foreach(var customer in customers){await using var cmd=new SqlCommand("INSERT bi.EntidadReferencia(IdEntidad,Tipo,Nombre,FechaAlta,IdLote) VALUES(@id,'cliente',@name,'20250701',@batch)",c,tx);cmd.Parameters.AddWithValue("@id",-1000000-Convert.ToInt32(customer["IdCliente"]));cmd.Parameters.AddWithValue("@name",customer["Nombre"]!);cmd.Parameters.AddWithValue("@batch",Guid.Parse(batch));await cmd.ExecuteNonQueryAsync();}
foreach(var supplier in suppliers){await using var cmd=new SqlCommand("INSERT bi.EntidadReferencia(IdEntidad,Tipo,Nombre,FechaAlta,IdLote) VALUES(@id,'proveedor',@name,'20250701',@batch)",c,tx);cmd.Parameters.AddWithValue("@id",-1000000-Convert.ToInt32(supplier["IdProveedor"]));cmd.Parameters.AddWithValue("@name",supplier["RazonSocial"]!);cmd.Parameters.AddWithValue("@batch",Guid.Parse(batch));await cmd.ExecuteNonQueryAsync();}
var rng=new Random(202610144);var begin=new DateOnly(2025,7,1);var end=new DateOnly(2026,10,13);var serial=0;
async Task<int> Operation(string type,DateOnly date,string state,int? customer,int? supplier,List<(int Element,int Qty,decimal Price)> lines,int? quote=null){var total=lines.Sum(x=>decimal.Round(x.Qty*x.Price,2));var net=decimal.Round(total/1.18m,2);await using var cmd=new SqlCommand("INSERT bi.OperacionReferencia(Clave,IdLote,Tipo,Fecha,Estado,IdCliente,IdProveedor,IdCotizacionReferencia,Base,Igv,Total) OUTPUT INSERTED.IdOperacion VALUES(@key,@batch,@type,@date,@state,@client,@vendor,@quote,@net,@tax,@total)",c,tx);foreach(var (k,v) in new Dictionary<string,object?>{{"@key","IAD4-"+(++serial).ToString("D6")},{"@batch",Guid.Parse(batch)},{"@type",type},{"@date",date.ToDateTime(new TimeOnly(type=="compra"?8:12,0))},{"@state",state},{"@client",customer},{"@vendor",supplier},{"@quote",quote},{"@net",net},{"@tax",total-net},{"@total",total}})cmd.Parameters.AddWithValue(k,v??DBNull.Value);var id=Convert.ToInt32(await cmd.ExecuteScalarAsync());foreach(var l in lines){await using var detail=new SqlCommand("INSERT bi.DetalleReferencia(IdOperacion,IdElementoCatalogo,Cantidad,Importe) VALUES(@id,@e,@q,@v)",c,tx);detail.Parameters.AddWithValue("@id",id);detail.Parameters.AddWithValue("@e",l.Element);detail.Parameters.AddWithValue("@q",l.Qty);detail.Parameters.AddWithValue("@v",decimal.Round(l.Qty*l.Price,2));await detail.ExecuteNonQueryAsync();}return id;}
async Task Movement(int p,DateOnly day,string type,int qty,int? op){await using var cmd=new SqlCommand("INSERT bi.MovimientoReferencia(Clave,IdLote,IdProducto,Fecha,Tipo,Cantidad,IdOperacion) VALUES(@key,@batch,@p,@date,@type,@q,@op)",c,tx);foreach(var (k,v) in new Dictionary<string,object?>{{"@key","IAD4-M-"+(++serial).ToString("D6")},{"@batch",Guid.Parse(batch)},{"@p",p},{"@date",day.ToDateTime(new TimeOnly(type=="Salida"?12:8,0))},{"@type",type},{"@q",qty},{"@op",op}})cmd.Parameters.AddWithValue(k,v??DBNull.Value);await cmd.ExecuteNonQueryAsync();}
var physical=catalog.Where(x=>x["IdProducto"]!=null).ToList();
foreach(var p in physical)await Movement(Convert.ToInt32(p["IdProducto"]),begin,"Apertura",60,null);
for(var day=begin;day<=end;day=day.AddDays(1)){
 if(day.DayOfWeek is DayOfWeek.Sunday)continue;
 var lines=new List<(int Element,int Qty,decimal Price)>();var movements=new List<(int P,int Q)>();
 for(int index=0;index<catalog.Count;index++){
 var p=catalog[index];var period=index%3==0?3:index%3==1?7:14;
 if((day.DayNumber-begin.DayNumber+index)%period!=0)continue;
 var seasonal=1+0.25*Math.Sin(day.Month*Math.PI/6+index);var trend=1+(day.DayNumber-begin.DayNumber)/470.0*(index%2==0?.4:-.2);
 var qty=Math.Max(1,(int)Math.Round((2+index%6)*seasonal*trend))+rng.Next(0,3);var price=decimal.Round(Convert.ToDecimal(p["Precio"])*(1+(day.Month%3-1)*.015m),2);
 lines.Add((Convert.ToInt32(p["IdElementoCatalogo"]),qty,price));
 if(p["IdProducto"]!=null){var pid=Convert.ToInt32(p["IdProducto"]);movements.Add((pid,qty));
 // Every sale is supplied beforehand in the separate reference ledger. End stocks vary by SKU.
 var cost=p["Costo"]!=null?Convert.ToDecimal(p["Costo"]):Convert.ToDecimal(p["Precio"])*.7m;
 var extra=day.Day==1&&index%3==0?2:0;var supplier=-1000000-Convert.ToInt32(suppliers[index%suppliers.Count]["IdProveedor"]);
 var purchase=await Operation("compra",day,"Registrada",null,supplier,[(Convert.ToInt32(p["IdElementoCatalogo"]),qty+extra,decimal.Round(cost,2))]);await Movement(pid,day,"Entrada",qty+extra,purchase);}
 }
 if(lines.Count==0)continue;
 var ci=(day.Day%5==0?customers.Count-1:day.Day%Math.Max(1,Math.Min(3,customers.Count)));var client=-1000000-Convert.ToInt32(customers[ci]["IdCliente"]);
 var quote=await Operation("cotizacion",day,"Convertida en venta",client,null,lines);var sale=await Operation("venta",day,"Confirmada",client,null,lines,quote);foreach(var m in movements)await Movement(m.P,day,"Salida",m.Q,sale);
 if(day.Day%11==0)await Operation("cotizacion",day,day.Day%2==0?"Cancelada":"Pendiente",client,null,lines);
}
foreach(var p in physical){await using var cmd=new SqlCommand("""
IF NOT EXISTS(SELECT 1 FROM ia.AbastecimientoReferencia WHERE IdProducto=@p)
INSERT ia.AbastecimientoReferencia(IdProducto,IdLote,IdProveedor,PlazoDias,CoberturaDias,Seguridad,Minimo,Multiplo,Reservado,EntradaConfirmada,Evidencia,VerificadaHasta)
VALUES(@p,@batch,@v,7+@p%8,30,@safe,1,1,0,0,N'DEMOSTRACIÓN: plazo y reservas ficticios para validación técnica; no son condiciones reales del proveedor. Costo sin compra auténtica se deriva al 70 % del precio de catálogo.', '20270112');
""",c,tx);cmd.Parameters.AddWithValue("@p",p["IdProducto"]!);cmd.Parameters.AddWithValue("@batch",Guid.Parse(batch));cmd.Parameters.AddWithValue("@v",suppliers[0]["IdProveedor"]!);cmd.Parameters.AddWithValue("@safe",Math.Max(3,Convert.ToInt32(p["StockMinimo"])));await cmd.ExecuteNonQueryAsync();}
await new SqlCommand($"UPDATE bi.LoteReferencia SET Estado='Completo' WHERE IdLote='{batch}'",c,tx).ExecuteNonQueryAsync();
}
var invalid=Convert.ToInt32(await new SqlCommand($"""
SELECT (SELECT COUNT(*) FROM bi.OperacionReferencia r WHERE r.IdLote='{batch}' AND r.Total<>(SELECT SUM(Importe) FROM bi.DetalleReferencia d WHERE d.IdOperacion=r.IdOperacion))
+(SELECT COUNT(*) FROM bi.OperacionReferencia v JOIN bi.OperacionReferencia q ON q.IdOperacion=v.IdCotizacionReferencia WHERE v.IdLote='{batch}' AND (q.Tipo<>'cotizacion' OR q.IdCliente<>v.IdCliente OR q.Total<>v.Total OR q.Fecha>v.Fecha))
+(SELECT COUNT(*) FROM (SELECT IdProducto,SUM(CAST(Delta AS bigint)) OVER(PARTITION BY IdProducto ORDER BY Fecha,IdMovimiento) Saldo FROM bi.FactMovimiento WHERE Procedencia='referencia') m WHERE m.Saldo<0);
""",c,tx).ExecuteScalarAsync());
if(invalid>0)throw new Exception("La validación de importes, conversiones o saldo detectó inconsistencias. Se revierte el lote.");
var after=await Fingerprint(c,tx,batch);if(before.Count!=after.Count||before.Any(p=>after.GetValueOrDefault(p.Key)!=p.Value))throw new Exception("Cambió una fila original. Se revierte la carga.");
await tx.CommitAsync();committed=true;await File.WriteAllTextAsync(Path.Combine(output,"integridad.json"),JsonSerializer.Serialize(new{lote=batch,originalesConservados=true,tablasVerificadas=before.Count,inconsistencias=invalid,idempotente=existing,nuevaBaseCreada=false}));
}
var con=new BiConexion(config);var data=new IaDatos(con);var env=new PrivateEnv(Path.Combine(root,"Sistema3S.Web"));
// Env Development is fixed by this private dev-only CLI, never by a browser parameter.
var process=new IaProceso(con,data,new IaMotor(config,env,NullLogger<IaMotor>.Instance),config);var service=new IaService(con,data,config,env);
await process.Ejecutar("referencia",default);await service.ActualizarAvisos("referencia",default);
var views=new Dictionary<string,object>();foreach(var v in new[]{"resumen","pronostico","proyeccion","inventario","compras","seguimiento"})views[v]=await service.Consultar(v,"referencia",null,default);
await File.WriteAllTextAsync(Path.Combine(output,"analisis.json"),JsonSerializer.Serialize(views,IaMotor.Json));Console.WriteLine("Demostración incorporada y procesada. Operaciones auténticas conservadas.");
}catch(Exception e){await File.WriteAllTextAsync(Path.Combine(output,"error.json"),JsonSerializer.Serialize(new{fechaUtc=DateTime.UtcNow,tipo=e.GetType().Name,historialIncorporado=committed,analisisCompletado=false}));Console.Error.WriteLine("No se pudo completar la ejecución: "+(e is SqlException?"SQL Server no disponible o acceso denegado. Revisa la conexión desde tu sesión de Windows.":e.Message));Environment.ExitCode=1;}

static async Task<Dictionary<string,string>> Fingerprint(SqlConnection c,SqlTransaction tx,string batch){
 var names=new List<(string S,string T)>();await using(var cmd=new SqlCommand("SELECT s.name,t.name FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE s.name<>'ia' AND t.name<>'AuditoriaLog' ORDER BY s.name,t.name",c,tx)){await using var reader=await cmd.ExecuteReaderAsync();while(await reader.ReadAsync())names.Add((reader.GetString(0),reader.GetString(1)));}
 var result=new Dictionary<string,string>();foreach(var (s,t) in names){var filter=s=="bi"?(t=="DetalleReferencia"?$" WHERE IdOperacion NOT IN(SELECT IdOperacion FROM bi.OperacionReferencia WHERE IdLote='{batch}')":$" WHERE IdLote<>'{batch}'"):"";if(s=="bi"&&!new[]{"LoteReferencia","EntidadReferencia","OperacionReferencia","DetalleReferencia","MovimientoReferencia"}.Contains(t))filter="";
 await using var cmd=new SqlCommand($"SELECT * FROM [{s}].[{t}]{filter} FOR JSON PATH,INCLUDE_NULL_VALUES",c,tx){CommandTimeout=120};await using var r=await cmd.ExecuteReaderAsync();var json=new StringBuilder();while(await r.ReadAsync())json.Append(r.GetString(0));using var doc=JsonDocument.Parse(json.Length>0?json.ToString():"[]");result[s+"."+t]=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n",doc.RootElement.EnumerateArray().Select(x=>x.GetRawText()).Order(StringComparer.Ordinal)))));}
 return result;
}
sealed class PrivateEnv(string root):IWebHostEnvironment{public string EnvironmentName{get;set;}="Development";public string ApplicationName{get;set;}="Sistema3S";public string ContentRootPath{get;set;}=root;public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();public string WebRootPath{get;set;}=Path.Combine(root,"wwwroot");public IFileProvider WebRootFileProvider{get;set;}=new NullFileProvider();}
