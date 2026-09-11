using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using Sistema3S.Web.Data;
using Sistema3S.Web.Services.Implementations;
using Sistema3S.Web.Services.Interfaces;
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
builder.Services.AddControllersWithViews(options => options.Filters.Add<OperacionAuditadaFilter>(-3000));

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



// Servicios de negocio
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IServicioService, ServicioService>();
builder.Services.AddScoped<IPublicoService, PublicoService>();

builder.Services.AddHttpClient<IConsultaDocumentoService, ConsultaDocumentoService>();





builder.Services.AddScoped<PasswordHashService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IClienteWebService, ClienteWebService>();
builder.Services.AddScoped<ICatalogoArchivoService, CatalogoArchivoService>();

// Servicios para PDF / correo / WhatsApp
builder.Services.AddHttpClient();

// CORS para Angular
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
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

// Necesario para abrir PDFs, imágenes y archivos desde wwwroot
app.UseStaticFiles();

app.UseRouting();

app.UseCors("AngularPolicy");

// Primero autenticación, luego autorización
app.UseAuthentication();
app.UseRateLimiter();

app.UseAuthorization();

// Controladores API
app.MapControllers();

// Ruta MVC por defecto
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();
