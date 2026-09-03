import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { SessionService } from '../services/session.service';
import { AuthService } from '../services/auth.service';

export const permissionGuard: CanActivateFn = (route, state) => {
  const session = inject(SessionService);
  const auth = inject(AuthService);
  const router = inject(Router);
  const login = () => router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
  if (!session.estaAutenticado()) return login();
  return auth.perfil().pipe(
    map(() => session.tieneAlgunPermiso(route.data?.['permisos'])
      ? true : router.createUrlTree([session.obtenerRutaInicial()])),
    catchError(() => of(login()))
  );
};
