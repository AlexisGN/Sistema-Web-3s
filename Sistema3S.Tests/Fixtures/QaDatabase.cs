using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Sistema3S.Web.Data;
using Sistema3S.Web.Services.Seguridad;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace Sistema3S.Tests.Fixtures;

public sealed class TestDb(DbContextOptions<Bd3sContext> options) : Bd3sContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder builder) { }
}

public sealed class QaDatabase : IAsyncDisposable
{
    private const string InstanceName = "Sistema3S_QA";
    private static readonly SemaphoreSlim ServerGate = new(1, 1);
    private static bool serverReady;
    public const string Server = @"(localdb)\Sistema3S_QA";
    public const string Password = "SoloQA_12345!";
    public string Name { get; } = "Sistema3S_QA_" + Guid.NewGuid().ToString("N");
    public string Connection => $"Server={Server};Database={Name};Integrated Security=true;TrustServerCertificate=true;Connect Timeout=15";
    public TestDb Open() => new(new DbContextOptionsBuilder<Bd3sContext>().UseSqlServer(Connection).AddInterceptors(new ConexionAuditoriaInterceptor()).Options);
    public IConfiguration Config => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
        ["Jwt:Key"]="Only-QA-local-synthetic-signing-key-1234567890",["Jwt:Issuer"]="Sistema3S",["Jwt:Audience"]="Sistema3SAdmin",
        ["Decolecta:ApiKey"]="qa-mock-only",["Decolecta:BaseUrl"]="https://external.example.test/",["ClienteWeb:UrlPublica"]="http://localhost:4200"
    }).Build();
    public static async Task<QaDatabase> CreateAsync()
    {
        await EnsureServerAsync();
        var db = new QaDatabase();
        await using(var c = new SqlConnection($"Server={Server};Database=master;Integrated Security=true;TrustServerCertificate=true")) {
            await c.OpenAsync(); await new SqlCommand($"CREATE DATABASE [{db.Name}]",c).ExecuteNonQueryAsync();
        }
        try {
            await db.Script("schema.sql"); await db.Script("seed.sql");
            var hash = new PasswordHashService().CrearHash(Password);
            await using var sql = new SqlConnection(db.Connection); await sql.OpenAsync();
            await using var cmd = new SqlCommand("UPDATE dbo.Usuario SET ContrasenaHash=@h", sql);cmd.Parameters.AddWithValue("@h",hash);await cmd.ExecuteNonQueryAsync();
            return db;
        } catch { await db.DisposeAsync(); throw; }
    }
    private static async Task EnsureServerAsync()
    {
        await ServerGate.WaitAsync();
        try
        {
            if (serverReady) return;
            if (!OperatingSystem.IsWindows())
                throw new InvalidOperationException("Las pruebas SQL requieren Windows y SQL Server Express LocalDB. No usan la base de la aplicación.");
            var sqlRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft SQL Server");
            var executable = Directory.Exists(sqlRoot)
                ? Directory.GetDirectories(sqlRoot).OrderByDescending(Path.GetFileName)
                    .Select(dir => Path.Combine(dir, "Tools", "Binn", "SqlLocalDB.exe")).FirstOrDefault(File.Exists)
                : null;
            executable ??= "SqlLocalDB.exe";
            async Task<string> Run(params string[] args)
            {
                var info = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true };
                foreach (var arg in args) info.ArgumentList.Add(arg);
                Process p;
                try { p = Process.Start(info) ?? throw new InvalidOperationException("No se pudo iniciar SqlLocalDB."); }
                catch (System.ComponentModel.Win32Exception ex)
                { throw new InvalidOperationException("Instala SQL Server Express LocalDB para ejecutar las pruebas SQL en Visual Studio. No cambies appsettings ni la conexión productiva.", ex); }
                using (p)
                {
                    var stdout = p.StandardOutput.ReadToEndAsync();
                    var stderr = p.StandardError.ReadToEndAsync();
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    try { await p.WaitForExitAsync(timeout.Token); }
                    catch (OperationCanceledException)
                    { if (!p.HasExited) p.Kill(true); throw new TimeoutException("SqlLocalDB excedió 30 segundos al preparar la instancia exclusiva de QA."); }
                    await Task.WhenAll(stdout, stderr);
                    if (p.ExitCode != 0) throw new InvalidOperationException("SqlLocalDB no pudo preparar la instancia exclusiva de QA.");
                    return await stdout;
                }
            }
            // Nombre literal dedicado; no inicia ni modifica MSSQLLocalDB ni la base del usuario.
            static bool ContainsInstance(string output) => output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Any(line => line.Trim() == InstanceName);
            if (!ContainsInstance(await Run("info"))) await Run("create", InstanceName);
            // Algunas versiones devuelven exit code 0 incluso al fallar: comprobar existencia y conexión.
            if (!ContainsInstance(await Run("info")))
                throw new InvalidOperationException("No se pudo crear Sistema3S_QA. Revisa la instalación de SQL Server Express LocalDB y los permisos del usuario de Windows.");
            await Run("start", InstanceName);
            try
            {
                await using var probe = new SqlConnection($"Server={Server};Database=master;Integrated Security=true;TrustServerCertificate=true;Connect Timeout=15");
                await probe.OpenAsync();
            }
            catch (SqlException ex)
            { throw new InvalidOperationException("No se pudo conectar a Sistema3S_QA. Revisa SqlLocalDB info Sistema3S_QA; no cambies la conexión productiva.", ex); }
            serverReady = true;
        }
        finally { ServerGate.Release(); }
    }
    private async Task Script(string name)
    {
        var script = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory,"Fixtures",name));
        await using var c = new SqlConnection(Connection); await c.OpenAsync();
        foreach(var batch in Regex.Split(script,@"^GO\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase))
            if(!string.IsNullOrWhiteSpace(batch)) await new SqlCommand(batch,c){CommandTimeout=60}.ExecuteNonQueryAsync();
    }
    public async Task Exec(string text)
    {
        await using var c = new SqlConnection(Connection);await c.OpenAsync();await new SqlCommand(text,c){CommandTimeout=30}.ExecuteNonQueryAsync();
    }
    public async Task<decimal> Scalar(string text)
    {
        await using var c = new SqlConnection(Connection);await c.OpenAsync();return Convert.ToDecimal(await new SqlCommand(text,c).ExecuteScalarAsync());
    }
    public async ValueTask DisposeAsync()
    {
        if(!Regex.IsMatch(Name,@"^Sistema3S_QA_[a-f0-9]{32}$")) throw new InvalidOperationException("Base no autorizada para limpieza QA");
        SqlConnection.ClearAllPools();
        await using var c = new SqlConnection($"Server={Server};Database=master;Integrated Security=true;TrustServerCertificate=true");await c.OpenAsync();
        await new SqlCommand($"ALTER DATABASE [{Name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{Name}]",c).ExecuteNonQueryAsync();
    }
}

public sealed class ApiHost : IAsyncDisposable
{
    private readonly Process process;
    public HttpClient Http { get; }
    private ApiHost(Process process,int port) { this.process=process;Http=new HttpClient{BaseAddress=new Uri($"http://127.0.0.1:{port}"),Timeout=TimeSpan.FromSeconds(40)}; }
    public static async Task<ApiHost> Start(QaDatabase db, IConfiguration? correo = null)
    {
        var socket = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback,0);socket.Start();int port=((System.Net.IPEndPoint)socket.LocalEndpoint).Port;socket.Stop();
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));
        var output = new DirectoryInfo(AppContext.BaseDirectory);
        var configuration = output.Parent!.Name;
        var dll = Path.Combine(root,"Sistema3S.Web","bin",configuration,output.Name,"Sistema3S.Web.dll");
        if (!File.Exists(dll)) throw new FileNotFoundException("Compila la solución antes de ejecutar las pruebas de API.", dll);
        var info=new ProcessStartInfo("dotnet"){WorkingDirectory=Path.Combine(root,"Sistema3S.Web"),UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
        info.ArgumentList.Add(dll);info.Environment["ASPNETCORE_URLS"]=$"http://127.0.0.1:{port}";
        info.Environment["ASPNETCORE_ENVIRONMENT"]="Testing";info.Environment["DOTNET_ENVIRONMENT"]="Testing";
        info.Environment["ConnectionStrings__BD_3S"]=db.Connection;
        foreach(var x in db.Config.AsEnumerable()) if(x.Value!=null) info.Environment[x.Key.Replace(":","__")]=x.Value;
        info.Environment["Smtp__Host"]="127.0.0.1";info.Environment["Smtp__Port"]="1";info.Environment["Smtp__User"]="qa";info.Environment["Smtp__Password"]="qa";info.Environment["Smtp__From"]="qa@example.test";
        if(correo != null) foreach(var x in correo.AsEnumerable()) if(x.Value != null) info.Environment[x.Key.Replace(":","__")]=x.Value;
        var p=Process.Start(info)!;p.BeginOutputReadLine();p.BeginErrorReadLine();var host=new ApiHost(p,port);
        for(int i=0;i<80;i++){try{await host.Http.GetAsync("/api/cliente-web/perfil");return host;}catch(HttpRequestException){if(p.HasExited)throw new Exception("API QA no inició");await Task.Delay(100);}}
        await host.DisposeAsync();throw new TimeoutException("API QA no inició dentro del límite");
    }
    public async ValueTask DisposeAsync(){Http.Dispose();if(!process.HasExited){process.Kill(true);await process.WaitForExitAsync();}process.Dispose();}
}
