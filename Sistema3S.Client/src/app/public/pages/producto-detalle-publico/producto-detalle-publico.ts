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
import { Subject, takeUntil } from 'rxjs';
import { ClienteWebService } from '../../../core/services/cliente-web.service';

import {
  ImagenPublica,
  ProductoDetallePublico,
  ProductoPublico
} from '../../../core/models/publico.model';
import { PublicoService } from '../../../core/services/publico.service';

@Component({
  selector: 'app-producto-detalle-publico',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './producto-detalle-publico.html',
  styleUrl: './producto-detalle-publico.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProductoDetallePublicoComponent implements OnInit, OnDestroy {
  producto?: ProductoDetallePublico;
  imagenes: ImagenPublica[] = [];
  imagenSeleccionada = '';

  relacionados: ProductoPublico[] = [];

  cargando = false;
  error = '';
  mensajeOperacion = '';

  clienteLogueado = false;

  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private publicoService: PublicoService,
    private clienteWebService: ClienteWebService,
    private cd: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.verificarSesionCliente();

    this.route.paramMap
      .pipe(takeUntil(this.destroy$))
      .subscribe(params => {
        const idProducto = Number(params.get('id') || 0);

        if (idProducto <= 0) {
          this.error = 'No pudimos identificar el producto solicitado.';
          this.cd.markForCheck();
          return;
        }

        this.cargarProducto(idProducto);
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  cargarProducto(idProducto: number): void {
    this.cargando = true;
    this.error = '';
    this.producto = undefined;
    this.imagenes = [];
    this.imagenSeleccionada = '';
    this.relacionados = [];
    this.cd.markForCheck();

    this.publicoService.obtenerProductoDetalle(idProducto)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: producto => {
          this.producto = producto;
          this.producto.cantidad = 1;

          this.imagenes = this.construirGaleria(producto);
          this.imagenSeleccionada = this.imagenes[0]?.urlImagen || producto.imagenUrl || '';

          this.cargando = false;
          this.cargarRelacionados(producto);
          this.cd.markForCheck();
        },
        error: () => {
          this.error = 'El producto solicitado no se encuentra disponible en el catálogo público.';
          this.cargando = false;
          this.cd.markForCheck();
        }
      });
  }

  cargarRelacionados(producto: ProductoDetallePublico): void {
    this.publicoService.obtenerProductos({
      idCategoria: producto.idCategoria,
      pagina: 1,
      tamanioPagina: 8
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: response => {
          this.relacionados = (response.items || [])
            .filter(item => (item.idProducto || item.id) !== (producto.idProducto || producto.id))
            .slice(0, 4);

          this.cd.markForCheck();
        },
        error: () => {
          this.relacionados = [];
          this.cd.markForCheck();
        }
      });
  }

  construirGaleria(producto: ProductoDetallePublico): ImagenPublica[] {
    const imagenes = producto.imagenes || [];

    if (imagenes.length > 0) {
      return imagenes;
    }

    if (producto.imagenUrl) {
      return [
        {
          idImagen: 0,
          urlImagen: producto.imagenUrl,
          textoAlternativo: producto.nombre,
          esPrincipal: true
        }
      ];
    }

    return [];
  }

  seleccionarImagen(imagen: ImagenPublica): void {
    this.imagenSeleccionada = imagen.urlImagen;
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

  abrirFichaTecnica(): void {
    if (!this.producto?.fichaTecnicaPdf) {
      return;
    }

    window.open(this.producto.fichaTecnicaPdf, '_blank');
  }

  verProductoRelacionado(producto: ProductoPublico): void {
    this.router.navigate(['/productos', producto.idProducto || producto.id]);
  }

  volverCatalogo(): void {
    this.router.navigate(['/productos']);
  }

  private verificarSesionCliente(): void {
  this.clienteLogueado = !!this.clienteWebService.obtenerSesion();
}

  trackByImagen(_: number, imagen: ImagenPublica): number {
    return imagen.idImagen;
  }

  trackByProducto(_: number, producto: ProductoPublico): number {
    return producto.idProducto || producto.id;
  }
}