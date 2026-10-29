import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ClienteWebService } from './cliente-web.service';

export interface ClientePerfil {
  idCliente: number; correo: string; tipoDocumento: string; numeroDocumento: string;
  nombreCliente: string; esEmpresa: boolean; telefono: string; direccion: string;
  idUbigeo: number | null; ubicacion: string; nombreComercial: string | null;
}
export interface UbicacionCliente { idUbigeo: number; nombre: string; }
@Injectable({ providedIn: 'root' })
export class ClienteCuentaService {
  private readonly url = `${environment.apiUrl}/cliente-web`;
  constructor(private http: HttpClient, private sesion: ClienteWebService, private router: Router) {}
  private headers() { return { headers: new HttpHeaders({ Authorization: `Bearer ${this.sesion.obtenerToken()}` }) }; }
  private sesionError = (error: any) => {
    if (error.status === 401) { this.sesion.cerrarSesion(); this.router.navigate(['/cliente/login']); }
    return throwError(() => error);
  };
  perfil() { return this.http.get<ClientePerfil>(`${this.url}/perfil`, this.headers()).pipe(catchError(this.sesionError)); }
  guardar(perfil: ClientePerfil) {
    const { telefono, direccion, nombreComercial, idUbigeo } = perfil;
    return this.http.put<ClientePerfil>(`${this.url}/perfil`, { telefono, direccion, nombreComercial, idUbigeo }, this.headers()).pipe(catchError(this.sesionError));
  }
  ubicaciones(q: string) { return this.http.get<UbicacionCliente[]>(`${this.url}/ubicaciones`, { ...this.headers(), params: { q } }).pipe(catchError(this.sesionError)); }
  recuperar(correo: string) { return this.http.post<{ mensaje: string }>(`${this.url}/recuperar-contrasena`, { correo }); }
  restablecer(token: string, nuevaContrasena: string, confirmarContrasena: string) { return this.http.post<{ mensaje: string }>(`${this.url}/restablecer-contrasena`, { token, nuevaContrasena, confirmarContrasena }); }
  cambiar(contrasenaActual: string, nuevaContrasena: string, confirmarContrasena: string) {
    return this.http.post<{ mensaje: string }>(`${this.url}/cambiar-contrasena`, { contrasenaActual, nuevaContrasena, confirmarContrasena }, this.headers()).pipe(catchError(this.sesionError));
  }
}
export function mensajeCuenta(error: any): string {
  const validation = error?.error?.errors as Record<string, string[]> | undefined;
  return error?.error?.mensaje || (validation ? Object.values(validation).flat()[0] : '') || 'No se pudo completar la operación. Revisa tu conexión e inténtalo nuevamente.';
}
