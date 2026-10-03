import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  AnularVenta,
  ClienteVenta,
  ElementoVentaDisponible,
  PagoVentaCrear,
  ResultadoPaginado,
  SiguienteComprobante,
  VentaCrear,
  VentaDetalleCompleto,
  VentaListado,
  VentaRegistroResultado
} from '../models/venta.model';

@Injectable({
  providedIn: 'root'
})
export class VentaService {
  private readonly apiUrl = `${environment.apiUrl}/venta`;
  private readonly clienteUrl = `${environment.apiUrl}/cliente`;
  private readonly productoUrl = `${environment.apiUrl}/producto`;
  private readonly servicioUrl = `${environment.apiUrl}/servicio`;
  private readonly cotizacionUrl = `${environment.apiUrl}/cotizacion`;

  constructor(private http: HttpClient) { }

  listar(
    buscar: string = '',
    estadoPago: string = '',
    tipoComprobante: string = '',
    origenVenta: string = '',
    fechaInicio: string = '',
    fechaFin: string = '',
    pagina: number = 1,
    tamanioPagina: number = 8
  ): Observable<ResultadoPaginado<VentaListado>> {
    let params = new HttpParams()
      .set('pagina', pagina)
      .set('tamanioPagina', tamanioPagina);

    if (buscar.trim().length > 0) {
      params = params.set('buscar', buscar.trim());
    }

    if (estadoPago.trim().length > 0) {
      params = params.set('estadoPago', estadoPago.trim());
    }

    if (tipoComprobante.trim().length > 0) {
      params = params.set('tipoComprobante', tipoComprobante.trim());
    }

    if (origenVenta.trim().length > 0) {
      params = params.set('origenVenta', origenVenta.trim());
    }

    if (fechaInicio) {
      params = params.set('fechaInicio', fechaInicio);
    }

    if (fechaFin) {
      params = params.set('fechaFin', fechaFin);
    }

    return this.http.get<ResultadoPaginado<VentaListado>>(this.apiUrl, { params });
  }

  registrar(venta: VentaCrear): Observable<VentaRegistroResultado> {
    return this.http.post<VentaRegistroResultado>(this.apiUrl, venta);
  }

  obtenerDetalle(idVenta: number): Observable<VentaDetalleCompleto> {
    return this.http.get<VentaDetalleCompleto>(`${this.apiUrl}/${idVenta}/detalle`);
  }

  registrarPago(pago: PagoVentaCrear): Observable<VentaRegistroResultado> {
    return this.http.post<VentaRegistroResultado>(`${this.apiUrl}/pago`, pago);
  }

  anular(idVenta: number, dto: AnularVenta): Observable<{ mensaje: string }> {
    return this.http.put<{ mensaje: string }>(`${this.apiUrl}/${idVenta}/anular`, dto);
  }

  obtenerPdf(idVenta: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${idVenta}/pdf`, {
      responseType: 'blob'
    });
  }
  obtenerSiguienteComprobante(tipoComprobante: string): Observable<SiguienteComprobante> {
  const params = new HttpParams().set('tipoComprobante', tipoComprobante);

  return this.http.get<SiguienteComprobante>(`${this.apiUrl}/siguiente-comprobante`, {
    params
  });
}

  obtenerReportePdf(
    buscar: string = '',
    estadoPago: string = '',
    tipoComprobante: string = '',
    origenVenta: string = '',
    fechaInicio: string = '',
    fechaFin: string = ''
  ): Observable<Blob> {
    let params = this.crearParametrosReporte(
      buscar,
      estadoPago,
      tipoComprobante,
      origenVenta,
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
    tipoComprobante: string = '',
    origenVenta: string = '',
    fechaInicio: string = '',
    fechaFin: string = ''
  ): Observable<Blob> {
    let params = this.crearParametrosReporte(
      buscar,
      estadoPago,
      tipoComprobante,
      origenVenta,
      fechaInicio,
      fechaFin
    );

    return this.http.get(`${this.apiUrl}/reporte/excel`, {
      params,
      responseType: 'blob'
    });
  }

  listarClientes(
    buscar: string = '',
    pagina: number = 1,
    tamanioPagina: number = 10
  ): Observable<ResultadoPaginado<ClienteVenta>> {
    let params = new HttpParams()
      .set('pagina', pagina)
      .set('tamanioPagina', tamanioPagina);

    if (buscar.trim().length > 0) {
      params = params.set('buscar', buscar.trim());
    }

    return this.http.get<ResultadoPaginado<ClienteVenta>>(this.clienteUrl, { params });
  }

  listarProductos(
    buscar: string = '',
    pagina: number = 1,
    tamanioPagina: number = 10
  ): Observable<ResultadoPaginado<ElementoVentaDisponible>> {
    let params = new HttpParams()
      .set('pagina', pagina)
      .set('tamanioPagina', tamanioPagina);

    if (buscar.trim().length > 0) {
      params = params.set('buscar', buscar.trim());
    }

    return this.http.get<ResultadoPaginado<ElementoVentaDisponible>>(this.productoUrl, { params });
  }

  listarServicios(
    buscar: string = '',
    pagina: number = 1,
    tamanioPagina: number = 10
  ): Observable<ResultadoPaginado<ElementoVentaDisponible>> {
    let params = new HttpParams()
      .set('pagina', pagina)
      .set('tamanioPagina', tamanioPagina);

    if (buscar.trim().length > 0) {
      params = params.set('buscar', buscar.trim());
    }

    return this.http.get<ResultadoPaginado<ElementoVentaDisponible>>(this.servicioUrl, { params });
  }

  obtenerCotizacionParaVenta(idCotizacion: number): Observable<any> {
    return this.http.get<any>(`${this.cotizacionUrl}/${idCotizacion}`);
  }

  private crearParametrosReporte(
    buscar: string,
    estadoPago: string,
    tipoComprobante: string,
    origenVenta: string,
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

    if (tipoComprobante.trim().length > 0) {
      params = params.set('tipoComprobante', tipoComprobante.trim());
    }

    if (origenVenta.trim().length > 0) {
      params = params.set('origenVenta', origenVenta.trim());
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