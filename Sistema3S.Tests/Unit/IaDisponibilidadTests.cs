using Sistema3S.Web.Services.Ia;
namespace Sistema3S.Tests.Unit;
[Trait("Category","Unit")]
public class IaDisponibilidadTests
{
 [Fact] public void DiagnosticoDistingueVentasPrecioYCosto()
 {
  Assert.Equal(new[]{"historialVentas","precioVenta","costoCompra"},IaDisponibilidad.Pendientes(null,null,null));
  Assert.Contains("90 días",IaDisponibilidad.Motivo(null,100,50));
  Assert.Contains("venta confirmada",IaDisponibilidad.Motivo(10,null,50));
  Assert.Contains("compra vigente",IaDisponibilidad.Motivo(10,100,null));
  Assert.Contains("incluido hoy",IaDisponibilidad.Motivo(10,100,null));
 }
 [Fact] public void CeroPronosticadoNoEsAusenciaDeInformacion()
 {
  Assert.Empty(IaDisponibilidad.Pendientes(0,100,50));
  Assert.Contains("Datos disponibles",IaDisponibilidad.Motivo(0,100,50));
 }
}
