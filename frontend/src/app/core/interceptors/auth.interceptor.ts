import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { SessionService } from '../services/session.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  /*
    Si una petición ya trae Authorization, no lo sobrescribimos.
    Esto permite que cliente-web use su propio token de cliente.
  */
  if (req.headers.has('Authorization')) {
    return next(req);
  }

  /*
    Las rutas de cliente web no deben recibir el token del panel admin.
    - login cliente
    - registro cliente
    - consultar DNI/RUC
    - carrito/cotizaciones web
  */
  if (req.url.includes('/cliente-web')) {
    return next(req);
  }

  /*
    Solo para el panel administrador usamos el token del SessionService admin.
  */
  const sessionService = inject(SessionService);
  const token = sessionService.obtenerToken();

  if (!token) {
    return next(req);
  }

  const requestConToken = req.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });

  return next(requestConToken);
};