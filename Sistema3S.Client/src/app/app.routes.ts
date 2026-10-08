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
      {
        path: 'cliente/recuperar',
        title: 'Recuperar cuenta | 3S', data: { modo: 'recuperar', titulo: 'Recuperar cuenta' },
        loadComponent: () => import('./public/pages/cliente-seguridad/cliente-seguridad').then(m => m.ClienteSeguridadComponent)
      },
      {
        path: 'cliente/restablecer',
        title: 'Restablecer contraseña | 3S', data: { modo: 'restablecer', titulo: 'Restablecer contraseña' },
        loadComponent: () => import('./public/pages/cliente-seguridad/cliente-seguridad').then(m => m.ClienteSeguridadComponent)
      },
      {
        path: 'cliente/seguridad',
        title: 'Seguridad de cuenta | 3S', data: { modo: 'cambiar', titulo: 'Seguridad de cuenta' },
        loadComponent: () => import('./public/pages/cliente-seguridad/cliente-seguridad').then(m => m.ClienteSeguridadComponent)
      },
      {
        path: 'cliente/login',
        title: 'Iniciar sesión | 3S', data: { titulo: 'Iniciar sesión' },
        loadComponent: () =>
          import('./public/pages/cliente-login-publico/cliente-login-publico').then(m => m.ClienteLoginPublicoComponent)
      },
      {
        path: 'cliente/registro',
        title: 'Crear cuenta | 3S', data: { titulo: 'Crear cuenta' },
        loadComponent: () =>
          import('./public/pages/cliente-registro-publico/cliente-registro-publico').then(m => m.ClienteRegistroPublicoComponent)
      },
      {
        path: 'cliente/perfil',
        title: 'Mi perfil | 3S', data: { titulo: 'Mi perfil' },
        loadComponent: () =>
          import('./public/pages/cliente-perfil-publico/cliente-perfil-publico').then(m => m.ClientePerfilPublicoComponent)
      },
      {
        path: 'cliente/carrito',
        title: 'Carrito de cotización | 3S', data: { titulo: 'Carrito de cotización' },
        loadComponent: () =>
          import('./public/pages/cliente-carrito-publico/cliente-carrito-publico').then(m => m.ClienteCarritoPublicoComponent)
      },
      {
        path: 'cliente/historial-cotizaciones',
        title: 'Mis cotizaciones | 3S', data: { titulo: 'Mis cotizaciones' },
        loadComponent: () =>
          import('./public/pages/cliente-historial-cotizaciones-publico/cliente-historial-cotizaciones-publico').then(m => m.ClienteHistorialCotizacionesPublicoComponent)
      },
      {
        path: 'cliente/historial-cotizaciones/:id',
        title: 'Detalle de cotización | 3S', data: { titulo: 'Detalle de cotización' },
        loadComponent: () =>
          import('./public/pages/cliente-cotizacion-detalle-publico/cliente-cotizacion-detalle-publico').then(m => m.ClienteCotizacionDetallePublicoComponent)
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
    loadComponent: () => import('./shared/admin-layout/admin-layout').then(m => m.AdminLayoutComponent),
    canActivate: [authGuard],
    children: [
      {
        path: 'auditoria',
        canActivate: [permissionGuard],
        data: { permisos: ['AUDITORIA_VER'] },
        loadComponent: () => import('./pages/auditoria/auditoria').then(m => m.AuditoriaComponent)
      },
      {
        path: '',
        redirectTo: 'inicio',
        pathMatch: 'full'
      },
      {
        path: 'inicio',
        loadComponent: () => import('./pages/dashboard/dashboard').then(m => m.DashboardComponent),
        canActivate: [permissionGuard],
        data: {
          permisos: ['INICIO_VER']
        }
      },
      {
        path: 'productos',
        loadComponent: () => import('./pages/productos/productos').then(m => m.ProductosComponent),
        canActivate: [permissionGuard],
        data: {
          permisos: ['PRODUCTOS_VER']
        }
      },
      {
        path: 'servicios',
        loadComponent: () => import('./pages/servicios/servicios').then(m => m.ServiciosComponent),
        canActivate: [permissionGuard],
        data: {
          permisos: ['SERVICIOS_VER']
        }
      },
      {
        path: 'cotizaciones',
        loadComponent: () => import('./pages/cotizaciones/cotizaciones').then(m => m.CotizacionesComponent),
        canActivate: [permissionGuard],
        data: {
          permisos: ['COTIZACIONES_VER']
        }
      },
      {
        path: 'clientes',
        loadComponent: () => import('./pages/clientes/clientes').then(m => m.ClientesComponent),
        canActivate: [permissionGuard],
        data: {
          permisos: ['CLIENTES_VER']
        }
      },
      {
        path: 'ventas',
        loadComponent: () => import('./pages/ventas/ventas').then(m => m.VentasComponent),
        canActivate: [permissionGuard],
        data: {
          permisos: ['VENTAS_VER']
        }
      },
      {
        path: 'proveedores',
        canActivate: [permissionGuard],
        data: {
          permisos: ['PROVEEDORES_VER']
        },
        loadComponent: () =>
          import('./pages/proveedores/proveedores').then(m => m.ProveedoresComponent)
      },
      {
        path: 'compras',
        canActivate: [permissionGuard],
        data: {
          permisos: ['COMPRAS_VER']
        },
        loadComponent: () =>
          import('./pages/compras/compras').then(m => m.ComprasComponent)
      },
      {
        path: 'inventario',
        canActivate: [permissionGuard],
        data: {
          permisos: ['INVENTARIO_VER']
        },
        loadComponent: () =>
          import('./pages/inventario/inventario').then(m => m.InventarioComponent)
      },
      {
        path: 'caja',
        canActivate: [permissionGuard],
        data: {
          permisos: ['CAJA_VER']
        },
        loadComponent: () =>
          import('./pages/caja/caja').then(m => m.CajaComponent)
      },
      {
        path: 'usuarios-roles',
        canActivate: [permissionGuard],
        data: {
          permisos: ['USUARIOS_VER', 'ROLES_VER', 'ROLES_GESTIONAR_PERMISOS']
        },
        loadComponent: () =>
          import('./pages/usuarios-roles/usuarios-roles').then(m => m.UsuariosRolesComponent)
      }
    ]
  },
  {
    path: '**',
    loadComponent: () => import('./public/layout/public-layout').then(m => m.PublicLayoutComponent),
    children: [{ path: '', title: 'Página no encontrada | 3S', data: { titulo: 'Página no encontrada' }, loadComponent: () => import('./public/pages/no-encontrado/no-encontrado').then(m => m.NoEncontradoComponent) }]
  }
];
