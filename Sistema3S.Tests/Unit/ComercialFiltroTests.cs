using Sistema3S.Web.Services.Comercial;
namespace Sistema3S.Tests.Unit;
public sealed class ComercialFiltroTests
{
 [Fact]public void DiferenteDuracionEsExplicitaYValida(){var f=new ComercialFiltro{Desde=new(2026,9,1),Hasta=new(2026,9,10),DesdeB=new(2026,8,1),HastaB=new(2026,8,31)};f.Validar();Assert.Equal(31,f.PeriodoB.Hasta.DayNumber-f.PeriodoB.Desde.DayNumber+1);}
 [Fact]public void FechasYParametrosIncompletosSeRechazan(){var f=new ComercialFiltro{Desde=new(2026,9,1),Hasta=new(2026,9,10),DesdeB=new(2026,8,1)};Assert.Throws<ArgumentException>(()=>f.Validar());f.HastaB=new(2026,8,10);f.Umbral=101;Assert.Throws<ArgumentException>(()=>f.Validar());}
 [Fact]public void ReferenciaPorDefectoConservaDiasInclusivos(){var f=new ComercialFiltro{Desde=new(2026,3,1),Hasta=new(2026,3,7)};Assert.Equal((new DateOnly(2026,2,22),new DateOnly(2026,2,28)),f.PeriodoB);}
}
