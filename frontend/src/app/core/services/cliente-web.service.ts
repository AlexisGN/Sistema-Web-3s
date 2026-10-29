import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  ClienteWebConsultaDocumentoResponse,
  ClienteWebLoginRequest,
  ClienteWebRegistroRequest,
  ClienteWebSesion
} from '../models/cliente-web.model';

@Injectable({
  providedIn: 'root'
})
export class ClienteWebService {
  private readonly apiUrl = `${environment.apiUrl}/cliente-web`;
  private readonly storageKey = 'clienteWebSesion';

  constructor(private http: HttpClient) {}

  consultarDocumento(
    tipoDocumento: 'DNI' | 'RUC',
    numeroDocumento: string
  ): Observable<ClienteWebConsultaDocumentoResponse> {
    const params = new HttpParams()
      .set('tipoDocumento', tipoDocumento)
      .set('numeroDocumento', numeroDocumento);

    return this.http
      .get<ClienteWebConsultaDocumentoResponse>(`${this.apiUrl}/consultar-documento`, { params })
      .pipe(
        map(response => this.mapearConsultaDocumento(response))
      );
  }

  registrar(data: ClienteWebRegistroRequest): Observable<ClienteWebSesion> {
    return this.http.post<ClienteWebSesion>(`${this.apiUrl}/registro`, data).pipe(
      map(response => this.mapearSesion(response)),
      tap(sesion => this.guardarSesion(sesion))
    );
  }

  login(data: ClienteWebLoginRequest): Observable<ClienteWebSesion> {
    return this.http.post<ClienteWebSesion>(`${this.apiUrl}/login`, data).pipe(
      map(response => this.mapearSesion(response)),
      tap(sesion => this.guardarSesion(sesion))
    );
  }

  guardarSesion(sesion: ClienteWebSesion): void {
    const sesionNormalizada = this.mapearSesion(sesion);

    localStorage.setItem(this.storageKey, JSON.stringify(sesionNormalizada));

    window.dispatchEvent(new Event('clienteWebSesionActualizada'));
    window.dispatchEvent(new Event('carritoCotizacionActualizado'));
  }

  obtenerSesion(): ClienteWebSesion | null {
    const sesionTexto = localStorage.getItem(this.storageKey);

    if (!sesionTexto) {
      return null;
    }

    try {
      const sesion = JSON.parse(sesionTexto) as ClienteWebSesion;
      const sesionNormalizada = this.mapearSesion(sesion);

      if (!this.sesionVigente(sesionNormalizada)) {
        this.cerrarSesion();
        return null;
      }

      return sesionNormalizada;
    } catch {
      this.cerrarSesion();
      return null;
    }
  }

  estaLogueado(): boolean {
    return !!this.obtenerSesion();
  }

  obtenerToken(): string {
    return this.obtenerSesion()?.token || '';
  }

  cerrarSesion(): void {
    localStorage.removeItem(this.storageKey);

    window.dispatchEvent(new Event('clienteWebSesionActualizada'));
    window.dispatchEvent(new Event('carritoCotizacionActualizado'));
  }

  private mapearSesion(response: ClienteWebSesion): ClienteWebSesion {
    return {
      idCliente: response.idCliente ?? response.IdCliente ?? 0,
      idUsuario: response.idUsuario ?? response.IdUsuario ?? 0,
      idRol: response.idRol ?? response.IdRol ?? 0,

      correo: response.correo ?? response.Correo ?? '',
      rol: response.rol ?? response.Rol ?? 'Cliente',

      tipoDocumento: response.tipoDocumento ?? response.TipoDocumento ?? '',
      numeroDocumento: response.numeroDocumento ?? response.NumeroDocumento ?? '',
      tipoCliente: response.tipoCliente ?? response.TipoCliente ?? '',
      nombreCliente: response.nombreCliente ?? response.NombreCliente ?? 'Cliente',

      esEmpresa: response.esEmpresa ?? response.EsEmpresa ?? false,

      token: response.token ?? response.Token ?? '',
      expira: String(response.expira ?? response.Expira ?? ''),

      mensaje: response.mensaje ?? response.Mensaje ?? ''
    };
  }

  private mapearConsultaDocumento(
    response: ClienteWebConsultaDocumentoResponse
  ): ClienteWebConsultaDocumentoResponse {
    return {
      tipoDocumento: response.tipoDocumento ?? response.TipoDocumento ?? '',
      numeroDocumento: response.numeroDocumento ?? response.NumeroDocumento ?? '',

      exitoso: response.exitoso ?? response.Exitoso ?? false,
      clienteYaExiste: response.clienteYaExiste ?? response.ClienteYaExiste ?? false,
      cuentaWebVinculada: response.cuentaWebVinculada ?? response.CuentaWebVinculada ?? false,

      idClienteExistente: response.idClienteExistente ?? response.IdClienteExistente ?? null,

      correoExistente: response.correoExistente ?? response.CorreoExistente ?? null,
      telefonoExistente: response.telefonoExistente ?? response.TelefonoExistente ?? null,
      direccionExistente: response.direccionExistente ?? response.DireccionExistente ?? null,

      nombres: response.nombres ?? response.Nombres ?? null,
      apellidoPaterno: response.apellidoPaterno ?? response.ApellidoPaterno ?? null,
      apellidoMaterno: response.apellidoMaterno ?? response.ApellidoMaterno ?? null,
      nombreCompleto: response.nombreCompleto ?? response.NombreCompleto ?? null,

      razonSocial: response.razonSocial ?? response.RazonSocial ?? null,
      nombreComercial: response.nombreComercial ?? response.NombreComercial ?? null,

      estadoSunat: response.estadoSunat ?? response.EstadoSunat ?? null,
      condicionSunat: response.condicionSunat ?? response.CondicionSunat ?? null,

      codigoUbigeo: response.codigoUbigeo ?? response.CodigoUbigeo ?? null,
      idUbigeo: response.idUbigeo ?? response.IdUbigeo ?? null,
      ubicacion: response.ubicacion ?? response.Ubicacion ?? null,

      mensaje: response.mensaje ?? response.Mensaje ?? ''
    };
  }

  private sesionVigente(sesion: ClienteWebSesion): boolean {
    if (!sesion.token || !sesion.expira) {
      return false;
    }

    const fechaExpiracion = new Date(sesion.expira);

    if (Number.isNaN(fechaExpiracion.getTime())) {
      return false;
    }

    return fechaExpiracion.getTime() > Date.now();
  }
}