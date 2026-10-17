using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Sistema3S.Tests.Fixtures;
using Sistema3S.Web.Services.Bi;
using Sistema3S.Web.Services.Ia;

namespace Sistema3S.Tests.Integration;

public sealed class IaProcedenciaTests
{
 [Fact(DisplayName="PI-IA-007 | Parámetros de demostración aislados y compatibilidad sin tabla adicional")]
 public async Task PoliticasAisladas(){
  await using var db=await QaDatabase.CreateAsync();await IaTests.Migrar(db);
  var id="a9410140-2026-4100-8100-000000000004";
  await db.Exec($"""
  INSERT bi.LoteReferencia(IdLote,Nombre,Inicio,Fin,Generador,Estado) VALUES('{id}',N'Sólo QA','20250701','20261013','QA','Completo');
  INSERT ia.AbastecimientoReferencia(IdProducto,IdLote,IdProveedor,PlazoDias,CoberturaDias,Seguridad,Minimo,Multiplo,Reservado,EntradaConfirmada,Evidencia,VerificadaHasta)
  VALUES(1,'{id}',1,14,30,3,1,1,0,0,N'DEMOSTRACIÓN QA','20990101');
  """);
  var config=new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:BD_3S",db.Connection}}).Build();
  var data=new IaDatos(new BiConexion(config));
  Assert.Null((await data.Contexto("operativo",default)).Single(p=>p.IdProducto==1).Politica);
  Assert.Equal(14,(await data.Contexto("referencia",default)).Single(p=>p.IdProducto==1).Politica?.PlazoDias);
  Assert.Equal(0,await db.Scalar("SELECT COUNT(*) FROM ia.Abastecimiento"));
  await db.Exec("DROP TABLE ia.AbastecimientoReferencia");
  Assert.Null((await data.Contexto("referencia",default)).Single(p=>p.IdProducto==1).Politica);
 }
 [Fact(DisplayName="PS-IA-002 | Avisos por procedencia exigen permiso de referencia en backend")]
 public async Task AvisosProtegidos(){
  await using var db=await QaDatabase.CreateAsync();await IaTests.Migrar(db);await using var api=await ApiHost.Start(db);
  await ApiTests.Login(api,"compras@example.test");
  await db.Exec("INSERT RolPermiso(IdRol,IdPermiso) SELECT 2,IdPermiso FROM Permiso WHERE Nombre IN('IA_VER','IA_ALERTAS_VER')");
  Assert.Equal(HttpStatusCode.OK,(await api.Http.GetAsync("/api/ia/avisos")).StatusCode);
  Assert.Equal(HttpStatusCode.Forbidden,(await api.Http.GetAsync("/api/ia/avisos?procedencia=referencia")).StatusCode);
  var response=await api.Http.GetAsync("/api/ia/resumen");
  using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
  Assert.False(json.RootElement.TryGetProperty("ingresoNeto",out _));
  Assert.False(json.RootElement.TryGetProperty("precioNeto",out _));
 }
}
