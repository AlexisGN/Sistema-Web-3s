using Sistema3S.Web.Services.Ia;
namespace Sistema3S.Tests.Unit;
public sealed class IaReposicionTests
{
 [Fact] public void PedidoVencidoSeAccionaHoyYConservaSuFechaLimite()
 {
  var hoy=new DateOnly(2026,10,16);var p=new IaContexto(1,"QA","UND",4,1,100,70,null,null,"Proveedor",new(1,1,10,30,0,1,1,0,0,null,"QA",hoy.AddDays(20)));
  var riesgo=IaReglas.Riesgo(p,Enumerable.Repeat(1m,90).ToArray(),hoy.AddDays(1),hoy,true);
  Assert.Equal(hoy,riesgo.FechaPedido);Assert.Equal(hoy.AddDays(-6),riesgo.FechaLimitePedido);Assert.Equal(6,riesgo.RetrasoDias);Assert.Equal("Urgente",riesgo.Prioridad);Assert.Equal(36,riesgo.Cantidad);
 }
 [Fact] public void PedidoFuturoYStockCubiertoConservanElComportamientoAnterior()
 {
  var hoy=new DateOnly(2026,10,16);var policy=new IaPolitica(1,1,2,5,0,1,1,0,0,null,"QA",hoy.AddDays(20));var p=new IaContexto(1,"QA","UND",10,1,100,70,null,null,"Proveedor",policy);
  var r=IaReglas.Riesgo(p,Enumerable.Repeat(1m,90).ToArray(),hoy.AddDays(1),hoy,true);Assert.Equal(hoy.AddDays(8),r.FechaPedido);Assert.Equal(0,r.RetrasoDias);
  r=IaReglas.Riesgo(p with{Stock=100},Enumerable.Repeat(1m,90).ToArray(),hoy.AddDays(1),hoy,true);Assert.Null(r.FechaPedido);Assert.Null(r.FechaLimitePedido);Assert.Equal(0,r.Cantidad);
 }
}
