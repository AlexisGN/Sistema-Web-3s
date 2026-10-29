using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Globalization;
using System.Security;
using System.Text;

namespace Sistema3S.Web.Services.Bi;

// Reutiliza OpenXML, dependencia de ClosedXML; el gráfico sigue siendo editable en Excel.
internal static class BiExcelGrafico
{
    internal static void Datos(XLWorkbook wb,BiResultado r)
    {
        var sheet=wb.Worksheets.Add("Gráfico");sheet.Position=2;
        sheet.Cell(1,1).Value=r.Vista=="inventario"?"Movimientos de inventario (número de registros)":"Evolución comercial (S/)";
        sheet.Range(1,1,1,8).Merge().Style.Font.SetBold().Font.SetFontSize(16);
        sheet.Cell(19,1).Value="Periodo";
        var nombres=r.Vista switch {"inventario"=>new[]{"Entrada","Salida","Ajuste","Apertura"},"ventas" or "productos"=>["venta"],"clientes"=>["cotizacion"],"compras"=>["compra"],_=>["venta","compra","cotizacion"]};
        for(int i=0;i<nombres.Length;i++)sheet.Cell(19,i+2).Value=nombres[i] switch {"venta"=>"Ventas","compra"=>"Compras","cotizacion"=>"Cotizaciones",_=>nombres[i]};
        var rows=r.Datos.GetValueOrDefault(r.Vista=="inventario"?"movimientosPeriodo":"evolucion")??[];
        var periodos=rows.Select(d=>Convert.ToDateTime(d["Periodo"])).Distinct().Order().ToArray();
        for(int i=0;i<periodos.Length;i++)
        {
            sheet.Cell(i+20,1).Value=periodos[i];sheet.Cell(i+20,1).Style.NumberFormat.Format="dd/mm/yyyy";
            for(int j=0;j<nombres.Length;j++)sheet.Cell(i+20,j+2).Value=rows.Where(d=>Convert.ToDateTime(d["Periodo"])==periodos[i]&&Equals(d["Tipo"],nombres[j])).Sum(d=>Convert.ToDecimal(d[r.Vista=="inventario"?"Movimientos":"Importe"]));
        }
        sheet.Columns(1,nombres.Length+1).Width=20;sheet.Range(19,1,19,nombres.Length+1).Style.Fill.BackgroundColor=XLColor.FromHtml("#193149");sheet.Range(19,1,19,nombres.Length+1).Style.Font.FontColor=XLColor.White;
        sheet.Row(19).Height=24;sheet.Range(19,1,19,nombres.Length+1).Style.Font.Bold=true;
        if(periodos.Length>0){sheet.Range(20,2,periodos.Length+19,nombres.Length+1).Style.NumberFormat.Format=r.Vista=="inventario"?"#,##0":"#,##0.00";sheet.Rows(20,periodos.Length+19).Height=22;}
        sheet.SheetView.FreezeRows(19);
    }
    internal static void Incorporar(MemoryStream stream)
    {
        using var doc=SpreadsheetDocument.Open(stream,true);
        var book=doc.WorkbookPart!;var sheet=book.Workbook.Sheets!.Elements<Sheet>().Single(s=>s.Name=="Gráfico");
        var part=(WorksheetPart)book.GetPartById(sheet.Id!);
        var rows=part.Worksheet.GetFirstChild<SheetData>()!.Elements<Row>().Where(r=>r.RowIndex?.Value>=20).ToArray();
        if(rows.Length==0)return;
        var headers=part.Worksheet.GetFirstChild<SheetData>()!.Elements<Row>().Single(r=>r.RowIndex?.Value==19).Elements<Cell>().Skip(1).ToArray();
        string Value(Cell cell)=>cell.DataType?.Value==CellValues.SharedString?book.SharedStringTablePart!.SharedStringTable.Elements<SharedStringItem>().ElementAt(int.Parse(cell.CellValue!.Text)).InnerText:cell.InnerText;
        var drawing=part.AddNewPart<DrawingsPart>();var chart=drawing.AddNewPart<ChartPart>();
        var series=new StringBuilder();
        for(int j=0;j<headers.Length;j++)
        {
            string col=((char)('B'+j)).ToString();
            var categories=string.Join("",rows.Select((r,i)=>$"<c:pt idx='{i}'><c:v>{DateTime.FromOADate(double.Parse(Value(r.Elements<Cell>().First()),CultureInfo.InvariantCulture)):dd/MM/yyyy}</c:v></c:pt>"));
            var values=string.Join("",rows.Select((r,i)=>$"<c:pt idx='{i}'><c:v>{r.Elements<Cell>().ElementAt(j+1).CellValue!.Text}</c:v></c:pt>"));
            series.Append($"<c:ser><c:idx val='{j}'/><c:order val='{j}'/><c:tx><c:v>{SecurityElement.Escape(Value(headers[j]))}</c:v></c:tx><c:cat><c:strRef><c:f>'Gráfico'!$A$20:$A${rows.Length+19}</c:f><c:strCache><c:ptCount val='{rows.Length}'/>{categories}</c:strCache></c:strRef></c:cat><c:val><c:numRef><c:f>'Gráfico'!${col}$20:${col}${rows.Length+19}</c:f><c:numCache><c:formatCode>#,##0.00</c:formatCode><c:ptCount val='{rows.Length}'/>{values}</c:numCache></c:numRef></c:val></c:ser>");
        }
        string chartXml=$"<c:chartSpace xmlns:c='http://schemas.openxmlformats.org/drawingml/2006/chart' xmlns:a='http://schemas.openxmlformats.org/drawingml/2006/main'><c:lang val='es-PE'/><c:chart><c:autoTitleDeleted val='1'/><c:plotArea><c:layout/><c:barChart><c:barDir val='col'/><c:grouping val='clustered'/>{series}<c:axId val='1'/><c:axId val='2'/></c:barChart><c:catAx><c:axId val='1'/><c:scaling><c:orientation val='minMax'/></c:scaling><c:axPos val='b'/><c:crossAx val='2'/><c:crosses val='autoZero'/></c:catAx><c:valAx><c:axId val='2'/><c:scaling><c:orientation val='minMax'/></c:scaling><c:axPos val='l'/><c:majorGridlines/><c:numFmt formatCode='#,##0' sourceLinked='0'/><c:crossAx val='1'/><c:crosses val='autoZero'/></c:valAx></c:plotArea><c:legend><c:legendPos val='b'/><c:layout/></c:legend><c:plotVisOnly val='1'/></c:chart></c:chartSpace>";
        string drawingXml=$"<xdr:wsDr xmlns:xdr='http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing' xmlns:a='http://schemas.openxmlformats.org/drawingml/2006/main'><xdr:twoCellAnchor><xdr:from><xdr:col>0</xdr:col><xdr:colOff>0</xdr:colOff><xdr:row>2</xdr:row><xdr:rowOff>0</xdr:rowOff></xdr:from><xdr:to><xdr:col>8</xdr:col><xdr:colOff>0</xdr:colOff><xdr:row>17</xdr:row><xdr:rowOff>0</xdr:rowOff></xdr:to><xdr:graphicFrame macro=''><xdr:nvGraphicFramePr><xdr:cNvPr id='2' name='Evolución BI'/><xdr:cNvGraphicFramePr/></xdr:nvGraphicFramePr><xdr:xfrm><a:off x='0' y='0'/><a:ext cx='0' cy='0'/></xdr:xfrm><a:graphic><a:graphicData uri='http://schemas.openxmlformats.org/drawingml/2006/chart'><c:chart xmlns:c='http://schemas.openxmlformats.org/drawingml/2006/chart' xmlns:r='http://schemas.openxmlformats.org/officeDocument/2006/relationships' r:id='{drawing.GetIdOfPart(chart)}'/></a:graphicData></a:graphic></xdr:graphicFrame><xdr:clientData/></xdr:twoCellAnchor></xdr:wsDr>";
        using(var input=new MemoryStream(Encoding.UTF8.GetBytes(chartXml)))chart.FeedData(input);
        using(var input=new MemoryStream(Encoding.UTF8.GetBytes(drawingXml)))drawing.FeedData(input);
        // AddChild respeta el orden del esquema; Append colocaba drawing después de extLst.
        part.Worksheet.AddChild(new Drawing{Id=part.GetIdOfPart(drawing)},true);part.Worksheet.Save();
    }
}
