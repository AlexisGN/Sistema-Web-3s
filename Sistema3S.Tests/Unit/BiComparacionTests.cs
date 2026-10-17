using Sistema3S.Web.DTOs.Bi;
namespace Sistema3S.Tests.Unit;
public sealed class BiComparacionTests
{
    [Fact] public void DoceDiasIncluyenExtremos(){var f=new BiFiltro{Desde=new(2026,10,1),Hasta=new(2026,10,12),ModoComparacion="personalizado",CompararDesde=new(2026,9,19),CompararHasta=new(2026,9,30)};f.Validar(new(2026,10,12));Assert.Equal(12,f.Comparacion().Hasta.DayNumber-f.Comparacion().Desde.DayNumber+1);}
    [Fact] public void ApagadaIgnoraFechasAntiguas(){var f=new BiFiltro{Desde=new(2026,10,1),Hasta=new(2026,10,12),ModoComparacion="ninguna",CompararDesde=new(2026,9,1),CompararHasta=new(2026,9,30)};f.Validar(new(2026,10,12));Assert.False(f.ComparacionActiva);}
    [Fact] public void AnteriorSeRegeneraAunqueExistanFechasObsoletas(){var f=new BiFiltro{Desde=new(2026,10,1),Hasta=new(2026,10,12),ModoComparacion="anterior",CompararDesde=new(2026,9,1),CompararHasta=new(2026,9,30)};f.Validar(new(2026,10,12));Assert.Equal((new DateOnly(2026,9,19),new DateOnly(2026,9,30)),f.Comparacion());}
    [Theory][InlineData(2024,2,1,29,2023,2,1,2023,3,1)][InlineData(2026,10,1,12,2025,10,1,2025,10,12)]
    public void AnioAnteriorMantieneDuracion(int y,int m,int d,int final,int y2,int m2,int d2,int yf,int mf,int df){var f=new BiFiltro{Desde=new(y,m,d),Hasta=new(y,m,final),ModoComparacion="anioAnterior"};f.Validar(new(2026,10,12));Assert.Equal((new DateOnly(y2,m2,d2),new DateOnly(yf,mf,df)),f.Comparacion());}
    [Fact] public void PersonalizadaIncompletaSeRechaza(){var f=new BiFiltro{Desde=new(2026,10,1),Hasta=new(2026,10,12),ModoComparacion="personalizado",CompararDesde=new(2026,9,19)};Assert.Throws<ArgumentException>(()=>f.Validar(new(2026,10,12)));}
}
