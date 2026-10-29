using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;
using Sistema3S.Web.Services.Bi;
namespace Sistema3S.Web.Services.Ia;

public sealed class IaService(BiConexion conexion,IaDatos datos,IConfiguration config,IWebHostEnvironment env)
{
    public string Procedencia(string source)
    {
        if(source is not ("operativo" or "referencia"))throw new ArgumentException("Selecciona una procedencia válida.");
        if(source=="referencia"&&(!env.IsDevelopment()||!config.GetValue("Ia:ReferenciaTecnica",false)))throw new ArgumentException("La referencia técnica sólo está disponible en desarrollo autorizado.");
        return source;
    }
    private async Task<(bool installed,long? id,DateOnly? cutoff,DateTime? updated,JsonNode? result)> Ultima(string source,CancellationToken ct)
    {
        Procedencia(source);
        await using var c=await conexion.AbrirAsync(ct);await Sistema3S.Web.Services.Seguridad.ConexionAuditada.PrepararAsync(c,ct);await using var cmd=c.CreateCommand();
        cmd.CommandText="IF OBJECT_ID('ia.Ejecucion') IS NULL SELECT CAST(0 AS bit) Instalada,CAST(NULL AS bigint) Id,CAST(NULL AS date) Corte,CAST(NULL AS datetime2) Finalizada,CAST(NULL AS nvarchar(max)) Resultado; ELSE SELECT TOP(1) CAST(1 AS bit) Instalada,Id,Corte,Finalizada,Resultado FROM ia.Ejecucion WHERE Procedencia=@s AND Estado='Completada' ORDER BY Corte DESC,Id DESC;";
        cmd.Parameters.AddWithValue("@s",source);var r=await BiConexion.LeerAsync(cmd,["ultima"],ct);
        var row=r["ultima"].FirstOrDefault();
        row??=new Dictionary<string,object?>{["Instalada"]=true,["Id"]=null,["Corte"]=null,["Finalizada"]=null,["Resultado"]=null};
        var verified=source=="referencia"||config.GetValue("Ia:HistorialOperativoVerificado",false);
        if(source=="operativo"&&verified){
            var priorResult=row["Resultado"] is string priorText?JsonNode.Parse(priorText):null;
            var version=priorResult?["version"]?.GetValue<string>()??"";
            var modelCutoff=DateOnly.TryParse(priorResult?["modelCutoff"]?.GetValue<string>(),out var trained)?trained:(DateOnly?)null;
            await using var candidate=c.CreateCommand();candidate.CommandText="""
            IF OBJECT_ID('ia.Modelo') IS NOT NULL
            SELECT TOP(1) Corte,Creado,Evaluacion FROM ia.Modelo WHERE Version IN('3s-ia-6','3s-ia-7','3s-ia-8') AND Alcance='production'
            AND NOT EXISTS(SELECT 1 FROM ia.Modelo WHERE Activo=1 AND (@version NOT IN('3s-ia-6','3s-ia-7','3s-ia-8') OR (Version=@version AND Corte=@trained))) ORDER BY Corte DESC,Creado DESC;
            ELSE SELECT CAST(NULL AS date) Corte,CAST(NULL AS datetime2) Creado,CAST(NULL AS nvarchar(max)) Evaluacion WHERE 1=0;
            """;
            candidate.Parameters.AddWithValue("@version",version);BiConexion.Parametro(candidate,"@trained",SqlDbType.Date,modelCutoff?.ToDateTime(TimeOnly.MinValue));
            var rejected=(await BiConexion.LeerAsync(candidate,["candidato"],ct))["candidato"].FirstOrDefault();
            if(rejected?["Corte"] is DateTime rejectedCutoff){
                var cutoff=DateOnly.FromDateTime(row["Corte"] is DateTime prior&&prior>rejectedCutoff?prior:rejectedCutoff);var data=await new IaDatosAdministrados(conexion).Extraer(cutoff,ct);
                return(true,null,cutoff,rejected["Creado"] as DateTime?,IaDiagnostico.Crear(data.Datos,JsonNode.Parse((string)rejected["Evaluacion"]!)!.AsObject()));
            }
        }
        return(Convert.ToBoolean(row["Instalada"]),row["Id"]==null?null:Convert.ToInt64(row["Id"]),row["Corte"] is DateTime d?DateOnly.FromDateTime(d):null,
            row["Finalizada"] as DateTime?,verified&&row["Resultado"] is string text?JsonNode.Parse(text):null);
    }
    public async Task<object> Consultar(string view,string source,int? product,CancellationToken ct,DateOnly? fechaObjetivo=null)
    {
        IaPeriodo.ValidarFecha(fechaObjetivo,IaReglas.Hoy);
        var latest=await Ultima(source,ct);
        var periodo=IaPeriodo.Crear(fechaObjetivo,latest.cutoff,IaReglas.Hoy);
        var message=!latest.installed?"El análisis está pendiente de preparación por el responsable del sistema.":
            source=="operativo"&&!config.GetValue("Ia:HistorialOperativoVerificado",false)?"Las estimaciones se habilitarán cuando se verifique el historial comercial.":
            latest.result==null?"Aún no hay estimaciones disponibles. Se generarán cuando exista historial suficiente.":
            latest.result?["diagnostic"]?.GetValue<bool>()==true?"El historial ya se analizó. El nuevo modelo todavía no cumple la validación necesaria para ofrecer estimaciones confiables.":
            latest.cutoff<IaReglas.Hoy.AddDays(-1)?"Estas estimaciones no están actualizadas. Revisa su fecha antes de tomar decisiones.":latest.result?["version"]?.GetValue<string>()=="3s-ia-4"?"Estimaciones anteriores sólo para consulta. Aún no hay una previsión validada para recomendar cantidades de compra.":"Estimaciones disponibles para revisión comercial.";
        if(!latest.installed)return new{procedencia=source,mensaje=message,actualizado=latest.updated,periodo,productos=Array.Empty<object>()};
        var context=await datos.Contexto(source,ct);
        if(product.HasValue){if(product<=0||!context.Any(x=>x.IdProducto==product))throw new ArgumentException("Producto no disponible.");context=context.Where(x=>x.IdProducto==product).ToList();}
        var predictions=latest.result?["products"]?.AsArray().Where(x=>x!=null).ToDictionary(x=>x!["id"]!.GetValue<int>())??[];
        if(view=="resumen")
        {
            return new{procedencia=source,mensaje=message,actualizado=latest.updated,periodo,
                historialOperativoVerificado=config.GetValue("Ia:HistorialOperativoVerificado",false),productos=context.Count,
                conEstimacion=context.Count(x=>predictions.GetValueOrDefault(x.IdProducto)?["sufficient"]?.GetValue<bool>()==true),
                advertencia=source=="referencia"?"Validación técnica con datos de referencia. No representa resultados comerciales reales ni habilita compras.":null};
        }
        if(view=="seguimiento")return new{procedencia=source,mensaje=message,actualizado=latest.updated,periodo,
            recomendaciones=await Recomendaciones(source,product,ct,fechaObjetivo),confiabilidad=await Confiabilidad(source,product,ct,fechaObjetivo)};
        if(view=="compras")return new{procedencia=source,mensaje=message,actualizado=latest.updated,periodo,
            recomendaciones=await Recomendaciones(source,product,ct,fechaObjetivo)};
        var fresh=latest.cutoff==IaReglas.Hoy.AddDays(-1);
        var historicalIncome=view=="proyeccion"?(await datos.IngresosMensuales(source,ct)).ToLookup(x=>x.IdProducto):null;
        var products=context.Select(p=>
        {
            var prediction=predictions.GetValueOrDefault(p.IdProducto);
            var daily=prediction?["daily"]?.AsArray().Select(d=>d!["quantity"]!.GetValue<decimal>()).ToList()??[];
            var first=prediction?["first"] is JsonNode f?DateOnly.Parse(f.GetValue<string>()):periodo.Desde;
            var risk=IaReglas.Riesgo(p,IaPublicacion.Confiable(latest.result,prediction)?daily:[],first,IaReglas.Hoy,fresh,IaReglas.Desviacion(prediction),fechaObjetivo);
            if(view=="inventario")return (object)new IaInventarioProducto(p.IdProducto,p.Nombre,p.Unidad,risk,p.Politica,p.Proveedor,p.Politica?.IdProveedor??p.IdProveedor);
            var selected=fechaObjetivo.HasValue?IaPeriodo.Cantidad(prediction,periodo):null;
            var history=IaHistorial.Leer(prediction,latest.cutoff);
            var estimated=prediction?["daily"]?.AsArray().Where(d=>!fechaObjetivo.HasValue||DateOnly.Parse(d!["date"]!.GetValue<string>())<=fechaObjetivo).ToArray();
            if(view=="proyeccion")
            {
                object Importes(decimal? q,int? h,string? mes){var margin=q*(p.PrecioNeto-p.CostoNeto);return new{horizonte=h,mes,cantidad=q,ingresoNeto=q*p.PrecioNeto,costoReferencia=q*p.CostoNeto,margenBruto=margin,
                    porcentajeMargen=q*p.PrecioNeto>0?margin/(q*p.PrecioNeto)*100:null,
                    estado=margin==null?"Información insuficiente":margin<0?"Margen negativo estimado":margin==0?"Margen bruto estimado nulo":"Margen bruto estimado positivo",
                    datosPendientes=IaDisponibilidad.Pendientes(q,p.PrecioNeto,p.CostoNeto),
                    recomendacion=margin==null?IaDisponibilidad.Motivo(q,p.PrecioNeto,p.CostoNeto):margin<0?"Revisa el precio de venta y el costo de abastecimiento antes de comprometer una oferta; con estas referencias el margen bruto sería negativo.":
                        "Contrasta precio, costo y demanda estimada con las condiciones comerciales antes de comprometer abastecimiento. El margen bruto no incluye gastos operativos."};}
                var horizons=new[]{30,60,90,7}.Select(h=>Importes(IaReglas.Cantidad(prediction,h),h,null)).ToList();
                var emittedToday=first.AddDays(-1);var nextMonth=new DateOnly(emittedToday.Year,emittedToday.Month,1).AddMonths(1);
                var monthly=estimated?.GroupBy(d=>d!["date"]!.GetValue<string>()[..7]).OrderBy(g=>g.Key)
                    .Select(g=>new{mes=g.Key,cantidad=g.Sum(d=>d!["quantity"]!.GetValue<decimal>()),ingresoNeto=g.Sum(d=>d!["quantity"]!.GetValue<decimal>())*p.PrecioNeto,
                        desde=g.First()!["date"]!.GetValue<string>(),hasta=g.Last()!["date"]!.GetValue<string>()}).ToList();
                return new{p.IdProducto,p.Nombre,p.Unidad,horizontes=horizons,precioNeto=p.PrecioNeto,costoNeto=p.CostoNeto,p.FechaPrecio,p.FechaCosto,
                    periodoPersonalizado=fechaObjetivo.HasValue?Importes(selected,periodo.Dias,null):null,
                    intervalos=prediction?["intervals"]?.DeepClone(),
                    mesSiguienteFinanciero=Importes(prediction?["nextMonth"]?.GetValue<decimal>(),null,nextMonth.ToString("yyyy-MM")),
                    historicoMensual=historicalIncome![p.IdProducto].Select(x=>new{mes=x.Mes,ingresoNeto=x.IngresoNeto}).ToList(),estimadoMensual=monthly,
                    politicaPrecio="Precio neto ponderado de ventas confirmadas recientes, mantenido constante en la proyección. No incluye IGV.",
                    alcanceCosto="Costo neto de adquisición reciente como referencia. Margen bruto estimado; no es utilidad neta ni costo contable valorizado."};
            }
            return new{p.IdProducto,p.Nombre,p.Unidad,p.Stock,suficiente=prediction?["sufficient"]?.GetValue<bool>()??false,
                motivo=prediction?["sufficient"]?.GetValue<bool>()==true?"Estimación disponible para revisión comercial.":prediction?["reason"]?.GetValue<string>()??(history.DiasFaltantes==0&&history.DiasConVentaFaltantes==0?"Las fechas cumplen el mínimo; no hay suficientes observaciones diarias útiles para entrenar.":"No hay ventas confirmadas válidas en el historial analizado."),
                historial=history,confiable=IaPublicacion.Confiable(latest.result,prediction),calidad=prediction?["quality"]?.DeepClone(),metodo=prediction?["method"]?.DeepClone(),cambioPatron=prediction?["regimeChanged"]?.GetValue<bool>()??false,periodoPersonalizado=fechaObjetivo.HasValue?new{cantidad=selected}:null,
                cantidades=prediction?["totals"]?.DeepClone(),mesSiguiente=prediction?["nextMonth"]?.DeepClone(),
                intervalos=prediction?["intervals"]?.DeepClone(),desde=first,
                observado30=prediction?["history"]?.AsArray().TakeLast(30).Sum(d=>d!["quantity"]!.GetValue<decimal>()),
                variacion30=Variacion(prediction),
                orientacion=prediction?["sufficient"]?.GetValue<bool>()==true?"Usa esta estimación para revisar cobertura y prioridades de abastecimiento; confirma los compromisos comerciales con el cliente antes de comprar.":"Conserva el registro de ventas y verifica el inventario para disponer de información suficiente.",
                historico=prediction?["history"]?.DeepClone(),estimado=estimated};
        }).ToList();
        if(view=="inventario")products=products.OrderBy(x=>
            ((IaInventarioProducto)x).Riesgo.Prioridad switch{"Urgente"=>0,"Alta"=>1,"Revisar stock"=>2,"Planificar"=>3,_=>4}).ToList();
        return new{procedencia=source,mensaje=message,actualizado=latest.updated,periodo,productos=products,
            advertencia=source=="referencia"?"Referencia técnica; no equivale a demanda real ni a precisión comercial validada.":null};
    }
    private static decimal? Variacion(JsonNode? prediction){var actual=prediction?["history"]?.AsArray().TakeLast(30).Sum(d=>d!["quantity"]!.GetValue<decimal>());return actual>0?(IaReglas.Cantidad(prediction,30)-actual)/actual*100:null;}
    private async Task<List<Dictionary<string,object?>>> Recomendaciones(string source,int? product,CancellationToken ct,DateOnly? fechaObjetivo=null)
    {
        await using var c=await conexion.AbrirAsync(ct);await Sistema3S.Web.Services.Seguridad.ConexionAuditada.PrepararAsync(c,ct);await using var cmd=c.CreateCommand();cmd.CommandText="""
        SELECT TOP(200) r.Id,r.IdProducto,e.Nombre,e.Unidad,r.Prioridad,r.Cantidad,r.FechaPedido,r.Mensaje,r.Estado,r.Creada,r.Revisada,r.Motivo,
        s.Id IdSolicitud,s.IdCompra,
        CAST(CASE WHEN EXISTS(SELECT 1 FROM ia.Recomendacion n WHERE n.Procedencia=r.Procedencia AND n.IdProducto=r.IdProducto AND n.Id>r.Id) THEN 0 ELSE 1 END AS bit) Actual
        FROM ia.Recomendacion r JOIN bi.DimElemento e ON e.IdProducto=r.IdProducto
        
        LEFT JOIN ia.SolicitudCompra s ON s.IdRecomendacion=r.Id
        WHERE r.Procedencia=@s AND (@p IS NULL OR r.IdProducto=@p)
        AND (@fecha IS NULL OR r.FechaPedido IS NULL OR r.FechaPedido<DATEADD(day,1,@fecha)) ORDER BY r.Id DESC;
        """;
        cmd.Parameters.AddWithValue("@s",source);cmd.Parameters.AddWithValue("@p",(object?)product??DBNull.Value);
        BiConexion.Parametro(cmd,"@fecha",SqlDbType.Date,fechaObjetivo?.ToDateTime(TimeOnly.MinValue));
        var rows=(await BiConexion.LeerAsync(cmd,["r"],ct))["r"];
        var context=(await datos.Contexto(source,ct)).ToDictionary(p=>p.IdProducto);
        var latest=await Ultima(source,ct);
        var predictions=latest.result?["products"]?.AsArray().ToDictionary(p=>p!["id"]!.GetValue<int>())??[];
        foreach(var row in rows){var id=Convert.ToInt32(row["IdProducto"]);var p=context.GetValueOrDefault(id);var prediction=predictions.GetValueOrDefault(id);
            row["Stock"]=p?.Stock;row["IdProveedor"]=p?.Politica?.IdProveedor??p?.IdProveedor;row["Proveedor"]=p?.Proveedor;
            foreach(var h in new[]{30,60,90})row["Demanda"+h]=IaReglas.Cantidad(prediction,h);
            row["DemandaFecha"]=fechaObjetivo.HasValue?IaPeriodo.Cantidad(prediction,IaPeriodo.Crear(fechaObjetivo,latest.cutoff,IaReglas.Hoy)):null;
        }
        return rows;
    }
    public async Task<object> Avisos(CancellationToken ct,string source="operativo")
    {
        var latest=await Ultima(source,ct);if(!latest.installed)return new{pendientes=0};
        await using var c=await conexion.AbrirAsync(ct);await Sistema3S.Web.Services.Seguridad.ConexionAuditada.PrepararAsync(c,ct);await using var cmd=c.CreateCommand();
        cmd.CommandText="""
        SELECT COUNT(*) FROM ia.Recomendacion r WHERE r.Procedencia=@source AND r.Estado='Pendiente'
        AND r.Prioridad IN('Urgente','Alta','Revisar stock','Verificar abastecimiento') AND NOT EXISTS(SELECT 1 FROM ia.Recomendacion n WHERE n.Procedencia=r.Procedencia AND n.IdProducto=r.IdProducto AND n.Id>r.Id);
        """;
        cmd.Parameters.AddWithValue("@source",source);return new{pendientes=Convert.ToInt32(await cmd.ExecuteScalarAsync(ct))};
    }
    public async Task ActualizarAvisos(string source,CancellationToken ct)
    {
        var latest=await Ultima(source,ct);if(!latest.installed)return;
        var products=await datos.Contexto(source,ct);
        var predictions=latest.result?["products"]?.AsArray().Where(x=>x!=null).ToDictionary(x=>x!["id"]!.GetValue<int>())??[];
        await using var c=await conexion.AbrirAsync(ct);await Sistema3S.Web.Services.Seguridad.ConexionAuditada.PrepararAsync(c,ct);
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        foreach(var p in products)
        {
            var prediction=predictions.GetValueOrDefault(p.IdProducto);
            var days=prediction?["daily"]?.AsArray().Select(d=>d!["quantity"]!.GetValue<decimal>()).ToList()??[];
            var first=prediction?["first"] is JsonNode f?DateOnly.Parse(f.GetValue<string>()):IaReglas.Hoy.AddDays(1);
            var risk=IaReglas.Riesgo(p,IaPublicacion.Confiable(latest.result,prediction)?days:[],first,IaReglas.Hoy,latest.cutoff==IaReglas.Hoy.AddDays(-1),IaReglas.Desviacion(prediction));
            // Cambios materiales: prioridad, necesidad >=25% (mínimo una unidad), o semana de pedido.
            await using var read=c.CreateCommand();read.Transaction=tx;read.CommandText="SELECT TOP(1) Prioridad,Cantidad,FechaPedido FROM ia.Recomendacion WITH(UPDLOCK,HOLDLOCK) WHERE Procedencia=@s AND IdProducto=@p ORDER BY Id DESC";
            read.Parameters.AddWithValue("@s",source);read.Parameters.AddWithValue("@p",p.IdProducto);
            var old=(await BiConexion.LeerAsync(read,["old"],ct))["old"].FirstOrDefault();
            if(old==null && risk.Prioridad=="Cubierto")continue;
            if(old!=null && (string)old["Prioridad"]! ==risk.Prioridad)
            {
                var oldQuantity=old["Cantidad"]==null?(decimal?)null:Convert.ToDecimal(old["Cantidad"]);
                var sameQuantity=oldQuantity==risk.Cantidad || oldQuantity!=null&&risk.Cantidad!=null&&Math.Abs(oldQuantity.Value-risk.Cantidad.Value)<Math.Max(1,Math.Abs(oldQuantity.Value)*.25m);
                var sameWeek=old["FechaPedido"]==null&&risk.FechaPedido==null || old["FechaPedido"] is DateTime d&&risk.FechaPedido!=null&&Math.Abs(DateOnly.FromDateTime(d).DayNumber-risk.FechaPedido.Value.DayNumber)<7;
                if(sameQuantity&&sameWeek)continue;
            }
            var signature=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{latest.id}:{risk.Prioridad}:{risk.Cantidad}:{risk.FechaPedido}:{IaReglas.Hoy}"))).ToLowerInvariant();
            await using var insert=c.CreateCommand();insert.Transaction=tx;insert.CommandText="""
            IF NOT EXISTS(SELECT 1 FROM ia.Recomendacion WHERE Procedencia=@s AND IdProducto=@p AND Firma=@f)
            INSERT ia.Recomendacion(Procedencia,IdProducto,IdEjecucion,Prioridad,Cantidad,FechaPedido,Mensaje,Firma)
            VALUES(@s,@p,@e,@priority,@q,@date,@message,@f);
            """;
            insert.Parameters.AddWithValue("@s",source);insert.Parameters.AddWithValue("@p",p.IdProducto);insert.Parameters.AddWithValue("@e",(object?)latest.id??DBNull.Value);
            insert.Parameters.AddWithValue("@priority",risk.Prioridad);insert.Parameters.AddWithValue("@q",(object?)risk.Cantidad??DBNull.Value);
            insert.Parameters.AddWithValue("@date",risk.FechaPedido.HasValue?risk.FechaPedido.Value.ToDateTime(TimeOnly.MinValue):DBNull.Value);
            insert.Parameters.AddWithValue("@message",risk.Explicacion);insert.Parameters.AddWithValue("@f",signature);await insert.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
    }
    private async Task<object> Confiabilidad(string source,int? product,CancellationToken ct,DateOnly? fechaObjetivo=null)
    {
        await using var c=await conexion.AbrirAsync(ct);await Sistema3S.Web.Services.Seguridad.ConexionAuditada.PrepararAsync(c,ct);await using var cmd=c.CreateCommand();cmd.CommandText="""
        SELECT p.Horizonte,COUNT(*) Ventanas,AVG(ABS(p.Cantidad-p.Real)) Desviacion,
        CASE WHEN SUM(p.Real)>0 THEN SUM(ABS(p.Cantidad-p.Real))*100/SUM(p.Real) END DesviacionPorcentual,
        MAX(p.Evaluada) Actualizada FROM ia.Pronostico p JOIN ia.Ejecucion e ON e.Id=p.IdEjecucion
        WHERE e.Procedencia=@s AND p.Evaluada IS NOT NULL AND (@p IS NULL OR p.IdProducto=@p) AND (@fecha IS NULL OR p.Hasta<=@fecha)
        GROUP BY p.Horizonte;
        """;
        cmd.Parameters.AddWithValue("@s",source);cmd.Parameters.AddWithValue("@p",(object?)product??DBNull.Value);
        BiConexion.Parametro(cmd,"@fecha",SqlDbType.Date,fechaObjetivo?.ToDateTime(TimeOnly.MinValue));
        var r=(await BiConexion.LeerAsync(cmd,["r"],ct))["r"];
        // Sin producto no se agregan cantidades de unidades incompatibles.
        return new{mensaje=source=="referencia"?"La referencia permite validar el funcionamiento técnico, no la confiabilidad comercial.":
            !product.HasValue?"Selecciona un producto para revisar estimaciones emitidas antes del período y ventas observadas después.":
            r.Count==0?"Aún no han transcurrido los períodos necesarios para comprobar estas estimaciones.":"Desviaciones observadas por producto; no garantizan resultados futuros.",
            resultados=source=="operativo"&&product.HasValue?r:[]};
    }
    public async Task<object> PrevisualizarAbastecimiento(IaPolitica policy,CancellationToken ct)
    {
        IaReglas.Validar(policy,IaReglas.Hoy);
        var latest=await Ultima("operativo",ct);
        if(latest.cutoff!=IaReglas.Hoy.AddDays(-1)||latest.result==null)throw new ArgumentException("Actualiza las estimaciones antes de calcular el plan.");
        var context=(await datos.Contexto("operativo",ct)).FirstOrDefault(p=>p.IdProducto==policy.IdProducto)??throw new ArgumentException("Producto no disponible.");
        await using var c=await conexion.AbrirAsync(ct);await using var cmd=c.CreateCommand();
        cmd.CommandText="SELECT RazonSocial FROM dbo.Proveedor WHERE IdProveedor=@id AND Estado=1";cmd.Parameters.AddWithValue("@id",policy.IdProveedor);
        var proveedor=await cmd.ExecuteScalarAsync(ct) as string??throw new ArgumentException("Proveedor no disponible.");
        var prediction=latest.result["products"]?.AsArray().FirstOrDefault(p=>p?["id"]?.GetValue<int>()==policy.IdProducto);
        var days=prediction?["daily"]?.AsArray().Select(d=>d!["quantity"]!.GetValue<decimal>()).ToArray()??[];
        if(days.Length<90||prediction?["sufficient"]?.GetValue<bool>()!=true)throw new ArgumentException("No hay un pronóstico vigente para calcular este plan.");
        if(!IaPublicacion.Confiable(latest.result,prediction))throw new ArgumentException("La estimación aún no tiene confiabilidad suficiente para calcular una compra. Puedes conservar las condiciones y revisar el stock manualmente.");
        var first=DateOnly.Parse(prediction!["first"]!.GetValue<string>());
        var actual=context with{Politica=policy,IdProveedor=policy.IdProveedor,Proveedor=proveedor};
        var riesgo=IaReglas.Riesgo(actual,days,first,IaReglas.Hoy,true,IaReglas.Desviacion(prediction));
        return new{riesgo,demanda=days.Take(policy.PlazoDias+policy.CoberturaDias).Sum(),
            stockDisponible=context.Stock-policy.Reservado,recepcion=IaReglas.Hoy.AddDays(policy.PlazoDias),
            costoNeto=riesgo.Cantidad*context.CostoNeto,proveedor,guardado=false};
    }
    public async Task GuardarPolitica(IaPolitica policy,int user,CancellationToken ct)
    {
        IaReglas.Validar(policy,IaReglas.Hoy);
        await using var c=await conexion.AbrirAsync(ct);await Sistema3S.Web.Services.Seguridad.ConexionAuditada.PrepararAsync(c,ct);await using var cmd=c.CreateCommand();cmd.CommandText="""
        SET XACT_ABORT ON; BEGIN TRANSACTION;
        IF NOT EXISTS(SELECT 1 FROM dbo.Producto p JOIN dbo.ElementoCatalogo e ON e.IdElementoCatalogo=p.IdElementoCatalogo WHERE p.IdProducto=@p AND p.AplicaInventario=1 AND e.Estado=1)
        OR NOT EXISTS(SELECT 1 FROM dbo.Proveedor WHERE IdProveedor=@v AND Estado=1) THROW 51000,'Producto o proveedor no disponible.',1;
        UPDATE ia.Abastecimiento WITH(UPDLOCK,SERIALIZABLE) SET IdProveedor=@v,PlazoDias=@lead,CoberturaDias=@cov,Seguridad=@safe,Minimo=@min,Multiplo=@mul,
        Reservado=@reserve,EntradaConfirmada=@entry,FechaEntrada=@date,Evidencia=@proof,VerificadaHasta=@until,IdUsuario=@user,Actualizada=SYSUTCDATETIME() WHERE IdProducto=@p;
        IF @@ROWCOUNT=0 INSERT ia.Abastecimiento(IdProducto,IdProveedor,PlazoDias,CoberturaDias,Seguridad,Minimo,Multiplo,Reservado,EntradaConfirmada,FechaEntrada,Evidencia,VerificadaHasta,IdUsuario)
        VALUES(@p,@v,@lead,@cov,@safe,@min,@mul,@reserve,@entry,@date,@proof,@until,@user);
        COMMIT;
        """;
        foreach(var pair in new Dictionary<string,object?>{{"@p",policy.IdProducto},{"@v",policy.IdProveedor},{"@lead",policy.PlazoDias},{"@cov",policy.CoberturaDias},{"@safe",policy.Seguridad},{"@min",policy.Minimo},{"@mul",policy.Multiplo},
            {"@reserve",policy.Reservado},{"@entry",policy.EntradaConfirmada},{"@date",policy.FechaEntrada?.ToDateTime(TimeOnly.MinValue)},{"@proof",policy.Evidencia},{"@until",policy.VerificadaHasta.ToDateTime(TimeOnly.MinValue)},{"@user",user}})cmd.Parameters.AddWithValue(pair.Key,pair.Value??DBNull.Value);
        try{await cmd.ExecuteNonQueryAsync(ct);}catch(SqlException ex)when(ex.Number==51000){throw new ArgumentException("Producto o proveedor no disponible.");}
        await ActualizarAvisos("operativo",ct);
    }
    public async Task Revisar(long id,IaRevision review,int user,string source,CancellationToken ct)
    {
        Procedencia(source);
        if(review.Estado is not ("Revisada" or "Aceptada" or "Descartada" or "Atendida")||string.IsNullOrWhiteSpace(review.Motivo)||review.Motivo.Length>500)throw new ArgumentException("Selecciona un estado y explica la revisión.");
        await using var c=await conexion.AbrirAsync(ct);await Sistema3S.Web.Services.Seguridad.ConexionAuditada.PrepararAsync(c,ct);await using var cmd=c.CreateCommand();cmd.CommandText="""
        UPDATE r SET Estado=@state,Motivo=@reason,IdUsuario=@user,Revisada=SYSUTCDATETIME()
        FROM ia.Recomendacion r WHERE r.Id=@id AND r.Procedencia=@s AND r.Estado<>'Atendida' AND
        (@state<>'Atendida' OR EXISTS(SELECT 1 FROM ia.SolicitudCompra s JOIN dbo.Compra c ON c.IdCompra=s.IdCompra
        JOIN dbo.EstadoCompra e ON e.IdEstadoCompra=c.IdEstadoCompra WHERE s.IdRecomendacion=r.Id AND e.Nombre IN(N'Registrada',N'Confirmada')));
        """;
        cmd.Parameters.AddWithValue("@id",id);cmd.Parameters.AddWithValue("@state",review.Estado);cmd.Parameters.AddWithValue("@reason",review.Motivo);cmd.Parameters.AddWithValue("@user",user);
        cmd.Parameters.AddWithValue("@s",source);
        if(await cmd.ExecuteNonQueryAsync(ct)!=1)throw new ArgumentException("La recomendación no admite este cambio. Para atenderla debe existir una compra vinculada vigente.");
    }
}
