using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Sistema3S.Web.Services.Pdf;
using System.Globalization;
using System.Security;
using System.Text;

namespace Sistema3S.Web.Services.Bi;

public sealed class BiExportacion(IWebHostEnvironment environment)
{
    private static readonly CultureInfo Es=CultureInfo.GetCultureInfo("es-PE");
    private static readonly Dictionary<string,string> Titulos=new(){["resumen"]="Resumen gerencial",["ventas"]="Análisis de ventas",["productos"]="Productos y categorías",["clientes"]="Clientes y cotizaciones",["compras"]="Compras y proveedores",["inventario"]="Inventario y rotación"};
    private static readonly Dictionary<string,string> Secciones=new(){["kpis"]="Indicadores comerciales",["comparacion"]="Periodo comparable",["cantidades"]="Cantidades por unidad",["evolucion"]="Evolución comercial",["evolucionComparacion"]="Evolución comparable",["estados"]="Estados comerciales",["categorias"]="Categorías",["productosIngresos"]="Productos por ingresos",["productosUnidades"]="Productos por cantidad",["menoresVentas"]="Productos con menores ventas",["clientesIngresos"]="Clientes por ingresos",["clientesFrecuencia"]="Frecuencia de compra",["proveedores"]="Compras por proveedor",["productosComprados"]="Productos comprados",["sinVentas"]="Elementos sin ventas",["clientesAltas"]="Altas de clientes",["clientesSegmentos"]="Tipos de compradores",["operaciones"]="Operaciones consultadas",["productosEvolucion"]="Evolución por producto",["estadoInventario"]="Estado del inventario",["inventario"]="Existencias y movimientos",["mayorMovimiento"]="Mayor movimiento",["menorMovimiento"]="Menor movimiento",["kardex"]="Kardex",["saldosHistoricos"]="Evolución del saldo",["rotacion"]="Rotación y cobertura",["movimientosPeriodo"]="Movimientos por periodo",["comparacionElementos"]="Resultados de A y B",["comparacionMetricas"]="Diferencias de B respecto de A",["cantidadesElementos"]="Volumen por elemento y unidad",["oportunidadesClientes"]="Oportunidades por cliente",["abastecimiento"]="Costos de abastecimiento",["filtros"]="Criterios de consulta"};
    private static string Etiqueta(string campo)=>campo switch {"Importe"=>"Importe con IGV", "TicketPromedio"=>"Ticket promedio", "VariacionAbsoluta"=>"Variación absoluta", "VariacionPorcentual"=>"Variación porcentual", "ConversionPorcentual"=>"Conversión porcentual", "StockActual"=>"Stock actual", "StockMinimo"=>"Stock mínimo", "TotalFilas"=>"Total de filas", "TipoElemento"=>"Tipo de elemento", "CostoUnitario"=>"Costo unitario con IGV", "ImportePendiente"=>"Importe de oportunidades", "Oportunidades"=>"Oportunidades abiertas", "Primero"=>"A", "Segundo"=>"B", "DiferenciaAbsoluta"=>"Diferencia B - A", "DiferenciaPorcentual"=>"Diferencia %", "SinStock"=>"Sin stock", "BajoMinimo"=>"Por debajo del mínimo", "AltasEnPeriodo"=>"Altas en el periodo", "Id"=>"Código interno", _=>campo};
    private static string Texto(object? v) => v switch {null=>"No calculable",DateTime d=>d.ToString("dd/MM/yyyy",Es),decimal d=>d.ToString("N2",Es),"venta"=>"Ventas", "compra"=>"Compras", "cotizacion"=>"Cotizaciones",_=>Convert.ToString(v,Es)??""};
    private static bool EsPorcentaje(string key)=>key=="Participacion"||key.EndsWith("Porcentual",StringComparison.Ordinal);
    private static IReadOnlyDictionary<string,List<Dictionary<string,object?>>> DatosVista(BiResultado r)
    {
        var keys=r.Vista switch {
          "resumen"=>new[]{"kpis","comparacion","evolucion","categorias","productosIngresos","clientesIngresos","proveedores","estados","estadoInventario","resumenActividad","filtros"},
          "ventas"=>["cantidadesElementos","kpis","comparacion","cantidades","evolucion","evolucionComparacion","productosIngresos","categorias","clientesIngresos","operaciones","filtros"],
          "productos"=>["comparacionElementos","comparacionMetricas","kpis","cantidades","cantidadesElementos","categorias","productosIngresos","productosUnidades","menoresVentas","sinVentas","productosEvolucion","operaciones","filtros"],
          "clientes"=>["oportunidadesClientes","kpis","evolucion","estados","clientesIngresos","clientesFrecuencia","clientesSegmentos","clientesAltas","operaciones","resumenActividad","filtros"],
          "compras"=>["abastecimiento","kpis","comparacion","cantidades","evolucion","evolucionComparacion","proveedores","productosComprados","categorias","estados","operaciones","filtros"],
          _=>r.Datos.Keys.ToArray()};
        var tipos=r.Vista switch {"ventas" or "productos"=>new[]{"venta"},"clientes"=>["venta","cotizacion"],"compras"=>["compra"],_=>new[]{"venta","compra","cotizacion"}};
        if(r.Vista=="productos"&&r.Filtros.CompararElemento.HasValue)keys=["comparacionElementos","comparacionMetricas","productosEvolucion","filtros"];
        return keys.Where(r.Datos.ContainsKey).Where(k=>r.CompararDesde.HasValue||k is not ("comparacion" or "evolucionComparacion")).ToDictionary(k=>k,k=>r.Datos[k].Where(d=>!d.ContainsKey("Tipo")||!new[]{"venta","compra","cotizacion"}.Contains(Convert.ToString(d["Tipo"]))||tipos.Contains(Convert.ToString(d["Tipo"]))).ToList());
    }
    public byte[] Excel(BiResultado r)
    {
        using var wb=new XLWorkbook();var intro=wb.Worksheets.Add("Informe");
        intro.Cell(1,1).Value="3S Servicios y Soluciones Superiores";intro.Cell(2,1).Value=Titulos[r.Vista];
        intro.Cell(3,1).Value=$"Periodo {r.Filtros.Desde:dd/MM/yyyy} a {r.Filtros.Hasta:dd/MM/yyyy}";
        intro.Cell(4,1).Value=r.CompararDesde.HasValue?$"Comparación {r.CompararDesde:dd/MM/yyyy} a {r.CompararHasta:dd/MM/yyyy}":"Comparación desactivada";
        intro.Cell(5,1).Value=$"Procedencia {r.Filtros.Procedencia} | PEN | America/Lima | Generado {r.GeneradoUtc:u}";
        intro.Cell(6,1).Value=Filtros(r);
        int row=8;foreach(var nota in r.Notas)intro.Cell(row++,1).Value=nota;
        intro.Range(1,1,2,1).Style.Font.SetBold().Font.SetFontSize(16);intro.Column(1).Width=100;intro.Column(1).Style.Alignment.WrapText=true;
        int seq=1;
        foreach(var (nombre,datos) in DatosVista(r))
        {
            var nombreHoja=$"{seq++:00} {Secciones.GetValueOrDefault(nombre,nombre)}";
            var sheet=wb.Worksheets.Add(nombreHoja[..Math.Min(31,nombreHoja.Length)]);
            sheet.Cell(1,1).Value=Secciones.GetValueOrDefault(nombre,nombre);sheet.Cell(2,1).Value=$"{r.Filtros.Desde:dd/MM/yyyy} - {r.Filtros.Hasta:dd/MM/yyyy} | {r.Filtros.Procedencia}";
            if(datos.Count==0){sheet.Cell(4,1).Value="Sin información para los filtros seleccionados.";continue;}
            var columnas=datos.SelectMany(d=>d.Keys).Distinct().ToArray();
            for(int i=0;i<columnas.Length;i++)sheet.Cell(4,i+1).Value=Etiqueta(columnas[i]);
            row=5;
            foreach(var d in datos)
            {
                for(int i=0;i<columnas.Length;i++)
                {
                    var cell=sheet.Cell(row,i+1);d.TryGetValue(columnas[i],out var value);
                    if(value==null)cell.Value="No calculable";
                    else if(value is DateTime date){cell.Value=date;cell.Style.NumberFormat.Format="dd/mm/yyyy";}
                    else if(value is decimal money){cell.Value=EsPorcentaje(columnas[i])?money/100:money;cell.Style.NumberFormat.Format=EsPorcentaje(columnas[i])?"0.0%":"#,##0.00";}
                    else if(value is long or int or double)cell.Value=Convert.ToDouble(value);
                    else cell.Value=Convert.ToString(value)??""; // XLCellValue texto: no interpreta fórmulas de nombres comerciales.
                }
                row++;
            }
            var table=sheet.Range(4,1,row-1,columnas.Length).CreateTable();table.Theme=XLTableTheme.TableStyleMedium2;
            sheet.SheetView.FreezeRows(4);sheet.Columns().AdjustToContents(8,38);sheet.PageSetup.PageOrientation=XLPageOrientation.Landscape;
        }
        var indicadores=wb.Worksheets.Add("Resumen de la vista");indicadores.Cell(1,1).Value=Titulos[r.Vista];
        indicadores.Cell(3,1).Value="Indicador";indicadores.Cell(3,2).Value="Valor";indicadores.Cell(3,3).Value="Interpretación";
        for(int i=0;i<r.Indicadores.Count;i++){var k=r.Indicadores[i];indicadores.Cell(i+4,1).Value=k.Etiqueta;indicadores.Cell(i+4,3).Value=k.Descripcion;if(k.Valor is { } value){indicadores.Cell(i+4,2).Value=k.Formato=="porcentaje"?value/100:value;indicadores.Cell(i+4,2).Style.NumberFormat.Format=k.Formato=="moneda"?"\"S/ \"#,##0.00":k.Formato=="porcentaje"?"0.0%":"#,##0.##";}else indicadores.Cell(i+4,2).Value="No calculable";}
        indicadores.Columns().AdjustToContents(12,65);
        if(r.Hallazgos.Count>0){var lectura=wb.Worksheets.Add("Visión ejecutiva");var headers=new[]{"Hallazgo","Evidencia","Interpretación","Acción sugerida","Impacto esperado"};for(int i=0;i<headers.Length;i++)lectura.Cell(1,i+1).Value=headers[i];for(int i=0;i<r.Hallazgos.Count;i++){var h=r.Hallazgos[i];var cells=new[]{h.Titulo,h.Evidencia,h.Interpretacion,h.AccionSugerida,h.ImpactoEsperado};for(int j=0;j<cells.Length;j++)lectura.Cell(i+2,j+1).Value=cells[j];}lectura.Range(1,1,r.Hallazgos.Count+1,5).CreateTable();lectura.Columns().Width=38;lectura.Columns().Style.Alignment.WrapText=true;lectura.Rows().AdjustToContents();}
        BiExcelGrafico.Datos(wb,r);
        foreach(var sheet in wb.Worksheets){sheet.ShowGridLines=false;sheet.Style.Font.FontName="Arial";sheet.Style.Font.FontSize=10;}
        using var stream=new MemoryStream();wb.SaveAs(stream);BiExcelGrafico.Incorporar(stream);
        stream.Position=0;using(var validado=DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Open(stream,false)){if(new DocumentFormat.OpenXml.Validation.OpenXmlValidator().Validate(validado).Any())throw new InvalidOperationException("La estructura del archivo Excel no es válida.");}
        return stream.ToArray();
    }
    public byte[] Pdf(BiResultado r)
    {
        var www=environment.WebRootPath??Path.Combine(environment.ContentRootPath,"wwwroot");
        var logo=new[]{Path.Combine(www,"assets","images","logo-3s.png"),Path.Combine(www,"images","logo-3s.png")}.FirstOrDefault(File.Exists);
        return Document.Create(doc=>doc.Page(page=>
        {
            page.Size(PageSizes.A4);page.Margin(30);page.DefaultTextStyle(s=>s.FontSize(9).FontColor(PdfEstilo3S.Texto));
            page.Header().Element(c=>PdfEstilo3S.Encabezado(c,logo,Titulos[r.Vista],"INFORME BI",$"{r.Filtros.Desde:dd/MM/yyyy} - {r.Filtros.Hasta:dd/MM/yyyy}"));
            page.Footer().Element(c=>PdfEstilo3S.Pie(c,$"Uso de gestión | {r.Filtros.Procedencia} | Generado {r.GeneradoUtc:u}"));
            page.Content().PaddingTop(8).Column(column=>
            {
                column.Spacing(8);
                column.Item().Text($"Procedencia: {(r.Filtros.Procedencia=="referencia"?"SINTÉTICA DE REFERENCIA":"Operaciones del sistema")} | Moneda PEN").SemiBold();
                column.Item().Text(Filtros(r)).FontSize(8);
                if(r.CompararDesde.HasValue)column.Item().Text($"Comparación de igual duración: {r.CompararDesde:dd/MM/yyyy} a {r.CompararHasta:dd/MM/yyyy}").FontSize(8);
                if(r.Vista!="productos"||!r.Filtros.CompararElemento.HasValue)column.Item().EnsureSpace(90).Column(section=>{section.Spacing(5);section.Item().Text("Indicadores principales").SemiBold();foreach(var k in r.Indicadores)section.Item().Text($"{k.Etiqueta}: {BiLectura.Formato(k.Valor,k.Formato)} · {k.Descripcion}").FontSize(8);});
                if(r.Vista=="resumen"||(r.Vista=="productos"&&r.Filtros.CompararElemento.HasValue))column.Item().EnsureSpace(100).Column(section=>{section.Spacing(6);section.Item().Text("Visión ejecutiva").SemiBold();foreach(var h in r.Hallazgos.Take(3))section.Item().ShowEntire().Text($"{h.Titulo}. {h.Evidencia}. {h.Interpretacion} Acción sugerida: {h.AccionSugerida} Impacto esperado: {h.ImpactoEsperado}").FontSize(8);});
                if(r.Datos.TryGetValue("evolucion",out var serie)&&(r.Vista!="productos"||!r.Filtros.CompararElemento.HasValue))
                {
                    foreach(var tipo in r.Vista switch {"compras"=>new[]{"compra"},"clientes"=>new[]{"cotizacion"},"resumen"=>new[]{"venta","compra"},_=>new[]{"venta"}})
                    {
                        var filas=serie.Where(d=>Equals(d["Tipo"],tipo)).TakeLast(60).ToList();
                        if(filas.Count>0){column.Item().EnsureSpace(150).Column(section=>{section.Item().Text($"Evolución de {(tipo=="venta"?"ventas":tipo=="compra"?"compras":"cotizaciones")} por {r.Filtros.Agrupacion}").SemiBold();section.Item().Height(115).Svg(Grafico(filas));});}
                    }
                }
                if(r.Datos.TryGetValue("saldosHistoricos",out var saldos)&&saldos.Count>0){column.Item().EnsureSpace(190).Text("Evolución del saldo promedio").SemiBold();column.Item().Height(150).Svg(Grafico(saldos.TakeLast(60).ToList(),"Promedio","Unidades"));}
                string[] secciones=r.Vista switch {
                    "ventas"=>["evolucionComparacion","clientesIngresos","operaciones"],
                    "productos"=>["comparacionElementos","comparacionMetricas","productosIngresos","productosUnidades","menoresVentas","sinVentas","productosEvolucion","categorias"],
                    "clientes"=>["oportunidadesClientes","clientesSegmentos","clientesIngresos","clientesFrecuencia","estados","operaciones"],
                    "compras"=>["abastecimiento","proveedores","productosComprados","categorias","estados"],
                    "inventario"=>["estadoInventario","inventario","movimientosPeriodo","mayorMovimiento","menorMovimiento","saldosHistoricos","rotacion","kardex"],
                    _=>["categorias","productosIngresos","clientesIngresos","proveedores","estados"]};
                if(r.Vista=="productos"&&r.Filtros.CompararElemento.HasValue)secciones=["comparacionElementos","comparacionMetricas","productosEvolucion"];
                foreach(var key in secciones)
                {
                    if(!DatosVista(r).TryGetValue(key,out var rows)||rows.Count==0)continue;
                    var keys=key=="comparacionElementos"?new[]{"Nombre","Importe","Operaciones","Cantidad","Unidad"}:key=="comparacionMetricas"?new[]{"Indicador","Primero","Segundo","DiferenciaAbsoluta","DiferenciaPorcentual"}:rows.SelectMany(d=>d.Keys).Where(k=>k is not ("Id" or "TotalFilas" or "FechaActualizacion")).Distinct().Take(5).ToArray();
                    column.Item().EnsureSpace(110).Column(section=>
                    {
                      section.Spacing(6);
                      section.Item().Text(Secciones.GetValueOrDefault(key,key)).SemiBold();
                      section.Item().Table(t=>{
                        t.ColumnsDefinition(cols=>{foreach(var _ in keys)cols.RelativeColumn();});
                        t.Header(header=>{foreach(var k in keys)header.Cell().Element(PdfEstilo3S.CabeceraTabla).Text(Etiqueta(k));});
                        foreach(var row in rows.Take(30))foreach(var k in keys)t.Cell().ShowEntire().BorderBottom(0.5f).BorderColor(PdfEstilo3S.Borde).PaddingVertical(4).PaddingHorizontal(6).Text(Texto(row.GetValueOrDefault(k))).FontSize(8);
                      });
                    });
                }
                column.Item().Text($"Tablas paginadas: página {r.Filtros.Pagina}, hasta {r.Filtros.Tamanio} filas. PDF: hasta treinta filas y cinco columnas por tabla; gráficos: últimos sesenta periodos. Excel conserva todos los conjuntos consultados. Los KPI abarcan todo el filtro.").FontSize(8);
                var notas=r.Notas.Where((_,i)=>r.Vista switch {"ventas" or "productos"=>i is 0 or 3 or 4,"compras"=>i is 0 or 1 or 4,"clientes"=>i is 0 or 2 or 4,"inventario"=>i is 3 or 4,_=>true});
                column.Item().ShowEntire().Column(notes=>{notes.Spacing(3);foreach(var note in notas)notes.Item().Text(note).FontSize(8).FontColor(PdfEstilo3S.Secundario);});
            });
        })).GeneratePdf();
    }
    private static string Filtros(BiResultado r)
    {
        var datos=r.Datos.GetValueOrDefault("filtros")?.FirstOrDefault();
        var segundo=r.Filtros.CompararElemento.HasValue?$"; comparado con {r.Datos.GetValueOrDefault("comparacionElementos")?.LastOrDefault()?.GetValueOrDefault("Nombre")}":"";
        return $"Agrupación: {r.Filtros.Agrupacion}; {datos?.GetValueOrDefault("Categoria")}; {datos?.GetValueOrDefault("Elemento")}{segundo}; {datos?.GetValueOrDefault("Cliente")}; {datos?.GetValueOrDefault("Proveedor")}; estado: {r.Filtros.Estado??"todos"}.";
    }
    private static string Grafico(List<Dictionary<string,object?>> rows,string valor="Importe",string unidad="S/")
    {
        var max=Math.Max(1,rows.Max(d=>Convert.ToDecimal(d[valor])));var width=540.0/rows.Count;var b=new StringBuilder("<svg xmlns='http://www.w3.org/2000/svg' width='600' height='175' viewBox='0 0 600 175'><rect width='600' height='175' fill='white'/>");
        for(int i=0;i<rows.Count;i++)
        {
            var height=(double)(Convert.ToDecimal(rows[i][valor])/max)*110;var x=40+i*width;
            b.Append(CultureInfo.InvariantCulture,$"<rect x='{x:F2}' y='{125-height:F2}' width='{Math.Max(1,width-3):F2}' height='{height:F2}' fill='#CF1827'/>");
            if(rows.Count<=12)b.Append(CultureInfo.InvariantCulture,$"<text x='{x:F2}' y='145' font-family='Arial' font-size='8'>{SecurityElement.Escape(Texto(rows[i]["Periodo"]))}</text>");
        }
        b.Append($"<text x='5' y='15' font-family='Arial' font-size='9'>{SecurityElement.Escape(unidad)} {SecurityElement.Escape(max.ToString("N2",Es))}</text></svg>");return b.ToString();
    }
}
