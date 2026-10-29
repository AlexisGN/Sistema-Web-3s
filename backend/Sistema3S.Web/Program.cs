using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using System.Text;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using Sistema3S.Web.Services.Publico;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using Sistema3S.Web.Data;
using Sistema3S.Web.Services.Implementations;
using Sistema3S.Web.Services.Interfaces;
using Sistema3S.Web.Services.Pdf;
using Sistema3S.Web.Services.Seguridad;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("ia.local.json", optional: true, reloadOnChange: false).AddEnvironmentVariables();

// El entorno local comparte el origen Angular permitido por CORS. En producción
// se exige configurar el dominio real; nunca se construyen enlaces desde Host/Origin.
ClienteWebConfiguracion.AplicarValoresLocales(builder.Configuration, builder.Environment);

// Licencia QuestPDF
QuestPDF.Settings.License = LicenseType.Community;

// MVC / Controllers
builder.Services.AddScoped<OperacionAuditadaFilter>();
builder.Services.AddScoped<InvalidarCatalogoPublicoFilter>();
builder.Services.AddControllersWithViews(options => {
    options.Filters.Add<OperacionAuditadaFilter>(-3000);
    options.Filters.Add<InvalidarCatalogoPublicoFilter>();
});
builder.Services.AddSingleton<EstadoCatalogoPublico>();
builder.Services.AddOutputCache(options => {
    options.SizeLimit = 32 * 1024 * 1024;
    options.MaximumBodySize = 1024 * 1024;
    options.AddPolicy(EstadoCatalogoPublico.Politica, policy => {
        var seconds = Math.Clamp(builder.Configuration.GetValue("Publico:CacheSegundos",15),0,60);
        if(seconds==0) { policy.NoCache(); return; }
        policy.Expire(TimeSpan.FromSeconds(seconds)).Tag(EstadoCatalogoPublico.Etiqueta)
            .SetVaryByQuery("*").SetVaryByHeader("Origin","Accept-Encoding")
            .VaryByValue(context => new KeyValuePair<string,string>("catalogo",
                context.RequestServices.GetRequiredService<EstadoCatalogoPublico>().Version.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    });
});
builder.Services.AddResponseCompression(options => {
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

// DbContext - SQL Server
builder.Services.AddDbContext<Bd3sContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BD_3S"))
        .AddInterceptors(new ConexionAuditoriaInterceptor())
);

// Configuración JWT
var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException("La clave JWT no está configurada correctamente.");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Sistema3S";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "Sistema3SAdmin";

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = false;
        options.Events = new JwtBearerEvents { OnTokenValidated = ClienteVersionSesion.ValidarAsync };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

// Políticas estándar para los atributos Authorize de los controladores.
builder.Services.AddAuthorization();

builder.Services.AddScoped<ClienteCuentaService>();
builder.Services.AddSingleton<RecuperacionClienteQueue>();
builder.Services.AddHostedService<RecuperacionClienteWorker>();
builder.Services.AddRateLimiter(options => {
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) => {
        if (context.HttpContext.Request.Path.StartsWithSegments("/api/bi") || context.HttpContext.Request.Path.StartsWithSegments("/api/ia"))
        {
            context.HttpContext.Response.Headers.RetryAfter = "2";
            await context.HttpContext.Response.WriteAsJsonAsync(new { mensaje = "Las consultas de análisis están ocupadas. Intenta nuevamente en unos segundos." }, ct);
            return;
        }
        context.HttpContext.Response.Headers.RetryAfter = "900";
        await context.HttpContext.Response.WriteAsJsonAsync(new { mensaje = "Has realizado varios intentos. Espera 15 minutos antes de volver a intentar." }, ct);
    };
    options.AddConcurrencyLimiter("ia", x => { x.PermitLimit=4; x.QueueLimit=4; x.QueueProcessingOrder=QueueProcessingOrder.OldestFirst; });
    options.AddConcurrencyLimiter("bi", options => { options.PermitLimit = Math.Clamp(builder.Configuration.GetValue("Bi:ConsultasConcurrentes",4),1,32); options.QueueLimit = Math.Clamp(builder.Configuration.GetValue("Bi:ConsultasEnEspera",4),0,64); options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst; });
    options.AddPolicy("cuenta-cliente", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "local",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(15), QueueLimit = 0, AutoReplenishment = true }));
});

// Servicios de seguridad / autenticación
builder.Services.AddScoped<PasswordHashService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<Sistema3S.Web.Services.Bi.BiConexion>();
builder.Services.AddScoped<Sistema3S.Web.Services.Comercial.ComercialService>();
builder.Services.AddScoped<Sistema3S.Web.Services.Comercial.ComercialExportacion>();
builder.Services.AddScoped<Sistema3S.Web.Services.Comercial.ComercialPredictivo>();
builder.Services.AddScoped<Sistema3S.Web.Services.Bi.IBiService, Sistema3S.Web.Services.Bi.BiService>();
builder.Services.AddScoped<Sistema3S.Web.Services.Bi.BiExportacion>();

builder.Services.AddScoped<Sistema3S.Web.Services.Ia.IaDatos>();
builder.Services.AddScoped<Sistema3S.Web.Services.Ia.IaMotor>();
builder.Services.AddScoped<Sistema3S.Web.Services.Ia.IaProceso>();
builder.Services.AddScoped<Sistema3S.Web.Services.Ia.IaService>();
builder.Services.AddScoped<Sistema3S.Web.Services.Ia.IaCompras>();
builder.Services.AddScoped<Sistema3S.Web.Services.Ia.IaDatosAdministrados>();
builder.Services.AddScoped<Sistema3S.Web.Services.Ia.IaMotorAdministrado>();
builder.Services.AddScoped<Sistema3S.Web.Services.Ia.IaAdministrador>();
builder.Services.AddHostedService<Sistema3S.Web.Services.Ia.IaInferenciaWorker>();
builder.Services.AddHostedService<Sistema3S.Web.Services.Ia.IaEntrenamientoWorker>();

// Servicios de negocio
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IServicioService, ServicioService>();
builder.Services.AddScoped<ICotizacionService, CotizacionService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IPublicoService, PublicoService>();

builder.Services.AddHttpClient<IConsultaDocumentoService, ConsultaDocumentoService>();
builder.Services.AddHttpClient<IProveedorService, ProveedorService>();

builder.Services.AddScoped<IInventarioService, InventarioService>();

builder.Services.AddScoped<ICompraService, CompraService>();
builder.Services.AddScoped<IPdfCompraService, PdfCompraService>();
builder.Services.AddScoped<IExcelCompraService, ExcelCompraService>();

builder.Services.AddScoped<IVentaService, VentaService>();
builder.Services.AddScoped<IPdfVentaService, PdfVentaService>();
builder.Services.AddScoped<IExcelVentaService, ExcelVentaService>();

builder.Services.AddScoped<ICajaService, CajaService>();

builder.Services.AddScoped<PasswordHashService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRolService, RolService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IClienteWebService, ClienteWebService>();
builder.Services.AddScoped<ICatalogoArchivoService, CatalogoArchivoService>();

// Servicios para PDF / correo / WhatsApp
builder.Services.AddHttpClient();
builder.Services.AddScoped<IPdfCotizacionService, PdfCotizacionService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();

// El origen local se conserva; el servidor puede declarar sus dominios de frontend.
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];
corsOrigins = corsOrigins.Select(origin =>
{
    if (!Uri.TryCreate(origin.Trim(), UriKind.Absolute, out var uri)
        || (uri.Scheme != "https" && uri.Scheme != "http")
        || uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0
        || uri.AbsolutePath != "/")
        throw new InvalidOperationException("Cors:AllowedOrigins requiere orígenes HTTP/HTTPS sin rutas ni credenciales.");
    return uri.GetLeftPart(UriPartial.Authority);
}).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularPolicy", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Sólo información pública: nunca comprimir aquí tokens, perfiles ni operaciones privadas.
app.UseWhen(context => context.Request.Path.StartsWithSegments("/api/publico")
    && (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)),
    branch => branch.UseResponseCompression());

// Necesario para abrir PDFs, imágenes y archivos desde wwwroot
app.UseStaticFiles();

app.UseRouting();

app.UseCors("AngularPolicy");

// Primero autenticación, luego autorización
app.UseAuthentication();
app.UseRateLimiter();

app.UseAuthorization();
app.UseOutputCache();

// Controladores API
app.MapControllers();

// Ruta MVC por defecto
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();
