import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  BusquedaPublicaResponse,
  CategoriaPublica,
  ImagenPublica,
  InicioPublicoResponse,
  MarcaPublica,
  NosotrosPublicoResponse,
  ProductoDetallePublico,
  ProductoPublico,
  ProductoPublicoFiltros,
  ProductoPublicoListado,
  ServicioDetallePublico,
  ServicioPublico
} from '../models/publico.model';

@Injectable({
  providedIn: 'root'
})
export class PublicoService {
  private readonly apiUrl = `${environment.apiUrl}/publico`;
  private readonly baseArchivosUrl = environment.apiUrl.replace(/\/api\/?$/, '');

  constructor(private http: HttpClient) { }

  obtenerInicio(): Observable<InicioPublicoResponse> {
    return this.http.get<InicioPublicoResponse>(`${this.apiUrl}/inicio`).pipe(
      map(response => ({
        categorias: (response.categorias || []).map(categoria => this.mapearCategoria(categoria)),
        marcas: (response.marcas || []).map(marca => this.mapearMarca(marca)),
        productosNuevos: (response.productosNuevos || []).map(producto =>
          this.mapearProducto(producto, true)
        ),
        productos: (response.productos || []).map(producto =>
          this.mapearProducto(producto, producto.nuevo)
        ),
        servicios: (response.servicios || []).map(servicio => this.mapearServicio(servicio))
      }))
    );
  }

  obtenerNosotros(): Observable<NosotrosPublicoResponse> {
    return this.http.get<NosotrosPublicoResponse>(`${this.apiUrl}/nosotros`).pipe(
      map(response => ({
        ...response,
        empresa: response.empresa,
        compromisos: response.compromisos || [],
        industrias: response.industrias || [],
        valores: response.valores || []
      }))
    );
  }

  buscarGlobal(q: string, limitePorTipo: number = 8): Observable<BusquedaPublicaResponse> {
    let params = new HttpParams()
      .set('q', q.trim())
      .set('limitePorTipo', limitePorTipo);

    return this.http.get<BusquedaPublicaResponse>(`${this.apiUrl}/buscar`, { params }).pipe(
      map(response => ({
        query: response.query || '',
        productos: (response.productos || []).map(producto =>
          this.mapearProducto(producto, producto.nuevo)
        ),
        servicios: (response.servicios || []).map(servicio =>
          this.mapearServicio(servicio)
        ),
        categorias: (response.categorias || []).map(categoria =>
          this.mapearCategoria(categoria)
        ),
        marcas: (response.marcas || []).map(marca =>
          this.mapearMarca(marca)
        ),
        totalResultados: response.totalResultados || 0
      }))
    );
  }

  obtenerCategorias(): Observable<CategoriaPublica[]> {
    return this.http.get<CategoriaPublica[]>(`${this.apiUrl}/categorias`).pipe(
      map(categorias => (categorias || []).map(categoria => this.mapearCategoria(categoria)))
    );
  }

  obtenerMarcas(): Observable<MarcaPublica[]> {
    return this.http.get<MarcaPublica[]>(`${this.apiUrl}/marcas`).pipe(
      map(marcas => (marcas || []).map(marca => this.mapearMarca(marca)))
    );
  }

  obtenerProductos(filtros: ProductoPublicoFiltros = {}): Observable<ProductoPublicoListado> {
    let params = new HttpParams();

    if (filtros.q?.trim()) {
      params = params.set('q', filtros.q.trim());
    }

    if (filtros.idCategoria && filtros.idCategoria > 0) {
      params = params.set('idCategoria', filtros.idCategoria);
    }

    if (filtros.idMarca && filtros.idMarca > 0) {
      params = params.set('idMarca', filtros.idMarca);
    }

    params = params.set('pagina', filtros.pagina || 1);
    params = params.set('tamanioPagina', filtros.tamanioPagina || 24);

    return this.http.get<ProductoPublicoListado>(`${this.apiUrl}/productos`, { params }).pipe(
      map(response => ({
        ...response,
        items: (response.items || []).map(producto =>
          this.mapearProducto(producto, producto.nuevo)
        )
      }))
    );
  }

  obtenerProductoDetalle(idProducto: number): Observable<ProductoDetallePublico> {
    return this.http.get<ProductoDetallePublico>(`${this.apiUrl}/productos/${idProducto}`).pipe(
      map(producto => this.mapearProductoDetalle(producto))
    );
  }

  obtenerServicios(): Observable<ServicioPublico[]> {
    return this.http.get<ServicioPublico[]>(`${this.apiUrl}/servicios`).pipe(
      map(servicios => (servicios || []).map(servicio => this.mapearServicio(servicio)))
    );
  }

  obtenerServicioDetalle(idServicio: number): Observable<ServicioDetallePublico> {
    return this.http.get<ServicioDetallePublico>(`${this.apiUrl}/servicios/${idServicio}`).pipe(
      map(servicio => this.mapearServicioDetalle(servicio))
    );
  }

  normalizarUrlPublica(url: string | null | undefined): string {
    return this.normalizarUrlArchivo(url);
  }

  private mapearCategoria(categoria: CategoriaPublica): CategoriaPublica {
    return {
      ...categoria,
      id: categoria.id || categoria.idCategoria,
      descripcion: categoria.descripcion || 'Productos y soluciones industriales disponibles para cotización.',
      icono: categoria.icono || '🏭'
    };
  }

  private mapearMarca(marca: MarcaPublica): MarcaPublica {
    const logoLocal = this.obtenerLogoMarcaLocal(marca.nombre);

    return {
      ...marca,
      id: marca.id || marca.idMarca,
      logoUrl: this.normalizarUrlArchivo(marca.logoUrl) || logoLocal
    };
  }

  private mapearProducto(producto: ProductoPublico, nuevo: boolean): ProductoPublico {
    return {
      ...producto,
      id: producto.id || producto.idProducto,
      marca: producto.marca || '3S',
      descripcion: producto.descripcion || 'Producto industrial disponible para cotización.',
      imagenUrl: this.normalizarUrlArchivo(producto.imagenUrl),
      fichaTecnicaPdf: producto.fichaTecnicaPdf
        ? this.normalizarUrlArchivo(producto.fichaTecnicaPdf)
        : null,
      nuevo,
      cantidad: producto.cantidad && producto.cantidad > 0 ? producto.cantidad : 1
    };
  }

  private mapearProductoDetalle(producto: ProductoDetallePublico): ProductoDetallePublico {
    const productoMapeado = this.mapearProducto(producto, producto.nuevo) as ProductoDetallePublico;

    return {
      ...productoMapeado,
      imagenes: (producto.imagenes || []).map(imagen => this.mapearImagen(imagen))
    };
  }

  private mapearServicio(servicio: ServicioPublico): ServicioPublico {
    return {
      ...servicio,
      id: servicio.id || servicio.idServicio,
      descripcion: servicio.descripcion || 'Servicio técnico industrial disponible para consulta.',
      imagenUrl: this.normalizarUrlArchivo(servicio.imagenUrl)
    };
  }

  private mapearServicioDetalle(servicio: ServicioDetallePublico): ServicioDetallePublico {
    const servicioMapeado = this.mapearServicio(servicio) as ServicioDetallePublico;

    return {
      ...servicioMapeado,
      imagenes: (servicio.imagenes || []).map(imagen => this.mapearImagen(imagen))
    };
  }

  private mapearImagen(imagen: ImagenPublica): ImagenPublica {
    return {
      ...imagen,
      urlImagen: this.normalizarUrlArchivo(imagen.urlImagen)
    };
  }

  private normalizarUrlArchivo(url: string | null | undefined): string {
    if (!url) {
      return '';
    }

    const valor = url.trim();

    if (!valor) {
      return '';
    }

    if (valor.startsWith('http://') || valor.startsWith('https://')) {
      return valor;
    }

    if (valor.startsWith('/assets/') || valor.startsWith('assets/')) {
      return valor.startsWith('/') ? valor : `/${valor}`;
    }

    if (valor.startsWith('/')) {
      return `${this.baseArchivosUrl}${valor}`;
    }

    return `${this.baseArchivosUrl}/${valor}`;
  }

  private obtenerLogoMarcaLocal(nombre: string): string {
    const marca = nombre.trim().toLowerCase();

    const logos: Record<string, string> = {
      abb: '/assets/images/Abb.webp',
      autonics: '/assets/images/Autonics.webp',
      danfoss: '/assets/images/Danfoss.webp',
      honeywell: '/assets/images/Honeywell.webp',
      lamtec: '/assets/images/Lamtec.webp',
      novus: '/assets/images/Novus.webp',
      siemens: '/assets/images/Siemens.webp',
      yokogawa: '/assets/images/Yokogawa.webp'
    };

    return logos[marca] || '';
  }
}