import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  ProductoActualizar,
  ProductoCrear,
  ProductoListado
} from '../models/producto.model';
import { ResultadoPaginado } from '../models/resultado-paginado.model';

@Injectable({
  providedIn: 'root'
})
export class ProductoService {
  private readonly apiUrl = `${environment.apiUrl}/producto`;

  constructor(private http: HttpClient) {}

  listar(
    buscar?: string,
    pagina: number = 1,
    tamanioPagina: number = 10,
    estado?: boolean
  ): Observable<ResultadoPaginado<ProductoListado>> {
    let params = new HttpParams()
      .set('pagina', String(pagina))
      .set('tamanioPagina', String(tamanioPagina));

    if (estado !== undefined) {
      params = params.set('estado', String(estado));
    }

    if (buscar && buscar.trim().length > 0) {
      params = params.set('buscar', buscar.trim());
    }

    return this.http.get<ResultadoPaginado<ProductoListado>>(this.apiUrl, { params });
  }

  crear(
    producto: ProductoCrear,
    imagenArchivo: File,
    fichaTecnicaArchivo?: File | null
  ): Observable<ProductoListado> {
    return this.http.post<ProductoListado>(
      this.apiUrl,
      this.crearFormulario(producto, imagenArchivo, fichaTecnicaArchivo)
    );
  }

  actualizar(
    idProducto: number,
    producto: ProductoActualizar,
    imagenArchivo?: File | null,
    fichaTecnicaArchivo?: File | null
  ): Observable<{ mensaje: string }> {
    return this.http.put<{ mensaje: string }>(
      `${this.apiUrl}/${idProducto}`,
      this.crearFormulario(producto, imagenArchivo, fichaTecnicaArchivo)
    );
  }

  eliminar(idProducto: number): Observable<{ mensaje: string }> {
    return this.http.delete<{ mensaje: string }>(`${this.apiUrl}/${idProducto}`);
  }
  
  contarActivos() {
  return this.http.get<{ total: number }>(`${this.apiUrl}/total-activos`);
}

  private crearFormulario(
    producto: ProductoCrear | ProductoActualizar,
    imagenArchivo?: File | null,
    fichaTecnicaArchivo?: File | null
  ): FormData {
    const formulario = new FormData();

    formulario.append('nombre', producto.nombre);
    formulario.append('codigoProducto', producto.codigoProducto);
    formulario.append('idCategoria', String(producto.idCategoria));
    formulario.append('stockMinimo', String(producto.stockMinimo));

    this.agregarCampoOpcional(formulario, 'descripcion', producto.descripcion);
    this.agregarCampoOpcional(formulario, 'precioReferencial', producto.precioReferencial);
    this.agregarCampoOpcional(formulario, 'idMarca', producto.idMarca);
    this.agregarCampoOpcional(formulario, 'idUnidadMedida', producto.idUnidadMedida);

    if ('stockInicial' in producto) {
      formulario.append('stockInicial', String(producto.stockInicial));
    }

    if ('estado' in producto) {
      formulario.append('estado', String(producto.estado));
    }

    if (imagenArchivo) {
      formulario.append('imagenArchivo', imagenArchivo, imagenArchivo.name);
    }

    if (fichaTecnicaArchivo) {
      formulario.append('fichaTecnicaArchivo', fichaTecnicaArchivo, fichaTecnicaArchivo.name);
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
