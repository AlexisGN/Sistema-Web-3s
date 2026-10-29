import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CatalogoItem } from '../models/catalogo-item.model';
import { CategoriaGestion, CategoriaGuardar } from '../models/categoria.model';

@Injectable({
  providedIn: 'root'
})
export class CatalogoService {
  private readonly apiUrl = `${environment.apiUrl}/catalogo`;

  constructor(private http: HttpClient) {}

  listarCategorias(): Observable<CatalogoItem[]> {
    return this.http.get<CatalogoItem[]>(`${this.apiUrl}/categorias`);
  }

  gestionarCategorias(): Observable<CategoriaGestion[]> {
    return this.http.get<CategoriaGestion[]>(`${this.apiUrl}/categorias/gestion`);
  }

  crearCategoria(categoria: CategoriaGuardar): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.apiUrl}/categorias`, categoria);
  }

  actualizarCategoria(id: number, categoria: CategoriaGuardar): Observable<unknown> {
    return this.http.put(`${this.apiUrl}/categorias/${id}`, categoria);
  }

  cambiarEstadoCategoria(id: number, estado: boolean): Observable<unknown> {
    return this.http.patch(`${this.apiUrl}/categorias/${id}/estado`, { estado });
  }

  eliminarCategoria(id: number): Observable<unknown> {
    return this.http.delete(`${this.apiUrl}/categorias/${id}`);
  }

  listarMarcas(): Observable<CatalogoItem[]> {
    return this.http.get<CatalogoItem[]>(`${this.apiUrl}/marcas`);
  }

  listarUnidadesMedida(): Observable<CatalogoItem[]> {
    return this.http.get<CatalogoItem[]>(`${this.apiUrl}/unidades-medida`);
  }
}
