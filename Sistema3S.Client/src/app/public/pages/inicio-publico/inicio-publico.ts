import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  NgZone,
  OnDestroy,
  OnInit
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import {
  CategoriaPublica,
  MarcaPublica,
  ProductoPublico,
  ServicioPublico
} from '../../../core/models/publico.model';
import { ClienteWebService } from '../../../core/services/cliente-web.service';
import { PublicoService } from '../../../core/services/publico.service';

type CategoriaConProductos = CategoriaPublica & {
  productosInicio: ProductoPublico[];
};

@Component({
  selector: 'app-inicio-publico',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './inicio-publico.html',
  styleUrl: './inicio-publico.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class InicioPublicoComponent implements OnInit, AfterViewInit, OnDestroy {
  busqueda = '';

  cargandoInicio = false;
  errorInicio = '';
  mensajeOperacion = '';

  clienteLogueado = false;

  categorias: CategoriaPublica[] = [];
  categoriasConProductos: CategoriaConProductos[] = [];

  marcas: MarcaPublica[] = [];
  marcasCarrusel: MarcaPublica[] = [];

  productos: ProductoPublico[] = [];
  productosNuevos: ProductoPublico[] = [];
  productosNuevosCarrusel: ProductoPublico[] = [];

  servicios: ServicioPublico[] = [];

  private observadorSecciones?: IntersectionObserver;
  private timeoutEfectoSecciones?: number;
  private seccionActivaActual?: Element;

  private destroy$ = new Subject<void>();

  private readonly actualizarSesionClienteHandler = () => {
    this.verificarSesionCliente();
  };

  constructor(
    private route: ActivatedRoute,
    private publicoService: PublicoService,
    private clienteWebService: ClienteWebService,
    private cd: ChangeDetectorRef,
    private ngZone: NgZone
  ) { }

  ngOnInit(): void {
    this.verificarSesionCliente();
    this.registrarEventosSesionCliente();
    this.cargarInicio();

    this.route.queryParamMap
      .pipe(takeUntil(this.destroy$))
      .subscribe(params => {
        const texto = params.get('q');

        if (!texto) {
          this.busqueda = '';
          this.cd.markForCheck();
          return;
        }

        this.busqueda = texto;

        setTimeout(() => {
          document.getElementById('catalogo')?.scrollIntoView({
            behavior: 'smooth',
            block: 'start'
          });
        }, 100);

        this.cd.markForCheck();
      });
  }

  ngAfterViewInit(): void {
    this.programarInicializacionEfectoSecciones();
  }

  ngOnDestroy(): void {
    this.quitarEventosSesionCliente();
    this.observadorSecciones?.disconnect();

    if (this.timeoutEfectoSecciones) {
      window.clearTimeout(this.timeoutEfectoSecciones);
    }

    this.destroy$.next();
    this.destroy$.complete();
  }

  cargarInicio(): void {
    this.verificarSesionCliente();

    this.cargandoInicio = true;
    this.errorInicio = '';
    this.mensajeOperacion = '';
    this.cd.markForCheck();

    this.publicoService
      .obtenerInicio()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: response => {
          this.categorias = response.categorias || [];

          this.marcas = response.marcas || [];
          this.marcasCarrusel = this.marcas.length > 0
            ? [...this.marcas, ...this.marcas]
            : [];

          this.productosNuevos = (response.productosNuevos || []).map(producto => ({
            ...producto,
            cantidad: Number(producto.cantidad || 1)
          }));

          this.productosNuevosCarrusel = this.productosNuevos.length > 0
            ? [...this.productosNuevos, ...this.productosNuevos]
            : [];

          this.productos = (response.productos || []).map(producto => ({
            ...producto,
            cantidad: Number(producto.cantidad || 1)
          }));

          this.servicios = response.servicios || [];

          this.construirProductosPorCategoria();

          this.cargandoInicio = false;
          this.programarInicializacionEfectoSecciones();
          this.cd.markForCheck();
        },
        error: () => {
          this.categorias = [];
          this.categoriasConProductos = [];

          this.marcas = [];
          this.marcasCarrusel = [];

          this.productosNuevos = [];
          this.productosNuevosCarrusel = [];

          this.productos = [];
          this.servicios = [];

          this.errorInicio = 'No se pudo cargar la información pública.';
          this.cargandoInicio = false;

          this.programarInicializacionEfectoSecciones();
          this.cd.markForCheck();
        }
      });
  }

  normalizarCantidad(producto: ProductoPublico): void {
    producto.cantidad = Number(producto.cantidad || 1);

    if (producto.cantidad < 1) {
      producto.cantidad = 1;
    }

    producto.cantidad = Math.floor(producto.cantidad);
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

  abrirWhatsAppServicio(servicio: ServicioPublico): void {
    const telefono = '51948327667';

    const mensajeBase = servicio.mensajeWhatsApp?.trim();

    const mensaje = mensajeBase
      ? mensajeBase
      : [
        'Hola, deseo información sobre este servicio:',
        '',
        `Servicio: ${servicio.nombre}`,
        `Detalle: ${servicio.descripcion}`,
        servicio.sectorAplicacion ? `Sector de aplicación: ${servicio.sectorAplicacion}` : '',
        servicio.requiereVisitaTecnica ? 'Requiere visita técnica: Sí' : '',
        '',
        'Quedo atento a su respuesta.'
      ]
        .filter(linea => linea !== '')
        .join('\n');

    const url = `https://wa.me/${telefono}?text=${encodeURIComponent(mensaje)}`;

    window.open(url, '_blank');
  }

  abrirWhatsAppGeneral(): void {
    const telefono = '51948327667';
    const mensaje = 'Hola, deseo información sobre los productos y servicios industriales de 3S.';
    const url = `https://wa.me/${telefono}?text=${encodeURIComponent(mensaje)}`;

    window.open(url, '_blank');
  }

  private construirProductosPorCategoria(): void {
    const productosPorIdCategoria = new Map<number, ProductoPublico[]>();
    const productosPorNombreCategoria = new Map<string, ProductoPublico[]>();

    for (const producto of this.productos) {
      const idCategoria = producto.idCategoria;
      const nombreCategoria = this.normalizarTexto(producto.categoria);

      if (idCategoria) {
        this.agregarProductoAlMapa(productosPorIdCategoria, idCategoria, producto);
      }

      if (nombreCategoria) {
        this.agregarProductoAlMapa(productosPorNombreCategoria, nombreCategoria, producto);
      }
    }

    this.categoriasConProductos = this.categorias.map(categoria => {
      const idCategoria = categoria.idCategoria || categoria.id;
      const nombreCategoria = this.normalizarTexto(categoria.nombre);

      const productosInicio =
        productosPorIdCategoria.get(idCategoria) ||
        productosPorNombreCategoria.get(nombreCategoria) ||
        [];

      return {
        ...categoria,
        productosInicio
      };
    });
  }

  private agregarProductoAlMapa<K>(
    mapa: Map<K, ProductoPublico[]>,
    clave: K,
    producto: ProductoPublico
  ): void {
    if (!mapa.has(clave)) {
      mapa.set(clave, []);
    }

    const productos = mapa.get(clave)!;

    if (productos.length < 4) {
      productos.push(producto);
    }
  }

  private normalizarTexto(valor: string | null | undefined): string {
    return (valor || '').trim().toLowerCase();
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

  trackByCategoria(_: number, categoria: CategoriaPublica): number {
    return categoria.idCategoria || categoria.id;
  }

  trackByProducto(_: number, producto: ProductoPublico): number {
    return producto.idProducto || producto.id;
  }

  trackByServicio(_: number, servicio: ServicioPublico): number {
    return servicio.idServicio || servicio.id;
  }

  trackByMarca(_: number, marca: MarcaPublica): number {
    return marca.idMarca || marca.id;
  }

  trackByProductoCarrusel(index: number, producto: ProductoPublico): string {
    return `${producto.idProducto || producto.id}-${index}`;
  }

  trackByMarcaCarrusel(index: number, marca: MarcaPublica): string {
    return `${marca.idMarca || marca.id}-${index}`;
  }

  private programarInicializacionEfectoSecciones(): void {
    if (this.timeoutEfectoSecciones) {
      window.clearTimeout(this.timeoutEfectoSecciones);
    }

    this.ngZone.runOutsideAngular(() => {
      this.timeoutEfectoSecciones = window.setTimeout(() => {
        requestAnimationFrame(() => {
          this.inicializarEfectoSecciones();
        });
      }, 180);
    });
  }

  private inicializarEfectoSecciones(): void {
    if (!('IntersectionObserver' in window)) {
      return;
    }

    this.observadorSecciones?.disconnect();
    this.seccionActivaActual = undefined;

    const secciones = Array.from(
      document.querySelectorAll<HTMLElement>('.seccion-scroll')
    );

    if (secciones.length === 0) {
      return;
    }

    this.observadorSecciones = new IntersectionObserver(
      entradas => {
        const visibles = entradas
          .filter(entrada => entrada.isIntersecting)
          .sort((a, b) => b.intersectionRatio - a.intersectionRatio);

        if (visibles.length === 0) {
          return;
        }

        this.activarSeccion(visibles[0].target);
      },
      {
        threshold: [0.25, 0.45],
        rootMargin: '-10% 0px -52% 0px'
      }
    );

    secciones.forEach(seccion => {
      this.observadorSecciones?.observe(seccion);
    });

    this.marcarSeccionInicialActiva(secciones);
  }

  private activarSeccion(seccionActiva: Element): void {
    if (this.seccionActivaActual === seccionActiva) {
      return;
    }

    this.seccionActivaActual?.classList.remove('section-active');

    seccionActiva.classList.add('section-active');
    this.seccionActivaActual = seccionActiva;
  }

  private marcarSeccionInicialActiva(secciones: HTMLElement[]): void {
    const centroPantalla = window.innerHeight * 0.42;

    const seccionActual = secciones
      .map(seccion => {
        const rect = seccion.getBoundingClientRect();

        return {
          seccion,
          distancia: Math.abs(rect.top - centroPantalla)
        };
      })
      .sort((a, b) => a.distancia - b.distancia)[0]?.seccion;

    if (!seccionActual) {
      return;
    }

    this.activarSeccion(seccionActual);
  }
}