using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using Sistema3S.Tests.Fixtures;

namespace Sistema3S.Tests.Integration;
[Trait("Category", "Integration")]
public sealed class AutorizacionTests
{
    private static readonly string[] Rutas = ["/api/caja/resumen?idUsuario=1", "/api/compra", "/api/venta", "/api/cotizacion", "/api/producto", "/api/servicio", "/api/cliente", "/api/proveedor", "/api/inventario", "/api/usuario", "/api/rol", "/api/catalogo/categorias", "/api/auditoria", "/api/prueba/conexion"];
    private static readonly List<object> Evidencia = [];
    private static readonly string Archivo = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../docs/testing/evidencias/authorize-matriz.json"));
    private static async Task Comprobar(ApiHost api, string escenario, HttpStatusCode esperado)
    {
        foreach(var ruta in Rutas) {
            using var r=await api.Http.GetAsync(ruta);
            Evidencia.Add(new { escenario,ruta,esperado=(int)esperado,obtenido=(int)r.StatusCode,pass=r.StatusCode==esperado });
            await Guardar();
            Assert.True(r.StatusCode==esperado,$"{escenario}: {ruta}; esperado {(int)esperado}, obtenido {(int)r.StatusCode}");
        }
    }
    private static Task Guardar()=>File.WriteAllTextAsync(Archivo,JsonSerializer.Serialize(new {fechaUtc=DateTimeOffset.UtcNow,comprobaciones=Evidencia},new JsonSerializerOptions{WriteIndented=true}));
    [Fact(DisplayName="PS-AUTH-004 | Los catorce módulos internos rechazan solicitudes sin sesión")]
    public async Task SinSesion(){await using var q=await QaDatabase.CreateAsync();await using var api=await ApiHost.Start(q);await Comprobar(api,"Sin token",HttpStatusCode.Unauthorized);}
    [Fact(DisplayName="PS-AUTH-005 | JWT malformado, firma incorrecta y sesión expirada se rechazan")]
    public async Task JwtInvalido(){await using var q=await QaDatabase.CreateAsync();await using var api=await ApiHost.Start(q);
        foreach(var (nombre,token) in new[]{("Malformado","token-no-valido"),("Firma incorrecta",Token("Otra-clave-ficticia-QA-no-autorizada-1234567890",false)),("Expirado",Token(q.Config["Jwt:Key"]!,true))}) {
            api.Http.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",token);await Comprobar(api,nombre,HttpStatusCode.Unauthorized);
        }
    }
    private static string Token(string key,bool expirado){var inicio=DateTime.UtcNow.AddHours(-2);var fin=expirado?DateTime.UtcNow.AddHours(-1):DateTime.UtcNow.AddMinutes(10);return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("Sistema3S","Sistema3SAdmin",[new Claim("idUsuario","1"),new Claim("rol","Administrador")],inicio,fin,new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),SecurityAlgorithms.HmacSha256)));}
    [Fact(DisplayName="PS-AUTH-006 | Una sesión cliente válida no autoriza acceso a módulos internos")]
    public async Task Cliente(){await using var q=await QaDatabase.CreateAsync();await using var api=await ApiHost.Start(q);
        await ApiTests.Json(await api.Http.PostAsJsonAsync("/api/cliente-web/registro",new{tipoDocumento="DNI",numeroDocumento="00000002",correo="nuevo@example.test",telefono="+51999999998",direccion="QA",contrasena=QaDatabase.Password,confirmarContrasena=QaDatabase.Password,nombres="Cliente",apellidoPaterno="Nuevo",apellidoMaterno="QA"}));
        var login=await ApiTests.Json(await api.Http.PostAsJsonAsync("/api/cliente-web/login",new{correo="nuevo@example.test",contrasena=QaDatabase.Password}));api.Http.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",login.GetProperty("token").GetString());await Comprobar(api,"Cliente autenticado",HttpStatusCode.Forbidden);
    }
    [Fact(DisplayName="PS-AUTH-007 | Administrador autenticado conserva los catorce módulos internos")]
    public async Task Administrador(){await using var q=await QaDatabase.CreateAsync();await using var api=await ApiHost.Start(q);await ApiTests.Login(api);await ApiTests.Json(await api.Http.PostAsJsonAsync("/api/caja/abrir",new{idUsuarioApertura=1,saldoInicial=2000,observacion="Apertura sintética QA"}));await Comprobar(api,"Administrador activo",HttpStatusCode.OK);}
    [Fact(DisplayName="PS-AUTH-008 | Desactivar usuario o rol invalida el acceso aun con JWT firmado")]
    public async Task Inactivo(){await using var q=await QaDatabase.CreateAsync();await using var api=await ApiHost.Start(q);await ApiTests.Login(api);await q.Exec("UPDATE Usuario SET Estado=0 WHERE IdUsuario=1");await Comprobar(api,"Usuario desactivado",HttpStatusCode.Unauthorized);await q.Exec("UPDATE Usuario SET Estado=1 WHERE IdUsuario=1; UPDATE Rol SET Estado=0 WHERE IdRol=1");await Comprobar(api,"Rol desactivado",HttpStatusCode.Unauthorized);}
    [Fact(DisplayName="PS-AUTH-009 | Catálogo y entradas de cuenta pública conservan acceso anónimo")]
    public async Task Publico(){await using var q=await QaDatabase.CreateAsync();await using var api=await ApiHost.Start(q);
        foreach(var ruta in new[]{"/api/publico/productos","/api/publico/servicios","/api/publico/categorias","/api/publico/marcas"}) Assert.Equal(HttpStatusCode.OK,(await api.Http.GetAsync(ruta)).StatusCode);
        foreach(var ruta in new[]{"/api/auth/login","/api/auth/cambiar-contrasena-inicial","/api/cliente-web/login","/api/cliente-web/registro","/api/cliente-web/recuperar-contrasena","/api/cliente-web/restablecer-contrasena"}) {
            using var r=await api.Http.PostAsJsonAsync(ruta,new{});Assert.Equal(HttpStatusCode.BadRequest,r.StatusCode);
        }
        var invalido=await ApiTests.Json(await api.Http.GetAsync("/api/cliente-web/consultar-documento?tipoDocumento=DNI&numeroDocumento=1"));Assert.False(invalido.GetProperty("exitoso").GetBoolean());
    }
    [Fact(DisplayName="PS-AUTH-010 | Preflight CORS de Angular conserva las rutas protegidas")]
    public async Task Cors(){await using var q=await QaDatabase.CreateAsync();await using var api=await ApiHost.Start(q);
        foreach(var ruta in Rutas) {using var req=new HttpRequestMessage(HttpMethod.Options,ruta);req.Headers.Add("Origin","http://localhost:4200");req.Headers.Add("Access-Control-Request-Method","GET");req.Headers.Add("Access-Control-Request-Headers","authorization");using var r=await api.Http.SendAsync(req);Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);Assert.Equal("http://localhost:4200",r.Headers.GetValues("Access-Control-Allow-Origin").Single());}
    }
}
