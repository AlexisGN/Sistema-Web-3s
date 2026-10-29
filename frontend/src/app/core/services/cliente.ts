import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  ClienteActualizar,
  ClienteCrear,
  ClienteListado,
  ConsultaDniResultado,
  ConsultaRucResultado,
  ResultadoPaginadoClientes,
  TipoCliente,
  TipoDocumento,
  Ubigeo
} from '../models/cliente.model';

@Injectable({
  providedIn: 'root'
})
export class ClienteService {
  private readonly apiUrl = `${environment.apiUrl}/cliente`;

  constructor(private http: HttpClient) {}

  listar(
    buscar: string = '',
    pagina: number = 1,
    tamanioPagina: number = 5
  ): Observable<ResultadoPaginadoClientes> {
    let params = new HttpParams()
      .set('pagina', pagina)
      .set('tamanioPagina', tamanioPagina);

    if (buscar.trim().length > 0) {
      params = params.set('buscar', buscar.trim());
    }

    return this.http.get<ResultadoPaginadoClientes>(this.apiUrl, { params });
  }

  obtenerPorId(idCliente: number): Observable<ClienteListado> {
    return this.http.get<ClienteListado>(`${this.apiUrl}/${idCliente}`);
  }

  crear(cliente: ClienteCrear): Observable<ClienteListado> {
    return this.http.post<ClienteListado>(this.apiUrl, cliente);
  }

  actualizar(idCliente: number, cliente: ClienteActualizar): Observable<{ mensaje: string }> {
    return this.http.put<{ mensaje: string }>(`${this.apiUrl}/${idCliente}`, cliente);
  }

  eliminar(idCliente: number): Observable<{ mensaje: string }> {
    return this.http.delete<{ mensaje: string }>(`${this.apiUrl}/${idCliente}`);
  }

  contarActivos(): Observable<{ total: number }> {
    return this.http.get<{ total: number }>(`${this.apiUrl}/total-activos`);
  }

  listarTiposCliente(): Observable<TipoCliente[]> {
    return this.http.get<TipoCliente[]>(`${this.apiUrl}/tipos-cliente`);
  }

  listarTiposDocumento(): Observable<TipoDocumento[]> {
    return this.http.get<TipoDocumento[]>(`${this.apiUrl}/tipos-documento`);
  }

  listarUbigeos(buscar: string = ''): Observable<Ubigeo[]> {
  let params = new HttpParams();

  if (buscar.trim().length > 0) {
    params = params.set('buscar', buscar.trim());
  }

  return this.http.get<Ubigeo[]>(`${this.apiUrl}/ubigeos`, { params });
}
consultarDni(dni: string): Observable<ConsultaDniResultado> {
  return this.http.get<ConsultaDniResultado>(`${this.apiUrl}/consultar-dni/${dni}`);
}

consultarRuc(ruc: string): Observable<ConsultaRucResultado> {
  return this.http.get<ConsultaRucResultado>(`${this.apiUrl}/consultar-ruc/${ruc}`);
}
}