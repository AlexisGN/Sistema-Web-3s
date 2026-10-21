using Sistema3S.Web.Services.Ia;
using System.Text.Json.Nodes;
namespace Sistema3S.Tests.Unit;
public sealed class IaAprendizajeTests
{
 [Fact] public void PrimeraEjecucionEntrenaYCorteSinVentasNoRepite(){Assert.Equal("Entrenar",IaAprendizaje.Decidir("a","v",null,null,20,null,null,false,false));Assert.Equal("SinCambios",IaAprendizaje.Decidir("a","v","a","v",20,20,DateTime.UtcNow.AddDays(-30),false,false));}
 [Fact] public void VentasNuevasEsperanInformacionYPeriodoSuficientes(){Assert.Equal("SinInformacionNueva",IaAprendizaje.Decidir("b","v","a","v",22,20,DateTime.UtcNow.AddDays(-8),false,false));Assert.Equal("Entrenar",IaAprendizaje.Decidir("b","v","a","v",27,20,DateTime.UtcNow.AddDays(-8),false,false));Assert.Equal("SinInformacionNueva",IaAprendizaje.Decidir("b","v","a","v",27,20,DateTime.UtcNow.AddDays(-1),true,true));}
 [Fact] public void HistoriaRectificadaYCambioSostenidoSeEvalua(){Assert.Equal("Entrenar",IaAprendizaje.Decidir("b","v","a","v",20,20,DateTime.UtcNow.AddDays(-8),false,false));Assert.Equal("Entrenar",IaAprendizaje.Decidir("b","v","a","v",21,20,DateTime.UtcNow.AddDays(-8),true,false));Assert.Equal("Entrenar",IaAprendizaje.Decidir("b","v2","a","v1",20,20,DateTime.UtcNow,false,false));}
 [Fact] public void ExperimentalNuncaOriginaCompras(){var r=new JsonObject{["version"]="3s-ia-7",["published"]=true,["experimental"]=true};Assert.False(IaPublicacion.Confiable(r,new JsonObject{["accepted"]=true}));var m=IaPublicacionTests.Validado();m["version"]="3s-ia-7";m["experimental"]=true;Assert.False(IaPublicacion.Aceptable(m));Assert.False(IaPublicacion.Mejora(m,IaPublicacionTests.Validado()));}
 [Fact] public void CambioSostenidoSinVentasNuevasYDegradacionSeEvaluaSinRepetir(){Assert.Equal("Entrenar",IaAprendizaje.Decidir("a","v","a","v",20,20,DateTime.UtcNow.AddDays(-8),true,false,newRegime:true));Assert.Equal("Entrenar",IaAprendizaje.Decidir("a","v","a","v",20,20,DateTime.UtcNow.AddDays(-8),false,true));Assert.Equal("SinCambios",IaAprendizaje.Decidir("a","v","a","v",20,20,DateTime.UtcNow.AddDays(-8),true,false));}
 [Fact] public void RevisionDiariaNoEquivaleAReentrenamientoDiario(){
  Assert.Equal(24,IaAprendizaje.HorasHastaRevision("Entrenamiento",false));
  Assert.Equal("SinInformacionNueva",IaAprendizaje.Decidir("nuevo","v","anterior","v",27,20,DateTime.UtcNow.AddDays(-6),false,false));
  Assert.Equal("Entrenar",IaAprendizaje.Decidir("nuevo","v","anterior","v",27,20,DateTime.UtcNow.AddDays(-8),false,false));
  Assert.Equal("SinCambios",IaAprendizaje.Decidir("igual","v","igual","v",27,27,DateTime.UtcNow.AddDays(-8),false,false));
 }
 [Fact] public void RetirarUnaVentaDelHistorialTambienRequiereAprender(){
  Assert.Equal("Entrenar",IaAprendizaje.Decidir("rectificado","v","antes","v",19,20,DateTime.UtcNow.AddDays(-8),false,false));
  Assert.Equal("SinInformacionNueva",IaAprendizaje.Decidir("rectificado","v","antes","v",19,20,DateTime.UtcNow.AddDays(-1),false,false));
 }
 [Theory][InlineData("Inferencia",false,12,12)][InlineData("Inferencia",false,0,1)][InlineData("Inferencia",false,500,168)][InlineData("Entrenamiento",true,24,1)][InlineData("Inferencia",true,24,1)]
 public void RevisionRespetaReintentoYAjustes(string tipo,bool error,int horas,int esperado)=>Assert.Equal(esperado,IaAprendizaje.HorasHastaRevision(tipo,error,horas));
}
