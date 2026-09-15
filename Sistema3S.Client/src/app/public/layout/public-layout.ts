import { CommonModule } from '@angular/common';
import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, RouterOutlet } from '@angular/router';

import { ClienteWebSesion } from '../../core/models/cliente-web.model';
import { ClienteWebService } from '../../core/services/cliente-web.service';

@Component({
  selector: 'app-public-layout',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterOutlet, RouterLink],
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

  private actualizarCarritoHandler = (event: Event) => {
    this.verificarSesionCliente();
  };

  constructor(
    private router: Router,
    private clienteWebService: ClienteWebService,
  ) { }

  @HostListener('window:scroll')
  onWindowScroll(): void {
    this.headerScrolled = window.scrollY > 80;
  }

  ngOnInit(): void {
    this.verificarSesionCliente();
    this.onWindowScroll();

    window.addEventListener('storage', this.actualizarCarritoHandler);
    window.addEventListener('clienteWebSesionActualizada', this.actualizarCarritoHandler);
  }

  ngOnDestroy(): void {
    window.removeEventListener('storage', this.actualizarCarritoHandler);
    window.removeEventListener('clienteWebSesionActualizada', this.actualizarCarritoHandler);
  }

  verificarSesionCliente(): void {
  const sesionCliente = this.clienteWebService.obtenerSesion();

  this.clienteSesion = sesionCliente;
  this.clienteLogueado = !!sesionCliente;

  if (!this.clienteLogueado) {
    this.cuentaAbierta = false;
    return;
  }

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