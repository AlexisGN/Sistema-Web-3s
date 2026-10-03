import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  AnularCompra,
  CompraCrear,
  CompraDetalleCompleto,
  CompraListado,
  CompraRegistroResultado,
  PagoCompraCrear,
  ProductoCompraDisponible,
  ProveedorCompra,
  ResultadoPaginado
} from '../models/compra.model';

@Injectable({
  providedIn: 'root'
})
export class CompraService {
  private readonly apiUrl = `${environment.apiUrl}/compra`;
  private readonly proveedorUrl = `${environment.apiUrl}/proveedor`;
  private readonly productoUrl = `${environment.apiUrl}/producto`;

  constructor(private http: HttpClient) { }

  listar(
    buscar: string = '',
    estadoPago: string = '',
    fechaInicio: string = '',
    fechaFin: string = '',
    pagina: number = 1,
    tamanioPagina: number = 8
  ): Observable<ResultadoPaginado<CompraListado>> {
    let params = new HttpParams()
      .set('pagina', pagina)
      .set('tamanioPagina', tamanioPagina);

    if (buscar.trim().length > 0) {
      params = params.set('buscar', buscar.trim());
    }

    if (estadoPago.trim().length > 0) {
      params = params.set('estadoPago', estadoPago.trim());
    }

    if (fechaInicio) {
      params = params.set('fechaInicio', fechaInicio);
    }

    if (fechaFin) {
      params = params.set('fechaFin', fechaFin);
    }

    return this.http.get<ResultadoPaginado<CompraListado>>(this.apiUrl, { params });
  }

  registrar(compra: CompraCrear): Observable<CompraRegistroResultado> {
    return this.http.post<CompraRegistroResultado>(this.apiUrl, compra);
  }

  registrarPago(pago: PagoCompraCrear): Observable<CompraRegistroResultado> {
    return this.http.post<CompraRegistroResultado>(`${this.apiUrl}/pago`, pago);
  }

  anular(idCompra: number, dto: AnularCompra): Observable<{ mensaje: string }> {
    return this.http.put<{ mensaje: string }>(`${this.apiUrl}/${idCompra}/anular`, dto);
  }

  obtenerDetalle(idCompra: number): Observable<CompraDetalleCompleto> {
    return this.http.get<CompraDetalleCompleto>(`${this.apiUrl}/${idCompra}/detalle`);
  }

  obtenerPdf(idCompra: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${idCompra}/pdf`, {
      responseType: 'blob'
    });
  }

  obtenerReportePdf(
    buscar: string = '',
    estadoPago: string = '',
    fechaInicio: string = '',
    fechaFin: string = ''
  ): Observable<Blob> {
    const params = this.crearParametrosReporte(
      buscar,
      estadoPago,
      fechaInicio,
      fechaFin
    );

    return this.http.get(`${this.apiUrl}/reporte/pdf`, {
      params,
      responseType: 'blob'
    });
  }

  obtenerReporteExcel(
    buscar: string = '',
    estadoPago: string = '',
    fechaInicio: string = '',
    fechaFin: string = ''
  ): Observable<Blob> {
    const params = this.crearParametrosReporte(
      buscar,
      estadoPago,
      fechaInicio,
      fechaFin
    );

    return this.http.get(`${this.apiUrl}/reporte/excel`, {
      params,
      responseType: 'blob'
    });
  }

  listarProveedores(
    buscar: string = '',
    pagina: number = 1,
    tamanioPagina: number = 10
  ): Observable<ResultadoPaginado<ProveedorCompra>> {
    let params = new HttpParams()
      .set('pagina', pagina)
      .set('tamanioPagina', tamanioPagina);

    if (buscar.trim().length > 0) {
      params = params.set('buscar', buscar.trim());
    }

    return this.http.get<ResultadoPaginado<ProveedorCompra>>(this.proveedorUrl, { params });
  }

  listarProductos(
    buscar: string = '',
    pagina: number = 1,
    tamanioPagina: number = 10
  ): Observable<ResultadoPaginado<ProductoCompraDisponible>> {
    let params = new HttpParams()
      .set('pagina', pagina)
      .set('tamanioPagina', tamanioPagina);

    if (buscar.trim().length > 0) {
      params = params.set('buscar', buscar.trim());
    }

    return this.http.get<ResultadoPaginado<ProductoCompraDisponible>>(this.productoUrl, { params });
  }

  private crearParametrosReporte(
    buscar: string,
    estadoPago: string,
    fechaInicio: string,
    fechaFin: string
  ): HttpParams {
    let params = new HttpParams();

    if (buscar.trim().length > 0) {
      params = params.set('buscar', buscar.trim());
    }

    if (estadoPago.trim().length > 0) {
      params = params.set('estadoPago', estadoPago.trim());
    }

    if (fechaInicio) {
      params = params.set('fechaInicio', fechaInicio);
    }

    if (fechaFin) {
      params = params.set('fechaFin', fechaFin);
    }

    return params;
  }
}