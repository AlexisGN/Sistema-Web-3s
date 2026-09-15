export interface ProductoListado {
  idProducto: number;
  idElementoCatalogo: number;

  idCategoria?: number;
  idMarca?: number | null;
  idUnidadMedida?: number | null;

  nombre: string;
  descripcion?: string;
  precioReferencial?: number;
  imagenUrl?: string;

  categoria: string;
  marca?: string;
  unidadMedida?: string;

  codigoProducto: string;
  fichaTecnicaPdf?: string;

  stockActual: number;
  stockMinimo: number;

  estado: boolean;
}

export interface ProductoCrear {
  nombre: string;
  descripcion?: string;
  precioReferencial?: number | null;
  imagenUrl?: string | null;

  idCategoria: number;
  idMarca?: number | null;
  idUnidadMedida?: number | null;

  codigoProducto: string;
  fichaTecnicaPdf?: string | null;

  stockInicial: number;
  stockMinimo: number;
}

export interface ProductoActualizar {
  nombre: string;
  descripcion?: string;
  precioReferencial?: number | null;
  imagenUrl?: string | null;

  idCategoria: number;
  idMarca?: number | null;
  idUnidadMedida?: number | null;

  codigoProducto: string;
  fichaTecnicaPdf?: string | null;

  stockMinimo: number;
  estado: boolean;
}