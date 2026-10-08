using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Sistema3S.Tests.Fixtures;
using Sistema3S.Web.Services.Seguridad;

namespace Sistema3S.Tests.Integration;
[Trait("Category", "Integration")]
public sealed class PermisosTests
{
    private static readonly List<object> Evidencia = [];
    private static async Task Evidence(string scenario, string route, int expected, HttpResponseMessage r)
    {
        Evidencia.Add(new { scenario, route, expected, actual=(int)r.StatusCode, pass=expected==(int)r.StatusCode });
        var file=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../docs/testing/evidencias/permisos-v5.json"));
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        await File.WriteAllTextAsync(file,JsonSerializer.Serialize(new { fechaUtc=DateTimeOffset.UtcNow,comprobaciones=Evidencia },new JsonSerializerOptions{WriteIndented=true}));
        Assert.True(expected==(int)r.StatusCode,$"{scenario} {route}: esperado {expected}, obtenido {(int)r.StatusCode}; {await r.Content.ReadAsStringAsync()}");
    }
    private static async Task Grants(QaDatabase q, params string[] names)
    {
        // Nombres literales de la matriz de prueba; nunca valores del usuario ni SQL de la base principal.
        Assert.All(names,n=>Assert.Matches("^[A-Z_]+$",n));
        foreach(var n in names) await q.Exec($"IF NOT EXISTS(SELECT 1 FROM Permiso WHERE Nombre=N'{n}') INSERT Permiso(Nombre,Descripcion) VALUES(N'{n}',N'Permiso sintetico QA')");
        await q.Exec("DELETE RolPermiso WHERE IdRol=2");
        foreach(var n in names) await q.Exec($"INSERT RolPermiso(IdRol,IdPermiso) SELECT 2,IdPermiso FROM Permiso WHERE Nombre=N'{n}' AND Estado=1");
    }
    private static MultipartFormDataContent Product(string name="Producto permisos QA",bool image=true)
    {
        var f=new MultipartFormDataContent();
        foreach(var (key,value) in new[]{("Nombre",name),("Descripcion","Evidencia aislada"),("PrecioReferencial","100"),("IdCategoria","1"),("IdUnidadMedida","1"),("CodigoProducto","PERM-"+Guid.NewGuid().ToString("N")[..8])})f.Add(new StringContent(value),key);
        f.Add(new StringContent("true"),"Estado");
        if(image){var b=new ByteArrayContent(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScLbtAAAAABJRU5ErkJggg=="));b.Headers.ContentType=new("image/png");f.Add(b,"ImagenArchivo","qa.png");}
        return f;
    }
    [Theory(DisplayName="PS-PERM-001 | Productos respeta las cinco combinaciones del rol Compras")]
    [InlineData(false,false,false,false)]
    [InlineData(true,false,false,false)]
    [InlineData(true,true,false,false)]
    [InlineData(true,false,true,false)]
    [InlineData(true,true,true,true)]
    public async Task Productos(bool view,bool create,bool edit,bool delete)
    {
        await using var q=await QaDatabase.CreateAsync();await using var api=await ApiHost.Start(q);
        await q.Exec("UPDATE ElementoCatalogo SET ImagenUrl=N'/uploads/catalogo/qa-existente.png' WHERE IdElementoCatalogo=1");
        await Grants(q,"PRODUCTOS_VER");await ApiTests.Login(api,"compras@example.test");
        await Grants(q,new[]{(view,"PRODUCTOS_VER"),(create,"PRODUCTOS_CREAR"),(edit,"PRODUCTOS_EDITAR"),(delete,"PRODUCTOS_ELIMINAR")}.Where(x=>x.Item1).Select(x=>x.Item2).ToArray());
        var scenario=$"Productos VER={view} CREAR={create} EDITAR={edit} ELIMINAR={delete}";
        var profile=await ApiTests.Json(await api.Http.GetAsync("/api/auth/perfil"));
        Assert.Equal(view,profile.GetProperty("permisos").EnumerateArray().Any(p=>p.GetString()=="PRODUCTOS_VER"));
        await Evidence(scenario,"GET /api/producto",view?200:403,await api.Http.GetAsync("/api/producto"));
        await Evidence(scenario,"GET /api/producto/1",view?200:403,await api.Http.GetAsync("/api/producto/1"));
        var count=await q.Scalar("SELECT COUNT(*) FROM Producto");
        using(var f=Product()) await Evidence(scenario,"POST /api/producto",create?201:403,await api.Http.PostAsync("/api/producto",f));
        Assert.Equal(count+(create?1:0),await q.Scalar("SELECT COUNT(*) FROM Producto"));
        using(var f=Product("Actualizado por permisos QA",false)) await Evidence(scenario,"PUT /api/producto/1",edit?200:403,await api.Http.PutAsync("/api/producto/1",f));
        Assert.Equal(edit?1:0,await q.Scalar("SELECT COUNT(*) FROM ElementoCatalogo WHERE IdElementoCatalogo=1 AND Nombre=N'Actualizado por permisos QA'"));
        await Evidence(scenario,"DELETE /api/producto/1",delete?200:403,await api.Http.DeleteAsync("/api/producto/1"));
        Assert.Equal(delete?0:1,await q.Scalar("SELECT CAST(Estado AS int) FROM ElementoCatalogo WHERE IdElementoCatalogo=1"));
        if(edit||create) Assert.True(await q.Scalar("SELECT COUNT(*) FROM AuditoriaLog WHERE IdUsuario=2 AND Accion='SOLICITUD' AND Descripcion LIKE 'Producto.%'")>0);
        if(!edit||!delete) Assert.True(await q.Scalar("SELECT COUNT(*) FROM AuditoriaLog WHERE IdUsuario=2 AND Accion='DENEGADA'")>0);
    }
    [Fact(DisplayName="PS-PERM-002 | Proveedores bloquea y habilita crear, editar y desactivar por separado")]
    public async Task Proveedores()
    {
        await using var q=await QaDatabase.CreateAsync();await using var api=await ApiHost.Start(q);await Grants(q,"PROVEEDORES_VER");await ApiTests.Login(api,"compras@example.test");
        await q.Exec("INSERT Ubigeo(Departamento,Provincia,Distrito,CodigoUbigeo) VALUES(N'Lima QA',N'Lima QA',N'Distrito QA',N'999999')");
        var dto=new{ruc="20000000002",razonSocial="Proveedor permisos QA",correo="proveedor@example.test",telefono="+51999999999",direccion="QA",idUbigeo=1};
        await Evidence("Proveedor solo ver","GET /api/proveedor",200,await api.Http.GetAsync("/api/proveedor"));
        await Evidence("Proveedor solo ver","POST /api/proveedor",403,await api.Http.PostAsJsonAsync("/api/proveedor",dto));
        await Evidence("Proveedor solo ver","PUT /api/proveedor/1",403,await api.Http.PutAsJsonAsync("/api/proveedor/1",dto));
        await Evidence("Proveedor solo ver","DELETE /api/proveedor/1",403,await api.Http.DeleteAsync("/api/proveedor/1"));
        Assert.Equal(1,await q.Scalar("SELECT COUNT(*) FROM Proveedor"));
        await Grants(q,"PROVEEDORES_CREAR");var created=await api.Http.PostAsJsonAsync("/api/proveedor",dto);Assert.True(created.IsSuccessStatusCode,await created.Content.ReadAsStringAsync());
        await Grants(q,"PROVEEDORES_EDITAR");await Evidence("Proveedor editar","PUT /api/proveedor/1",200,await api.Http.PutAsJsonAsync("/api/proveedor/1",new{ruc="20000000001",razonSocial="Proveedor editado QA",estado=true,idUbigeo=1,correo="proveedor@example.test",telefono="+51999999999",direccion="QA"}));
        await Grants(q,"PROVEEDORES_DESACTIVAR");await Evidence("Proveedor desactivar","DELETE /api/proveedor/1",200,await api.Http.DeleteAsync("/api/proveedor/1"));
        Assert.Equal(0,await q.Scalar("SELECT CAST(Estado AS int) FROM Proveedor WHERE IdProveedor=1"));
    }
    [Fact(DisplayName="PS-PERM-003 | Inventario respeta VER, AJUSTAR y REPORTE sin privilegios por nombre del rol")]
    public async Task Inventario()
    {
        await using var q=await QaDatabase.CreateAsync();await using var api=await ApiHost.Start(q);await Grants(q,"INVENTARIO_VER");await ApiTests.Login(api,"compras@example.test");
        await Evidence("Inventario solo ver","GET /api/inventario",200,await api.Http.GetAsync("/api/inventario"));
        await Evidence("Inventario solo ver","PUT /api/inventario/1/stock-minimo",403,await api.Http.PutAsJsonAsync("/api/inventario/1/stock-minimo",new{stockMinimo=7}));
        await Evidence("Inventario solo ver","GET /api/inventario/1/movimientos",403,await api.Http.GetAsync("/api/inventario/1/movimientos"));
        Assert.Equal(5,await q.Scalar("SELECT StockMinimo FROM Inventario WHERE IdProducto=1"));
        await Grants(q,"INVENTARIO_AJUSTAR");await Evidence("Inventario ajustar","PUT /api/inventario/1/stock-minimo",200,await api.Http.PutAsJsonAsync("/api/inventario/1/stock-minimo",new{stockMinimo=7}));
        Assert.Equal(7,await q.Scalar("SELECT StockMinimo FROM Inventario WHERE IdProducto=1"));
        await Grants(q,"INVENTARIO_REPORTE");await Evidence("Inventario reporte","GET /api/inventario/1/movimientos",200,await api.Http.GetAsync("/api/inventario/1/movimientos"));
        Assert.True(await q.Scalar("SELECT COUNT(*) FROM AuditoriaLog WHERE IdUsuario=2 AND TablaAfectada='Inventario' AND Accion='ACTUALIZAR'")>0);
    }
    [Fact(DisplayName="PS-PERM-004 | Cambiar permisos por API revoca JWT anterior y conserva auditoría del administrador")]
    public async Task Revocacion()
    {
        await using var q=await QaDatabase.CreateAsync();await using var user=await ApiHost.Start(q);await using var admin=await ApiHost.Start(q);
        await Grants(q,"PRODUCTOS_VER","PRODUCTOS_EDITAR");await ApiTests.Login(user,"compras@example.test");await ApiTests.Login(admin);
        var id=(int)await q.Scalar("SELECT IdPermiso FROM Permiso WHERE Nombre='PRODUCTOS_VER'");
        await Evidence("Administrador cambia permisos","PUT /api/rol/2/permisos",200,await admin.Http.PutAsJsonAsync("/api/rol/2/permisos",new{idsPermisos=new[]{id}}));
        var perfil=await ApiTests.Json(await user.Http.GetAsync("/api/auth/perfil"));Assert.DoesNotContain(perfil.GetProperty("permisos").EnumerateArray(),p=>p.GetString()=="PRODUCTOS_EDITAR");
        using var f=Product("Revocado",false);await Evidence("JWT anterior revocado","PUT /api/producto/1",403,await user.Http.PutAsync("/api/producto/1",f));
        Assert.True(await q.Scalar("SELECT COUNT(*) FROM AuditoriaLog WHERE IdUsuario=1 AND TablaAfectada='RolPermiso'")>0);
        Assert.True(await q.Scalar("SELECT COUNT(*) FROM AuditoriaLog WHERE IdUsuario=2 AND Accion='DENEGADA'")>0);
        Assert.Equal(0,await q.Scalar("SELECT COUNT(*) FROM ElementoCatalogo WHERE Nombre='Revocado'"));
        await q.Exec("UPDATE Permiso SET Estado=0 WHERE Nombre='PRODUCTOS_VER'");await Evidence("Permiso inactivo","GET /api/producto",403,await user.Http.GetAsync("/api/producto"));
    }
    [Fact(DisplayName="PS-PERM-005 | Cada endpoint interno sensible deniega sin permiso antes de modificar datos")]
    public async Task TodosLosEndpoints()
    {
        await using var q=await QaDatabase.CreateAsync();await using var api=await ApiHost.Start(q);await Grants(q,"PRODUCTOS_VER");await ApiTests.Login(api,"compras@example.test");await Grants(q);
        var before=await q.Scalar("SELECT (SELECT COUNT(*) FROM Producto)+(SELECT COUNT(*) FROM Cliente)+(SELECT COUNT(*) FROM Usuario)+(SELECT COUNT(*) FROM Rol)+(SELECT COUNT(*) FROM Proveedor)+(SELECT COUNT(*) FROM Compra)+(SELECT COUNT(*) FROM Venta)+(SELECT COUNT(*) FROM Cotizacion)+(SELECT COUNT(*) FROM Caja)");
        var map=(IReadOnlyDictionary<string,string[]>)typeof(OperacionAuditadaFilter).GetField("PermisosPorOperacion",BindingFlags.NonPublic|BindingFlags.Static)!.GetValue(null)!;
        var asm=typeof(OperacionAuditadaFilter).Assembly;
        foreach(var c in asm.GetTypes().Where(t=>t.Name.EndsWith("Controller")))
        foreach(var m in c.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly))
        {
            var key=c.Name.Replace("Controller","")+"."+m.Name;
            if(!map.TryGetValue(key,out var perms)||perms.Length==0)continue;
            var http=m.GetCustomAttributes().OfType<Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute>().First();
            var template=http.Template ?? "";
            var route="/api/"+c.Name.Replace("Controller","").ToLowerInvariant()+"/"+System.Text.RegularExpressions.Regex.Replace(template,@"\{[^}]+\}","999999");
            using var req=new HttpRequestMessage(new HttpMethod(http.HttpMethods.First()),route);
            if(req.Method!=HttpMethod.Get && req.Method!=HttpMethod.Delete)
            {
                if(c.Name is "ProductoController" or "ServicioController")req.Content=new MultipartFormDataContent { { new StringContent("No autorizado"), "Nombre" } };
                else req.Content=JsonContent.Create(new{});
            }
            await Evidence("Sin permisos: "+key,req.Method+" "+route,403,await api.Http.SendAsync(req));
        }
        var after=await q.Scalar("SELECT (SELECT COUNT(*) FROM Producto)+(SELECT COUNT(*) FROM Cliente)+(SELECT COUNT(*) FROM Usuario)+(SELECT COUNT(*) FROM Rol)+(SELECT COUNT(*) FROM Proveedor)+(SELECT COUNT(*) FROM Compra)+(SELECT COUNT(*) FROM Venta)+(SELECT COUNT(*) FROM Cotizacion)+(SELECT COUNT(*) FROM Caja)");Assert.Equal(before,after);
    }
    [Fact(DisplayName="PS-PERM-006 | Editar cotización no concede aprobar, convertir ni cancelar mediante endpoint genérico")]
    public async Task EstadosCotizacion()
    {
        await using var q=await QaDatabase.CreateAsync();await using var api=await ApiHost.Start(q);await Grants(q,"COTIZACIONES_EDITAR");await ApiTests.Login(api,"compras@example.test");
        foreach(var estado in new[]{"Aprobada","Convertida en venta","Cancelada"})await Evidence("Estado sin permiso "+estado,"PUT /api/cotizacion/999999/estado",403,await api.Http.PutAsJsonAsync("/api/cotizacion/999999/estado",new{nuevoEstado=estado}));
    }
    [Fact(DisplayName="PS-PERM-007 | Permiso de roles no permite crear un alias con acceso total de administrador")]
    public async Task AdministradorReservado()
    {
        await using var q=await QaDatabase.CreateAsync();await using var api=await ApiHost.Start(q);await Grants(q,"ROLES_CREAR","ROLES_EDITAR");await ApiTests.Login(api,"compras@example.test");
        await Evidence("Admin reservado","POST /api/rol",400,await api.Http.PostAsJsonAsync("/api/rol",new{nombre="Admin"}));
        await Evidence("Admin reservado","PUT /api/rol/2",400,await api.Http.PutAsJsonAsync("/api/rol/2",new{nombre="Admin",estado=true}));
        Assert.Equal(0,await q.Scalar("SELECT COUNT(*) FROM Rol WHERE Nombre='Admin'"));
    }
    [Fact(DisplayName="PS-PERM-008 | Los permisos VER de cada módulo habilitan sólo sus consultas, sin depender del rol")]
    public async Task ConsultasPorModulo()
    {
        await using var q=await QaDatabase.CreateAsync();await using var api=await ApiHost.Start(q);await Grants(q,"PRODUCTOS_VER");await ApiTests.Login(api,"compras@example.test");
        foreach(var (permiso,ruta) in new[]{("PRODUCTOS_VER","/api/producto"),("SERVICIOS_VER","/api/servicio"),("CLIENTES_VER","/api/cliente"),("PROVEEDORES_VER","/api/proveedor"),("INVENTARIO_VER","/api/inventario"),("COMPRAS_VER","/api/compra"),("VENTAS_VER","/api/venta"),("COTIZACIONES_VER","/api/cotizacion"),("USUARIOS_VER","/api/usuario"),("ROLES_VER","/api/rol"),("CAJA_VER","/api/caja/activa?idUsuario=2")})
        {
            await Grants(q,permiso);await Evidence("Sólo "+permiso,"GET "+ruta,permiso=="CAJA_VER"?204:200,await api.Http.GetAsync(ruta));
            var otra=ruta.StartsWith("/api/producto")?"/api/servicio":"/api/producto";
            await Evidence("Sólo "+permiso+": otro módulo","GET "+otra,403,await api.Http.GetAsync(otra));
        }
    }
    [Fact(DisplayName="PS-PERM-009 | Caja respeta abrir, movimiento, reporte y cerrar por permisos conservando sus saldos")]
    public async Task CajaPorPermisos()
    {
        await using var q=await QaDatabase.CreateAsync();await using var api=await ApiHost.Start(q);await Grants(q,"CAJA_VER");await ApiTests.Login(api,"compras@example.test");
        var apertura=new{saldoInicial=100,idUsuarioApertura=1};
        await Evidence("Caja sólo ver","POST /api/caja/abrir",403,await api.Http.PostAsJsonAsync("/api/caja/abrir",apertura));Assert.Equal(0,await q.Scalar("SELECT COUNT(*) FROM Caja"));
        await Grants(q,"CAJA_VER","CAJA_ABRIR");var opened=await api.Http.PostAsJsonAsync("/api/caja/abrir",apertura);Assert.True(opened.IsSuccessStatusCode,await opened.Content.ReadAsStringAsync());
        Assert.Equal(2,await q.Scalar("SELECT IdUsuarioApertura FROM Caja"));
        var movimiento=new{tipoMovimiento="Ingreso manual",metodoPago="Efectivo",monto=25,descripcion="Caja permisos QA",idUsuarioRegistro=1};
        await Evidence("Caja sin movimiento","POST /api/caja/movimiento-manual",403,await api.Http.PostAsJsonAsync("/api/caja/movimiento-manual",movimiento));
        await Grants(q,"CAJA_VER","CAJA_MOVIMIENTO_MANUAL");await Evidence("Caja con movimiento","POST /api/caja/movimiento-manual",200,await api.Http.PostAsJsonAsync("/api/caja/movimiento-manual",movimiento));
        var resumen=await ApiTests.Json(await api.Http.GetAsync("/api/caja/resumen?idUsuario=1"));Assert.Equal(125,resumen.GetProperty("saldoSistema").GetDecimal());
        await Evidence("Caja sin reporte","GET /api/caja/reporte",403,await api.Http.GetAsync("/api/caja/reporte"));
        await Grants(q,"CAJA_REPORTE");await Evidence("Caja con reporte","GET /api/caja/reporte",200,await api.Http.GetAsync("/api/caja/reporte"));
        var id=(int)await q.Scalar("SELECT IdCaja FROM Caja");var cierre=new{idCaja=id,saldoContado=125,idUsuarioCierre=1};
        await Evidence("Caja sin cerrar","POST /api/caja/cerrar",403,await api.Http.PostAsJsonAsync("/api/caja/cerrar",cierre));
        await Grants(q,"CAJA_CERRAR");await Evidence("Caja con cerrar","POST /api/caja/cerrar",200,await api.Http.PostAsJsonAsync("/api/caja/cerrar",cierre));
        Assert.Equal(2,await q.Scalar("SELECT IdUsuarioCierre FROM Caja"));
        Assert.True(await q.Scalar("SELECT COUNT(*) FROM AuditoriaLog WHERE IdUsuario=2 AND TablaAfectada IN ('Caja','MovimientoCaja')")>0);
    }
    [Fact(DisplayName="PS-PERM-010 | Editar personal autorizado conserva responsable y cambios en auditoría")]
    public async Task PersonalAuditado()
    {
        await using var q=await QaDatabase.CreateAsync();await using var api=await ApiHost.Start(q);await Grants(q,"USUARIOS_VER");await ApiTests.Login(api,"compras@example.test");
        var dto=new{idRol=2,correo="compras-editado@example.test",estado=true};
        await Evidence("Personal sólo ver","PUT /api/usuario/2",403,await api.Http.PutAsJsonAsync("/api/usuario/2",dto));
        Assert.Equal(0,await q.Scalar("SELECT COUNT(*) FROM Usuario WHERE Correo='compras-editado@example.test'"));
        await Grants(q,"USUARIOS_EDITAR");await Evidence("Personal editar","PUT /api/usuario/2",200,await api.Http.PutAsJsonAsync("/api/usuario/2",dto));
        Assert.Equal(1,await q.Scalar("SELECT COUNT(*) FROM Usuario WHERE IdUsuario=2 AND Correo='compras-editado@example.test'"));
        Assert.True(await q.Scalar("SELECT COUNT(*) FROM AuditoriaLog WHERE IdUsuario=2 AND TablaAfectada='Usuario' AND Accion='ACTUALIZAR' AND IdRegistro=2 AND DatosAntes LIKE '%compras@example.test%' AND DatosDespues LIKE '%compras-editado@example.test%'")>0);
    }
}
