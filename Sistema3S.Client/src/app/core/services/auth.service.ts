import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, catchError, throwError, timeout, shareReplay, finalize } from 'rxjs';
import { SessionService } from './session.service';

import { environment } from '../../../environments/environment';
import { LoginRequest, LoginResponse } from '../models/auth.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = `${environment.apiUrl}/auth`;

  private refresco?: Observable<any>;

  constructor(private http: HttpClient, private session: SessionService) {}

  login(data: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, data);
  }

  perfil(): Observable<any> {
    if (!this.refresco) {
      this.refresco = this.http.get<any>(`${this.apiUrl}/perfil`).pipe(
        timeout(8000),
        tap(data => this.session.actualizarPermisos(data)),
        catchError(err => {
          if (err.status === 401 || err.status === 403) this.session.cerrarSesion();
          else this.session.retirarPermisos();
          return throwError(() => err);
        }),
        finalize(() => this.refresco = undefined),
        shareReplay({ bufferSize: 1, refCount: true })
      );
    }
    return this.refresco;
  }
}