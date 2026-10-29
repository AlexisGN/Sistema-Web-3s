import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { vi } from 'vitest';
import { CategoriasDialogoComponent } from './categorias-dialogo';
import { ProductosComponent } from './productos';
import { CatalogoService } from '../../core/services/catalogo';
import { ProductoService } from '../../core/services/producto';
import { SessionService } from '../../core/services/session.service';

const lista = () => [
  { id: 1, nombre: 'Válvulas', descripcion: 'Control de fluidos', estado: true, productosAsignados: 2, productosActivos: 1 },
  { id: 2, nombre: 'Eléctricos', descripcion: null, estado: false, productosAsignados: 0, productosActivos: 0 }
];
describe('Categorías dentro de Productos', () => {
  let admin = true;
  const api = {
    gestionarCategorias: vi.fn(), crearCategoria: vi.fn(), actualizarCategoria: vi.fn(),
    cambiarEstadoCategoria: vi.fn(), eliminarCategoria: vi.fn(),
    listarCategorias: () => of([{ id: 1, nombre: 'Válvulas' }]), listarMarcas: () => of([]), listarUnidadesMedida: () => of([])
  };
  beforeEach(async () => {
    admin = true;
    for (const metodo of ['gestionarCategorias','crearCategoria','actualizarCategoria','cambiarEstadoCategoria','eliminarCategoria'] as const) api[metodo].mockReset();
    api.gestionarCategorias.mockReturnValue(of(lista()));
    api.crearCategoria.mockReturnValue(of({ id: 3 })); api.actualizarCategoria.mockReturnValue(of({}));
    api.cambiarEstadoCategoria.mockReturnValue(of({})); api.eliminarCategoria.mockReturnValue(of({}));
    await TestBed.configureTestingModule({ imports: [CategoriasDialogoComponent, ProductosComponent], providers: [
      { provide: CatalogoService, useValue: api },
      { provide: SessionService, useValue: { esAdministrador: () => admin, tienePermiso: () => true } },
      { provide: ProductoService, useValue: { listar: () => of({ items: [], pagina: 1, tamanioPagina: 5, totalRegistros: 0, totalPaginas: 0 }) } }
    ] }).compileComponents();
    // El componente usa el atributo open cuando jsdom no implementa showModal.
    vi.spyOn(window, 'scrollTo').mockImplementation(() => {});
  });
  afterEach(() => vi.restoreAllMocks());
  const abrir = () => { const f = TestBed.createComponent(CategoriasDialogoComponent); f.detectChanges(); return f; };

  it('lista activas e inactivas y filtra por nombre y estado', () => {
    const f = abrir(); expect(f.nativeElement.querySelectorAll('.categoria-fila')).toHaveLength(2);
    f.componentInstance.filtro = 'inactivas'; f.componentInstance.buscar = 'eléctr'; f.changeDetectorRef.markForCheck(); f.detectChanges();
    expect(f.nativeElement.querySelectorAll('.categoria-fila')).toHaveLength(1);
    expect(f.nativeElement.querySelector('.categoria-fila').textContent).toContain('Eléctricos');
  });
  it('impide duplicados incluso de categorías inactivas y recorta el nombre al crear', () => {
    const f = abrir(); const c = f.componentInstance;
    c.formulario = { nombre: ' ELÉCTRICOS ', descripcion: null }; c.guardar(); expect(api.crearCategoria).not.toHaveBeenCalled();
    c.formulario = { nombre: ' Sensores ', descripcion: ' Instrumentación ' }; c.guardar();
    expect(api.crearCategoria).toHaveBeenCalledWith({ nombre: 'Sensores', descripcion: 'Instrumentación' });
    expect(c.formulario.nombre).toBe(''); expect(api.gestionarCategorias).toHaveBeenCalledTimes(2);
  });
  it('actualiza el registro elegido y avisa al módulo de productos', () => {
    const f = abrir(); const c = f.componentInstance; const changed = vi.fn(); c.cambiado.subscribe(changed);
    c.editar(lista()[0]); c.formulario.nombre = 'Válvulas industriales'; c.guardar();
    expect(api.actualizarCategoria).toHaveBeenCalledWith(1, { nombre: 'Válvulas industriales', descripcion: 'Control de fluidos' });
    expect(changed).toHaveBeenCalledOnce();
  });
  it('confirma la desactivación y bloquea cierre y doble guardado mientras espera', () => {
    const f = abrir(); const c = f.componentInstance; const response = new Subject<unknown>(); const close = vi.fn(); c.cerrar.subscribe(close);
    api.cambiarEstadoCategoria.mockReturnValue(response);
    c.cambiarEstado({ ...lista()[0], productosActivos: 0 }); expect(api.cambiarEstadoCategoria).not.toHaveBeenCalled();
    expect(c.confirmacion?.detalle).toContain('existencias y su historial'); c.aceptarConfirmacion();
    c.solicitarCierre(); expect(close).not.toHaveBeenCalled(); expect(c.guardando).toBe(true);
    response.next({}); response.complete(); expect(c.guardando).toBe(false);
    expect(api.cambiarEstadoCategoria).toHaveBeenCalledWith(1, false);
  });
  it('no ofrece eliminación con productos y conserva el formulario si falla el guardado', () => {
    const f = abrir(); const c = f.componentInstance;
    expect(f.nativeElement.querySelector('[aria-label="Eliminar Válvulas"]')).toBeNull();
    c.eliminar(lista()[0]); expect(c.confirmacion).toBeNull();
    api.actualizarCategoria.mockReturnValue(throwError(() => ({ error: { mensaje: 'Conflicto de nombre' } })));
    c.editar(lista()[0]); c.formulario.nombre = 'Cambios del usuario'; c.guardar();
    expect(c.error).toBe('Conflicto de nombre'); expect(c.formulario.nombre).toBe('Cambios del usuario'); expect(c.guardando).toBe(false);
  });
  it('bloquea desactivar categorías con productos activos en la pantalla y en la acción', () => {
    const f = abrir(); const c = f.componentInstance;
    const botones = [...f.nativeElement.querySelectorAll('.categoria-fila:first-child button')] as HTMLButtonElement[];
    expect(botones.find(b => b.textContent?.trim() === 'Desactivar')?.disabled).toBe(true);
    c.cambiarEstado(lista()[0]); expect(c.confirmacion).toBeNull(); expect(api.cambiarEstadoCategoria).not.toHaveBeenCalled();
    expect(c.error).toContain('producto activo');
  });
  it('confirma eliminar una categoría vacía y descartar un borrador', () => {
    const f = abrir(); const c = f.componentInstance; const close = vi.fn(); c.cerrar.subscribe(close);
    c.eliminar(lista()[1]); c.aceptarConfirmacion(); expect(api.eliminarCategoria).toHaveBeenCalledWith(2);
    c.formulario.nombre = 'Sin guardar'; c.solicitarCierre(); expect(close).not.toHaveBeenCalled();
    c.aceptarConfirmacion(); expect(close).toHaveBeenCalledOnce();
  });
  it('no carga ni escribe si la sesión no es de administrador', () => {
    admin = false; const f = abrir(); f.componentInstance.formulario.nombre = 'Categoría'; f.componentInstance.guardar();
    expect(api.gestionarCategorias).not.toHaveBeenCalled(); expect(api.crearCategoria).not.toHaveBeenCalled();
  });
  it('refresca las categorías sin perder el producto, la imagen ni su categoría inactiva actual', () => {
    const f = TestBed.createComponent(ProductosComponent); f.detectChanges(); const c = f.componentInstance;
    c.editarProducto({ idProducto: 5, idCategoria: 1, nombre: 'Producto existente', codigoProducto: 'P5', categoria: 'Válvulas', estado: true } as any);
    const imagen = new File(['x'], 'imagen.png', { type: 'image/png' }); c.imagenArchivo = imagen; c.producto.nombre = 'Producto sin guardar';
    api.gestionarCategorias.mockReturnValue(of([{ ...lista()[0], estado: false }, { ...lista()[1], estado: true }]));
    c.actualizarCategorias(); expect(c.producto.nombre).toBe('Producto sin guardar'); expect(c.imagenArchivo).toBe(imagen);
    expect(c.producto.idCategoria).toBe(1); expect(c.categorias.find(x => x.id === 1)?.nombre).toContain('(inactiva)');
    c.limpiarFormulario(); expect(c.categorias.map(x => x.id)).toEqual([2]);
  });
});
