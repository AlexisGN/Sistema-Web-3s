namespace Sistema3S.Web.Services.Comercial;

/// <summary>Definición común para consulta, detalle y exportaciones. No define metas empresariales.</summary>
public static class ComercialKpi
{
 public static List<Dictionary<string,object?>> Diccionario()=>[
  new(){["Id"]="crecimiento",["Objetivo"]="Aumentar ventas",["Nombre"]="Crecimiento de ventas confirmadas",["Formula"]="100 × (VentaNeta A − VentaNeta B) / VentaNeta B; base B positiva",["Fuente"]="Venta.Subtotal y EstadoVenta Confirmada; FactVenta.Base en filtros de línea",["Unidad"]="% y PEN sin IGV",["Meta"]=null,["Naturaleza"]="Flujo por FechaVenta",["Accion"]="Revisar operaciones, ticket y contribución de clientes/productos"},
  new(){["Id"]="conversion",["Objetivo"]="Aumentar ventas",["Nombre"]="Conversión observada a D días",["Formula"]="100 × cotizaciones maduras con venta Confirmada dentro de D / emitidas maduras",["Fuente"]="Cotizacion.FechaCotizacion; Venta.IdCotizacion o Cotizacion.IdVentaGenerada y Venta.FechaVenta",["Unidad"]="% de documentos",["Meta"]=null,["Naturaleza"]="Cohorte con ventana cerrada hasta ayer; estados actuales",["Accion"]="Revisar seguimiento de propuestas y tiempos de cierre"},
  new(){["Id"]="recurrencia",["Objetivo"]="Aumentar ventas",["Nombre"]="Compradores recurrentes",["Formula"]="100 × compradores de P con venta Confirmada anterior al inicio / compradores de P",["Fuente"]="Venta.IdCliente, FechaVenta y EstadoVenta Confirmada",["Unidad"]="% de compradores",["Meta"]=null,["Naturaleza"]="Flujo con historia previa del cliente",["Accion"]="Revisar continuidad, captación y evolución por cuenta"},
  new(){["Id"]="sinStock",["Objetivo"]="Evitar desabastecimiento",["Nombre"]="Productos activos sin stock",["Formula"]="100 × productos activos inventariables con saldo cero / productos activos inventariables",["Fuente"]="Producto.AplicaInventario; ElementoCatalogo.Estado; Inventario.StockActual",["Unidad"]="% de productos",["Meta"]=null,["Naturaleza"]="Fotografía actual; los saldos desconocidos se señalan",["Accion"]="Verificar disponibilidad y compromisos de abastecimiento"},
  new(){["Id"]="stockCritico",["Objetivo"]="Evitar desabastecimiento",["Nombre"]="Productos con stock crítico",["Formula"]="100 × productos activos inventariables con 0 ≤ stock ≤ mínimo / productos activos inventariables",["Fuente"]="Inventario.StockActual, StockMinimo; catálogo activo inventariable",["Unidad"]="% de productos",["Meta"]=null,["Naturaleza"]="Fotografía actual; incluye productos sin stock",["Accion"]="Priorizar revisión de inventario y planificación de reposición"}
 ];
}
