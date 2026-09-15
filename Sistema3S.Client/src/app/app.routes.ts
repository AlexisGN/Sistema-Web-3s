import { Routes } from '@angular/router';




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
        path: 'nosotros',
        loadComponent: () =>
          import('./public/pages/nosotros-publico/nosotros-publico').then(m => m.NosotrosPublicoComponent)
      }
]
  },
{
    path: '**',
    redirectTo: ''
  }
];