import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CatalogoItem } from '../models/catalogo-item.model';

@Injectable({
  providedIn: 'root'
})
export class CatalogoService {
  private readonly apiUrl = `${environment.apiUrl}/catalogo`;

  constructor(private http: HttpClient) {}

  listarCategorias(): Observable<CatalogoItem[]> {
    return this.http.get<CatalogoItem[]>(`${this.apiUrl}/categorias`);
  }

  listarMarcas(): Observable<CatalogoItem[]> {
    return this.http.get<CatalogoItem[]>(`${this.apiUrl}/marcas`);
  }

  listarUnidadesMedida(): Observable<CatalogoItem[]> {
    return this.http.get<CatalogoItem[]>(`${this.apiUrl}/unidades-medida`);
  }
}