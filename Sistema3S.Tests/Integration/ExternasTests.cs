using System.Net;
using Microsoft.Extensions.Configuration;
using Sistema3S.Tests.Fixtures;
using Sistema3S.Web.Services.Implementations;

namespace Sistema3S.Tests.Integration;
[Trait("Category","Integration")]
public class ExternasTests
{
    [Theory]
    [InlineData("PI-REN-001","dni","ok",true)]
    [InlineData("PI-REN-002","dni","null",false)]
    [InlineData("PI-REN-003","dni","http",false)]
    [InlineData("PI-REN-004","dni","timeout",false)]
    [InlineData("PI-REN-005","dni","malformed",false)]
    [InlineData("PI-SUN-001","ruc","ok",true)]
    [InlineData("PI-SUN-002","ruc","null",false)]
    [InlineData("PI-SUN-003","ruc","http",false)]
    public async Task RespuestaControlada(string id,string tipo,string modo,bool esperado){await using var q=await QaDatabase.CreateAsync();using var db=q.Open();var h=new ExternalHandler(tipo,modo);using var http=new HttpClient(h);var s=new ConsultaDocumentoService(db,http,q.Config);bool real=tipo=="dni"?(await s.ConsultarDniAsync("00000002",1)).Exitoso:(await s.ConsultarRucAsync("20000000002",1)).Exitoso;Assert.True(esperado==real,$"{id}: esperado={esperado}, obtenido={real}");Assert.Equal(1,h.Calls);Assert.True(h.Bearer);Assert.Equal(1,await q.Scalar("SELECT COUNT(*) FROM ConsultaExterna"));Assert.Equal(esperado?1:0,await q.Scalar("SELECT CAST(Exitoso as int) FROM ConsultaExterna"));}
    [Fact(DisplayName="PI-SUN-004 | Respuesta RUC sin razón social debe rechazarse")]
    public async Task RucIncompleto(){await using var q=await QaDatabase.CreateAsync();using var db=q.Open();using var h=new HttpClient(new ExternalHandler("ruc","incomplete"));var r=await new ConsultaDocumentoService(db,h,q.Config).ConsultarRucAsync("20000000002",1);Assert.False(r.Exitoso,"SUNAT devolvió sólo número de documento y el servicio lo marcó como exitoso sin razón social.");}
    private sealed class ExternalHandler(string tipo,string modo):HttpMessageHandler
    {
        public int Calls;public bool Bearer;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken c){Calls++;Bearer=r.Headers.Authorization?.Scheme=="Bearer";if(modo=="timeout")throw new TaskCanceledException("Timeout simulado QA");var json=modo switch{"null"=>"null","malformed"=>"{","incomplete"=>"{\"numero_documento\":\"20000000002\"}",_=>tipo=="dni"?"{\"document_number\":\"00000002\",\"first_name\":\"Persona QA\",\"first_last_name\":\"Prueba\",\"second_last_name\":\"Sintética\",\"full_name\":\"Persona QA Prueba Sintética\"}":"{\"numero_documento\":\"20000000002\",\"razon_social\":\"Empresa QA\",\"estado\":\"ACTIVO\",\"condicion\":\"HABIDO\",\"direccion\":\"QA\"}"};return Task.FromResult(new HttpResponseMessage(modo=="http"?HttpStatusCode.ServiceUnavailable:HttpStatusCode.OK){Content=new StringContent(json)});}
    }
}
