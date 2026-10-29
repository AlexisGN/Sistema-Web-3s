import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Permiso } from '../models/permiso.model';
import {
  RolActualizar,
  RolCrear,
  RolListado,
  RolOperacionResultado,
  RolPermisosActualizar,
  RolRespuestaListado
} from '../models/rol.model';

@Injectable({
  providedIn: 'root'
})
export class RolService {
  private readonly apiUrl = `${environment.apiUrl}/rol`;

  constructor(private http: HttpClient) {}

  listar(soloActivos?: boolean | null): Observable<RolListado[]> {
    let params = new HttpParams();

    if (soloActivos !== null && soloActivos !== undefined) {
      params = params.set('soloActivos', String(soloActivos));
    }

    return this.http.get<RolListado[]>(this.apiUrl, { params });
  }

  crear(data: RolCrear): Observable<RolRespuestaListado> {
    return this.http.post<RolRespuestaListado>(this.apiUrl, data);
  }

  actualizar(idRol: number, data: RolActualizar): Observable<RolRespuestaListado> {
    return this.http.put<RolRespuestaListado>(`${this.apiUrl}/${idRol}`, data);
  }

  desactivar(idRol: number): Observable<RolOperacionResultado> {
    return this.http.put<RolOperacionResultado>(`${this.apiUrl}/${idRol}/desactivar`, {});
  }

  listarPermisos(): Observable<Permiso[]> {
    return this.http.get<Permiso[]>(`${this.apiUrl}/permisos`);
  }

  obtenerPermisosPorRol(idRol: number): Observable<Permiso[]> {
    return this.http.get<Permiso[]>(`${this.apiUrl}/${idRol}/permisos`);
  }

  asignarPermisos(
    idRol: number,
    data: RolPermisosActualizar
  ): Observable<RolOperacionResultado> {
    return this.http.put<RolOperacionResultado>(
      `${this.apiUrl}/${idRol}/permisos`,
      data
    );
  }
}