namespace Sistema3S.Web.Services.Ia;

public static class IaDisponibilidad
{
 public static string[] Pendientes(decimal? cantidad,decimal? precio,decimal? costo)
 {
  var result=new List<string>();
  if(cantidad is null)result.Add("historialVentas");
  if(precio is null)result.Add("precioVenta");
  if(costo is null)result.Add("costoCompra");
  return result.ToArray();
 }
 public static string Motivo(decimal? cantidad,decimal? precio,decimal? costo)
 {
  if(cantidad is null)return "Revisa el historial del producto en Pronóstico de ventas: necesita 90 días de historia y ventas confirmadas en al menos cuatro fechas.";
  if(precio is null)return "Revisa una venta confirmada de este producto con precio válido dentro de los últimos 90 días, incluido hoy.";
  if(costo is null)return "Revisa una compra vigente de este producto con cantidad y costo válidos en el historial, incluido hoy. Primero se usan compras recientes y, si no hay, la última compra válida. Sin ese costo no se calcula el margen.";
  return "Datos disponibles para revisar ingresos y margen bruto. El margen no incluye gastos operativos.";
 }
}
