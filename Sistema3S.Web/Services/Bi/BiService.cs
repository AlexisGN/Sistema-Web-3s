using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Sistema3S.Web.DTOs.Bi;

namespace Sistema3S.Web.Services.Bi;

public sealed class BiService(BiConexion conexiones, ILogger<BiService> logger) : IBiService
{
    public async Task<BiResultado> ConsultarAsync(string vista,BiFiltro f,CancellationToken ct)
    {
        f.Validar(BiReglas.HoyPeru());
        if(!BiConsultas.Vistas.Contains(vista))throw new ArgumentException("Selecciona un análisis válido.");
        var prev=f.Comparacion();var watch=Stopwatch.StartNew();
        await using var conn=await conexiones.AbrirAsync(ct);
        await using var cmd=new SqlCommand(){Connection=conn,CommandTimeout=30};
        Parametros(cmd,f);
        BiConexion.Parametro(cmd,"@tipoVista",SqlDbType.VarChar,vista switch {"ventas" or "productos"=>"venta","compras"=>"compra","clientes"=>"cotizacion",_=>null},12);
        string[] nombres;
        if(vista=="inventario") {cmd.CommandText=BiInventarioConsulta.Sql(f);nombres=["estadoInventario","inventario","mayorMovimiento","movimientosPeriodo","menorMovimiento","kardex","saldosHistoricos","rotacion","movimientosResumen"];}
        else
        {
            cmd.CommandText=BiConsultas.Preparar(f)+BiConsultas.Kpis+BiConsultas.Series(f)+BiConsultas.Rankings+BiConsultas.SinVenta(f)+BiConsultas.Clientes+BiConsultas.Operaciones+BiConsultas.EvolucionProductos(f)+BiConsultas.Sintesis(f)+BiConsultas.ComparacionElementos+BiConsultas.Desgloses;
            nombres=["kpis","comparacion","cantidades","evolucion","evolucionComparacion","estados","categorias","productosIngresos","productosUnidades","menoresVentas","clientesIngresos","clientesFrecuencia","proveedores","productosComprados","sinVentas","clientesAltas","clientesSegmentos","operaciones","productosEvolucion","estadoInventario","resumenActividad","comparacionElementos","cantidadesElementos","oportunidadesClientes","abastecimiento"];
        }
        cmd.CommandText+="\n"+BiConsultas.DescripcionFiltros;nombres=[..nombres,"filtros"];
        var datos=await BiConexion.LeerAsync(cmd,nombres,ct);
        AñadirCalculos(datos,f.ComparacionActiva);
        if(f.CompararElemento.HasValue && vista!="inventario" && datos["comparacionElementos"].Count!=2)throw new ArgumentException("Selecciona dos productos o servicios existentes.");
        BiLectura.CompararElementos(datos);
        logger.LogInformation("BI {Vista} {Procedencia}: {DuracionMs} ms, {Conjuntos} conjuntos; lotes SQL analíticos: 1, excluye autorización y auditoría",vista,f.Procedencia,watch.Elapsed.TotalMilliseconds,datos.Count);
        var resultado=new BiResultado(vista,f,f.ComparacionActiva?prev.Desde:null,f.ComparacionActiva?prev.Hasta:null,DateTimeOffset.UtcNow,"PEN","America/Lima",datos,
         ["Importes en soles: total incluye IGV; base neta e IGV se muestran por separado.",
          "Compras vigentes: Registrada y Confirmada. El sistema registra adquisiciones como Registrada; no se consideran ventas ni compras anuladas en los KPI vigentes.",
          "La conversión mide cotizaciones del periodo con una venta confirmada vinculada, hasta la consulta; no se infiere sólo del nombre de estado.",
          "Cantidades separadas por unidad de medida; stock actual a la consulta, evolución según el kardex registrado.",
          f.Procedencia=="referencia"?"Información sintética de referencia. No constituye ventas, pagos, comprobantes ni existencias reales.":"Operaciones persistidas del sistema. Los registros anteriores no incluyen una certificación de procedencia auténtica."]);
        return resultado with {Indicadores=BiLectura.Indicadores(resultado),Hallazgos=BiLectura.Hallazgos(resultado)};
    }
    private static void AñadirCalculos(Dictionary<string,List<Dictionary<string,object?>>> datos,bool comparar)
    {
        if(!datos.TryGetValue("kpis",out var kpis))return;
        foreach(var tipo in new[]{"venta","compra","cotizacion"})
            if(!kpis.Any(k=>Equals(k["Tipo"],tipo))) kpis.Add(new(){["Tipo"]=tipo,["Operaciones"]=0L,["Importe"]=0m,["Base"]=0m,["Igv"]=0m,["Clientes"]=0,["Proveedores"]=0,["Convertidas"]=0,["OperacionesSinDetalleValido"]=0});
        var prev=datos["comparacion"];
        foreach(var k in kpis)
        {
            var p=prev.FirstOrDefault(r=>Equals(r["Tipo"],k["Tipo"]));
            decimal actual=Convert.ToDecimal(k["Importe"]),anterior=p==null?0:Convert.ToDecimal(p["Importe"]);
            k["Anterior"]=comparar?anterior:null;k["VariacionAbsoluta"]=comparar?actual-anterior:null;k["VariacionPorcentual"]=comparar?BiReglas.Variacion(actual,anterior):null;
            k["TicketPromedio"]=BiReglas.Cociente(actual,Convert.ToDecimal(k["Operaciones"]));
            k["ElementosVendidos"]=datos["cantidades"].Where(r=>Equals(r["Tipo"],k["Tipo"])).Sum(r=>Convert.ToInt32(r["Elementos"]));
            k["ConversionPorcentual"]=Equals(k["Tipo"],"cotizacion")?BiReglas.Cociente(Convert.ToDecimal(k["Convertidas"])*100,Convert.ToDecimal(k["Operaciones"])):null;
        }
        var orden=new[]{"venta","compra","cotizacion"};kpis.Sort((a,b)=>Array.IndexOf(orden,Convert.ToString(a["Tipo"])).CompareTo(Array.IndexOf(orden,Convert.ToString(b["Tipo"]))));
        foreach(var grupo in new[]{"categorias","proveedores"})
        {
            foreach(var row in datos[grupo])
            {
                var tipo=grupo=="categorias"?row["Tipo"]:"compra";
                var total=Convert.ToDecimal(kpis.Single(k=>Equals(k["Tipo"],tipo))["Importe"]);
                row["Participacion"]=BiReglas.Cociente(Convert.ToDecimal(row["Importe"])*100,total);
            }
        }
    }
    internal static void Parametros(SqlCommand cmd,BiFiltro f)
    {
        var p=f.Comparacion();
        BiConexion.Parametro(cmd,"@comparar",SqlDbType.Bit,f.ComparacionActiva);
        BiConexion.Parametro(cmd,"@desde",SqlDbType.DateTime2,f.Desde.ToDateTime(TimeOnly.MinValue));
        BiConexion.Parametro(cmd,"@hasta",SqlDbType.DateTime2,f.Hasta.AddDays(1).ToDateTime(TimeOnly.MinValue));
        BiConexion.Parametro(cmd,"@compararDesde",SqlDbType.DateTime2,p.Desde.ToDateTime(TimeOnly.MinValue));
        BiConexion.Parametro(cmd,"@compararHasta",SqlDbType.DateTime2,p.Hasta.AddDays(1).ToDateTime(TimeOnly.MinValue));
        BiConexion.Parametro(cmd,"@procedencia",SqlDbType.VarChar,f.Procedencia,12);
        BiConexion.Parametro(cmd,"@categoria",SqlDbType.Int,f.Categoria);BiConexion.Parametro(cmd,"@elemento",SqlDbType.Int,f.Elemento);
        BiConexion.Parametro(cmd,"@compararElemento",SqlDbType.Int,f.CompararElemento);
        BiConexion.Parametro(cmd,"@cliente",SqlDbType.Int,f.Cliente);BiConexion.Parametro(cmd,"@proveedor",SqlDbType.Int,f.Proveedor);
        BiConexion.Parametro(cmd,"@estado",SqlDbType.NVarChar,f.Estado,50);
        BiConexion.Parametro(cmd,"@offset",SqlDbType.Int,(f.Pagina-1)*f.Tamanio);BiConexion.Parametro(cmd,"@tamanio",SqlDbType.Int,f.Tamanio);
    }
    public async Task<Dictionary<string,List<Dictionary<string,object?>>>> OpcionesAsync(string procedencia,string? buscar,CancellationToken ct)
    {
        if(procedencia is not ("operativo" or "referencia"))throw new ArgumentException("Procedencia no válida.");
        if(buscar?.Length>100)throw new ArgumentException("La búsqueda debe tener hasta 100 caracteres.");
        var palabras=(buscar??"").Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        // Sólo los nombres de parámetros se generan; cada palabra permanece como dato literal.
        var coincidencia=palabras.Length==0?"1=1":string.Join(" AND ",palabras.Select((_,i)=>$"Nombre COLLATE Latin1_General_100_CI_AI LIKE @palabra{i} ESCAPE N'~'"));
        await using var conn=await conexiones.AbrirAsync(ct);await using var cmd=new SqlCommand($"""
          SELECT TOP(100) IdCategoria Id,Nombre FROM dbo.Categoria WHERE Estado=1 AND {coincidencia} ORDER BY Nombre;
          SELECT TOP(100) IdElementoCatalogo Id,Nombre,TipoElemento FROM bi.DimElemento WHERE Estado=1 AND {coincidencia} ORDER BY Nombre;
          SELECT TOP(100) IdCliente Id,Nombre FROM bi.DimCliente WHERE Procedencia=@procedencia AND {coincidencia} ORDER BY Nombre;
          SELECT TOP(100) IdProveedor Id,Nombre FROM bi.DimProveedor WHERE Procedencia=@procedencia AND {coincidencia} ORDER BY Nombre;
          """,conn){CommandTimeout=15};
        BiConexion.Parametro(cmd,"@procedencia",SqlDbType.VarChar,procedencia,12);
        for(var i=0;i<palabras.Length;i++)
        {
            var literal=palabras[i].Replace("~","~~").Replace("%","~%").Replace("_","~_").Replace("[","~[").Replace("]","~]");
            BiConexion.Parametro(cmd,$"@palabra{i}",SqlDbType.NVarChar,"%"+literal+"%",220);
        }
        return await BiConexion.LeerAsync(cmd,["categorias","elementos","clientes","proveedores"],ct);
    }
    public async Task<List<Dictionary<string,object?>>> DetalleAsync(string tipo,int id,CancellationToken ct)
    {
        var tablas=new Dictionary<string,string>{{"venta","Venta"},{"compra","Compra"},{"cotizacion","Cotizacion"}};
        if(!tablas.TryGetValue(tipo,out var tabla)||id<=0)throw new ArgumentException("Selecciona una operación válida.");
        await using var conn=await conexiones.AbrirAsync(ct);
        await using var cmd=new SqlCommand($"SELECT TOP(100) f.IdElementoCatalogo Id,f.Fecha,f.Estado,e.Nombre,e.Unidad,f.Cantidad,CAST(f.Total AS decimal(18,2)) Importe,CAST(f.Base AS decimal(18,2)) Base,CAST(f.Igv AS decimal(18,2)) Igv FROM bi.Fact{tabla} f LEFT JOIN bi.DimElemento e ON e.IdElementoCatalogo=f.IdElementoCatalogo WHERE f.Procedencia='operativo' AND f.IdOperacion=@id ORDER BY e.Nombre",conn){CommandTimeout=15};
        BiConexion.Parametro(cmd,"@id",SqlDbType.Int,id);
        return (await BiConexion.LeerAsync(cmd,["detalle"],ct))["detalle"];
    }
}
