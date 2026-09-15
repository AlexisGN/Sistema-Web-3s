import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { timeout } from 'rxjs';

import { environment } from '../../../environments/environment';
import { SessionService } from '../../core/services/session.service';

interface MenuItem {
  label: string;
  icon: string;
  route: string;
  permisos: string[];
}

interface MenuGroup {
  title: string;
  items: MenuItem[];
}

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './admin-layout.html',
  styleUrl: './admin-layout.scss'
})
export class AdminLayoutComponent implements OnInit, OnDestroy {
  sidebarCollapsed = false;
  mobileMenuOpen = false;

  estadoSistema: 'verificando' | 'activo' | 'inactivo' = 'verificando';
  textoEstadoSistema = 'Verificando sistema...';

  usuarioCorreo = '';
  usuarioRol = '';

  menuVisible: MenuGroup[] = [];

  private intervaloEstado?: ReturnType<typeof setInterval>;

  private readonly menuGroups: MenuGroup[] = [
{
      title: 'Comercial',
      items: [
{
          label: 'Productos',
          icon: 'PR',
          route: '/admin/productos',
          permisos: ['PRODUCTOS_VER']
        },
{
          label: 'Servicios',
          icon: 'SV',
          route: '/admin/servicios',
          permisos: ['SERVICIOS_VER']
        },
{
          label: 'Clientes',
          icon: 'CL',
          route: '/admin/clientes',
          permisos: ['CLIENTES_VER']
        }
]
    }
];

  constructor(
    private http: HttpClient,
    private router: Router,
    private sessionService: SessionService
  ) {}

  ngOnInit(): void {
    this.usuarioCorreo = this.sessionService.obtenerCorreo();
    this.usuarioRol = this.sessionService.obtenerRol();

    this.cargarMenuPermitido();
    this.verificarEstadoSistema();

    this.intervaloEstado = setInterval(() => {
      this.verificarEstadoSistema();
    }, 15000);
  }

  ngOnDestroy(): void {
    if (this.intervaloEstado) {
      clearInterval(this.intervaloEstado);
    }
  }

  cargarMenuPermitido(): void {
    if (this.sessionService.esAdministrador()) {
      this.menuVisible = this.menuGroups;
      return;
    }

    this.menuVisible = this.menuGroups
      .map(group => {
        const itemsPermitidos = group.items.filter(item =>
          this.sessionService.tieneAlgunPermiso(item.permisos)
        );

        return {
          title: group.title,
          items: itemsPermitidos
        };
      })
      .filter(group => group.items.length > 0);
  }

  verificarEstadoSistema(): void {
    this.estadoSistema = 'verificando';
    this.textoEstadoSistema = 'Verificando sistema...';

    this.http
      .get(`${environment.apiUrl}/prueba/conexion`)
      .pipe(timeout(4000))
      .subscribe({
        next: () => {
          this.estadoSistema = 'activo';
          this.textoEstadoSistema = 'Sistema activo';
        },
        error: () => {
          this.estadoSistema = 'inactivo';
          this.textoEstadoSistema = 'Backend desconectado';
        }
      });
  }

  toggleSidebar(): void {
    this.sidebarCollapsed = !this.sidebarCollapsed;
  }

  toggleMobileMenu(): void {
    this.mobileMenuOpen = !this.mobileMenuOpen;
  }

  closeMobileMenu(): void {
    this.mobileMenuOpen = false;
  }

  cerrarSesion(): void {
    this.sessionService.cerrarSesion();
    this.router.navigate(['/login']);
  }

  trackByGroup(index: number, group: MenuGroup): string {
    return group.title;
  }

  trackByItem(index: number, item: MenuItem): string {
    return item.route;
  }
}