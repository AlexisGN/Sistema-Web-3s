import {inject} from '@angular/core';
import {Router} from '@angular/router';
import { Routes } from '@angular/router';



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
        title: 'Inicio | 3S', data: { titulo: 'Inicio' },
        loadComponent: () =>
          import('./public/pages/inicio-publico/inicio-publico').then(m => m.InicioPublicoComponent)
      },
      {
        path: 'buscar',
        title: 'Buscar en el catálogo | 3S', data: { titulo: 'Buscar en el catálogo' },
        loadComponent: () =>
          import('./public/pages/busqueda-publica/busqueda-publica').then(m => m.BusquedaPublicaComponent)
      },
      {
        path: 'productos',
        title: 'Productos industriales | 3S', data: { titulo: 'Productos industriales',
          origen: 'productos'
        },
        loadComponent: () =>
          import('./public/pages/productos-publico/productos-publico').then(m => m.ProductosPublicoComponent)
      },
      {
        path: 'productos/:id',
        title: 'Detalle de producto | 3S', data: { titulo: 'Detalle de producto' },
        loadComponent: () =>
          import('./public/pages/producto-detalle-publico/producto-detalle-publico').then(m => m.ProductoDetallePublicoComponent)
      },
      {
        path: 'categorias',
        title: 'Categorías industriales | 3S', data: { titulo: 'Categorías industriales',
          origen: 'categorias'
        },
        loadComponent: () =>
          import('./public/pages/productos-publico/productos-publico').then(m => m.ProductosPublicoComponent)
      },
      {
        path: 'categorias/:id',
        title: 'Catálogo por categoría | 3S', data: { titulo: 'Catálogo por categoría',
          origen: 'categoria-detalle'
        },
        loadComponent: () =>
          import('./public/pages/productos-publico/productos-publico').then(m => m.ProductosPublicoComponent)
      },
      {
        path: 'marcas/:id',
        title: 'Catálogo por marca | 3S', data: { titulo: 'Catálogo por marca',
          origen: 'marca-detalle'
        },
        loadComponent: () =>
          import('./public/pages/productos-publico/productos-publico').then(m => m.ProductosPublicoComponent)
      },
      {
        path: 'servicios',
        title: 'Servicios industriales | 3S', data: { titulo: 'Servicios industriales' },
        loadComponent: () =>
          import('./public/pages/servicios-publico/servicios-publico').then(m => m.ServiciosPublicoComponent)
      },
      {
        path: 'nosotros',
        title: 'Nosotros | 3S', data: { titulo: 'Nosotros' },
        loadComponent: () =>
          import('./public/pages/nosotros-publico/nosotros-publico').then(m => m.NosotrosPublicoComponent)
      },
      {
        path: 'servicios/:id',
        title: 'Detalle de servicio | 3S', data: { titulo: 'Detalle de servicio' },
        loadComponent: () =>
          import('./public/pages/servicio-detalle-publico/servicio-detalle-publico').then(m => m.ServicioDetallePublicoComponent)
      },
      
      
      
      
      
      
      
      
      ]
  },
  
  
  {
    path: '**',
    loadComponent: () => import('./public/layout/public-layout').then(m => m.PublicLayoutComponent),
    children: [{ path: '', title: 'Página no encontrada | 3S', data: { titulo: 'Página no encontrada' }, loadComponent: () => import('./public/pages/no-encontrado/no-encontrado').then(m => m.NoEncontradoComponent) }]
  }
];
