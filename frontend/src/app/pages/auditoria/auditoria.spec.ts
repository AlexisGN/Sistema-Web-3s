import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { AuditoriaComponent } from './auditoria';

describe('Carga de auditoría', () => {
  function setup() {
    TestBed.configureTestingModule({ imports: [AuditoriaComponent], providers: [provideHttpClient(), provideHttpClientTesting()] });
    const fixture = TestBed.createComponent(AuditoriaComponent);
    return { fixture, c: fixture.componentInstance, http: TestBed.inject(HttpTestingController) };
  }
  afterEach(() => TestBed.resetTestingModule());
  const actividad = { id: 1, fecha: '2026-10-23T10:00:00', responsable: 'Responsable QA', modulo: 'Compras', titulo: 'Registró una compra', resultado: 'completado', resumen: 'Un cambio', detalles: [{ registro: 'Compra n.º 1', accion: 'Registrado', cambios: [{ campo: 'Total', antes: 'Sin dato', despues: 'S/ 118,00' }] }] };

  it('carga opciones y primera página de forma independiente y permite revisar los cambios', async () => {
    const { fixture, c, http } = setup();
    fixture.detectChanges();
    const options = http.expectOne(r => r.url.endsWith('/auditoria/opciones'));
    const list = http.expectOne(r => r.url.endsWith('/auditoria'));
    expect(list.request.params.get('pagina')).toBe('1');
    expect(c.cargando).toBe(true);
    list.flush({ items: [actividad], total: 21, tamanoPagina: 20 });
    await fixture.whenStable();
    expect(c.paginas).toBe(2);
    expect(fixture.nativeElement.textContent).toContain('Responsable QA');
    expect(c.cargando).toBe(false);
    const changes = Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>).find(b => b.textContent?.includes('Ver cambios'))!;
    changes.click();
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('S/ 118,00');
    options.flush({ modulos: ['Compras'], responsables: [{ id: 1, nombre: 'Responsable QA', correo: 'qa@example.test' }] });
    http.verify();
  });

  it('cancela la búsqueda anterior y conserva filtros y paginación del servidor', () => {
    const { c, http } = setup();
    c.cargar();
    const old = http.expectOne(r => r.url.endsWith('/auditoria'));
    c.modulo = 'Ventas'; c.usuario = 2; c.resultado = 'denegado'; c.desde = '2026-10-01'; c.hasta = '2026-10-23'; c.pagina = 8;
    c.buscar();
    expect(old.cancelled).toBe(true);
    const query = http.expectOne(r => r.url.endsWith('/auditoria'));
    expect(query.request.params.get('pagina')).toBe('1');
    expect(query.request.params.get('modulo')).toBe('Ventas');
    expect(query.request.params.get('idUsuario')).toBe('2');
    expect(query.request.params.get('resultado')).toBe('denegado');
    expect(query.request.params.get('desde')).toBe('2026-10-01');
    expect(query.request.params.get('hasta')).toBe('2026-10-23');
    query.flush({ items: [], total: 21, tamanoPagina: 20 });
    c.cambiarPagina(2);
    const page = http.expectOne(r => r.params.get('pagina') === '2');
    page.flush({ items: [actividad], total: 21, tamanoPagina: 20 });
    c.cambiarPagina(0); c.cambiarPagina(3);
    http.expectNone(r => r.url.endsWith('/auditoria'));
    http.verify();
  });

  it('un rango inválido cancela la carga pendiente y no deja la pantalla esperando', () => {
    const { c, http } = setup();
    c.cargar();
    const query = http.expectOne(r => r.url.endsWith('/auditoria'));
    c.desde = '2026-10-23'; c.hasta = '2026-10-01';
    c.buscar();
    expect(query.cancelled).toBe(true);
    expect(c.cargando).toBe(false);
    expect(c.error).toContain('fecha inicial');
    http.expectNone(r => r.url.endsWith('/auditoria'));
    http.verify();
  });

  it('un fallo de opciones no impide ver la actividad y permite reintentar sólo las opciones', () => {
    const { c, http } = setup();
    c.ngOnInit();
    http.expectOne(r => r.url.endsWith('/auditoria/opciones')).flush({}, { status: 503, statusText: 'No disponible' });
    http.expectOne(r => r.url.endsWith('/auditoria')).flush({ items: [actividad], total: 1, tamanoPagina: 20 });
    expect(c.items.length).toBe(1);
    expect(c.error).toBe('');
    expect(c.errorOpciones).not.toBe('');
    c.cargarOpciones();
    http.expectOne(r => r.url.endsWith('/auditoria/opciones')).flush({ modulos: ['Compras'], responsables: [] });
    expect(c.errorOpciones).toBe('');
    http.verify();
  });

  it('un error de carga no se presenta como ausencia de operaciones y se recupera al reintentar', async () => {
    const { fixture, c, http } = setup();
    fixture.detectChanges();
    http.expectOne(r => r.url.endsWith('/auditoria/opciones')).flush({ modulos: [], responsables: [] });
    http.expectOne(r => r.url.endsWith('/auditoria')).flush({ mensaje: 'Consulta no disponible.' }, { status: 503, statusText: 'No disponible' });
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('.activity-summary').textContent).toContain('No se pudo consultar');
    expect(fixture.nativeElement.querySelector('.activity-summary').textContent).not.toContain('0 operaciones');
    c.cargar();
    http.expectOne(r => r.url.endsWith('/auditoria')).flush({ items: [actividad], total: 1, tamanoPagina: 20 });
    expect(c.error).toBe('');
    expect(c.cargando).toBe(false);
    http.verify();
  });

  it('al salir cancela ambas solicitudes en curso', () => {
    const { c, http } = setup();
    c.ngOnInit();
    const options = http.expectOne(r => r.url.endsWith('/auditoria/opciones'));
    const list = http.expectOne(r => r.url.endsWith('/auditoria'));
    c.ngOnDestroy();
    expect(options.cancelled).toBe(true);
    expect(list.cancelled).toBe(true);
    http.verify();
  });
});
