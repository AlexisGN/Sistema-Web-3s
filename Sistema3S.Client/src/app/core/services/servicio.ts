import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ResultadoPaginado } from '../models/resultado-paginado.model';
import {
  ServicioActualizar,
  ServicioCrear,
  ServicioListado
} from '../models/servicio.model';

@Injectable({
  providedIn: 'root'
})
export class ServicioService {
  private readonly apiUrl = `${environment.apiUrl}/servicio`;

  constructor(private http: HttpClient) {}

  listar(
    buscar?: string,
    pagina: number = 1,
    tamanioPagina: number = 5,
    estado?: boolean
  ): Observable<ResultadoPaginado<ServicioListado>> {
    let params = new HttpParams()
      .set('pagina', String(pagina))
      .set('tamanioPagina', String(tamanioPagina));

    if (estado !== undefined) {
      params = params.set('estado', String(estado));
    }

    if (buscar && buscar.trim().length > 0) {
      params = params.set('buscar', buscar.trim());
    }

    return this.http.get<ResultadoPaginado<ServicioListado>>(this.apiUrl, { params });
  }

  crear(servicio: ServicioCrear, imagenArchivo: File): Observable<ServicioListado> {
    return this.http.post<ServicioListado>(
      this.apiUrl,
      this.crearFormulario(servicio, imagenArchivo)
    );
  }

  actualizar(
    idServicio: number,
    servicio: ServicioActualizar,
    imagenArchivo?: File | null
  ): Observable<{ mensaje: string }> {
    return this.http.put<{ mensaje: string }>(
      `${this.apiUrl}/${idServicio}`,
      this.crearFormulario(servicio, imagenArchivo)
    );
  }

  eliminar(idServicio: number): Observable<{ mensaje: string }> {
    return this.http.delete<{ mensaje: string }>(`${this.apiUrl}/${idServicio}`);
  }
  contarActivos() {
  return this.http.get<{ total: number }>(`${this.apiUrl}/total-activos`);
}

  private crearFormulario(
    servicio: ServicioCrear | ServicioActualizar,
    imagenArchivo?: File | null
  ): FormData {
    const formulario = new FormData();

    formulario.append('nombre', servicio.nombre);
    formulario.append('requiereVisitaTecnica', String(servicio.requiereVisitaTecnica));

    this.agregarCampoOpcional(formulario, 'descripcion', servicio.descripcion);
    this.agregarCampoOpcional(formulario, 'precioReferencial', servicio.precioReferencial);
    this.agregarCampoOpcional(formulario, 'sectorAplicacion', servicio.sectorAplicacion);
    this.agregarCampoOpcional(formulario, 'mensajeWhatsApp', servicio.mensajeWhatsApp);

    if ('estado' in servicio) {
      formulario.append('estado', String(servicio.estado));
    }

    if (imagenArchivo) {
      formulario.append('imagenArchivo', imagenArchivo, imagenArchivo.name);
    }

    return formulario;
  }

  private agregarCampoOpcional(
    formulario: FormData,
    nombre: string,
    valor: string | number | null | undefined
  ): void {
    if (valor !== null && valor !== undefined && valor !== '') {
      formulario.append(nombre, String(valor));
    }
  }
}
