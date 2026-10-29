import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { vi } from 'vitest';
import { DashboardComponent } from './dashboard';
import { ProductoService } from '../../core/services/producto';
import { ServicioService } from '../../core/services/servicio';
import { CotizacionService } from '../../core/services/cotizacion';
import { ProveedorService } from '../../core/services/proveedor';
import { InventarioService } from '../../core/services/inventario';
import { ClienteService } from '../../core/services/cliente';
import { CompraService } from '../../core/services/compra';
import { VentaService } from '../../core/services/venta';
import { CajaService } from '../../core/services/caja.service';
import { SessionService } from '../../core/services/session.service';

describe('Inicio: alertas sólo cuando hay operaciones que mostrar', () => {
  const permisos = new Set<string>();
  const inventario = new Subject<any>();

  beforeEach(async () => {
    permisos.clear();
    // La prueba controla las respuestas; nunca consulta una base de datos.
    vi.spyOn(DashboardComponent.prototype, 'ngOnInit').mockImplementation(() => {});
    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
        provideRouter([]),
        ...[ProductoService, ServicioService, CotizacionService, ProveedorService,
          ClienteService, CompraService, VentaService, CajaService].map(provide => ({ provide, useValue: {} })),
        { provide: InventarioService, useValue: { resumen: () => inventario } },
        { provide: SessionService, useValue: { tienePermiso: (permiso: string) => permisos.has(permiso) } }
      ]
    }).compileComponents();
  });

  afterEach(() => vi.restoreAllMocks());

  it('no muestra panel ni avisos vacíos, aunque el usuario tenga permisos', () => {
    ['INVENTARIO_VER', 'INVENTARIO_REPORTE', 'COTIZACIONES_VER', 'COMPRAS_VER'].forEach(p => permisos.add(p));
    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.changeDetectorRef.markForCheck(); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.alert-panel')).toBeNull();
    expect(fixture.nativeElement.querySelector('.dashboard-layout.sin-alertas')).not.toBeNull();
  });

  it('muestra stock bajo al recibir pendientes reales y lo retira al resolverlos', () => {
    permisos.add('INVENTARIO_VER');
    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.changeDetectorRef.markForCheck(); fixture.detectChanges();
    fixture.componentInstance.cargarResumenInventario();
    inventario.next({ totalStockBajo: 2, totalSinStock: 1 });
    fixture.changeDetectorRef.markForCheck(); fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.alert-list s3s-aviso')).toHaveLength(1);
    expect(fixture.nativeElement.querySelector('.alert-list').textContent).toContain('3 productos');
    inventario.next({ totalStockBajo: 0, totalSinStock: 0 });
    fixture.changeDetectorRef.markForCheck(); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.alert-panel')).toBeNull();
  });

  it('conserva los enlaces de pendientes y oculta datos sin permiso', () => {
    permisos.add('COTIZACIONES_VER');
    permisos.add('COMPRAS_VER');
    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.componentInstance.cards = fixture.componentInstance.cards.map(c => ({ ...c, value: '2' }));
    fixture.changeDetectorRef.markForCheck(); fixture.detectChanges();
    const panel = fixture.nativeElement.querySelector('.alert-list') as HTMLElement;
    expect(panel.querySelectorAll('s3s-aviso')).toHaveLength(2);
    expect(panel.querySelector('a[href="/admin/cotizaciones"]')).not.toBeNull();
    expect(panel.querySelector('a[href="/admin/compras"]')).not.toBeNull();
    expect(panel.textContent).not.toContain('Stock bajo');
    permisos.clear();
    fixture.changeDetectorRef.markForCheck(); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.alert-panel')).toBeNull();
  });

  it('sólo muestra movimientos cuando existen y se permite consultarlos', () => {
    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.componentInstance.movimientosRecientes = [{ tipoMovimiento: 'Entrada', producto: 'Producto de prueba', cantidad: 1 } as any];
    fixture.changeDetectorRef.markForCheck(); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.alert-panel')).toBeNull();
    permisos.add('INVENTARIO_REPORTE');
    fixture.changeDetectorRef.markForCheck(); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.movement-preview').textContent).toContain('Producto de prueba');
    fixture.componentInstance.movimientosRecientes = [];
    fixture.changeDetectorRef.markForCheck(); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.alert-panel')).toBeNull();
  });
});
