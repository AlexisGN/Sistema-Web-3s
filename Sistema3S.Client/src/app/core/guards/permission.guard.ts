import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { SessionService } from '../services/session.service';

export const permissionGuard: CanActivateFn = (route, state) => {
  const sessionService = inject(SessionService);
  const router = inject(Router);

  if (!sessionService.estaAutenticado()) {
    return router.createUrlTree(['/login'], {
      queryParams: {
        returnUrl: state.url
      }
    });
  }

  const permisos = route.data?.['permisos'] as string[] | undefined;

  if (!permisos || permisos.length === 0) {
    return true;
  }

  if (sessionService.tieneAlgunPermiso(permisos)) {
    return true;
  }

  const rutaInicial = sessionService.obtenerRutaInicial();

  if (state.url === rutaInicial) {
    return true;
  }

  return router.createUrlTree([rutaInicial]);
};