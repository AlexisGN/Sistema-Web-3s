import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ResultadoPaginado } from '../models/resultado-paginado.model';
import {
  ClienteSelector,
  CotizacionCambiarEstado,
  CotizacionConvertirVenta,
  CotizacionCrear,
  CotizacionEnviar,
  CotizacionEnviarWhatsAppResponse,
  CotizacionListado,
  CotizacionMarcarRespondida,
  CotizacionPdfResponse,
  CotizacionWhatsApp,
  ElementoCotizable,
  EstadoCotizacion,
  MensajeResponse
} from '../models/cotizacion.model';

@Injectable({
  providedIn: 'root'
})
export class CotizacionService {
  private readonly apiUrl = `${environment.apiUrl}/cotizacion`;

  constructor(private http: HttpClient) {}

  listar(
    buscar?: string,
    estado?: string,
    origen?: string,
    pagina: number = 1,
    tamanioPagina: number = 8
  ): Observable<ResultadoPaginado<CotizacionListado>> {
    let params = new HttpParams()
      .set('pagina', String(pagina))
      .set('tamanioPagina', String(tamanioPagina));

    if (buscar && buscar.trim().length > 0) {
      params = params.set('buscar', buscar.trim());
    }

    if (estado && estado.trim().length > 0) {
      params = params.set('estado', estado.trim());
    }

    if (origen && origen.trim().length > 0) {
      params = params.set('origen', origen.trim());
    }

    return this.http.get<ResultadoPaginado<CotizacionListado>>(this.apiUrl, { params });
  }

  obtenerPorId(idCotizacion: number): Observable<CotizacionListado> {
    return this.http.get<CotizacionListado>(`${this.apiUrl}/${idCotizacion}`);
  }

  crear(cotizacion: CotizacionCrear): Observable<CotizacionListado> {
    return this.http.post<CotizacionListado>(this.apiUrl, cotizacion);
  }

  cancelar(
    idCotizacion: number,
    idUsuarioAtencion: number | null = 1
  ): Observable<MensajeResponse> {
    return this.http.put<MensajeResponse>(`${this.apiUrl}/${idCotizacion}/cancelar`, {
      idUsuarioAtencion
    });
  }

  cambiarEstado(
    idCotizacion: number,
    nuevoEstado: string,
    idUsuarioAtencion: number | null = 1
  ): Observable<MensajeResponse> {
    const body: CotizacionCambiarEstado = {
      nuevoEstado,
      idUsuarioAtencion
    };

    return this.http.put<MensajeResponse>(`${this.apiUrl}/${idCotizacion}/estado`, body);
  }

  aprobar(
    idCotizacion: number,
    idUsuarioAtencion: number | null = 1
  ): Observable<MensajeResponse> {
    return this.cambiarEstado(idCotizacion, 'Aprobada', idUsuarioAtencion);
  }

  generarPdf(idCotizacion: number): Observable<CotizacionPdfResponse> {
    return this.http.post<CotizacionPdfResponse>(
      `${this.apiUrl}/${idCotizacion}/generar-pdf`,
      {}
    );
  }

  enviarCorreo(
    idCotizacion: number,
    idUsuarioAtencion: number | null = 1
  ): Observable<MensajeResponse> {
    const body: CotizacionEnviar = {
      idUsuarioAtencion
    };

    return this.http.post<MensajeResponse>(
      `${this.apiUrl}/${idCotizacion}/enviar-correo`,
      body
    );
  }

  obtenerWhatsApp(idCotizacion: number): Observable<CotizacionWhatsApp> {
    return this.http.get<CotizacionWhatsApp>(`${this.apiUrl}/${idCotizacion}/whatsapp`);
  }

  enviarWhatsApp(
    idCotizacion: number,
    idUsuarioAtencion: number | null = 1
  ): Observable<CotizacionEnviarWhatsAppResponse> {
    const body: CotizacionEnviar = {
      idUsuarioAtencion
    };

    return this.http.post<CotizacionEnviarWhatsAppResponse>(
      `${this.apiUrl}/${idCotizacion}/enviar-whatsapp`,
      body
    );
  }

  marcarRespondidaWhatsApp(
    idCotizacion: number,
    idUsuarioAtencion: number | null = 1
  ): Observable<MensajeResponse> {
    const body: CotizacionMarcarRespondida = {
      idUsuarioAtencion,
      canalEnvio: 'WhatsApp'
    };

    return this.http.put<MensajeResponse>(
      `${this.apiUrl}/${idCotizacion}/marcar-respondida`,
      body
    );
  }

  marcarRespondidaCorreo(
    idCotizacion: number,
    idUsuarioAtencion: number | null = 1
  ): Observable<MensajeResponse> {
    const body: CotizacionMarcarRespondida = {
      idUsuarioAtencion,
      canalEnvio: 'Correo'
    };

    return this.http.put<MensajeResponse>(
      `${this.apiUrl}/${idCotizacion}/marcar-respondida`,
      body
    );
  }

  prepararParaVenta(idCotizacion: number): Observable<CotizacionListado> {
    return this.http.get<CotizacionListado>(`${this.apiUrl}/${idCotizacion}/preparar-venta`);
  }

  marcarConvertidaVenta(
    idCotizacion: number,
    idVenta: number,
    idUsuarioAtencion: number | null = 1
  ): Observable<MensajeResponse> {
    const body: CotizacionConvertirVenta = {
      idVenta,
      idUsuarioAtencion
    };

    return this.http.put<MensajeResponse>(
      `${this.apiUrl}/${idCotizacion}/marcar-convertida-venta`,
      body
    );
  }

  contarPendientes(): Observable<{ total: number }> {
    return this.http.get<{ total: number }>(`${this.apiUrl}/total-pendientes`);
  }

  listarClientes(): Observable<ClienteSelector[]> {
    return this.http.get<ClienteSelector[]>(`${this.apiUrl}/clientes`);
  }

  listarElementosCotizables(): Observable<ElementoCotizable[]> {
    return this.http.get<ElementoCotizable[]>(`${this.apiUrl}/elementos-cotizables`);
  }

  listarEstados(): Observable<EstadoCotizacion[]> {
    return this.http.get<EstadoCotizacion[]>(`${this.apiUrl}/estados`);
  }

  obtenerUrlArchivo(archivoPdf?: string | null): string {
    if (!archivoPdf || archivoPdf.trim().length === 0) {
      return '';
    }

    if (archivoPdf.startsWith('http://') || archivoPdf.startsWith('https://')) {
      return archivoPdf;
    }

    const baseUrl = environment.apiUrl.replace('/api', '').replace(/\/$/, '');
    const ruta = archivoPdf.startsWith('/') ? archivoPdf : `/${archivoPdf}`;

    return `${baseUrl}${ruta}`;
  }
}