import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  ActualizarStockMinimo,
  InventarioListado,
  InventarioResumen,
  MovimientoStockListado,
  RegistrarMovimientoStock,
  ResultadoPaginado
} from '../models/inventario.model';

@Injectable({
  providedIn: 'root'
})
export class InventarioService {
  private readonly apiUrl = `${environment.apiUrl}/inventario`;

  constructor(private http: HttpClient) { }

  listar(
    buscar: string = '',
    estadoStock: string = 'todos',
    pagina: number = 1,
    tamanioPagina: number = 6
  ): Observable<ResultadoPaginado<InventarioListado>> {
    let params = new HttpParams()
      .set('pagina', pagina)
      .set('tamanioPagina', tamanioPagina)
      .set('estadoStock', estadoStock);

    if (buscar.trim().length > 0) {
      params = params.set('buscar', buscar.trim());
    }

    return this.http.get<ResultadoPaginado<InventarioListado>>(this.apiUrl, { params });
  }

  obtenerPorProducto(idProducto: number): Observable<InventarioListado> {
    return this.http.get<InventarioListado>(`${this.apiUrl}/${idProducto}`);
  }

  resumen(): Observable<InventarioResumen> {
    return this.http.get<InventarioResumen>(`${this.apiUrl}/resumen`);
  }

  listarMovimientosPorProducto(idProducto: number): Observable<MovimientoStockListado[]> {
    return this.http.get<MovimientoStockListado[]>(`${this.apiUrl}/${idProducto}/movimientos`);
  }

  listarMovimientosRecientes(cantidad: number = 5): Observable<MovimientoStockListado[]> {
    const params = new HttpParams().set('cantidad', cantidad);

    return this.http.get<MovimientoStockListado[]>(
      `${this.apiUrl}/movimientos-recientes`,
      { params }
    );
  }

  actualizarStockMinimo(
    idProducto: number,
    dto: ActualizarStockMinimo
  ): Observable<InventarioListado> {
    return this.http.put<InventarioListado>(
      `${this.apiUrl}/${idProducto}/stock-minimo`,
      dto
    );
  }

  registrarMovimientoManual(dto: RegistrarMovimientoStock): Observable<InventarioListado> {
    return this.http.post<InventarioListado>(
      `${this.apiUrl}/movimiento-manual`,
      dto
    );
  }
}