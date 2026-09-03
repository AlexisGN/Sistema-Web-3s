import { AvisoComponent } from '../../shared/mensajes/aviso';
import { UiIconComponent } from '../../shared/ui-icon/ui-icon';
import { Subscription, filter } from 'rxjs';
import { CommonModule } from '@angular/common';
import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive, RouterOutlet, NavigationEnd } from '@angular/router';

import { ClienteWebSesion } from '../../core/models/cliente-web.model';
import { ClienteWebService } from '../../core/services/cliente-web.service';
import { CarritoCotizacionService } from '../../core/services/carrito-cotizacion.service';

@Component({
  selector: 'app-public-layout',
  standalone: true,
  imports: [AvisoComponent, CommonModule, FormsModule, RouterOutlet, RouterLink, RouterLinkActive, UiIconComponent],
  templateUrl: './public-layout.html',
  styleUrl: './public-layout.scss'
})
export class PublicLayoutComponent implements OnInit, OnDestroy {
  menuAbierto = false;
  cuentaAbierta = false;
  busquedaGlobal = '';

  headerScrolled = false;

  clienteLogueado = false;
  clienteSesion: ClienteWebSesion | null = null;
  cantidadCarrito = 0;

  toastCarritoVisible = false;
  toastCarritoTitulo = '+ Producto agregado';
  toastCarritoMensaje = 'Se agregó al carrito de cotización.';

  private toastCarritoTimeout?: number;

  private actualizarCarritoHandler = (event: Event) => {
    this.verificarSesionCliente();

    const detalle = (event as CustomEvent<{
      mostrarToast?: boolean;
      producto?: string;
    }>).detail;

    if (detalle?.mostrarToast) {
      this.mostrarToastCarrito(detalle.producto);
    }
  };

  constructor(
    private router: Router,
    private clienteWebService: ClienteWebService,
    private carritoCotizacionService: CarritoCotizacionService
  ) { }

  @HostListener('window:scroll')
  onWindowScroll(): void {
    this.headerScrolled = window.scrollY > 80;
  }

  private navigation?: Subscription;
  @HostListener('document:keydown.escape') cerrarDesplegables(): void { this.menuAbierto = false; this.cuentaAbierta = false; }
  ngOnInit(): void {
    this.navigation = this.router.events.pipe(filter(event => event instanceof NavigationEnd)).subscribe(() => {
      this.cerrarDesplegables();
      window.scrollTo({ top: 0, behavior: 'instant' });
    });
    this.verificarSesionCliente();
    this.onWindowScroll();

    window.addEventListener('storage', this.actualizarCarritoHandler);
    window.addEventListener('carritoCotizacionActualizado', this.actualizarCarritoHandler);
    window.addEventListener('clienteWebSesionActualizada', this.actualizarCarritoHandler);
  }

  ngOnDestroy(): void {
    this.navigation?.unsubscribe();
    window.removeEventListener('storage', this.actualizarCarritoHandler);
    window.removeEventListener('carritoCotizacionActualizado', this.actualizarCarritoHandler);
    window.removeEventListener('clienteWebSesionActualizada', this.actualizarCarritoHandler);

    if (this.toastCarritoTimeout) {
      window.clearTimeout(this.toastCarritoTimeout);
    }
  }

  verificarSesionCliente(): void {
  const sesionCliente = this.clienteWebService.obtenerSesion();

  this.clienteSesion = sesionCliente;
  this.clienteLogueado = !!sesionCliente;

  if (!this.clienteLogueado) {
    this.cuentaAbierta = false;
    this.cantidadCarrito = 0;
    return;
  }

  this.cantidadCarrito = this.carritoCotizacionService.contarItems();
}

  get nombreClienteCorto(): string {
    const nombre = this.clienteSesion?.nombreCliente?.trim();

    if (!nombre) {
      return 'Mi cuenta';
    }

    const partes = nombre.split(' ').filter(Boolean);

    if (partes.length === 0) {
      return 'Mi cuenta';
    }

    if (partes[0].length > 12) {
      return partes[0].substring(0, 12) + '...';
    }

    return partes[0];
  }

  mostrarToastCarrito(nombreProducto?: string): void {
    this.toastCarritoTitulo = '+ Producto agregado';

    this.toastCarritoMensaje = nombreProducto
      ? `${nombreProducto} se agregó al carrito de cotización.`
      : 'El producto se agregó al carrito de cotización.';

    this.toastCarritoVisible = true;

    if (this.toastCarritoTimeout) {
      window.clearTimeout(this.toastCarritoTimeout);
    }

    this.toastCarritoTimeout = window.setTimeout(() => {
      this.toastCarritoVisible = false;
    }, 2600);
  }

  toggleMenu(): void {
    this.menuAbierto = !this.menuAbierto;
    this.cuentaAbierta = false;
  }

  cerrarMenu(): void {
    this.menuAbierto = false;
  }

  toggleCuenta(): void {
    if (!this.clienteLogueado) {
      this.irClienteLogin();
      return;
    }

    this.cuentaAbierta = !this.cuentaAbierta;
    this.menuAbierto = false;
  }

  cerrarCuenta(): void {
    this.cuentaAbierta = false;
  }

  buscarDesdeMenu(): void {
    const texto = this.busquedaGlobal.trim();

    this.router.navigate(['/buscar'], {
      queryParams: texto ? { q: texto } : {}
    });

    this.cerrarMenu();
    this.cerrarCuenta();
  }

  irCarrito(): void {
    this.router.navigate(['/cliente/carrito']);
    this.cerrarCuenta();
  }

  irClienteLogin(): void {
    this.router.navigate(['/cliente/login']);
    this.cerrarCuenta();
  }

  irClientePerfil(): void {
    this.router.navigate(['/cliente/perfil']);
    this.cerrarCuenta();
  }

  irClienteRegistro(): void {
    this.router.navigate(['/cliente/registro']);
    this.cerrarCuenta();
  }

  irClienteHistorial(): void {
    this.router.navigate(['/cliente/historial-cotizaciones']);
    this.cerrarCuenta();
  }

  cerrarSesionCliente(): void {
    this.clienteWebService.cerrarSesion();
    this.verificarSesionCliente();
    this.cuentaAbierta = false;
    this.router.navigate(['/']);
  }

  abrirWhatsAppGeneral(): void {
    const telefono = '51948327667';
    const mensaje = 'Hola, deseo información sobre los productos y servicios industriales de 3S.';
    const url = `https://wa.me/${telefono}?text=${encodeURIComponent(mensaje)}`;

    window.open(url, '_blank');
  }
}