import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { VentaService } from './venta';
import { CompraService } from './compra';
import { CotizacionService } from './cotizacion';
import { environment } from '../../../environments/environment';

describe('Selectores comerciales de entidades activas', () => {
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({providers: [provideHttpClient(), provideHttpClientTesting()]});
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('Ventas solicita sólo clientes activos al cargar el selector', () => {
    TestBed.inject(VentaService).listarClientes().subscribe();
    const req = http.expectOne(r => r.url === `${environment.apiUrl}/cliente`);
    expect(req.request.params.get('soloActivos')).toBe('true');
    expect(req.request.params.get('pagina')).toBe('1');
    expect(req.request.params.get('tamanioPagina')).toBe('10');
    expect(req.request.params.has('buscar')).toBe(false);
    req.flush({items: [], totalRegistros: 0});
  });

  it('Buscar clientes conserva el filtro activo y la paginación', () => {
    TestBed.inject(VentaService).listarClientes('  Cliente QA  ', 2, 3).subscribe();
    const req = http.expectOne(r => r.url === `${environment.apiUrl}/cliente`);
    expect(req.request.params.get('soloActivos')).toBe('true');
    expect(req.request.params.get('buscar')).toBe('Cliente QA');
    expect(req.request.params.get('pagina')).toBe('2');
    expect(req.request.params.get('tamanioPagina')).toBe('3');
    req.flush({items: [], totalRegistros: 0});
  });

  it('Compras solicita sólo proveedores activos al cargar el selector', () => {
    TestBed.inject(CompraService).listarProveedores().subscribe();
    const req = http.expectOne(r => r.url === `${environment.apiUrl}/proveedor`);
    expect(req.request.params.get('soloActivos')).toBe('true');
    expect(req.request.params.get('pagina')).toBe('1');
    expect(req.request.params.get('tamanioPagina')).toBe('10');
    req.flush({items: [], totalRegistros: 0});
  });

  it('Buscar proveedores conserva el filtro activo y la paginación', () => {
    TestBed.inject(CompraService).listarProveedores('  20000000001  ', 2, 3).subscribe();
    const req = http.expectOne(r => r.url === `${environment.apiUrl}/proveedor`);
    expect(req.request.params.get('soloActivos')).toBe('true');
    expect(req.request.params.get('buscar')).toBe('20000000001');
    expect(req.request.params.get('pagina')).toBe('2');
    expect(req.request.params.get('tamanioPagina')).toBe('3');
    req.flush({items: [], totalRegistros: 0});
  });

  it('El historial de ventas no recibe el filtro de clientes activos', () => {
    TestBed.inject(VentaService).listar('Histórico QA').subscribe();
    const req = http.expectOne(r => r.url === `${environment.apiUrl}/venta`);
    expect(req.request.params.has('soloActivos')).toBe(false);
    expect(req.request.params.get('buscar')).toBe('Histórico QA');
    req.flush({items: [], totalRegistros: 0});
  });

  it('El historial de compras no recibe el filtro de proveedores activos', () => {
    TestBed.inject(CompraService).listar('Histórico QA').subscribe();
    const req = http.expectOne(r => r.url === `${environment.apiUrl}/compra`);
    expect(req.request.params.has('soloActivos')).toBe(false);
    expect(req.request.params.get('buscar')).toBe('Histórico QA');
    req.flush({items: [], totalRegistros: 0});
  });
  for (const [modulo, endpoint] of [['venta', 'producto'], ['venta', 'servicio'], ['compra', 'producto']]) {
    for (const buscar of ['', '  Elemento QA  ']) {
      it(`${modulo}: ${endpoint} solicita activos antes de paginar, búsqueda '${buscar}'`, () => {
        const api = modulo === 'venta' ? TestBed.inject(VentaService) : TestBed.inject(CompraService);
        if (endpoint === 'servicio') {
          (api as VentaService).listarServicios(buscar, 2, 3).subscribe();
        } else if (modulo === 'venta') {
          (api as VentaService).listarProductos(buscar, 2, 3).subscribe();
        } else {
          (api as CompraService).listarProductos(buscar, 2, 3).subscribe();
        }
        const req = http.expectOne(r => r.url === `${environment.apiUrl}/${endpoint}`);
        expect(req.request.params.get('estado')).toBe('true');
        expect(req.request.params.get('pagina')).toBe('2');
        expect(req.request.params.get('tamanioPagina')).toBe('3');
        expect(req.request.params.get('buscar')).toBe(buscar.trim() || null);
        req.flush({items: [], totalRegistros: 0});
      });
    }
  }

  it('Cotizaciones usa sus selectores dedicados que filtran activos en el servidor', () => {
    const api = TestBed.inject(CotizacionService);
    api.listarClientes().subscribe();
    api.listarElementosCotizables().subscribe();
    http.expectOne(`${environment.apiUrl}/cotizacion/clientes`).flush([]);
    http.expectOne(`${environment.apiUrl}/cotizacion/elementos-cotizables`).flush([]);
  });
});
