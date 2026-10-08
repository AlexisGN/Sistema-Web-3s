using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Net.Http.Json;
using Sistema3S.Tests.Fixtures;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Sistema3S.Web.Services.Implementations;

namespace Sistema3S.Tests.Integration;
[Trait("Category","Integration")]
public class CorreoTests
{
    [Fact(DisplayName="PI-MAIL-003 | Endpoint de cotización envía PDF y termina con respuesta")]
    public async Task CotizacionPorCorreo(){await using var q=await QaDatabase.CreateAsync();using var smtp=new SmtpProbe();await using var api=await ApiHost.Start(q,smtp.Config);await ApiTests.Login(api);var cot=await ApiTests.Json(await api.Http.PostAsJsonAsync("/api/cotizacion",new{idCliente=1,origenCotizacion="Manual",detalles=new[]{new{idElementoCatalogo=1,cantidad=2,precioUnitario=118}}}));int id=cot.GetProperty("idCotizacion").GetInt32();var receive=smtp.Receive();var r=await ApiTests.Json(await api.Http.PostAsJsonAsync($"/api/cotizacion/{id}/enviar-correo",new{idUsuarioAtencion=1}));Assert.Contains("correctamente",r.ToString());var mime=await receive;Assert.Equal(1,smtp.Messages);Assert.Contains("application/pdf",mime);Assert.Contains(".pdf",mime);Assert.True(await q.Scalar("SELECT COUNT(*) FROM EnvioCorreo WHERE IdCotizacion="+id)>0);}
    [Fact(DisplayName="PI-MAIL-001 | SMTP local recibe destinatario, cuerpo y PDF exacto")]
    public async Task Adjunto(){using var smtp=new SmtpProbe();var path=Path.Combine(Path.GetTempPath(),"qa-"+Guid.NewGuid()+".pdf");try{QuestPDF.Settings.License=LicenseType.Community;var bytes=Document.Create(c=>c.Page(p=>p.Content().Text("PRUEBA QA 3S - sin valor comercial"))).GeneratePdf();await File.WriteAllBytesAsync(path,bytes);var receive=smtp.Receive();await new EmailService(smtp.Config).EnviarConAdjuntoAsync("cliente@example.test","PRUEBA QA 3S","Adjuntamos PDF ficticio.",path);var mime=await receive;Assert.Contains("RCPT TO:<cliente@example.test>",smtp.Envelope,StringComparison.OrdinalIgnoreCase);Assert.Contains("application/pdf",mime);Assert.Contains(Path.GetFileName(path),mime);Assert.Contains(Convert.ToBase64String(bytes),string.Concat(mime.Split('\n').Select(x=>x.Trim())));Assert.Equal(1,smtp.Messages);}finally{File.Delete(path);}}
    [Fact(DisplayName="PI-MAIL-002 | Rechazo SMTP produce error y no afirma envío")]
    public async Task Rechazo(){using var smtp=new SmtpProbe();var receive=smtp.Receive(true);var e=await Assert.ThrowsAsync<InvalidOperationException>(()=>new EmailService(smtp.Config).EnviarConAdjuntoAsync("cliente@example.test","QA","QA",null));await receive;Assert.Contains("SMTP",e.Message);Assert.Equal(0,smtp.Messages);}
    [Fact(DisplayName="PU-MAIL-001 | PDF inexistente se rechaza antes de conectar")]
    [Trait("Category","Unit")]
    public async Task Archivo(){using var smtp=new SmtpProbe();await Assert.ThrowsAsync<FileNotFoundException>(()=>new EmailService(smtp.Config).EnviarConAdjuntoAsync("cliente@example.test","QA","QA",Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".pdf")));Assert.Equal(0,smtp.Messages);}
    private sealed class SmtpProbe:IDisposable
    {
        private readonly TcpListener server=new(IPAddress.Loopback,0);public string Envelope="";public int Messages;
        public SmtpProbe(){server.Start();}
        public IConfiguration Config=>new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Smtp:Host","127.0.0.1"},{"Smtp:Port",((IPEndPoint)server.LocalEndpoint).Port.ToString()},{"Smtp:User","qa"},{"Smtp:Password","only-qa"},{"Smtp:From","qa@example.test"},{"Smtp:EnableSsl","false"}}).Build();
        public async Task<string> Receive(bool reject=false){using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(20));using var client=await server.AcceptTcpClientAsync(timeout.Token);using var stream=client.GetStream();using var reader=new StreamReader(stream);using var writer=new StreamWriter(stream,Encoding.ASCII){AutoFlush=true,NewLine="\r\n"};await writer.WriteLineAsync("220 QA SMTP");var body=new StringBuilder();while(await reader.ReadLineAsync(timeout.Token) is { } line){Envelope+=line+"\n";if(line.StartsWith("EHLO",StringComparison.OrdinalIgnoreCase)||line.StartsWith("HELO",StringComparison.OrdinalIgnoreCase))await writer.WriteLineAsync("250 QA");else if(line.StartsWith("MAIL",StringComparison.OrdinalIgnoreCase)){await writer.WriteLineAsync(reject?"550 QA rejected":"250 OK");if(reject)return "";}else if(line.StartsWith("RCPT",StringComparison.OrdinalIgnoreCase))await writer.WriteLineAsync("250 OK");else if(line=="DATA"){await writer.WriteLineAsync("354 End with dot");while(await reader.ReadLineAsync(timeout.Token) is { } data && data!=".")body.AppendLine(data);Messages++;await writer.WriteLineAsync("250 Accepted");}else if(line=="QUIT"){await writer.WriteLineAsync("221 Bye");break;}else await writer.WriteLineAsync("250 OK");}return body.ToString();}
        public void Dispose()=>server.Stop();
    }
}
