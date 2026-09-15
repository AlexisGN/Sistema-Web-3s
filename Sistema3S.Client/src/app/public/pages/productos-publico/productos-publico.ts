import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { combineLatest, forkJoin, Subject, takeUntil } from 'rxjs';

import {
  CategoriaPublica,
  MarcaPublica,
  ProductoPublico
} from '../../../core/models/publico.model';
import { ClienteWebService } from '../../../core/services/cliente-web.service';
import { PublicoService } from '../../../core/services/publico.service';

@Component({
  selector: 'app-productos-publico',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './productos-publico.html',
  styleUrl: './productos-publico.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProductosPublicoComponent implements OnInit, OnDestroy {
  cargando = false;
  cargandoMas = false;
  error = '';
  mensajeOperacion = '';

  clienteLogueado = false;

  categorias: CategoriaPublica[] = [];
  marcas: MarcaPublica[] = [];
  productos: ProductoPublico[] = [];

  busqueda = '';
  idCategoriaSeleccionada: number | null = null;
  idMarcaSeleccionada: number | null = null;

  pagina = 1;
  tamanioPagina = 24;
  totalRegistros = 0;
  totalPaginas = 0;
  hayMas = false;

  origenRuta = 'productos';

  private destroy$ = new Subject<void>();

  private readonly actualizarSesionClienteHandler = () => {
    this.verificarSesionCliente();
  };

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private publicoService: PublicoService,
    private clienteWebService: ClienteWebService,
    private cd: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.verificarSesionCliente();
    this.registrarEventosSesionCliente();
    this.cargarFiltros();

    combineLatest([
      this.route.queryParamMap,
      this.route.paramMap,
      this.route.data
    ])
      .pipe(takeUntil(this.destroy$))
      .subscribe(([queryParams, params, data]) => {
        this.origenRuta = String(data['origen'] || 'productos');

        this.busqueda = queryParams.get('q') || '';

        const idCategoriaQuery = Number(queryParams.get('idCategoria') || 0);
        const idMarcaQuery = Number(queryParams.get('idMarca') || 0);
        const idRuta = Number(params.get('id') || 0);

        this.idCategoriaSeleccionada = null;
        this.idMarcaSeleccionada = null;

        if (this.origenRuta === 'categoria-detalle' && idRuta > 0) {
          this.idCategoriaSeleccionada = idRuta;
        } else if (this.origenRuta === 'marca-detalle' && idRuta > 0) {
          this.idMarcaSeleccionada = idRuta;
        } else {
          this.idCategoriaSeleccionada = idCategoriaQuery > 0 ? idCategoriaQuery : null;
          this.idMarcaSeleccionada = idMarcaQuery > 0 ? idMarcaQuery : null;
        }

        this.reiniciarYCargarProductos();
      });
  }

  ngOnDestroy(): void {
    this.quitarEventosSesionCliente();

    this.destroy$.next();
    this.destroy$.complete();
  }

  get categoriaActual(): CategoriaPublica | undefined {
    if (!this.idCategoriaSeleccionada) {
      return undefined;
    }

    return this.categorias.find(c =>
      (c.idCategoria || c.id) === this.idCategoriaSeleccionada
    );
  }

  get marcaActual(): MarcaPublica | undefined {
    if (!this.idMarcaSeleccionada) {
      return undefined;
    }

    return this.marcas.find(m =>
      (m.idMarca || m.id) === this.idMarcaSeleccionada
    );
  }

  get tituloPagina(): string {
    if (this.categoriaActual) {
      return `Productos de ${this.categoriaActual.nombre}`;
    }

    if (this.marcaActual) {
      return `Productos ${this.marcaActual.nombre}`;
    }

    if (this.origenRuta === 'categorias') {
      return 'Productos por categoría';
    }

    return 'Catálogo de productos industriales';
  }

  get descripcionPagina(): string {
    if (this.categoriaActual) {
      return `Explora productos industriales de la categoría ${this.categoriaActual.nombre} y solicita una cotización personalizada.`;
    }

    if (this.marcaActual) {
      return `Consulta productos de la marca ${this.marcaActual.nombre} y solicita atención comercial especializada.`;
    }

    return 'Encuentra productos industriales para generación de vapor, automatización, instrumentación, mantenimiento y soluciones técnicas 3S.';
  }

  get hayFiltrosActivos(): boolean {
    return !!this.busqueda.trim() || !!this.idCategoriaSeleccionada || !!this.idMarcaSeleccionada;
  }

  cargarFiltros(): void {
    forkJoin({
      categorias: this.publicoService.obtenerCategorias(),
      marcas: this.publicoService.obtenerMarcas()
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: response => {
          this.categorias = response.categorias || [];
          this.marcas = response.marcas || [];
          this.cd.markForCheck();
        },
        error: () => {
          this.categorias = [];
          this.marcas = [];
          this.cd.markForCheck();
        }
      });
  }

  reiniciarYCargarProductos(): void {
    this.pagina = 1;
    this.productos = [];
    this.cargarProductos(false);
  }

  cargarProductos(agregar: boolean): void {
    this.verificarSesionCliente();

    if (agregar) {
      this.cargandoMas = true;
    } else {
      this.cargando = true;
      this.error = '';
    }

    this.mensajeOperacion = '';

    this.publicoService.obtenerProductos({
      q: this.busqueda,
      idCategoria: this.idCategoriaSeleccionada,
      idMarca: this.idMarcaSeleccionada,
      pagina: this.pagina,
      tamanioPagina: this.tamanioPagina
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: response => {
          const items = (response.items || []).map(producto => ({
            ...producto,
            cantidad: Number(producto.cantidad || 1)
          }));

          this.productos = agregar
            ? [...this.productos, ...items]
            : items;

          this.totalRegistros = response.totalRegistros || 0;
          this.totalPaginas = response.totalPaginas || 0;
          this.hayMas = !!response.hayMas;

          this.cargando = false;
          this.cargandoMas = false;
          this.cd.markForCheck();
        },
        error: () => {
          this.productos = agregar ? this.productos : [];
          this.totalRegistros = 0;
          this.totalPaginas = 0;
          this.hayMas = false;

          this.error = 'No pudimos cargar el catálogo en este momento. Intenta nuevamente en unos instantes.';
          this.cargando = false;
          this.cargandoMas = false;
          this.cd.markForCheck();
        }
      });
  }

  buscarProductos(): void {
    this.origenRuta = 'productos';
    this.reiniciarYCargarProductos();
  }

  seleccionarCategoria(idCategoria: number | null): void {
    this.idCategoriaSeleccionada = idCategoria;
    this.idMarcaSeleccionada = null;
    this.origenRuta = idCategoria ? 'categoria-detalle' : 'productos';
    this.reiniciarYCargarProductos();
  }

  seleccionarMarca(idMarca: number | null): void {
    this.idMarcaSeleccionada = idMarca;
    this.idCategoriaSeleccionada = null;
    this.origenRuta = idMarca ? 'marca-detalle' : 'productos';
    this.reiniciarYCargarProductos();
  }

  limpiarFiltros(): void {
    this.busqueda = '';
    this.idCategoriaSeleccionada = null;
    this.idMarcaSeleccionada = null;
    this.origenRuta = 'productos';
    this.reiniciarYCargarProductos();
  }

  cargarMas(): void {
    if (!this.hayMas || this.cargandoMas) {
      return;
    }

    this.pagina += 1;
    this.cargarProductos(true);
  }

  normalizarCantidad(producto: ProductoPublico): void {
    producto.cantidad = Number(producto.cantidad || 1);

    if (producto.cantidad < 1) {
      producto.cantidad = 1;
    }

    producto.cantidad = Math.floor(producto.cantidad);
  }

  verDetalle(producto: ProductoPublico): void {
    this.router.navigate(['/productos', producto.idProducto || producto.id]);
  }

  abrirWhatsAppProducto(producto: ProductoPublico): void {
    this.normalizarCantidad(producto);

    const telefono = '51948327667';

    const mensaje = [
      'Hola, deseo solicitar una cotización de este producto:',
      '',
      `Producto: ${producto.nombre}`,
      `Código: ${producto.codigo}`,
      `Categoría: ${producto.categoria}`,
      `Marca: ${producto.marca || '3S'}`,
      `Cantidad: ${producto.cantidad}`,
      '',
      'Quedo atento a la atención del área comercial.'
    ].join('\n');

    const url = `https://wa.me/${telefono}?text=${encodeURIComponent(mensaje)}`;

    window.open(url, '_blank');
  }

  abrirFichaTecnica(producto: ProductoPublico): void {
    if (!producto.fichaTecnicaPdf) {
      return;
    }

    window.open(producto.fichaTecnicaPdf, '_blank');
  }

  private verificarSesionCliente(): void {
    const sesionCliente = this.clienteWebService.obtenerSesion();

    this.clienteLogueado = !!sesionCliente;
    this.cd.markForCheck();
  }

  private registrarEventosSesionCliente(): void {
    window.addEventListener('storage', this.actualizarSesionClienteHandler);
    window.addEventListener('clienteWebSesionActualizada', this.actualizarSesionClienteHandler);
  }

  private quitarEventosSesionCliente(): void {
    window.removeEventListener('storage', this.actualizarSesionClienteHandler);
    window.removeEventListener('clienteWebSesionActualizada', this.actualizarSesionClienteHandler);
  }

  trackByProducto(_: number, producto: ProductoPublico): number {
    return producto.idProducto || producto.id;
  }

  trackByCategoria(_: number, categoria: CategoriaPublica): number {
    return categoria.idCategoria || categoria.id;
  }

  trackByMarca(_: number, marca: MarcaPublica): number {
    return marca.idMarca || marca.id;
  }
}