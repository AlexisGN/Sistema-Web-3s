namespace Sistema3S.Web.Services.Ia;

/// <summary>Decisión pura; la agenda, bloqueo y ejecución continúan en IaAdministrador.</summary>
public static class IaAprendizaje
{
 // Revisar diariamente no significa entrenar diariamente. Decidir conserva el mínimo
 // de días y las ventas nuevas; una espera no debe aplazar otra semana su revisión.
 public static int HorasHastaRevision(string tipo,bool error,int inferenciaHoras=24)
  =>error?1:tipo=="Entrenamiento"?24:Math.Clamp(inferenciaHoras,1,168);
 public static string Decidir(string history,string code,string? trained,string? trainedCode,int sales,int? priorSales,DateTime? last,bool drift,bool degraded,int minimumSales=7,int minimumDays=7,bool newRegime=false)
 {
  if(trained==history&&trainedCode==code&&!newRegime&&!degraded)return "SinCambios";
  if(trained==null||trainedCode!=code||last==null)return "Entrenar";
  var age=(DateTime.UtcNow-last.Value).TotalDays;
  if(age<minimumDays)return "SinInformacionNueva";
  var delta=sales-(priorSales??0);
  if(delta>=minimumSales||(delta>0&&(drift||degraded||age>=28))||(delta<=0&&trained!=history)||newRegime||degraded)return "Entrenar";
  return "SinInformacionNueva";
 }
}
