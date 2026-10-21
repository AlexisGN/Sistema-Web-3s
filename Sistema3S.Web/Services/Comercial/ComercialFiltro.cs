using Sistema3S.Web.Services.Bi;
namespace Sistema3S.Web.Services.Comercial;

public sealed class ComercialFiltro
{
 public DateOnly Desde{get;set;}=new(BiReglas.HoyPeru().Year,BiReglas.HoyPeru().Month,1);
 public DateOnly Hasta{get;set;}=BiReglas.HoyPeru();
 public bool Comparar{get;set;}=false;
 public bool NecesitaBase=>Comparar||Escenario is "clientes-caida" or "productos-caida" or "categorias-caida";
 public DateOnly? DesdeB{get;set;}
 public DateOnly? HastaB{get;set;}
 public int? Categoria{get;set;}
 public int? Elemento{get;set;}
 public int? Cliente{get;set;}
 public int? Proveedor{get;set;}
 public int VentanaConversion{get;set;}=30;
 public string Escenario{get;set;}="todos";
 public int DiasInactividad{get;set;}=90;
 public decimal Umbral{get;set;}=20;
 public int MinimoOperaciones{get;set;}=1;
 public decimal ImporteMinimo{get;set;}=0;
 public (DateOnly Desde,DateOnly Hasta) PeriodoB=> (DesdeB??Desde.AddDays(-(Hasta.DayNumber-Desde.DayNumber+1)),HastaB??Desde.AddDays(-1));
 public void Validar()
 {
  var b=PeriodoB;var hoy=BiReglas.HoyPeru();
  foreach(var p in new[]{(Desde,Hasta),b})if(p.Item1.Year<1900||p.Item2>hoy||p.Item2<p.Item1||p.Item2.DayNumber-p.Item1.DayNumber>1826)throw new ArgumentException("Selecciona fechas válidas de hasta cinco años, sin días futuros.");
  if(DesdeB.HasValue!=HastaB.HasValue)throw new ArgumentException("Completa las dos fechas del período B.");
  if(VentanaConversion<1||VentanaConversion>365||DiasInactividad<1||DiasInactividad>1826||Umbral<0||Umbral>100||MinimoOperaciones<1||MinimoOperaciones>10000||ImporteMinimo<0||ImporteMinimo>100000000)throw new ArgumentException("Revisa los parámetros del análisis comercial.");
  if(Escenario is not ("todos" or "clientes-caida" or "cotizaciones-seguimiento" or "productos-caida" or "categorias-caida" or "criticos-ventas" or "stock-inactivo" or "proveedor-concentrado"))throw new ArgumentException("Selecciona un filtro comercial disponible.");
  if(new[]{Categoria,Elemento,Cliente,Proveedor}.Any(x=>x<0))throw new ArgumentException("Selecciona filtros válidos.");
 }
}

public sealed record ComercialResultado(string Vista,ComercialFiltro Filtros,DateTimeOffset GeneradoUtc,Dictionary<string,List<Dictionary<string,object?>>> Datos,string[] Notas);
