using Sistema3S.Web.DTOs.Bi;
using Sistema3S.Web.Services.Bi;
namespace Sistema3S.Tests.Unit;

public sealed class BiReglasTests
{
    [Theory][InlineData(120,100,20)][InlineData(0,100,-100)][InlineData(75,100,-25)]
    public void Variacion(decimal a,decimal b,decimal esperado)=>Assert.Equal(esperado,BiReglas.Variacion(a,b));
    [Fact]public void SinBaseNoEsCero(){Assert.Null(BiReglas.Variacion(100,0));Assert.Null(BiReglas.Cociente(100,0));Assert.Equal(0,BiReglas.Cociente(0,10));}
    [Fact]public void ComparacionIncluyeDiasParciales(){var f=new BiFiltro{Desde=new(2026,10,1),Hasta=new(2026,10,12)};var p=f.Comparacion();Assert.Equal(new DateOnly(2026,9,19),p.Desde);Assert.Equal(new DateOnly(2026,9,30),p.Hasta);}
    [Theory][InlineData("dia")][InlineData("semana")][InlineData("mes")][InlineData("anio")]
    public void Agrupaciones(string a)=>Assert.NotEmpty(BiReglas.PeriodoSql(a));
    [Fact]public void ParametrosNoValidadosNuncaFormanSql()=>Assert.Throws<ArgumentException>(()=>BiReglas.PeriodoSql("mes;DROP TABLE Venta"));
    [Fact]public void RechazaPeriodosInvalidos(){var hoy=new DateOnly(2026,10,12);var f=new BiFiltro{Desde=new(2026,10,1),Hasta=new(2026,10,13)};Assert.Throws<ArgumentException>(()=>f.Validar(hoy));f.Hasta=hoy;f.CompararDesde=new(2026,9,1);f.CompararHasta=new(2026,9,30);Assert.Throws<ArgumentException>(()=>f.Validar(hoy));}
    [Fact]public void RechazaPaginacionOrigenYEstado(){var f=new BiFiltro{Desde=new(2026,10,1),Hasta=new(2026,10,12),Tamanio=101};Assert.Throws<ArgumentException>(()=>f.Validar(new(2026,10,12)));f.Tamanio=20;f.Procedencia="mixto";Assert.Throws<ArgumentException>(()=>f.Validar(new(2026,10,12)));f.Procedencia="operativo";f.Estado="inventado";Assert.Throws<ArgumentException>(()=>f.Validar(new(2026,10,12)));}
}
