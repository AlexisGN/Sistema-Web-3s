using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Sistema3S.Web.Controllers;
using Sistema3S.Web.Services.Ia;

namespace Sistema3S.Tests.Unit;

[Trait("Category","Unit")]
public class IaCalendarioTests
{
    private static readonly DateOnly Hoy=new(2026,10,15);
    private static JsonObject Prediction(int days=90)=>new(){["sufficient"]=true,
        ["daily"]=new JsonArray(Enumerable.Range(1,days).Select(i=>(JsonNode)new JsonObject
            {["date"]=Hoy.AddDays(i).ToString("yyyy-MM-dd"),["quantity"]=i}).ToArray()),
        ["totals"]=new JsonObject{["30"]=465,["60"]=1830,["90"]=4095}};

    [Theory]
    [InlineData(1,1)] [InlineData(10,55)] [InlineData(30,465)] [InlineData(90,4095)]
    public void FechaInclusivaSumaLaSerieSinCambiarLosTotalesGuardados(int days,int expected)
    {
        var prediction=Prediction();var original=prediction.ToJsonString();
        var p=IaPeriodo.Crear(Hoy.AddDays(days),Hoy.AddDays(-1),Hoy);
        Assert.Equal(expected,IaPeriodo.Cantidad(prediction,p));Assert.Equal(days,p.Dias);
        Assert.Equal(original,prediction.ToJsonString());
    }
    [Fact]
    public void AusenciasYDuplicadosNoSeConviertenEnCeros()
    {
        var period=IaPeriodo.Crear(Hoy.AddDays(10),Hoy.AddDays(-1),Hoy);
        Assert.Null(IaPeriodo.Cantidad(null,period));
        Assert.Null(IaPeriodo.Cantidad(Prediction(9),period));
        var p=Prediction();p["daily"]!.AsArray().RemoveAt(3);Assert.Null(IaPeriodo.Cantidad(p,period));
        p=Prediction();p["daily"]!.AsArray().Add(p["daily"]![0]!.DeepClone());Assert.Null(IaPeriodo.Cantidad(p,period));
        p=Prediction();p["sufficient"]=false;Assert.Null(IaPeriodo.Cantidad(p,period));
    }
    [Fact]
    public void VentasPrevistasCeroSonUnResultadoValido()
    {
        var p=Prediction(1);p["daily"]![0]!["quantity"]=0;
        Assert.Equal(0,IaPeriodo.Cantidad(p,IaPeriodo.Crear(Hoy.AddDays(1),Hoy.AddDays(-1),Hoy)));
    }
    [Theory]
    [InlineData(-1)] [InlineData(0)] [InlineData(91)]
    public void RechazaFechaPasadaHoyOFueraDeLaVentana(int days)
        =>Assert.Throws<ArgumentException>(()=>IaPeriodo.Crear(Hoy.AddDays(days),Hoy.AddDays(-1),Hoy));
    [Fact]
    public void NoExtrapolaUnSnapshotVencido()
    {
        Assert.Throws<ArgumentException>(()=>IaPeriodo.Crear(Hoy.AddDays(90),Hoy.AddDays(-10),Hoy));
        Assert.Equal(Hoy.AddDays(81),IaPeriodo.Crear(null,Hoy.AddDays(-10),Hoy).DisponibleHasta);
    }
    [Fact]
    public void HistoriaAntiguaAcreditadaNoDependeDelAltaDelCatalogo()
    {
        var old=Hoy.AddDays(-150);
        Assert.Equal(old,IaHistoria.Inicio(old,Hoy));
        Assert.Equal(old.AddDays(-10),IaHistoria.Inicio(old,Hoy,old.AddDays(-10),true));
        Assert.Equal(old,IaHistoria.Inicio(old,Hoy,old.AddDays(-10),false));
        Assert.Equal(Hoy.AddDays(-729),IaHistoria.Inicio(Hoy.AddYears(-4),Hoy));
        Assert.Equal(old,IaHistoria.Inicio(old,Hoy.AddDays(1)));
    }
    [Fact]
    public void DiagnosticoExplicaLosMinimosPendientesPorProducto()
    {
        var h=IaHistorial.Leer(new JsonObject{["days"]=70,["events"]=2},Hoy);
        Assert.Equal(20,h.DiasFaltantes);Assert.Equal(2,h.DiasConVentaFaltantes);Assert.Equal(Hoy.AddDays(-69),h.Desde);
        h=IaHistorial.Leer(new JsonObject{["days"]=120,["events"]=8},Hoy);
        Assert.Equal(0,h.DiasFaltantes);Assert.Equal(0,h.DiasConVentaFaltantes);
    }
    [Fact]
    public void InventarioHastaUnaFechaConservaLaPoliticaPredeterminada()
    {
        var policy=new IaPolitica(1,1,3,7,1,1,1,0,0,null,"QA",Hoy.AddDays(30));
        var context=new IaContexto(1,"QA","UND",5,1,10,5,null,null,"Proveedor",policy);
        var daily=Enumerable.Repeat(1m,90).ToArray();
        var original=IaReglas.Riesgo(context,daily,Hoy.AddDays(1),Hoy,true);
        Assert.Equal(6,original.Cantidad);Assert.Equal(Hoy.AddDays(5),original.Agotamiento);
        var selected=IaReglas.Riesgo(context,daily,Hoy.AddDays(1),Hoy,true,0,Hoy.AddDays(3));
        Assert.Equal(0,selected.Cantidad);Assert.Null(selected.Agotamiento);
        Assert.Equal(original,IaReglas.Riesgo(context,daily,Hoy.AddDays(1),Hoy,true));
    }
    [Theory]
    [InlineData("resumen")] [InlineData("pronostico")] [InlineData("proyeccion")]
    [InlineData("inventario")] [InlineData("compras")] [InlineData("seguimiento")]
    public async Task LasSeisVistasRechazanUnaFechaInvalidaAntesDeConsultarSql(string view)
    {
        var controller=new IaController(null!,null!,null!,NullLogger<IaController>.Instance);
        var invalid=IaReglas.Hoy.AddDays(91);
        var response=await (view switch {
            "resumen"=>controller.Resumen(fechaObjetivo:invalid),"pronostico"=>controller.Pronostico(fechaObjetivo:invalid),
            "proyeccion"=>controller.Proyeccion(fechaObjetivo:invalid),"inventario"=>controller.Inventario(fechaObjetivo:invalid),
            "compras"=>controller.Compras(fechaObjetivo:invalid),_=>controller.Seguimiento(fechaObjetivo:invalid)});
        Assert.IsType<BadRequestObjectResult>(response);
    }
}
