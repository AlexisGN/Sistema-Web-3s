import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, EventEmitter, OnDestroy, Output, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { CatalogoService } from '../../core/services/catalogo';
import { SessionService } from '../../core/services/session.service';
import { CategoriaGestion, CategoriaGuardar } from '../../core/models/categoria.model';
import { UiIconComponent } from '../../shared/ui-icon/ui-icon';

@Component({
  selector: 's3s-categorias-dialogo', standalone: true,
  imports: [CommonModule, FormsModule, UiIconComponent],
  templateUrl: './categorias-dialogo.html', styleUrl: './categorias-dialogo.scss'
})
export class CategoriasDialogoComponent implements AfterViewInit, OnDestroy {
  private readonly api = inject(CatalogoService);
  private readonly sesion = inject(SessionService);
  private readonly cdr = inject(ChangeDetectorRef);
  @ViewChild('dialogo', { static: true }) dialogo!: ElementRef<HTMLDialogElement>;
  @ViewChild('nombreInput') nombreInput?: ElementRef<HTMLInputElement>;
  @Output() cerrar = new EventEmitter<void>();
  @Output() cambiado = new EventEmitter<void>();
  categorias: CategoriaGestion[] = [];
  cargando = false;
  guardando = false;
  error = '';
  mensaje = '';
  buscar = '';
  filtro = 'todas';
  editando: number | null = null;
  formulario: CategoriaGuardar = { nombre: '', descripcion: null };
  confirmacion: { titulo: string; detalle: string; aceptar: string; ejecutar: () => void } | null = null;
  private original = '';
  private descripcionOriginal = '';
  private focoAnterior: HTMLElement | null = null;
  private destruido = false;

  ngAfterViewInit(): void {
    this.focoAnterior = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    if (!this.sesion.esAdministrador()) { this.cerrar.emit(); return; }
    const dialogo = this.dialogo.nativeElement;
    if (typeof dialogo.showModal === 'function') dialogo.showModal();
    else dialogo.setAttribute('open', '');
    this.cargar();
  }

  ngOnDestroy(): void {
    this.destruido = true;
    const dialogo = this.dialogo.nativeElement;
    if (dialogo.open && typeof dialogo.close === 'function') dialogo.close();
    this.focoAnterior?.focus();
  }

  get filtradas(): CategoriaGestion[] {
    const texto = this.buscar.trim().toLocaleLowerCase('es');
    return this.categorias.filter(c => (this.filtro === 'todas' || c.estado === (this.filtro === 'activas'))
      && (!texto || `${c.nombre} ${c.descripcion ?? ''}`.toLocaleLowerCase('es').includes(texto)));
  }

  get hayCambios(): boolean {
    return this.formulario.nombre !== this.original || (this.formulario.descripcion ?? '') !== this.descripcionOriginal;
  }

  cargar(): void {
    this.cargando = true;
    this.api.gestionarCategorias().subscribe({
      next: datos => { this.categorias = datos; this.cargando = false; this.refrescar(); },
      error: () => { this.cargando = false; this.error = 'No se pudieron cargar las categorías. Vuelve a intentarlo.'; this.refrescar(); }
    });
  }

  private refrescar(): void { if (!this.destruido) this.cdr.detectChanges(); }

  solicitarCierre(event?: Event): void {
    event?.preventDefault();
    if (this.guardando) return;
    if (this.confirmacion) { this.confirmacion = null; return; }
    this.conservarBorrador(() => this.cerrar.emit());
  }

  private conservarBorrador(accion: () => void): void {
    if (!this.hayCambios) { accion(); return; }
    this.confirmacion = { titulo: 'Tienes cambios sin guardar', detalle: 'Si continúas, se descartarán los cambios del formulario de categoría.',
      aceptar: 'Descartar cambios', ejecutar: accion };
  }

  editar(categoria: CategoriaGestion): void {
    if (this.guardando) return;
    this.conservarBorrador(() => {
      this.editando = categoria.id;
      this.formulario = { nombre: categoria.nombre, descripcion: categoria.descripcion };
      this.original = categoria.nombre;
      this.descripcionOriginal = categoria.descripcion ?? '';
      this.error = ''; this.mensaje = '';
      this.nombreInput?.nativeElement.focus();
    });
  }

  nuevo(): void { if (!this.guardando) this.conservarBorrador(() => this.limpiar()); }

  private limpiar(): void {
    this.editando = null; this.formulario = { nombre: '', descripcion: null }; this.original = ''; this.descripcionOriginal = '';
    this.nombreInput?.nativeElement.focus();
  }

  guardar(): void {
    if (this.guardando || !this.sesion.esAdministrador()) return;
    const datos = { nombre: this.formulario.nombre.trim(), descripcion: this.formulario.descripcion?.trim() || null };
    this.error = ''; this.mensaje = '';
    if (!datos.nombre) { this.error = 'Escribe el nombre de la categoría.'; this.nombreInput?.nativeElement.focus(); return; }
    if (datos.nombre.length > 100 || (datos.descripcion?.length ?? 0) > 300) { this.error = 'Revisa la longitud del nombre y la descripción.'; return; }
    if (this.categorias.some(c => c.id !== this.editando && c.nombre.toLocaleLowerCase('es') === datos.nombre.toLocaleLowerCase('es'))) {
      this.error = 'Ya existe una categoría con ese nombre. Revisa también las inactivas.'; return;
    }
    this.mutar(this.editando === null ? this.api.crearCategoria(datos) : this.api.actualizarCategoria(this.editando, datos),
      this.editando === null ? 'Categoría creada. Ya está disponible para los productos y la web pública.' : 'Categoría actualizada.', true);
  }

  cambiarEstado(categoria: CategoriaGestion): void {
    if (this.guardando) return;
    if (categoria.estado && categoria.productosActivos > 0) {
      this.error = `No puedes desactivar esta categoría: ${categoria.productosActivos === 1 ? 'tiene un producto activo' : `tiene ${categoria.productosActivos} productos activos`}. Primero reasigna o desactiva esos productos.`;
      return;
    }
    this.confirmacion = { titulo: `${categoria.estado ? 'Desactivar' : 'Activar'} ${categoria.nombre}`,
      detalle: categoria.estado
        ? `Se ocultará esta categoría en la web pública. No tiene productos activos. Se conservarán ${categoria.productosAsignados} productos inactivos asignados, sus existencias y su historial.`
        : 'La categoría volverá a estar disponible. En la web pública se mostrarán los productos que estén activos.',
      aceptar: categoria.estado ? 'Desactivar categoría' : 'Activar categoría',
      ejecutar: () => this.mutar(this.api.cambiarEstadoCategoria(categoria.id, !categoria.estado), categoria.estado ? 'Categoría desactivada.' : 'Categoría activada.') };
  }

  eliminar(categoria: CategoriaGestion): void {
    if (this.guardando || categoria.productosAsignados > 0) return;
    this.confirmacion = { titulo: `Eliminar ${categoria.nombre}`, detalle: 'Se eliminará esta categoría sin productos asignados. Esta acción no se puede deshacer.',
      aceptar: 'Eliminar categoría', ejecutar: () => this.mutar(this.api.eliminarCategoria(categoria.id), 'Categoría eliminada.', this.editando === categoria.id) };
  }

  aceptarConfirmacion(): void { const accion = this.confirmacion?.ejecutar; this.confirmacion = null; accion?.(); }

  private mutar(peticion: Observable<unknown>, mensaje: string, limpiar = false): void {
    if (this.guardando || !this.sesion.esAdministrador()) return;
    this.guardando = true; this.error = ''; this.mensaje = '';
    peticion.subscribe({
      next: () => { this.guardando = false; if (limpiar) this.limpiar(); this.mensaje = mensaje; this.cambiado.emit(); this.cargar(); },
      error: err => { this.guardando = false; this.error = err.error?.mensaje || 'No se pudo guardar el cambio. Vuelve a intentarlo.'; this.refrescar(); }
    });
  }
}
