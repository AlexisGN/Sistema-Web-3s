using Microsoft.Data.SqlClient;
using System.Data;
using Sistema3S.Web.Services.Bi;
namespace Sistema3S.Web.Services.Comercial;

public sealed class ComercialService(BiConexion conexion)
{
 public Task<ComercialDetalle> Detalle(string tipo,string segmento,ComercialFiltro filtro,int pagina,CancellationToken ct)=>ComercialDetalleConsulta.Consultar(conexion,tipo,segmento,filtro,pagina,ct);
 public async Task<ComercialResultado> Consultar(string vista,ComercialFiltro f,CancellationToken ct)
 {
  if(vista=="comparacion")f.Comparar=true;f.Validar();if(vista is not ("resumen" or "ventas" or "clientes" or "productos" or "inventario" or "compras" or "comparacion"))throw new ArgumentException("Selecciona una vista válida.");
  await using var c=await conexion.AbrirAsync(ct);await using var cmd=c.CreateCommand();cmd.CommandTimeout=15;
  var b=f.PeriodoB;cmd.CommandText=ComercialConsultas.Preparar+ComercialConsultas.Indicadores;
  List<string> names=["periodos","stock"];
  if(vista is "resumen" or "ventas" or "comparacion"){cmd.CommandText+=ComercialConsultas.Series;names.Add("evolucion");}
  if(vista is "resumen" or "ventas" or "productos" or "comparacion"){cmd.CommandText+=ComercialConsultas.Productos;names.AddRange(["productos","categorias"]);}
  if(vista is "clientes" or "comparacion"){cmd.CommandText+=ComercialConsultas.Clientes;names.Add("clientes");}
  if(vista=="clientes"){cmd.CommandText+=ComercialConsultas.Cotizaciones;names.Add("cotizaciones");}
  if(vista=="inventario"){cmd.CommandText+=ComercialConsultas.Inventario;names.Add("inventario");}
  if(vista=="compras"){cmd.CommandText+=ComercialConsultas.Compras+ComercialConsultas.ComprasIndicadores;names.AddRange(["proveedores","periodosCompras","evolucionCompras"]);}
  foreach(var(k,v)in new(string,object?)[]{("@comparar",f.Comparar),("@base",f.NecesitaBase),("@a",f.Desde.ToDateTime(TimeOnly.MinValue)),("@z",f.Hasta.AddDays(1).ToDateTime(TimeOnly.MinValue)),("@b",b.Desde.ToDateTime(TimeOnly.MinValue)),("@y",b.Hasta.AddDays(1).ToDateTime(TimeOnly.MinValue)),("@hoy",BiReglas.HoyPeru().ToDateTime(TimeOnly.MinValue)),("@d",f.VentanaConversion),("@categoria",f.Categoria),("@elemento",f.Elemento),("@cliente",f.Cliente),("@proveedor",f.Proveedor),("@escenario",f.Escenario),("@umbral",f.Umbral),("@inactividad",f.DiasInactividad),("@minOps",f.MinimoOperaciones),("@importe",f.ImporteMinimo)})cmd.Parameters.AddWithValue(k,v??DBNull.Value);
  var rows=await BiConexion.LeerAsync(cmd,names.ToArray(),ct);
  if(!f.Comparar){
   foreach(var key in new[]{"productos","clientes","categorias"})if(rows.TryGetValue(key,out var detail))foreach(var row in detail){
    if(!f.NecesitaBase){row.Remove("B");row.Remove("Diferencia");row.Remove("Variacion");row.Remove("OperacionesB");}
   }
  }
  rows["diccionario"]=ComercialKpi.Diccionario();
  return new(vista,f,DateTimeOffset.UtcNow,rows,["Ventas confirmadas sin IGV. Las compras son adquisiciones; no representan costo de venta ni utilidad.","Recurrencia: comprador del período con venta confirmada anterior al inicio; el historial previo no se restringe al SKU elegido.",$"Conversión observada a {f.VentanaConversion} días: sólo cotizaciones con ventana completa hasta ayer; incluye emitidas canceladas en el denominador. Es un parámetro exploratorio, no una meta.","Stock actual del catálogo activo inventariable. Sin stock forma parte del conjunto crítico: los porcentajes no se suman.","Se utilizan estados actuales y fechas comerciales. La cobertura del registro no certifica días sin demanda; un día en curso es parcial.","Los filtros comerciales seleccionan el detalle correspondiente; no modifican las cifras globales del período que sirven de contexto."]);
 }
}
