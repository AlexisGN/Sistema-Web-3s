using System.Text.Json.Nodes;
namespace Sistema3S.Web.Services.Ia;

public sealed record IaDia(string Date, double Quantity, int Buyers = 0, double? Availability = null);
public sealed record IaSerie(int Id, string Name, string Unit, string Start, List<IaDia> Days);
public sealed record IaIngresoMes(int IdProducto,string Mes,decimal IngresoNeto);
public sealed record IaEntrada(string Source, string Cutoff, List<IaSerie> Products);
public sealed record IaPolitica(int IdProducto, int IdProveedor, int PlazoDias, int CoberturaDias,
 decimal Seguridad, int Minimo, int Multiplo, decimal Reservado, decimal EntradaConfirmada,
 DateOnly? FechaEntrada, string Evidencia, DateOnly VerificadaHasta);
public sealed record IaRevision(string Estado, string Motivo);
public sealed record IaSolicitud(int Cantidad, int IdProveedor, string Justificacion);
public sealed record IaVinculo(int IdCompra);
public sealed record IaRiesgo(int IdProducto, string Nombre, string Unidad, decimal? Stock,
 decimal? CoberturaDias, DateOnly? Agotamiento, string Prioridad, decimal? Cantidad,
 DateOnly? FechaPedido, int? IdProveedor, string Explicacion,decimal ReservaVariabilidad=0,DateOnly? FechaLimitePedido=null,int RetrasoDias=0);
public sealed record IaContexto(int IdProducto, string Nombre, string Unidad, decimal? Stock,
 decimal StockMinimo, decimal? PrecioNeto, decimal? CostoNeto, DateOnly? FechaPrecio,
 DateOnly? FechaCosto, string? Proveedor, IaPolitica? Politica,int? IdProveedor=null);
public sealed record IaInventarioProducto(int IdProducto,string Nombre,string Unidad,IaRiesgo Riesgo,IaPolitica? Politica,string? Proveedor,int? IdProveedor=null);

public static class IaReglas
{
    public static IaRiesgo Riesgo(IaContexto p, IReadOnlyList<decimal> daily, DateOnly first, DateOnly today, bool fresh,double desviacionHistorica=0,DateOnly? fechaObjetivo=null)
    {
        var diasAnalisis=fechaObjetivo.HasValue?fechaObjetivo.Value.DayNumber-first.DayNumber+1:90;
        if(diasAnalisis<1 || diasAnalisis>90)throw new ArgumentException("La fecha de inventario queda fuera de los 90 días disponibles.");
        var politica = p.Politica;
        var valid = politica != null && politica.VerificadaHasta >= today && (politica.EntradaConfirmada==0 || politica.FechaEntrada>today);
        if (p.Stock == null || p.Stock < 0)
            return new(p.IdProducto,p.Nombre,p.Unidad,p.Stock,null,null,"Sin información",null,null,null,"No hay un saldo de inventario verificable.");
        if (daily.Count < diasAnalisis || !fresh)
            return new(p.IdProducto,p.Nombre,p.Unidad,p.Stock,null,null,p.Stock<=p.StockMinimo?"Revisar stock":"Sin estimación",null,null,null,
                "Sin estimación reciente de demanda. El aviso por stock mínimo es una regla de inventario, no un pronóstico entrenado.");
        var stock = p.Stock.Value - (valid ? politica!.Reservado : 0);
        var remaining=stock;
        DateOnly? agotamiento = remaining<=0 ? today : null;
        for(int i=0;i<diasAnalisis;i++)
        {
            var date=first.AddDays(i);
            if(valid && politica!.EntradaConfirmada>0 && date==politica.FechaEntrada)remaining+=politica.EntradaConfirmada;
            remaining-=daily[i];
            if(agotamiento==null && remaining<=0 && daily[i]>0)agotamiento=date;
        }
        var coverage=agotamiento.HasValue?(decimal)(agotamiento.Value.DayNumber-today.DayNumber):(decimal?)null;
        if(!valid)
            return new(p.IdProducto,p.Nombre,p.Unidad,p.Stock,coverage,agotamiento,"Verificar abastecimiento",null,null,p.IdProveedor,
                politica is not null?"Actualiza la vigencia de las condiciones de reposición o la fecha de la entrada confirmada. El proveedor registrado no necesita una segunda verificación.":
                p.IdProveedor.HasValue?"Proveedor identificado en el historial de compras. Faltan las condiciones de reposición: plazo de entrega y días a cubrir. Las reservas y entregas sólo se indican si existen.":
                "Falta asociar un proveedor a este producto y registrar sus condiciones de reposición. Esto no cuestiona la verificación de los proveedores del sistema.");
        var window=fechaObjetivo.HasValue?diasAnalisis:politica!.PlazoDias+politica.CoberturaDias;
        var incoming=politica!.EntradaConfirmada>0 && politica.FechaEntrada>=today && politica.FechaEntrada<=today.AddDays(window)?politica.EntradaConfirmada:0;
        var demand=daily.Take(window).Sum();
        var buffer=(decimal)(Math.Max(0,desviacionHistorica)*Math.Sqrt(politica.PlazoDias));
        var need=Math.Max(0,demand+politica.Seguridad+buffer-(stock+incoming));
        var qty=need>0?Math.Ceiling(Math.Max(need,politica.Minimo)/politica.Multiplo)*politica.Multiplo:0;
        DateOnly? limite=agotamiento?.AddDays(-politica.PlazoDias);
        DateOnly? order=limite.HasValue&&limite.Value<today?today:limite;
        var retraso=limite.HasValue?Math.Max(0,today.DayNumber-limite.Value.DayNumber):0;
        var priority=stock<=0 || limite<=today ? "Urgente" : order<=today.AddDays(7)?"Alta":qty>0?"Planificar":"Cubierto";
        return new(p.IdProducto,p.Nombre,p.Unidad,p.Stock,coverage,agotamiento,priority,qty,order,politica.IdProveedor,
            fechaObjetivo.HasValue?$"Necesidad estimada hasta {fechaObjetivo:dd/MM/yyyy} con abastecimiento verificado. Consulta para revisión; no modifica recomendaciones guardadas ni confirma compras.":
            qty>0?"Reposición estimada con demanda del plazo y cobertura, variabilidad de ventas observadas, reserva de seguridad, existencias y entregas verificadas. Requiere revisión humana; no confirma compras.":
                "La posición verificada cubre la demanda estimada y las reservas del período configurado.",buffer,limite,retraso);
    }
    public static void Validar(IaPolitica p, DateOnly today)
    {
        if(p.IdProducto<=0 || p.IdProveedor<=0 || p.PlazoDias<1 || p.PlazoDias>90 || p.CoberturaDias<1 || p.PlazoDias+p.CoberturaDias>90
            || p.Seguridad<0 || p.Seguridad>1000000 || p.Minimo<1 || p.Multiplo<1 || p.Minimo>1000000 || p.Multiplo>1000000
            || p.Reservado<0 || p.EntradaConfirmada<0 || p.Reservado>1000000 || p.EntradaConfirmada>1000000
            || p.VerificadaHasta<today || p.VerificadaHasta>today.AddDays(90) || string.IsNullOrWhiteSpace(p.Evidencia) || p.Evidencia.Length>500
            || (p.EntradaConfirmada>0 && (!p.FechaEntrada.HasValue || p.FechaEntrada<=today || p.FechaEntrada>today.AddDays(90))))
            throw new ArgumentException("Verifica plazo, cobertura, cantidades, vigencia y evidencia. La ventana no puede superar 90 días.");
    }
    public static DateOnly Hoy => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow,OperatingSystem.IsWindows()?"SA Pacific Standard Time":"America/Lima"));
    public static decimal? Cantidad(JsonNode? product,int horizon)=>product?["totals"]?[horizon.ToString()]?.GetValue<decimal>();
    public static double Desviacion(JsonNode? product){var values=product?["history"]?.AsArray().Select(d=>d!["quantity"]!.GetValue<double>()).ToArray()??[];if(values.Length<2)return 0;var mean=values.Average();return Math.Sqrt(values.Sum(v=>(v-mean)*(v-mean))/(values.Length-1));}
}
