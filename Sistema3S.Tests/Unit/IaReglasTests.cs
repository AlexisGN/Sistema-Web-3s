using Sistema3S.Web.Services.Ia;
namespace Sistema3S.Tests.Unit;

public sealed class IaReglasTests
{
 private static readonly DateOnly Today=new(2026,10,14);
 private static IaContexto Producto()=>new(1,"Producto QA","UND",5,2,100,60,Today,Today,"Proveedor QA",new(1,1,3,7,2,1,4,0,0,null,"Contrato QA",Today.AddDays(20)));
 [Fact(DisplayName="PU-IA-001 | Demanda acumulada, plazo, redondeo y fecha sin confirmar compra")]
 public void Reposicion(){var daily=Enumerable.Repeat(1m,90).ToList();var r=IaReglas.Riesgo(Producto(),daily,Today.AddDays(1),Today,true);Assert.Equal(8,r.Cantidad);Assert.Equal(Today.AddDays(5),r.Agotamiento);Assert.Equal(Today.AddDays(2),r.FechaPedido);Assert.Equal("Alta",r.Prioridad);var p=Producto() with{Stock=9};p=p with{Politica=p.Politica! with{Minimo=5,Multiplo=4}};r=IaReglas.Riesgo(p,daily,Today.AddDays(1),Today,true);Assert.Equal(8,r.Cantidad);Assert.True(r.Cantidad>=p.Politica.Minimo);Assert.Equal(0,r.Cantidad%p.Politica.Multiplo);r=IaReglas.Riesgo(p with{Stock=20},daily,Today.AddDays(1),Today,true);Assert.Equal(0,r.Cantidad);}
 [Fact(DisplayName="PU-IA-002 | Sin plazo o pronóstico reciente no inventa cantidades")]
 public void Incompleto(){var r=IaReglas.Riesgo(Producto() with{Politica=null},Enumerable.Repeat(1m,90).ToList(),Today.AddDays(1),Today,true);Assert.Null(r.Cantidad);Assert.Equal("Verificar abastecimiento",r.Prioridad);r=IaReglas.Riesgo(Producto() with{Stock=0},[],Today.AddDays(1),Today,false);Assert.Null(r.Cantidad);Assert.Contains("regla de inventario",r.Explicacion);}
 [Fact(DisplayName="PU-IA-003 | Reservas y entradas sólo verificadas, sin inventario negativo")]
 public void Reservas(){var p=Producto();p=p with{Politica=p.Politica! with{Reservado=3,EntradaConfirmada=10,FechaEntrada=Today.AddDays(1)}};var r=IaReglas.Riesgo(p,Enumerable.Repeat(1m,90).ToList(),Today.AddDays(1),Today,true);Assert.Equal(0,r.Cantidad);Assert.Equal(Today.AddDays(12),r.Agotamiento);}
 [Fact(DisplayName="PU-IA-004 | Configuración inválida no aplica plazos arbitrarios")]
 public void Validacion(){var p=Producto().Politica!;IaReglas.Validar(p,Today);Assert.Throws<ArgumentException>(()=>IaReglas.Validar(p with{PlazoDias=0},Today));Assert.Throws<ArgumentException>(()=>IaReglas.Validar(p with{CoberturaDias=90},Today));Assert.Throws<ArgumentException>(()=>IaReglas.Validar(p with{EntradaConfirmada=1,FechaEntrada=null},Today));}
}
