import { Routes } from '@angular/router';

import { AdminLayoutComponent } from './shared/admin-layout/admin-layout';

import { ProductosComponent } from './pages/productos/productos';
import { ServiciosComponent } from './pages/servicios/servicios';
import { ClientesComponent } from './pages/clientes/clientes';

import { authGuard } from './core/guards/auth.guard';
import { permissionGuard } from './core/guards/permission.guard';

export const routes: Routes = [
{
    path: '',
    loadComponent: () =>
      import('./public/layout/public-layout').then(m => m.PublicLayoutComponent),
    children: [
{
        path: '',
        loadComponent: () =>
          import('./public/pages/inicio-publico/inicio-publico').then(m => m.InicioPublicoComponent)
      },
{
        path: 'buscar',
        loadComponent: () =>
          import('./public/pages/busqueda-publica/busqueda-publica').then(m => m.BusquedaPublicaComponent)
      },
{
        path: 'productos',
        data: {
          origen: 'productos'
        },
        loadComponent: () =>
          import('./public/pages/productos-publico/productos-publico').then(m => m.ProductosPublicoComponent)
      },
{
        path: 'productos/:id',
        loadComponent: () =>
          import('./public/pages/producto-detalle-publico/producto-detalle-publico').then(m => m.ProductoDetallePublicoComponent)
      },
{
        path: 'categorias',
        data: {
          origen: 'categorias'
        },
        loadComponent: () =>
          import('./public/pages/productos-publico/productos-publico').then(m => m.ProductosPublicoComponent)
      },
{
        path: 'categorias/:id',
        data: {
          origen: 'categoria-detalle'
        },
        loadComponent: () =>
          import('./public/pages/productos-publico/productos-publico').then(m => m.ProductosPublicoComponent)
      },
{
        path: 'marcas/:id',
        data: {
          origen: 'marca-detalle'
        },
        loadComponent: () =>
          import('./public/pages/productos-publico/productos-publico').then(m => m.ProductosPublicoComponent)
      },
{
        path: 'servicios',
        loadComponent: () =>
          import('./public/pages/servicios-publico/servicios-publico').then(m => m.ServiciosPublicoComponent)
      },
{
        path: 'nosotros',
        loadComponent: () =>
          import('./public/pages/nosotros-publico/nosotros-publico').then(m => m.NosotrosPublicoComponent)
      },
{
        path: 'servicios/:id',
        loadComponent: () =>
          import('./public/pages/servicio-detalle-publico/servicio-detalle-publico').then(m => m.ServicioDetallePublicoComponent)
      },
{
        path: 'cliente/login',
        loadComponent: () =>
          import('./public/pages/cliente-login-publico/cliente-login-publico').then(m => m.ClienteLoginPublicoComponent)
      },
{
        path: 'cliente/registro',
        loadComponent: () =>
          import('./public/pages/cliente-registro-publico/cliente-registro-publico').then(m => m.ClienteRegistroPublicoComponent)
      },
{
        path: 'cliente/perfil',
        loadComponent: () =>
          import('./public/pages/cliente-perfil-publico/cliente-perfil-publico').then(m => m.ClientePerfilPublicoComponent)
      }
]
  },
{
    path: 'login',
    loadComponent: () =>
      import('./pages/login/login').then(m => m.LoginComponent)
  },
{
    path: 'admin',
    component: AdminLayoutComponent,
    canActivate: [authGuard],
    children: [
{
        path: '',
        redirectTo: 'productos',
        pathMatch: 'full'
      },
{
        path: 'productos',
        component: ProductosComponent,
        canActivate: [permissionGuard],
        data: {
          permisos: ['PRODUCTOS_VER']
        }
      },
{
        path: 'servicios',
        component: ServiciosComponent,
        canActivate: [permissionGuard],
        data: {
          permisos: ['SERVICIOS_VER']
        }
      },
{
        path: 'clientes',
        component: ClientesComponent,
        canActivate: [permissionGuard],
        data: {
          permisos: ['CLIENTES_VER']
        }
      }
]
  },
{
    path: '**',
    redirectTo: ''
  }
];