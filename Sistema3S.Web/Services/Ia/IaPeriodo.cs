using System.Text.Json;
using System.Text.Json.Nodes;

namespace Sistema3S.Web.Services.Ia;

public sealed record IaPeriodo(DateOnly Desde,DateOnly Hasta,int Dias,DateOnly DisponibleDesde,DateOnly DisponibleHasta,bool Personalizado)
{
    public static void ValidarFecha(DateOnly? fecha,DateOnly hoy)
    {
        if(fecha.HasValue && (fecha<=hoy || fecha>hoy.AddDays(90)))
            throw new ArgumentException("Selecciona una fecha futura dentro de los próximos 90 días.");
    }
    public static IaPeriodo Crear(DateOnly? fecha,DateOnly? corte,DateOnly hoy)
    {
        ValidarFecha(fecha,hoy);
        var primera=(corte??hoy.AddDays(-1)).AddDays(2);
        var disponibleDesde=primera>hoy?primera:hoy.AddDays(1);
        var disponibleHasta=primera.AddDays(89);
        if(disponibleHasta>hoy.AddDays(90))disponibleHasta=hoy.AddDays(90);
        if(fecha.HasValue && (fecha<disponibleDesde || fecha>disponibleHasta))
            throw new ArgumentException("La fecha elegida queda fuera del pronóstico guardado. Actualiza el análisis antes de consultar ese período.");
        var hasta=fecha??disponibleHasta;
        return new(primera,hasta,Math.Max(0,hasta.DayNumber-primera.DayNumber+1),disponibleDesde,disponibleHasta,fecha.HasValue);
    }
    public static decimal? Cantidad(JsonNode? producto,IaPeriodo periodo)
    {
        if(producto?["sufficient"]?.GetValue<bool>()!=true || producto["daily"] is not JsonArray days)return null;
        var expected=periodo.Hasta.DayNumber-periodo.Desde.DayNumber+1;
        if(expected<=0)return null;
        var values=new Dictionary<DateOnly,decimal>();
        foreach(var row in days)
        {
            if(row?["date"] is not JsonNode d || !DateOnly.TryParse(d.GetValue<string>(),out var date))return null;
            if(date<periodo.Desde || date>periodo.Hasta)continue;
            if(row?["quantity"] is not JsonNode q)return null;
            var quantity=q.Deserialize<decimal>();
            if(quantity<0 || !values.TryAdd(date,quantity))return null;
        }
        if(values.Count!=expected)return null;
        return values.Values.Sum();
    }
}

public static class IaHistoria
{
    public static DateOnly Inicio(DateOnly primeraVenta,DateOnly corte,DateOnly? primerMovimiento=null,bool referencia=false)
    {
        // Una venta confirmada anterior al alta acredita historia importada; el alta no la recorta.
        var inicio=referencia&&primerMovimiento.HasValue?primerMovimiento.Value:primeraVenta;
        return inicio<corte.AddDays(-729)?corte.AddDays(-729):inicio;
    }
}

public sealed record IaHistorial(int Dias,int DiasConVenta,int DiasFaltantes,int DiasConVentaFaltantes,DateOnly? Desde)
{
    public static IaHistorial Leer(JsonNode? prediction,DateOnly? corte)
    {
        var dias=prediction?["days"]?.GetValue<int>()??0;
        var eventos=prediction?["events"]?.GetValue<int>()??0;
        return new(dias,eventos,Math.Max(0,90-dias),Math.Max(0,4-eventos),corte.HasValue&&dias>0?corte.Value.AddDays(1-dias):null);
    }
}
