using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Sistema3S.Web.Services.Bi;
namespace Sistema3S.Web.Services.Ia;

public sealed class IaProceso(BiConexion conexion,IaDatos datos,IaMotor motor,IConfiguration config)
{
    public async Task Ejecutar(string source,CancellationToken ct)
    {
        if(source=="operativo"&&!config.GetValue("Ia:HistorialOperativoVerificado",false))return;
        await using var c=await conexion.AbrirAsync(ct);await Sistema3S.Web.Services.Seguridad.ConexionAuditada.PrepararAsync(c,ct);
        await using var gate=c.CreateCommand();
        gate.CommandText="DECLARE @r int,@name nvarchar(100)=N'Sistema3S.IA.'+@s; EXEC @r=sys.sp_getapplock @Resource=@name,@LockMode='Exclusive',@LockOwner='Session',@LockTimeout=0; SELECT @r;";
        gate.Parameters.AddWithValue("@s",source);
        if(Convert.ToInt32(await gate.ExecuteScalarAsync(ct))<0)return;
        try
        {
            await using var exists=c.CreateCommand();exists.CommandText="SELECT CASE WHEN OBJECT_ID('ia.Ejecucion') IS NULL THEN 0 ELSE 1 END";
            if(Convert.ToInt32(await exists.ExecuteScalarAsync(ct))==0)return;
            var cutoff=IaReglas.Hoy.AddDays(-1);
            var input=await datos.Extraer(source,cutoff,ct);
            var serialized=JsonSerializer.Serialize(input,IaMotor.Json);
            var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(serialized+motor.HuellaVersion))).ToLowerInvariant();
            await using var cmd=c.CreateCommand();
            cmd.CommandText="""
            UPDATE ia.Ejecucion SET Estado='Fallida',Error='IA_INTERRUPCION',Finalizada=SYSUTCDATETIME()
            WHERE Procedencia=@s AND Estado='EnCurso';
            IF EXISTS(SELECT 1 FROM ia.Ejecucion WHERE Procedencia=@s AND Corte=@d AND Huella=@h AND Estado='Completada') SELECT CAST(0 AS bigint);
            ELSE IF EXISTS(SELECT 1 FROM ia.Ejecucion WHERE Procedencia=@s AND Corte=@d AND Huella=@h)
            BEGIN UPDATE ia.Ejecucion SET Estado='EnCurso',Error=NULL,Finalizada=NULL,Creada=SYSUTCDATETIME() WHERE Procedencia=@s AND Corte=@d AND Huella=@h;
            SELECT Id FROM ia.Ejecucion WHERE Procedencia=@s AND Corte=@d AND Huella=@h; END
            ELSE BEGIN INSERT ia.Ejecucion(Procedencia,Corte,Huella,Estado) VALUES(@s,@d,@h,'EnCurso'); SELECT CAST(SCOPE_IDENTITY() AS bigint); END
            """;
            cmd.Parameters.AddWithValue("@s",source);cmd.Parameters.AddWithValue("@d",cutoff.ToDateTime(TimeOnly.MinValue));cmd.Parameters.AddWithValue("@h",hash);
            var id=Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
            if(id==0){await Evaluar(c,source,ct);return;}
            try
            {
                var result=await motor.Ejecutar(input,ct);
                if(IaReglas.Hoy>cutoff.AddDays(1))throw new InvalidOperationException("IA_CORTE_VENCIDO");
                await using var tx=(SqlTransaction)await c.BeginTransactionAsync(ct);
                foreach(var p in result["products"]!.AsArray())
                {
                    if(p!["sufficient"]?.GetValue<bool>()!=true)continue;
                    var start=DateOnly.Parse(p["first"]!.GetValue<string>());
                    foreach(var h in new[]{30,60,90})
                    {
                        await using var insert=c.CreateCommand();insert.Transaction=tx;insert.CommandText="""
                        INSERT ia.Pronostico(IdEjecucion,IdProducto,Desde,Hasta,Horizonte,Cantidad,Inferior,Superior)
                        VALUES(@id,@p,@desde,@hasta,@h,@q,@l,@u);
                        """;
                        insert.Parameters.AddWithValue("@id",id);insert.Parameters.AddWithValue("@p",p["id"]!.GetValue<int>());
                        insert.Parameters.AddWithValue("@desde",start.ToDateTime(TimeOnly.MinValue));insert.Parameters.AddWithValue("@hasta",start.AddDays(h-1).ToDateTime(TimeOnly.MinValue));
                        insert.Parameters.AddWithValue("@h",h);insert.Parameters.AddWithValue("@q",IaReglas.Cantidad(p,h)!.Value);
                        insert.Parameters.AddWithValue("@l",(object?)p["intervals"]?[h.ToString()]?["lower"]?.GetValue<decimal>()??DBNull.Value);
                        insert.Parameters.AddWithValue("@u",(object?)p["intervals"]?[h.ToString()]?["upper"]?.GetValue<decimal>()??DBNull.Value);
                        await insert.ExecuteNonQueryAsync(ct);
                    }
                }
                await using var finish=c.CreateCommand();finish.Transaction=tx;
                finish.CommandText="UPDATE ia.Ejecucion SET Resultado=@r,Estado='Completada',Finalizada=SYSUTCDATETIME(),Error=NULL WHERE Id=@id";
                finish.Parameters.AddWithValue("@r",result.ToJsonString());finish.Parameters.AddWithValue("@id",id);await finish.ExecuteNonQueryAsync(ct);
                await tx.CommitAsync(ct);await Evaluar(c,source,ct);
            }
            catch(Exception ex)
            {
                await using var failed=c.CreateCommand();failed.CommandText="UPDATE ia.Ejecucion SET Estado='Fallida',Error=@e,Finalizada=SYSUTCDATETIME() WHERE Id=@id";
                failed.Parameters.AddWithValue("@id",id);failed.Parameters.AddWithValue("@e",ex is OperationCanceledException?"IA_TIMEOUT_O_CANCELACION":"IA_PROCESO_FALLIDO");
                await failed.ExecuteNonQueryAsync(CancellationToken.None);throw;
            }
        }
        finally
        {
            await using var release=c.CreateCommand();release.CommandText="DECLARE @name nvarchar(100)=N'Sistema3S.IA.'+@s; EXEC sys.sp_releaseapplock @Resource=@name,@LockOwner='Session';";
            release.Parameters.AddWithValue("@s",source);await release.ExecuteNonQueryAsync(CancellationToken.None);
        }
    }
    public static async Task Evaluar(SqlConnection c,string source,CancellationToken ct)
    {
        await using var cmd=c.CreateCommand();cmd.CommandTimeout=60;
        cmd.CommandText="""
        UPDATE p SET Real=COALESCE(a.Cantidad,0),Evaluada=SYSUTCDATETIME()
        FROM ia.Pronostico p JOIN ia.Ejecucion e ON e.Id=p.IdEjecucion JOIN dbo.Producto prod ON prod.IdProducto=p.IdProducto
        OUTER APPLY(SELECT SUM(CAST(v.Cantidad AS decimal(18,6))) Cantidad FROM bi.FactVenta v
        WHERE v.Procedencia=@s AND v.IdElementoCatalogo=prod.IdElementoCatalogo AND v.Estado=N'Confirmada'
        AND v.SinDetalleValido=0 AND v.Fecha>=p.Desde AND v.Fecha<DATEADD(day,1,p.Hasta)) a
        WHERE e.Procedencia=@s AND e.Estado='Completada' AND p.Hasta<@today
        AND (p.Evaluada IS NULL OR p.Real<>COALESCE(a.Cantidad,0))
        AND CONVERT(date,DATEADD(hour,-5,e.Finalizada))<p.Desde;
        """;
        cmd.Parameters.AddWithValue("@s",source);cmd.Parameters.AddWithValue("@today",IaReglas.Hoy.ToDateTime(TimeOnly.MinValue));await cmd.ExecuteNonQueryAsync(ct);
    }
}

public sealed class IaWorker(IServiceScopeFactory scopes,IConfiguration config,IWebHostEnvironment env,ILogger<IaWorker> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if(!config.GetValue("Ia:Habilitada",false))return;
        while(!stoppingToken.IsCancellationRequested)
        {
            foreach(var source in new[]{"operativo","referencia"})
            {
                if(source=="referencia"&&(!env.IsDevelopment()||!config.GetValue("Ia:ReferenciaTecnica",false)))continue;
                for(int attempt=0;attempt<2;attempt++)
                {
                    try
                    {
                        await using var scope=scopes.CreateAsyncScope();
                        await scope.ServiceProvider.GetRequiredService<IaProceso>().Ejecutar(source,stoppingToken);
                        await scope.ServiceProvider.GetRequiredService<IaService>().ActualizarAvisos(source,stoppingToken);
                        break;
                    }
                    catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested){return;}
                    catch(Exception ex){logger.LogError(ex,"Ejecución IA aislada fallida; procedencia {Procedencia}, intento {Intento}",source,attempt+1);}
                    if(attempt==0)await Task.Delay(TimeSpan.FromSeconds(30),stoppingToken);
                }
            }
            // Periodicidad diaria; la exclusión SQL y la huella hacen los reinicios idempotentes.
            var next=DateTime.UtcNow.Date.AddHours(8);
            if(next<=DateTime.UtcNow)next=next.AddDays(1);
            await Task.Delay(next-DateTime.UtcNow,stoppingToken);
        }
    }
}
