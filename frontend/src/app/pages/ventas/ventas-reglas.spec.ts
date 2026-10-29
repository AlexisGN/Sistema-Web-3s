import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { of, Subject } from 'rxjs';
import { vi } from 'vitest';
import { VentasComponent } from './ventas';
import { VentaService } from '../../core/services/venta';
import { SessionService } from '../../core/services/session.service';

describe('Ventas: comprobantes por cliente y servicio de una unidad', () => {
  let api: any;
  const empresa = {idCliente: 2, numeroDocumento: '20111111111', tipoDocumento: 'RUC', razonSocial: 'Empresa QA'};
  const persona = {idCliente: 1, numeroDocumento: '00000001', tipoDocumento: 'DNI', nombres: 'Persona QA'};
  const servicio = {idElementoCatalogo: 2, idServicio: 1, nombre: 'Servicio QA', precioVenta: 100};
  const producto = {idElementoCatalogo: 1, idProducto: 1, nombre: 'Producto QA', precioVenta: 50, stockActual: 10};
  const crear = () => TestBed.runInInjectionContext(() => new VentasComponent(api, {} as any, {detectChanges: vi.fn()} as any));

  beforeEach(() => {
    api = {registrar: vi.fn(() => new Subject()), obtenerSiguienteComprobante: vi.fn(() => of({serie: 'B001', numero: '000001'}))};
    TestBed.configureTestingModule({providers: [
      {provide: SessionService, useValue: {tienePermiso: () => true, esAdministrador: () => false, obtenerUsuario: () => ({idUsuario: 1})}},
      {provide: VentaService, useValue: api}, {provide: ActivatedRoute, useValue: {}}
    ]});
  });
  afterEach(() => vi.restoreAllMocks());

  it('Empresa ofrece Factura y Boleta y retira una Nota de venta seleccionada previamente', () => {
    const c = crear();
    c.seleccionarCliente(persona);
    c.venta.tipoComprobante = 'Nota de venta'; c.venta.serie = 'NV01'; c.venta.numero = '000001';
    c.seleccionarCliente(empresa);
    expect(c.tiposComprobanteDisponibles()).toEqual(['Factura', 'Boleta']);
    expect(c.venta.tipoComprobante).toBe(''); expect(c.venta.serie).toBe(''); expect(c.venta.numero).toBe('');
  });

  it('DNI conserva Boleta y Nota de venta, sin habilitar Factura', () => {
    const c = crear(); c.seleccionarCliente(empresa); c.venta.tipoComprobante = 'Factura';
    c.seleccionarCliente(persona);
    expect(c.tiposComprobanteDisponibles()).toEqual(['Boleta', 'Nota de venta']);
    expect(c.venta.tipoComprobante).toBe('');
  });

  it('Empresa rechaza Nota de venta sin solicitar numeración ni guardar', () => {
    const c = crear(); c.seleccionarCliente(empresa); c.venta.tipoComprobante = 'Nota de venta';
    c.cambiarTipoComprobante();
    expect(c.erroresCampo['tipoComprobante']).toContain('Factura o Boleta');
    expect(api.obtenerSiguienteComprobante).not.toHaveBeenCalled();
    expect(api.registrar).not.toHaveBeenCalled();
  });

  it('Servicio se agrega con una unidad y el siguiente servicio sigue en una unidad', () => {
    const c = crear(); c.seleccionarTipoElemento('Servicio'); c.seleccionarElemento(servicio);
    expect(c.detalleFormulario.cantidad).toBe(1);
    c.agregarDetalle();
    expect(c.venta.detalles[0].cantidad).toBe(1);
    expect(c.calcularTotal()).toBe(100); expect(c.venta.montoPagado).toBe(100);
    expect(c.detalleFormulario.cantidad).toBe(1);
    c.venta.detalles[0].cantidad = 5; c.cambiarCantidadDetalleVenta(0);
    expect(c.venta.detalles[0].cantidad).toBe(1); expect(c.calcularTotal()).toBe(100);
  });

  it('Un servicio manipulado no se agrega con otra cantidad', () => {
    const c = crear(); c.seleccionarTipoElemento('Servicio'); c.seleccionarElemento(servicio);
    c.detalleFormulario.cantidad = 2; c.agregarDetalle();
    expect(c.venta.detalles).toHaveLength(0);
    expect(c.erroresCampo['cantidadDetalle']).toContain('fija de 1');
  });

  it('Productos conservan cantidad editable, validación de stock y recálculo de cobro', () => {
    const c = crear(); c.seleccionarTipoElemento('Servicio'); c.seleccionarTipoElemento('Producto');
    expect(c.detalleFormulario.cantidad).toBeNull();
    c.seleccionarElemento(producto); c.detalleFormulario.cantidad = 3; c.agregarDetalle();
    expect(c.venta.detalles[0].cantidad).toBe(3); expect(c.calcularTotal()).toBe(150);
    c.venta.detalles[0].cantidad = 4; c.cambiarCantidadDetalleVenta(0);
    expect(c.calcularTotal()).toBe(200); expect(c.venta.montoPagado).toBe(200);
    c.seleccionarElemento({...producto, idElementoCatalogo: 3}); c.detalleFormulario.cantidad = 11;
    c.agregarDetalle(); expect(c.venta.detalles).toHaveLength(1); expect(c.erroresCampo['cantidadDetalle']).toContain('stock');
  });

  it('Cotización carga servicio con una unidad, precio intacto e importe visible para revisión', () => {
    const c = crear();
    (c as any).aplicarCotizacionEnFormularioVenta({idCotizacion: 5, idCliente: 2, numeroDocumentoCliente: empresa.numeroDocumento,
      clienteNombre: 'Empresa QA', detalles: [
        {idElementoCatalogo: 2, tipoElemento: 'Servicio', cantidad: 3, precioUnitario: 100},
        {idElementoCatalogo: 1, tipoElemento: 'Producto', cantidad: 2, precioUnitario: 50}
      ]}, 5);
    expect(c.venta.detalles.map(d => d.cantidad)).toEqual([1, 2]);
    expect(c.venta.detalles[0].precioUnitario).toBe(100); expect(c.calcularTotal()).toBe(200);
    expect(c.venta.montoPagado).toBe(200); expect(c.mensaje).toContain('revisa los importes');
    expect(c.venta.tipoComprobante).toBe('Factura'); expect(api.registrar).not.toHaveBeenCalled();
  });

  it('Una cantidad de servicio alterada en el detalle impide enviar la venta', () => {
    const c = crear(); c.seleccionarCliente(persona);
    Object.assign(c.venta, {tipoComprobante: 'Boleta', serie: 'B001', numero: '000001', metodoPago: 'Efectivo', montoPagado: 200,
      detalles: [{idElementoCatalogo: 2, cantidad: 2, precioUnitario: 100, tipoElemento: 'Servicio'}]});
    c.guardarVenta(); expect(api.registrar).not.toHaveBeenCalled();
    expect(c.erroresCampo['detalles']).toContain('fija de 1');
  });

  it('La plantilla bloquea cantidad de servicios y mantiene editable la de productos', async () => {
    vi.spyOn(VentasComponent.prototype, 'ngOnInit').mockImplementation(() => {});
    const fixture = TestBed.createComponent(VentasComponent); const c = fixture.componentInstance;
    fixture.detectChanges(); await fixture.whenStable();
    c.seleccionarTipoElemento('Servicio'); c.seleccionarElemento(servicio);
    fixture.detectChanges(); await fixture.whenStable();
    const cantidad = () => fixture.nativeElement.querySelector('input[name="cantidadDetalle"]') as HTMLInputElement;
    expect(cantidad().readOnly).toBe(true); expect(cantidad().value).toBe('1');
    c.agregarDetalle(); fixture.detectChanges(); await fixture.whenStable();
    const servicioCantidad = fixture.nativeElement.querySelectorAll('input[aria-label="Cantidad"]')[0] as HTMLInputElement;
    expect(servicioCantidad.readOnly).toBe(true); expect(servicioCantidad.value).toBe('1');
    c.seleccionarTipoElemento('Producto'); c.seleccionarElemento(producto); c.detalleFormulario.cantidad = 3;
    fixture.detectChanges(); await fixture.whenStable(); expect(cantidad().readOnly).toBe(false);
    c.agregarDetalle(); fixture.detectChanges(); await fixture.whenStable();
    const productoCantidad = fixture.nativeElement.querySelectorAll('input[aria-label="Cantidad"]')[1] as HTMLInputElement;
    expect(productoCantidad.readOnly).toBe(false); expect(productoCantidad.value).toBe('3');
    fixture.destroy();
  });
});
