using System.Globalization;

namespace Sistema3S.Web.Services.Bi;

// Lectura descriptiva compartida por web y exportaciones. No predice resultados futuros.
public static class BiLectura
{
    private static readonly CultureInfo Es=CultureInfo.GetCultureInfo("es-PE");
    private static List<Dictionary<string,object?>> Filas(BiResultado r,string key)=>r.Datos.GetValueOrDefault(key)??[];
    private static Dictionary<string,object?> Kpi(BiResultado r,string tipo)=>Filas(r,"kpis").FirstOrDefault(x=>Equals(x["Tipo"],tipo))??[];
    private static decimal? Valor(Dictionary<string,object?>? row,string key)=>row?.GetValueOrDefault(key) is { } value?Convert.ToDecimal(value):null;
    private static decimal N(Dictionary<string,object?>? row,string key)=>Valor(row,key)??0;
    public static string Formato(decimal? valor,string tipo)=>valor is null?"No calculable":tipo switch {"moneda"=>"S/ "+valor.Value.ToString("N2",Es),"porcentaje"=>valor.Value.ToString("N1",Es)+" %",_=>valor.Value.ToString("N2",Es).TrimEnd('0').TrimEnd(Es.NumberFormat.NumberDecimalSeparator[0])};

    public static void CompararElementos(Dictionary<string,List<Dictionary<string,object?>>> datos)
    {
        var filas=datos.GetValueOrDefault("comparacionElementos")??[];
        if(filas.Count!=2)return;
        var a=filas[0];var b=filas[1];
        var mismaUnidad=string.Equals(Convert.ToString(a["Unidad"])?.Trim(),Convert.ToString(b["Unidad"])?.Trim(),StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(Convert.ToString(a["Unidad"])) && !Equals(a["Unidad"],"Sin unidad");
        var metricas=new List<Dictionary<string,object?>>();
        foreach(var (campo,nombre,unidad) in new[]{("Importe","Ingresos con IGV","S/"),("Operaciones","Ventas confirmadas","Ventas"),("Cantidad","Cantidad comercializada",Convert.ToString(a["Unidad"])!)})
        {
            var av=N(a,campo);var bv=N(b,campo);var compatible=campo!="Cantidad"||mismaUnidad;
            metricas.Add(new(){["Indicador"]=nombre,["Primero"]=av,["Segundo"]=bv,["Unidad"]=campo=="Cantidad"&&!compatible?"Unidades diferentes":unidad,
                ["DiferenciaAbsoluta"]=compatible?bv-av:null,["DiferenciaPorcentual"]=compatible?BiReglas.Variacion(bv,av):null});
        }
        datos["comparacionMetricas"]=metricas;
    }

    public static IReadOnlyList<BiIndicador> Indicadores(BiResultado r)
    {
        var v=Kpi(r,"venta");var c=Kpi(r,"compra");var q=Kpi(r,"cotizacion");
        var s=Filas(r,"resumenActividad").FirstOrDefault();var inventario=Filas(r,"estadoInventario").FirstOrDefault();
        BiIndicador I(string nombre,Dictionary<string,object?>? fila,string campo,string formato,string texto)=>new(nombre,Valor(fila,campo),formato,texto);
        var dinero=I("Ventas confirmadas",v,"Importe","moneda",$"{N(v,"Operaciones"):N0} ventas · importe con IGV");
        var compras=I("Compras vigentes",c,"Importe","moneda",$"{N(c,"Operaciones"):N0} compras Registrada / Confirmada");
        var quotes=I("Cotizaciones emitidas",q,"Operaciones","numero",$"{Formato(Valor(q,"ConversionPorcentual"),"porcentaje")} convertidas · importe {Formato(Valor(q,"Importe"),"moneda")}");
        var variacion=I("Variación de ventas",v,"VariacionPorcentual","porcentaje",Valor(v,"VariacionPorcentual") is null?"Sin información comparable":"Frente al periodo comparable de igual duración");
        var cantidad=Filas(r,"cantidades").Where(x=>Equals(x["Tipo"],"venta")).ToList();
        var unidades=new BiIndicador("Cantidad vendida",cantidad.Count==1?N(cantidad[0],"Cantidad"):null,"numero",cantidad.Count==1?Convert.ToString(cantidad[0]["Unidad"])!:"Cantidades separadas por unidad en el detalle");
        List<BiIndicador> result=r.Vista switch {
            "resumen"=>[dinero,compras,quotes,I("Clientes compradores",v,"Clientes","numero","Clientes distintos con venta confirmada"),I("Productos sin movimiento",s,"SinMovimiento","numero","Productos del catálogo sin movimiento en el periodo")],
            "ventas"=>[dinero,I("Número de ventas",v,"Operaciones","numero","Una cabecera por operación"),unidades,I("Ticket promedio",v,"TicketPromedio","moneda","Importe / ventas confirmadas")],
            "productos"=>[I("Elementos vendidos",v,"ElementosVendidos","numero","Productos y servicios distintos"),dinero,unidades,I("Sin ventas",s,"SinVentas","numero","Catálogo habilitado sin venta confirmada en el periodo")],
            "clientes"=>[I("Clientes compradores",v,"Clientes","numero","Con ventas confirmadas en el periodo"),I("Compradores recurrentes",s,"ClientesRecurrentes","numero","Con más de una venta en el periodo o compras previas"),quotes,I("Tasa de conversión",q,"ConversionPorcentual","porcentaje","Cotizaciones con venta confirmada vinculada"),I("Importe cotizado",q,"Importe","moneda","Incluye todos los estados de cotización")],
            "compras"=>[compras,I("Número de compras",c,"Operaciones","numero","Compras vigentes del periodo"),I("Proveedores del periodo",c,"Proveedores","numero","Proveedores con compras vigentes"),I("Elementos adquiridos",c,"ElementosVendidos","numero","Productos distintos comprados")],
            "inventario"=>[I("Productos en inventario",inventario,"Productos","numero","Saldo actual a la consulta; cantidades por unidad en detalle"),I("Sin movimiento",Filas(r,"movimientosResumen").FirstOrDefault(),"SinMovimiento","numero","Productos sin movimiento dentro del periodo"),I("Productos sin stock",inventario,"SinStock","numero","Con saldo actual igual a cero"),I("Rotación física",Filas(r,"rotacion").FirstOrDefault(),"Rotacion","numero","Cantidad vendida / promedio diario de saldo del SKU")],
            _=>[]};
        if(r.Vista=="inventario" && r.Filtros.Elemento.HasValue && Filas(r,"inventario").FirstOrDefault() is { } sku){
            var unidad=Convert.ToString(sku.GetValueOrDefault("Unidad"))??"Unidad del producto";
            result[0]=I("Stock actual",sku,"StockActual","numero",unidad+" · saldo actual del producto seleccionado");
            result.Add(I("Entradas del periodo",sku,"Entradas","numero",unidad+" · movimientos registrados en el filtro"));
            result.Add(I("Salidas del periodo",sku,"Salidas","numero",unidad+" · movimientos registrados en el filtro"));
        }
        if(r.CompararDesde.HasValue && r.Vista is "resumen" or "ventas")result.Add(variacion);
        return result;
    }

    public static IReadOnlyList<BiHallazgo> Hallazgos(BiResultado r)
    {
        var findings=new List<BiHallazgo>();var v=Kpi(r,"venta");var q=Kpi(r,"cotizacion");var c=Kpi(r,"compra");
        var elementos=Filas(r,"comparacionElementos");
        if(r.Vista=="productos" && elementos.Count==2)
        {
            var a=elementos[0];var b=elementos[1];
            foreach(var (campo,nombre,formato) in new[]{("Importe","Ingresos","moneda"),("Operaciones","Ventas confirmadas","numero")})
            {
                var av=N(a,campo);var bv=N(b,campo);var ganador=av>bv?a:b;
                var lectura=av==bv?"Ambos elementos tienen el mismo resultado.":$"{ganador["Nombre"]} presenta el mayor resultado en este indicador.";
                findings.Add(new(nombre,$"A · {a["Nombre"]}: {Formato(av,formato)}; B · {b["Nombre"]}: {Formato(bv,formato)}",lectura,
                    "Revisar la evolución y el volumen antes de priorizar un elemento.","Orientar la atención comercial; mayores ingresos no demuestran mayor rentabilidad."));
            }
            var cantidad=Filas(r,"comparacionMetricas").Last();
            findings.Add(cantidad["DiferenciaAbsoluta"] is null
                ? new("Unidades diferentes",$"A: {a["Unidad"]}; B: {b["Unidad"]}","Las cantidades se presentan por separado; no se calcula una diferencia física ni un porcentaje entre unidades incompatibles.","Comparar ingresos y número de ventas o elegir elementos con la misma unidad.","Evitar conclusiones basadas en magnitudes incompatibles.")
                : new("Volumen comercializado",$"A: {Formato(N(a,"Cantidad"),"numero")}; B: {Formato(N(b,"Cantidad"),"numero")} {a["Unidad"]}",N(a,"Cantidad")==N(b,"Cantidad")?"Ambos elementos tienen el mismo volumen.":$"{(N(a,"Cantidad")>N(b,"Cantidad")?a:b)["Nombre"]} tiene mayor cantidad comercializada.","Evaluar demanda y disponibilidad junto con los ingresos.","Sustentar la priorización comercial con unidades compatibles."));
            return findings;
        }
        var categoria=Filas(r,"categorias").FirstOrDefault(x=>Equals(x["Tipo"],"venta"));
        var cliente=Filas(r,"clientesIngresos").FirstOrDefault();var proveedor=Filas(r,"proveedores").FirstOrDefault();var actividad=Filas(r,"resumenActividad").FirstOrDefault();
        void Add(string titulo,string evidencia,string interpretacion,string accion,string impacto)=>findings.Add(new(titulo,evidencia,interpretacion,accion,impacto));
        if(r.CompararDesde.HasValue && Valor(v,"VariacionPorcentual") is { } cambio && N(v,"Operaciones")>=3)
            Add(cambio>=0?"Las ventas aumentaron":"Las ventas disminuyeron",$"Variación {Formato(cambio,"porcentaje")} · diferencia {Formato(Valor(v,"VariacionAbsoluta"),"moneda")}","El cambio describe dos periodos de igual duración; no identifica por sí solo una causa.","Revisar productos, clientes y cantidades que explican la diferencia.","Enfocar el seguimiento comercial en variaciones sustentadas.");
        if(categoria is not null && N(v,"Operaciones")>=3)
            Add("Categoría con mayor participación",$"{categoria["Nombre"]}: {Formato(Valor(categoria,"Participacion"),"porcentaje")} de ventas","Esta categoría concentra la mayor parte de los ingresos del filtro.","Evaluar disponibilidad y continuidad de los productos principales.","Priorizar la planificación comercial y de abastecimiento.");
        if(cliente is not null && N(v,"Importe")>0 && N(v,"Operaciones")>=3){var porcentaje=N(cliente,"Importe")/N(v,"Importe")*100;
            Add("Participación del principal comprador",$"{cliente["Nombre"]}: {Formato(porcentaje,"porcentaje")} de ventas","La participación permite observar dependencia comercial, sin atribuir riesgo de impago.","Revisar recurrencia y atención del cliente, junto con oportunidades en otros compradores.","Sustentar decisiones de seguimiento y diversificación comercial.");}
        if(proveedor is not null && N(c,"Operaciones")>=3)
            Add("Concentración de compras",$"{proveedor["Nombre"]}: {Formato(Valor(proveedor,"Participacion"),"porcentaje")} de compras","La concentración identifica el proveedor de mayor participación en adquisiciones.","Evaluar plazos, alternativas y condiciones de suministro.","Mejorar la planificación y continuidad del abastecimiento.");
        if(N(actividad,"SinMovimiento")>0)
            Add("Productos sin movimiento",$"{N(actividad,"SinMovimiento"):N0} productos sin movimiento entre {r.Filtros.Desde:dd/MM/yyyy} y {r.Filtros.Hasta:dd/MM/yyyy}","Ausencia de movimiento no implica obsolescencia ni exceso de stock.","Revisar demanda, existencias y vigencia antes de reponer.","Apoyar decisiones de inventario sin suponer ahorros cuantificados.");
        if(N(q,"Operaciones")>=3)
            Add("Seguimiento de cotizaciones",$"{N(q,"Convertidas"):N0} de {N(q,"Operaciones"):N0} convertidas · {Formato(Valor(q,"ConversionPorcentual"),"porcentaje")}","La conversión se comprueba con una venta confirmada vinculada; incluye conversiones posteriores al periodo.","Revisar cotizaciones pendientes y causas documentadas de cierre.","Ordenar el seguimiento de oportunidades comerciales.");
        if(findings.Count==0)Add("Información limitada",$"{N(v,"Operaciones"):N0} ventas y {N(q,"Operaciones"):N0} cotizaciones en el filtro","La muestra no permite una lectura gerencial concluyente.","Ampliar el periodo o revisar los filtros.","Evitar decisiones sustentadas en una muestra insuficiente.");
        return findings.Take(6).ToList();
    }
}
