using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
namespace Sistema3S.Web.Services.Comercial;

public sealed class ComercialExportacion
{
 static IEnumerable<KeyValuePair<string,List<Dictionary<string,object?>>>> Datos(ComercialResultado r){
  string[] keys=r.Vista switch{
   "inventario"=>["stock","inventario"],"compras"=>["periodosCompras","evolucionCompras","proveedores"],
   "ventas"=>["periodos","evolucion","productos","categorias"],"productos"=>["periodos","productos","categorias"],
   "clientes"=>["periodos","clientes","cotizaciones"],_=>["periodos","evolucion","productos","categorias","clientes","stock","diccionario"]};
  return r.Datos.Where(x=>keys.Contains(x.Key));
 }
 static string[] Notas(ComercialResultado r)=>r.Vista switch{
  "inventario"=>["Stock actual de productos activos inventariables. No es un saldo histórico.","El stock crítico incluye los productos sin stock; los porcentajes no se suman.","La cobertura histórica utiliza el ritmo de ventas del período."],
  "compras"=>["Compras vigentes sin IGV. No son costo de ventas ni utilidad.","La participación se calcula dentro de los filtros aplicados; elegir un solo proveedor puede producir 100 %."],
  "ventas" or "productos"=>["Ventas confirmadas sin IGV. No representan cobros ni utilidad.","Compare períodos completos y de duración equivalente."],
  "clientes"=>[$"Cotizaciones con venta confirmada dentro de {r.Filtros.VentanaConversion} días, entre las que ya cumplieron el plazo.","Recurrentes: compradores con una venta anterior al inicio del período.","Se muestran hasta 100 cotizaciones en el detalle."],
  _=>["Ventas confirmadas sin IGV; no equivalen a utilidad.","Las tasas requieren una base válida. El stock muestra las existencias actuales.","Stock crítico incluye los productos sin stock."]};
 static (string Key,string Label)[] Medidas(ComercialResultado r)=>r.Vista=="compras"
  ?[("CompraNeta","Compras sin IGV · S/"),("Compras","Número de compras"),("Proveedores","Proveedores con compras"),("PromedioCompra","Compra promedio · S/"),("Dias","Duración · días")]
  :r.Vista is "ventas" or "productos"
   ?[("VentaNeta","Ventas sin IGV · S/"),("Operaciones","Ventas confirmadas"),("Ticket","Venta promedio sin IGV · S/"),("Dias","Duración · días")]
   :[("VentaNeta","Ventas sin IGV · S/"),("Operaciones","Ventas confirmadas"),("Ticket","Venta promedio sin IGV · S/"),("Compradores","Compradores distintos"),("Conversion","Cotizaciones que terminaron en venta · %"),("TasaRecurrentes","Compradores recurrentes · %"),("Dias","Duración · días")];
 static string Celda(string tabla,string key,object? value)=>tabla.StartsWith("evolucion")&&key=="Nombre"&&DateTime.TryParseExact(Convert.ToString(value),"yyyy-MM",CultureInfo.InvariantCulture,DateTimeStyles.None,out var month)
  ?CultureInfo.GetCultureInfo("es-PE").TextInfo.ToTitleCase(month.ToString("MMMM 'de' yyyy",CultureInfo.GetCultureInfo("es-PE"))):Text(value);
 static string Cabecera(string tabla,string key,bool comparar)=>key=="Nombre"?(tabla.StartsWith("evolucion")?"Mes":tabla=="clientes"||tabla=="cotizaciones"?"Cliente":tabla=="proveedores"?"Proveedor":tabla=="categorias"?"Categoría":"Producto o servicio"):Etiqueta(key,comparar);
 static string Text(object? value)=>value switch{null=>"No calculable",DateTime d=>d.ToString("dd/MM/yyyy"),decimal d=>d.ToString("N2",CultureInfo.GetCultureInfo("es-PE")),_=>Convert.ToString(value,CultureInfo.GetCultureInfo("es-PE"))??""};
 static string Etiqueta(string key,bool comparar)=>key switch{
  "A"=>comparar?"Ventas A · S/":"Ventas · S/", "B"=>"Ventas B · S/", "Diferencia"=>"Cambio · S/", "Variacion"=>"Cambio %",
  "SinStock"=>"Sin stock", "Criticos"=>"Stock crítico", "SaldoNoValido"=>"Saldos por revisar", "PorcentajeSinStock"=>"Sin stock %", "PorcentajeCritico"=>"Stock crítico %",
  "StockActual"=>"Stock actual", "StockMinimo"=>"Mínimo", "CoberturaHistorica"=>"Cobertura · días", "OperacionesA"=>comparar?"Operaciones A":"Operaciones", "OperacionesB"=>"Operaciones B",
  "Periodo"=>"Período", "Nombre"=>"Nombre", "Importe"=>"Importe · S/", "Antiguedad"=>"Antigüedad · días", "Participacion"=>"Participación %",_=>key};
 public byte[] Excel(ComercialResultado r){using var wb=new XLWorkbook();var info=wb.AddWorksheet("Criterios");info.Cell(1,1).Value="Inteligencia Comercial · "+r.Vista;info.Cell(2,1).Value=$"{r.Filtros.Desde:dd/MM/yyyy}–{r.Filtros.Hasta:dd/MM/yyyy}"+(r.Filtros.Comparar?$"; B {r.Filtros.PeriodoB.Desde:dd/MM/yyyy}–{r.Filtros.PeriodoB.Hasta:dd/MM/yyyy}":"");info.Cell(3,1).Value=$"Filtro {r.Filtros.Escenario}; generado {r.GeneradoUtc:u}";int n=5;foreach(var note in Notas(r))info.Cell(n++,1).Value=note;info.Column(1).Width=100;info.Column(1).Style.Alignment.WrapText=true;
 foreach(var(key,rows)in Datos(r)){var s=wb.AddWorksheet(key);if(rows.Count==0){s.Cell(1,1).Value="Sin coincidencias";continue;}var columns=rows.SelectMany(x=>x.Keys).Distinct().Where(k=>key!="periodos"||r.Vista is not ("ventas" or "productos")||new[]{"Periodo","Desde","Hasta","VentaNeta","Operaciones","Ticket","Dias","DiaParcial"}.Contains(k)).ToArray();for(var j=0;j<columns.Length;j++)s.Cell(1,j+1).Value=columns[j];for(var i=0;i<rows.Count;i++)for(var j=0;j<columns.Length;j++){var v=rows[i].GetValueOrDefault(columns[j]);if(v is decimal or double or int or long)s.Cell(i+2,j+1).Value=Convert.ToDouble(v);else s.Cell(i+2,j+1).Value=Celda(key,columns[j],v);}s.Range(1,1,1,columns.Length).Style.Font.Bold=true;s.Columns().AdjustToContents(8,42);s.SheetView.FreezeRows(1);}
 using var stream=new MemoryStream();wb.SaveAs(stream);return stream.ToArray();}
 public byte[] Pdf(ComercialResultado r)=>Document.Create(doc=>doc.Page(page=>{
  page.Size(PageSizes.A4.Landscape());page.Margin(28);page.DefaultTextStyle(x=>x.FontSize(10));
  page.Content().Column(col=>{
   col.Item().Text("Inteligencia Comercial").Bold().FontSize(19);
   col.Item().Text($"{r.Vista} · {r.Filtros.Desde:dd/MM/yyyy}–{r.Filtros.Hasta:dd/MM/yyyy}"+(r.Filtros.Comparar?$" · B {r.Filtros.PeriodoB.Desde:dd/MM/yyyy}–{r.Filtros.PeriodoB.Hasta:dd/MM/yyyy}":""));
   col.Item().Text(r.Vista=="inventario"?"Existencias actuales; el rango de fechas se usa para las ventas observadas.":r.Vista=="compras"?"Compras vigentes en soles sin IGV.":"Ventas confirmadas en soles sin IGV.").FontSize(9);
   col.Item().Text($"Generado {r.GeneradoUtc.ToOffset(TimeSpan.FromHours(-5)):dd/MM/yyyy HH:mm} · America/Lima").FontSize(8);
   if(r.Vista!="inventario"){
   var periodos=r.Datos[r.Vista=="compras"?"periodosCompras":"periodos"];
   var a=periodos.First(x=>Equals(x["Periodo"],"A"));var b=periodos.FirstOrDefault(x=>Equals(x["Periodo"],"B"));
   col.Item().PaddingTop(15).Text(r.Filtros.Comparar?"Resultados comparables":"Resultados del período").Bold();
   col.Item().Table(t=>{t.ColumnsDefinition(c=>{c.RelativeColumn(2);c.RelativeColumn();if(r.Filtros.Comparar){c.RelativeColumn();c.RelativeColumn();}});
    t.Header(h=>{foreach(var k in r.Filtros.Comparar?new[]{"Medida","Período A","Período B","Diferencia A − B"}:new[]{"Medida","Período seleccionado"})h.Cell().Background("#E9EEF4").Padding(7).Text(k).Bold();});
    foreach(var(key,label)in Medidas(r)){
     if(a.GetValueOrDefault(key)==null||(b!=null&&b.GetValueOrDefault(key)==null))continue;
     t.Cell().BorderBottom(.5f).BorderColor("#E1E5EB").Padding(7).Text(r.Filtros.Comparar?label:label.Replace(" / diferencia en puntos",""));
     foreach(var row in b==null?new[]{a}:new[]{a,b})t.Cell().BorderBottom(.5f).BorderColor("#E1E5EB").Padding(7).Text(Text(row.GetValueOrDefault(key)));
     if(b!=null){object? delta=a.GetValueOrDefault(key)!=null&&b.GetValueOrDefault(key)!=null?Convert.ToDecimal(a[key])-Convert.ToDecimal(b[key]):null;
     t.Cell().BorderBottom(.5f).BorderColor("#E1E5EB").Padding(7).Text(Text(delta));}
    }
   });
   }
   foreach(var(key,rows)in Datos(r).Where(k=>k.Key is not ("periodos" or "periodosCompras" or "diccionario"))){
    col.Item().PaddingTop(16).Text(key switch{"stock"=>"Disponibilidad actual del catálogo activo","productos"=>"Productos y servicios sin IGV","clientes"=>"Clientes sin IGV","categorias"=>"Categorías sin IGV","proveedores"=>"Adquisiciones por proveedor sin IGV","inventario"=>"Existencias y ventas observadas","cotizaciones"=>"Cotizaciones para seguimiento","evolucionCompras"=>"Compras por mes sin IGV","evolucion"=>"Ventas por mes sin IGV",_=>key}).Bold();
    if(rows.Count==0){col.Item().Text("Sin coincidencias");continue;}
    var columns=key switch{
     "stock"=>new[]{"Productos","SinStock","Criticos","SaldoNoValido","PorcentajeSinStock","PorcentajeCritico"},
     "productos"=>r.Filtros.NecesitaBase?["Nombre","Unidad","A","B","Diferencia","Variacion"]:["Nombre","Unidad","A","Cantidad"],
     "clientes"=>r.Filtros.NecesitaBase?["Nombre","A","B","Diferencia","Variacion"]:["Nombre","A","OperacionesA"],
     "inventario"=>["Nombre","Unidad","StockActual","StockMinimo","Vendido","CoberturaHistorica"],
     "cotizaciones"=>["Nombre","Fecha","Estado","Importe","Antiguedad"],
     _=>rows.SelectMany(x=>x.Keys).Distinct().Where(x=>x is not ("Id" or "IdElementoCatalogo")&&(r.Filtros.Comparar||x!="Periodo")).ToArray()};
    col.Item().Table(t=>{t.ColumnsDefinition(c=>{foreach(var k in columns)c.RelativeColumn(k=="Nombre"?3:1);});t.Header(h=>{foreach(var k in columns)h.Cell().Background("#E9EEF4").Padding(5).Text(Cabecera(key,k,r.Filtros.Comparar)).Bold().FontSize(9);});foreach(var row in rows)foreach(var k in columns)t.Cell().BorderBottom(.5f).BorderColor("#E1E5EB").Padding(5).Text(Celda(key,k,row.GetValueOrDefault(k))).FontSize(9);});
   }
   col.Item().PaddingTop(18).Text("Definiciones y alcance").Bold();foreach(var note in Notas(r))col.Item().PaddingTop(4).Text(note).FontSize(8);
  });page.Footer().AlignRight().Text(x=>{x.Span("Página ");x.CurrentPageNumber();});
 })).GeneratePdf();
}
