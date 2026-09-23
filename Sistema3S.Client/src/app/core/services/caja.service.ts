import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  CajaAbrir,
  CajaActiva,
  CajaCerrar,
  CajaOperacionResultado,
  CajaReporte,
  CajaResumen,
  MovimientoCaja,
  MovimientoCajaManual
} from '../models/caja.model';

@Injectable({
  providedIn: 'root'
})
export class CajaService {
  private readonly apiUrl = `${environment.apiUrl}/caja`;

  constructor(private http: HttpClient) {}

  obtenerCajaActiva(idUsuario: number = 1): Observable<CajaActiva | null> {
    const params = new HttpParams().set('idUsuario', String(idUsuario));

    return this.http.get<CajaActiva | null>(`${this.apiUrl}/activa`, { params });
  }

  abrirCaja(dto: CajaAbrir): Observable<CajaActiva> {
    return this.http.post<CajaActiva>(`${this.apiUrl}/abrir`, dto);
  }

  obtenerResumen(idUsuario: number = 1, idCaja?: number | null): Observable<CajaResumen> {
    let params = new HttpParams().set('idUsuario', String(idUsuario));

    if (idCaja && idCaja > 0) {
      params = params.set('idCaja', String(idCaja));
    }

    return this.http.get<CajaResumen>(`${this.apiUrl}/resumen`, { params });
  }

  listarMovimientos(
    idUsuario: number = 1,
    idCaja?: number | null,
    fechaInicio?: string | null,
    fechaFin?: string | null
  ): Observable<MovimientoCaja[]> {
    let params = new HttpParams().set('idUsuario', String(idUsuario));

    if (idCaja && idCaja > 0) {
      params = params.set('idCaja', String(idCaja));
    }

    if (fechaInicio) {
      params = params.set('fechaInicio', fechaInicio);
    }

    if (fechaFin) {
      params = params.set('fechaFin', fechaFin);
    }

    return this.http.get<MovimientoCaja[]>(`${this.apiUrl}/movimientos`, { params });
  }

  registrarMovimientoManual(dto: MovimientoCajaManual): Observable<CajaOperacionResultado> {
    return this.http.post<CajaOperacionResultado>(`${this.apiUrl}/movimiento-manual`, dto);
  }

  cerrarCaja(dto: CajaCerrar): Observable<CajaActiva> {
    return this.http.post<CajaActiva>(`${this.apiUrl}/cerrar`, dto);
  }

  obtenerReporte(
    idUsuario: number = 1,
    fechaInicio?: string | null,
    fechaFin?: string | null
  ): Observable<CajaReporte[]> {
    let params = new HttpParams().set('idUsuario', String(idUsuario));

    if (fechaInicio) {
      params = params.set('fechaInicio', fechaInicio);
    }

    if (fechaFin) {
      params = params.set('fechaFin', fechaFin);
    }

    return this.http.get<CajaReporte[]>(`${this.apiUrl}/reporte`, { params });
  }
}