import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  ConsultaRucProveedorResultado,
  ProveedorActualizar,
  ProveedorCrear,
  ProveedorListado,
  ResultadoPaginado,
  Ubigeo
} from '../models/proveedor.model';

@Injectable({
  providedIn: 'root'
})
export class ProveedorService {
  private readonly apiUrl = `${environment.apiUrl}/proveedor`;

  constructor(private http: HttpClient) { }

  listar(
    buscar: string = '',
    pagina: number = 1,
    tamanioPagina: number = 5
  ): Observable<ResultadoPaginado<ProveedorListado>> {
    let params = new HttpParams()
      .set('pagina', pagina)
      .set('tamanioPagina', tamanioPagina);

    if (buscar.trim().length > 0) {
      params = params.set('buscar', buscar.trim());
    }

    return this.http.get<ResultadoPaginado<ProveedorListado>>(this.apiUrl, { params });
  }

  obtenerPorId(idProveedor: number): Observable<ProveedorListado> {
    return this.http.get<ProveedorListado>(`${this.apiUrl}/${idProveedor}`);
  }

  crear(proveedor: ProveedorCrear): Observable<ProveedorListado> {
    return this.http.post<ProveedorListado>(this.apiUrl, proveedor);
  }

  actualizar(
    idProveedor: number,
    proveedor: ProveedorActualizar
  ): Observable<{ mensaje: string }> {
    return this.http.put<{ mensaje: string }>(`${this.apiUrl}/${idProveedor}`, proveedor);
  }

  eliminar(idProveedor: number): Observable<{ mensaje: string }> {
    return this.http.delete<{ mensaje: string }>(`${this.apiUrl}/${idProveedor}`);
  }

  consultarRuc(ruc: string): Observable<ConsultaRucProveedorResultado> {
    return this.http.get<ConsultaRucProveedorResultado>(`${this.apiUrl}/consultar-ruc/${ruc}`);
  }

  contarActivos(): Observable<{ total: number }> {
    return this.http.get<{ total: number }>(`${this.apiUrl}/total-activos`);
  }

  listarUbigeos(buscar: string = ''): Observable<Ubigeo[]> {
    let params = new HttpParams();

    if (buscar.trim().length > 0) {
      params = params.set('buscar', buscar.trim());
    }

    return this.http.get<Ubigeo[]>(`${environment.apiUrl}/cliente/ubigeos`, { params });
  }
}