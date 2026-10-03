import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  UsuarioActualizar,
  UsuarioCambiarContrasena,
  UsuarioCrear,
  UsuarioListado,
  UsuarioOperacionResultado
} from '../models/usuario.model';

@Injectable({
  providedIn: 'root'
})
export class UsuarioService {
  private readonly apiUrl = `${environment.apiUrl}/usuario`;

  constructor(private http: HttpClient) {}

  listar(buscar?: string, soloActivos?: boolean | null, clientes = false): Observable<UsuarioListado[]> {
    let params = new HttpParams().set('clientes', clientes);

    if (buscar && buscar.trim()) {
      params = params.set('buscar', buscar.trim());
    }

    if (soloActivos !== null && soloActivos !== undefined) {
      params = params.set('estado', String(soloActivos));
    }

    return this.http.get<UsuarioListado[]>(this.apiUrl, { params });
  }

  obtenerPorId(idUsuario: number): Observable<UsuarioListado> {
    return this.http.get<UsuarioListado>(`${this.apiUrl}/${idUsuario}`);
  }

  crear(data: UsuarioCrear): Observable<UsuarioListado> {
    return this.http.post<UsuarioListado>(this.apiUrl, data);
  }

  actualizar(idUsuario: number, data: UsuarioActualizar): Observable<UsuarioListado> {
    return this.http.put<UsuarioListado>(`${this.apiUrl}/${idUsuario}`, data);
  }

  cambiarContrasena(
    idUsuario: number,
    data: UsuarioCambiarContrasena
  ): Observable<UsuarioOperacionResultado> {
    return this.http.put<UsuarioOperacionResultado>(
      `${this.apiUrl}/${idUsuario}/contrasena`,
      data
    );
  }

  desactivar(idUsuario: number): Observable<UsuarioOperacionResultado> {
    return this.http.put<UsuarioOperacionResultado>(
      `${this.apiUrl}/${idUsuario}/desactivar`,
      {}
    );
  }
}