using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Sistema3S.Tests.Fixtures;
using Sistema3S.Web.Data;
using Sistema3S.Web.DTOs.Auth;
using Sistema3S.Web.DTOs.Producto;
using Sistema3S.Web.DTOs.Venta;
using Sistema3S.Web.DTOs.Compra;
using Sistema3S.Web.DTOs.Inventario;
using Sistema3S.Web.Services.Implementations;
using Sistema3S.Web.Services.Seguridad;

namespace Sistema3S.Tests.Unit;
[Trait("Category","Unit")]
public class ValidacionesTests
{
    private static TestDb Db() => new(new DbContextOptionsBuilder<Bd3sContext>().Options);
    [Fact(DisplayName="PU-AUTH-001 | Hash verifica clave correcta y rechaza clave distinta")]
    public void Hash(){var p=new PasswordHashService();var h=p.CrearHash("ClaveSoloQA123!");Assert.True(p.Verificar("ClaveSoloQA123!",h));Assert.False(p.Verificar("incorrecta",h));Assert.NotEqual(h,p.CrearHash("ClaveSoloQA123!"));Assert.False(p.Verificar("ClaveSoloQA123!","texto-plano"));}
    [Fact(DisplayName="PU-AUTH-002 | Login vacío se rechaza antes de SQL")]
    public async Task LoginVacio(){using var db=Db();var s=new AuthService(db,new ConfigurationBuilder().Build(),new());var ex=await Assert.ThrowsAsync<InvalidOperationException>(()=>s.LoginAsync(new LoginDto()));Assert.Contains("Credenciales",ex.Message);}
    [Fact(DisplayName="PU-CLI-001 | Reglas de contraseña y confirmación")]
    public void Clave(){Assert.Throws<InvalidOperationException>(()=>ClienteCuentaService.ValidarClave("corta","corta"));Assert.Throws<InvalidOperationException>(()=>ClienteCuentaService.ValidarClave("ValidaSoloQA123!","distinta"));ClienteCuentaService.ValidarClave("ValidaSoloQA123!","ValidaSoloQA123!");}
    [Theory]
    [InlineData("nombre","nombre")][InlineData("codigo","código")][InlineData("precio","negativo")][InlineData("categoria","categoría")]
    public async Task PU_PRO_001_ProductoInvalido(string campo,string mensaje){using var db=Db();var d=new ProductoCrearDto{Nombre="QA",CodigoProducto="QA",Descripcion="QA",ImagenUrl="/qa.png",PrecioReferencial=10,IdCategoria=1,IdUnidadMedida=1};if(campo=="nombre")d.Nombre="";if(campo=="codigo")d.CodigoProducto="";if(campo=="precio")d.PrecioReferencial=-1;if(campo=="categoria")d.IdCategoria=0;var e=await Assert.ThrowsAsync<InvalidOperationException>(()=>new ProductoService(db).CrearAsync(d));Assert.Contains(mensaje,e.Message);}
    [Theory]
    [InlineData("cliente","cliente")][InlineData("cantidad","cantidad")][InlineData("precio","precio")][InlineData("cobro","mayor al total")]
    public async Task PU_VEN_001_VentaInvalida(string campo,string mensaje){using var db=Db();var d=new VentaCrearDto{IdCliente=1,IdUsuarioRegistro=1,TipoComprobante="Boleta",TipoPago="Total",MetodoPago="Efectivo",MontoPagado=100,Detalles=[new(){IdElementoCatalogo=1,Cantidad=1,PrecioUnitario=100}]};if(campo=="cliente")d.IdCliente=0;if(campo=="cantidad")d.Detalles[0].Cantidad=0;if(campo=="precio")d.Detalles[0].PrecioUnitario=0;if(campo=="cobro")d.MontoPagado=101;var e=await Assert.ThrowsAsync<InvalidOperationException>(()=>new VentaService(db).RegistrarVentaCompletaAsync(d));Assert.Contains(mensaje,e.Message);}
    [Theory]
    [InlineData(0,"proveedor")][InlineData(1,"producto")]
    public async Task PU_COM_001_CompraInvalida(int proveedor,string mensaje){using var db=Db();var d=new CompraCrearDto{IdProveedor=proveedor,IdUsuarioRegistro=1,TipoComprobanteProveedor="Factura",SerieComprobante="QA01",NumeroComprobante="1",FechaEmisionComprobante=DateTime.Today,TipoPago="Total",MetodoPago="Efectivo"};var e=await Assert.ThrowsAsync<InvalidOperationException>(()=>new CompraService(db).RegistrarCompraCompletaAsync(d));Assert.Contains(mensaje,e.Message,StringComparison.OrdinalIgnoreCase);}
    [Fact(DisplayName="PU-STO-001 | Movimiento de stock sin producto se rechaza")]
    public async Task Stock(){using var db=Db();var e=await Assert.ThrowsAsync<InvalidOperationException>(()=>new InventarioService(db).RegistrarMovimientoManualAsync(new RegistrarMovimientoStockDto()));Assert.Contains("producto",e.Message,StringComparison.OrdinalIgnoreCase);}
    [Fact(DisplayName="PU-EXT-001 | DNI y RUC cortos no realizan consulta externa")]
    public async Task Documento(){using var db=Db();using var h=new HttpClient(new NoNetwork());var s=new ConsultaDocumentoService(db,h,new ConfigurationBuilder().Build());Assert.False((await s.ConsultarDniAsync("123")).Exitoso);Assert.False((await s.ConsultarRucAsync("123")).Exitoso);}
    private sealed class NoNetwork:HttpMessageHandler{protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken c)=>throw new Exception("No se permite red en prueba unitaria");}
}
