using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Sistema3S.Web.DTOs.Permiso;
using Sistema3S.Web.Services.Seguridad;
using Sistema3S.Tests.Fixtures;

namespace Sistema3S.Tests.Integration;

public class SeleccionPermisosTests
{
    internal static List<PermisoDto> Catalogo() => JsonSerializer.Deserialize<List<PermisoDto>>(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures/permisos-catalogo.json")),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    public static IEnumerable<object[]> Acciones() => Catalogo().Where(p => p.Estado && ReglasSeleccionPermisos.BaseRequerida(p.Nombre) != null)
        .Select(p => new object[] { p.Nombre });

    [Theory(DisplayName="PU-SEL-001 | Cada acción real exige VER y funciona al incluirlo")]
    [MemberData(nameof(Acciones))]
    [Trait("Category","Unit")]
    public void DependenciaPorCadaAccionReal(string nombre)
    {
        var catalogo = Catalogo();
        var accion = catalogo.Single(p => p.Nombre == nombre);
        var requerido = ReglasSeleccionPermisos.BaseRequerida(nombre)!;
        var ver = catalogo.Single(p => p.Nombre == requerido);
        var otro = catalogo.First(p => ReglasSeleccionPermisos.EsVisualizacionFuncional(p.Nombre) && p.Nombre != requerido);
        Assert.Contains(requerido, Assert.Throws<InvalidOperationException>(() => ReglasSeleccionPermisos.Validar(catalogo, [accion.IdPermiso, otro.IdPermiso])).Message);
        ReglasSeleccionPermisos.Validar(catalogo, [accion.IdPermiso, ver.IdPermiso]);
    }

    [Fact(DisplayName="PU-SEL-002 | INICIO, vacío y permiso heredado no reemplazan un módulo operativo")]
    [Trait("Category","Unit")]
    public void MinimoModulo()
    {
        var c=Catalogo();
        foreach(var ids in new[]{Array.Empty<int>(),new[]{c.Single(p=>p.Nombre=="INICIO_VER").IdPermiso},new[]{c.Single(p=>p.Nombre=="Ver reportes").IdPermiso}})
            Assert.Equal(ReglasSeleccionPermisos.MinimoModuloVisible,Assert.Throws<InvalidOperationException>(()=>ReglasSeleccionPermisos.Validar(c,ids)).Message);
        foreach(var ver in c.Where(p=>p.Estado && ReglasSeleccionPermisos.EsVisualizacionFuncional(p.Nombre)))
            ReglasSeleccionPermisos.Validar(c,[ver.IdPermiso]);
    }

    [Fact(DisplayName="PU-SEL-003 | Convención futura y normalización no requieren una lista manual de módulos")]
    [Trait("Category","Unit")]
    public void ConvencionFutura()
    {
        PermisoDto[] c=[new(){IdPermiso=1,Nombre=" nuevo_ver ",Estado=true},new(){IdPermiso=2,Nombre="NUEVO_EXPORTAR_PDF",Estado=true}];
        Assert.Throws<InvalidOperationException>(()=>ReglasSeleccionPermisos.Validar(c,[2]));
        ReglasSeleccionPermisos.Validar(c,[1,2,2]);
    }

    [Fact(DisplayName="PU-SEL-004 | IDs inexistentes, negativos e inactivos nunca se guardan parcialmente")]
    [Trait("Category","Unit")]
    public void IdsInvalidos()
    {
        var c=Catalogo();var ver=c.Single(p=>p.Nombre=="PRODUCTOS_VER");
        foreach(var id in new[]{-1,0,int.MaxValue})Assert.Throws<InvalidOperationException>(()=>ReglasSeleccionPermisos.Validar(c,[ver.IdPermiso,id]));
        c.Single(p=>p.Nombre=="PRODUCTOS_EDITAR").Estado=false;
        Assert.Throws<InvalidOperationException>(()=>ReglasSeleccionPermisos.Validar(c,[ver.IdPermiso,c.Single(p=>p.Nombre=="PRODUCTOS_EDITAR").IdPermiso]));
    }

    [Fact(DisplayName="PI-SEL-001 | API/SQL rechaza configuraciones inválidas sin cambios, conserva accesos y auditoría")]
    [Trait("Category","Integration")]
    public async Task ApiSql()
    {
        await using var q=await QaDatabase.CreateAsync();
        await using (var connection=new SqlConnection(q.Connection))
        {
            await connection.OpenAsync();
            foreach(var p in Catalogo())
            {
                await using var command=new SqlCommand("INSERT Permiso(Nombre,Estado) VALUES(@n,@e)",connection);
                command.Parameters.AddWithValue("@n",p.Nombre);command.Parameters.AddWithValue("@e",p.Estado);
                await command.ExecuteNonQueryAsync();
            }
        }
        await q.Exec("INSERT RolPermiso(IdRol,IdPermiso) SELECT 1,IdPermiso FROM Permiso WHERE Estado=1; INSERT RolPermiso(IdRol,IdPermiso) SELECT 2,IdPermiso FROM Permiso WHERE Nombre='PRODUCTOS_VER';");
        await using var api=await ApiHost.Start(q);await ApiTests.Login(api);
        var catalogo=JsonSerializer.Deserialize<List<PermisoDto>>(await (await api.Http.GetAsync("/api/rol/permisos")).Content.ReadAsStringAsync(),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        int Id(string n)=>catalogo.Single(p=>p.Nombre==n).IdPermiso;
        async Task Rechazo(int[] ids,string texto)
        {
            var antes=await q.Scalar("SELECT SUM(CONVERT(bigint,IdPermiso)*IdPermiso) FROM RolPermiso WHERE IdRol=2");
            var audit=await q.Scalar("SELECT COUNT(*) FROM AuditoriaLog WHERE TablaAfectada='RolPermiso'");
            var r=await api.Http.PutAsJsonAsync("/api/rol/2/permisos",new{idsPermisos=ids});
            Assert.Equal(HttpStatusCode.BadRequest,r.StatusCode);Assert.Contains(texto,await r.Content.ReadAsStringAsync());
            Assert.Equal(antes,await q.Scalar("SELECT SUM(CONVERT(bigint,IdPermiso)*IdPermiso) FROM RolPermiso WHERE IdRol=2"));
            Assert.Equal(audit,await q.Scalar("SELECT COUNT(*) FROM AuditoriaLog WHERE TablaAfectada='RolPermiso'"));
        }
        foreach(var grupo in catalogo.Where(p=>ReglasSeleccionPermisos.BaseRequerida(p.Nombre)!=null).GroupBy(p=>ReglasSeleccionPermisos.BaseRequerida(p.Nombre)))
            await Rechazo([grupo.First().IdPermiso],grupo.Key!);
        await Rechazo([],"al menos un módulo");await Rechazo([Id("INICIO_VER")],"al menos un módulo");
        await Rechazo([Id("PRODUCTOS_VER"),int.MaxValue],"no existen");
        Assert.Equal(HttpStatusCode.OK,(await api.Http.PutAsJsonAsync("/api/rol/2/permisos",new{idsPermisos=new[]{Id("PRODUCTOS_VER")}})).StatusCode);
        Assert.Equal(1,await q.Scalar("SELECT COUNT(*) FROM RolPermiso WHERE IdRol=2"));
        Assert.Equal(HttpStatusCode.OK,(await api.Http.PutAsJsonAsync("/api/rol/2/permisos",new{idsPermisos=new[]{Id("COMPRAS_VER"),Id("COMPRAS_CREAR")}})).StatusCode);
        Assert.True(await q.Scalar("SELECT COUNT(*) FROM AuditoriaLog WHERE IdUsuario=1 AND TablaAfectada='RolPermiso'")>0);
        await ApiTests.Login(api,"compras@example.test");
        Assert.Equal(HttpStatusCode.OK,(await api.Http.GetAsync("/api/compra")).StatusCode);
        await NegocioTests.Abrir(q,200);
        Assert.Equal(HttpStatusCode.OK,(await api.Http.PostAsJsonAsync("/api/compra",NegocioTests.Compra(100,1))).StatusCode);
        Assert.Equal(2,await q.Scalar("SELECT IdUsuarioRegistro FROM Compra"));
        Assert.Equal(HttpStatusCode.Forbidden,(await api.Http.PostAsJsonAsync("/api/compra/pago",new{idCompra=1,montoPagado=1,metodoPago="Efectivo"})).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await api.Http.GetAsync("/api/producto")).StatusCode);
        await ApiTests.Login(api);Assert.Equal(HttpStatusCode.OK,(await api.Http.GetAsync("/api/usuario")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await api.Http.PutAsJsonAsync("/api/rol/1/permisos",new{idsPermisos=Array.Empty<int>()})).StatusCode);
        Assert.Equal(catalogo.Count,await q.Scalar("SELECT COUNT(*) FROM RolPermiso WHERE IdRol=1"));
        await q.Exec("INSERT Rol(Nombre) VALUES(N'Rol sin configuración QA')");
        int rolVacio=(int)await q.Scalar("SELECT MAX(IdRol) FROM Rol");
        var usuario=await api.Http.PostAsJsonAsync("/api/usuario",new{idRol=rolVacio,correo="sinmodulo@example.test",contrasenaInicial=QaDatabase.Password});
        Assert.Equal(HttpStatusCode.BadRequest,usuario.StatusCode);
        Assert.Equal(0,await q.Scalar("SELECT COUNT(*) FROM Usuario WHERE Correo='sinmodulo@example.test'"));
        await q.Exec("UPDATE Permiso SET Estado=0 WHERE Nombre='COMPRAS_VER'");
        await Rechazo([Id("COMPRAS_VER"),Id("COMPRAS_CREAR")],"inactivos");
    }
}
