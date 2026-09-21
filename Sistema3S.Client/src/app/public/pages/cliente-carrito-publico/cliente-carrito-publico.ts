import { AvisoComponent } from '../../../shared/mensajes/aviso';
import { PublicDialogFocusDirective } from '../../shared/dialog-focus';
import { PublicAccountNavComponent } from '../../shared/account-nav';
import { UiIconComponent } from '../../../shared/ui-icon/ui-icon';
import { ImagenCatalogoComponent } from '../../../shared/imagen-catalogo/imagen-catalogo';
import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import {
  CarritoCotizacionItem,
  CotizacionWebRegistradaResponse
} from '../../../core/models/cotizacion-web.model';
import { ClienteWebSesion } from '../../../core/models/cliente-web.model';
import { ClienteWebService } from '../../../core/services/cliente-web.service';
import { CotizacionWebService } from '../../../core/services/cotizacion-web.service';
import { CarritoCotizacionService } from '../../../core/services/carrito-cotizacion.service';

@Component({
  selector: 'app-cliente-carrito-publico',
  standalone: true,
  imports: [AvisoComponent, PublicDialogFocusDirective, PublicAccountNavComponent, UiIconComponent, ImagenCatalogoComponent, CommonModule, FormsModule, RouterLink],
  templateUrl: './cliente-carrito-publico.html',
  styleUrl: './cliente-carrito-publico.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ClienteCarritoPublicoComponent implements OnInit, OnDestroy {


  sesion: ClienteWebSesion | null = null;
  items: CarritoCotizacionItem[] = [];

  observacionGeneral = '';

  cargando = false;
  enviando = false;
  error = '';

  cotizacionRegistrada: CotizacionWebRegistradaResponse | null = null;

  private readonly actualizarCarritoHandler = () => {
    this.cargarCarrito();
  };

  constructor(
    private clienteWebService: ClienteWebService,
    private cotizacionWebService: CotizacionWebService,
    private carritoCotizacionService: CarritoCotizacionService,
    private router: Router,
    private cd: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.sesion = this.clienteWebService.obtenerSesion();

    if (!this.sesion) {
      this.router.navigate(['/cliente/login'], {
        queryParams: {
          returnUrl: '/cliente/carrito'
        }
      });

      return;
    }

    this.cargarCarrito();

    window.addEventListener('storage', this.actualizarCarritoHandler);
    window.addEventListener('carritoCotizacionActualizado', this.actualizarCarritoHandler);
  }

  ngOnDestroy(): void {
    window.removeEventListener('storage', this.actualizarCarritoHandler);
    window.removeEventListener('carritoCotizacionActualizado', this.actualizarCarritoHandler);
  }

  get cantidadTotal(): number {
    return this.items.reduce((total, item) => total + Number(item.cantidad || 0), 0);
  }

  get esEmpresa(): boolean {
    return !!this.sesion?.esEmpresa;
  }

  get nombreCliente(): string {
    return this.sesion?.nombreCliente || 'Cliente 3S';
  }

  cargarCarrito(): void {
    const items = this.carritoCotizacionService.obtenerItems<CarritoCotizacionItem>();

    this.items = Array.isArray(items)
      ? items
        .filter(item => Number(item.idProducto) > 0)
        .map(item => ({
          idProducto: Number(item.idProducto),
          idElementoCatalogo: Number(item.idElementoCatalogo || 0),
          codigo: item.codigo || '',
          nombre: item.nombre || 'Producto industrial',
          categoria: item.categoria || 'Catálogo industrial',
          marca: item.marca || null,
          imagenUrl: item.imagenUrl || '',
          cantidad: this.normalizarCantidadValor(item.cantidad),
          observacion: item.observacion || ''
        }))
      : [];

    this.guardarCarrito(false);
    this.cd.markForCheck();
  }

  aumentarCantidad(item: CarritoCotizacionItem): void {
    item.cantidad = this.normalizarCantidadValor(item.cantidad) + 1;
    this.guardarCarrito();
  }

  disminuirCantidad(item: CarritoCotizacionItem): void {
    item.cantidad = this.normalizarCantidadValor(item.cantidad) - 1;

    if (item.cantidad < 1) {
      item.cantidad = 1;
    }

    this.guardarCarrito();
  }

  normalizarCantidad(item: CarritoCotizacionItem): void {
    item.cantidad = this.normalizarCantidadValor(item.cantidad);
    this.guardarCarrito();
  }

  actualizarObservacion(): void {
    this.guardarCarrito();
  }

  eliminarItem(item: CarritoCotizacionItem): void {
    this.items = this.items.filter(producto => producto.idProducto !== item.idProducto);
    this.guardarCarrito();
  }

  vaciarCarrito(): void {
  this.items = [];
  this.observacionGeneral = '';
  this.carritoCotizacionService.limpiarCarrito();
  this.cd.markForCheck();
}

  enviarCotizacion(): void {
    this.error = '';
    this.cotizacionRegistrada = null;

    this.sesion = this.clienteWebService.obtenerSesion();

    if (!this.sesion) {
      this.router.navigate(['/cliente/login'], {
        queryParams: {
          returnUrl: '/cliente/carrito'
        }
      });
      return;
    }

    if (this.items.length === 0) {
      this.error = 'Agrega al menos un producto para enviar tu solicitud de cotización.';
      this.cd.markForCheck();
      return;
    }

    const payload = {
      observacionGeneral: this.observacionGeneral.trim() || null,
      items: this.items.map(item => ({
        idProducto: item.idProducto,
        cantidad: this.normalizarCantidadValor(item.cantidad),
        observacion: item.observacion?.trim() || null
      }))
    };

    this.enviando = true;
    this.cd.markForCheck();

    this.cotizacionWebService.registrarCotizacion(payload).subscribe({
      next: response => {
        this.enviando = false;
        this.cotizacionRegistrada = response;

        this.carritoCotizacionService.limpiarCarrito();
        this.items = [];
        this.observacionGeneral = '';

        this.cd.markForCheck();
      },
      error: error => {
        this.enviando = false;
        this.error =
          error?.error?.mensaje ||
          'No pudimos enviar tu solicitud de cotización. Revisa el carrito e intenta nuevamente.';
        this.cd.markForCheck();
      }
    });
  }

  irHistorial(): void {
    this.router.navigate(['/cliente/historial-cotizaciones']);
  }

  seguirCotizando(): void {
    this.router.navigate(['/productos']);
  }

  verCotizacionRegistrada(): void {
    if (!this.cotizacionRegistrada) {
      return;
    }

    this.router.navigate(['/cliente/historial-cotizaciones', this.cotizacionRegistrada.idCotizacion]);
  }

  cerrarResultado(): void {
    this.cotizacionRegistrada = null;
    this.cd.markForCheck();
  }

  private guardarCarrito(despacharEvento = true): void {
  this.carritoCotizacionService.guardarItems(
    this.items,
    undefined,
    despacharEvento
  );

  this.cd.markForCheck();
}

  private normalizarCantidadValor(valor: number): number {
    const cantidad = Math.floor(Number(valor || 1));

    return cantidad > 0 ? cantidad : 1;
  }

  trackByItem(_: number, item: CarritoCotizacionItem): number {
    return item.idProducto;
  }
}