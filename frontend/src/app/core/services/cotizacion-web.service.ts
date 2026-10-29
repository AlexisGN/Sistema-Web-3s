import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ClienteWebService } from './cliente-web.service';
import {
  CotizacionWebCrearRequest,
  CotizacionWebDetalle,
  CotizacionWebDetalleItem,
  CotizacionWebRegistradaResponse,
  CotizacionWebResumen,
  ResultadoPaginadoWeb
} from '../models/cotizacion-web.model';

@Injectable({
  providedIn: 'root'
})
export class CotizacionWebService {
  private readonly apiUrl = `${environment.apiUrl}/cliente-web`;

  constructor(
    private http: HttpClient,
    private clienteWebService: ClienteWebService
  ) {}

  registrarCotizacion(data: CotizacionWebCrearRequest): Observable<CotizacionWebRegistradaResponse> {
    return this.http
      .post<CotizacionWebRegistradaResponse>(
        `${this.apiUrl}/cotizaciones`,
        data,
        { headers: this.obtenerHeadersAutorizacion() }
      )
      .pipe(map(response => this.mapearCotizacionRegistrada(response)));
  }

  listarCotizaciones(
    pagina = 1,
    tamanioPagina = 10
  ): Observable<ResultadoPaginadoWeb<CotizacionWebResumen>> {
    const params = new HttpParams()
      .set('pagina', pagina)
      .set('tamanioPagina', tamanioPagina);

    return this.http
      .get<ResultadoPaginadoWeb<CotizacionWebResumen>>(
        `${this.apiUrl}/cotizaciones`,
        {
          params,
          headers: this.obtenerHeadersAutorizacion()
        }
      )
      .pipe(map(response => this.mapearResultadoPaginado(response)));
  }

  obtenerCotizacion(idCotizacion: number): Observable<CotizacionWebDetalle> {
    return this.http
      .get<CotizacionWebDetalle>(
        `${this.apiUrl}/cotizaciones/${idCotizacion}`,
        { headers: this.obtenerHeadersAutorizacion() }
      )
      .pipe(map(response => this.mapearCotizacionDetalle(response)));
  }

  private obtenerHeadersAutorizacion(): HttpHeaders {
    const token = this.clienteWebService.obtenerToken();

    return new HttpHeaders({
      Authorization: `Bearer ${token}`
    });
  }

  private mapearCotizacionRegistrada(
    response: CotizacionWebRegistradaResponse
  ): CotizacionWebRegistradaResponse {
    return {
      idCotizacion: response.idCotizacion ?? response.IdCotizacion ?? 0,
      codigoCotizacion: response.codigoCotizacion ?? response.CodigoCotizacion ?? '',
      fechaCotizacion: String(response.fechaCotizacion ?? response.FechaCotizacion ?? ''),
      estadoCotizacion: response.estadoCotizacion ?? response.EstadoCotizacion ?? '',
      origenCotizacion: response.origenCotizacion ?? response.OrigenCotizacion ?? 'Web',

      subtotal: response.subtotal ?? response.Subtotal ?? 0,
      descuento: response.descuento ?? response.Descuento ?? 0,
      igv: response.igv ?? response.Igv ?? 0,
      total: response.total ?? response.Total ?? 0,

      esEmpresa: response.esEmpresa ?? response.EsEmpresa ?? false,
      mensaje: response.mensaje ?? response.Mensaje ?? 'Tu solicitud fue enviada correctamente.'
    };
  }

  private mapearResultadoPaginado(
    response: ResultadoPaginadoWeb<CotizacionWebResumen>
  ): ResultadoPaginadoWeb<CotizacionWebResumen> {
    const itemsOriginales = response.items ?? response.Items ?? [];

    const pagina = response.pagina ?? response.Pagina ?? 1;
    const tamanioPagina = response.tamanioPagina ?? response.TamanioPagina ?? 10;
    const totalRegistros = response.totalRegistros ?? response.TotalRegistros ?? 0;
    const totalPaginas = Math.ceil(totalRegistros / tamanioPagina);

    return {
      items: itemsOriginales.map(item => this.mapearCotizacionResumen(item)),
      pagina,
      tamanioPagina,
      totalRegistros,
      totalPaginas,
      hayMas: pagina < totalPaginas
    };
  }

  private mapearCotizacionResumen(response: CotizacionWebResumen): CotizacionWebResumen {
    return {
      idCotizacion: response.idCotizacion ?? response.IdCotizacion ?? 0,
      codigoCotizacion: response.codigoCotizacion ?? response.CodigoCotizacion ?? '',
      fechaCotizacion: String(response.fechaCotizacion ?? response.FechaCotizacion ?? ''),
      estadoCotizacion: response.estadoCotizacion ?? response.EstadoCotizacion ?? '',
      origenCotizacion: response.origenCotizacion ?? response.OrigenCotizacion ?? 'Web',
      observacion: response.observacion ?? response.Observacion ?? null,
      cantidadProductos: response.cantidadProductos ?? response.CantidadProductos ?? 0,

      subtotal: response.subtotal ?? response.Subtotal ?? 0,
      descuento: response.descuento ?? response.Descuento ?? 0,
      igv: response.igv ?? response.Igv ?? 0,
      total: response.total ?? response.Total ?? 0
    };
  }

  private mapearCotizacionDetalle(response: CotizacionWebDetalle): CotizacionWebDetalle {
    const detalles = response.detalles ?? response.Detalles ?? [];

    return {
      idCotizacion: response.idCotizacion ?? response.IdCotizacion ?? 0,
      codigoCotizacion: response.codigoCotizacion ?? response.CodigoCotizacion ?? '',
      fechaCotizacion: String(response.fechaCotizacion ?? response.FechaCotizacion ?? ''),
      estadoCotizacion: response.estadoCotizacion ?? response.EstadoCotizacion ?? '',
      origenCotizacion: response.origenCotizacion ?? response.OrigenCotizacion ?? 'Web',
      observacion: response.observacion ?? response.Observacion ?? null,

      subtotal: response.subtotal ?? response.Subtotal ?? 0,
      descuento: response.descuento ?? response.Descuento ?? 0,
      igv: response.igv ?? response.Igv ?? 0,
      total: response.total ?? response.Total ?? 0,

      detalles: detalles.map(item => this.mapearDetalleItem(item))
    };
  }

  private mapearDetalleItem(response: CotizacionWebDetalleItem): CotizacionWebDetalleItem {
    return {
      idDetalleCotizacion: response.idDetalleCotizacion ?? response.IdDetalleCotizacion ?? 0,
      idElementoCatalogo: response.idElementoCatalogo ?? response.IdElementoCatalogo ?? 0,
      idProducto: response.idProducto ?? response.IdProducto ?? null,

      codigoProducto: response.codigoProducto ?? response.CodigoProducto ?? '',
      nombreProducto: response.nombreProducto ?? response.NombreProducto ?? '',
      imagenUrl: response.imagenUrl ?? response.ImagenUrl ?? '',

      cantidad: response.cantidad ?? response.Cantidad ?? 0,
      precioUnitario: response.precioUnitario ?? response.PrecioUnitario ?? 0,
      subtotal: response.subtotal ?? response.Subtotal ?? 0,

      observacion: response.observacion ?? response.Observacion ?? null
    };
  }
}