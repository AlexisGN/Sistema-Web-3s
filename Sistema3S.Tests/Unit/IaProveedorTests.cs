using Sistema3S.Web.Services.Ia;
namespace Sistema3S.Tests.Unit;
[Trait("Category","Unit")]
public class IaProveedorTests
{
 private static readonly DateOnly Hoy=new(2026,10,15);
 [Fact] public void ProveedorIdentificadoNoSeTrataComoProveedorSinVerificar()
 {
  var p=new IaContexto(1,"QA","UND",3,1,100,40,null,null,"Proveedor registrado",null,7);
  var risk=IaReglas.Riesgo(p,Enumerable.Repeat(1m,90).ToArray(),Hoy.AddDays(1),Hoy,true);
  Assert.Equal(7,risk.IdProveedor);Assert.Equal("Verificar abastecimiento",risk.Prioridad);Assert.Null(risk.Cantidad);
  Assert.Contains("Proveedor identificado",risk.Explicacion);Assert.Contains("plazo",risk.Explicacion);
 }
 [Fact] public void CondicionesGuardadasSiguenCalculandoSinModificarElProveedor()
 {
  var policy=new IaPolitica(1,9,3,7,0,1,1,0,0,null,"Registrado",Hoy.AddDays(30));
  var p=new IaContexto(1,"QA","UND",3,1,100,40,null,null,"Proveedor configurado",policy,9);
  var risk=IaReglas.Riesgo(p,Enumerable.Repeat(1m,90).ToArray(),Hoy.AddDays(1),Hoy,true);
  Assert.Equal(9,risk.IdProveedor);Assert.Equal(7,risk.Cantidad);Assert.Equal("Urgente",risk.Prioridad);Assert.Equal(Hoy,risk.FechaPedido);
  Assert.Equal(policy,p.Politica);
 }
 [Fact] public void UnaCondicionVencidaExplicaLaVigenciaSinInvalidarAlProveedor()
 {
  var policy=new IaPolitica(1,9,3,7,0,1,1,0,0,null,"Registrado",Hoy.AddDays(-1));
  var p=new IaContexto(1,"QA","UND",3,1,100,40,null,null,"Proveedor",policy,9);
  var risk=IaReglas.Riesgo(p,Enumerable.Repeat(1m,90).ToArray(),Hoy.AddDays(1),Hoy,true);
  Assert.Null(risk.Cantidad);Assert.Contains("vigencia",risk.Explicacion);Assert.Contains("no necesita una segunda verificación",risk.Explicacion);
 }
}
