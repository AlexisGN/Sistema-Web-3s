export interface CarritoCotizacionItem {
  idProducto: number;
  idElementoCatalogo: number;
  codigo: string;
  nombre: string;
  categoria: string;
  marca: string | null;
  imagenUrl: string;
  cantidad: number;
  observacion: string;
}

export interface CotizacionWebCrearRequest {
  observacionGeneral: string | null;
  items: CotizacionWebItemCrearRequest[];
}

export interface CotizacionWebItemCrearRequest {
  idProducto: number;
  cantidad: number;
  observacion: string | null;
}

export interface CotizacionWebRegistradaResponse {
  idCotizacion: number;
  codigoCotizacion: string;
  fechaCotizacion: string;
  estadoCotizacion: string;
  origenCotizacion: string;

  subtotal: number;
  descuento: number;
  igv: number;
  total: number;

  esEmpresa: boolean;
  mensaje: string;

  IdCotizacion?: number;
  CodigoCotizacion?: string;
  FechaCotizacion?: string;
  EstadoCotizacion?: string;
  OrigenCotizacion?: string;
  Subtotal?: number;
  Descuento?: number;
  Igv?: number;
  Total?: number;
  EsEmpresa?: boolean;
  Mensaje?: string;
}

export interface CotizacionWebResumen {
  idCotizacion: number;
  codigoCotizacion: string;
  fechaCotizacion: string;
  estadoCotizacion: string;
  origenCotizacion: string;
  observacion: string | null;
  cantidadProductos: number;

  subtotal: number;
  descuento: number;
  igv: number;
  total: number;

  IdCotizacion?: number;
  CodigoCotizacion?: string;
  FechaCotizacion?: string;
  EstadoCotizacion?: string;
  OrigenCotizacion?: string;
  Observacion?: string | null;
  CantidadProductos?: number;
  Subtotal?: number;
  Descuento?: number;
  Igv?: number;
  Total?: number;
}

export interface CotizacionWebDetalle {
  idCotizacion: number;
  codigoCotizacion: string;
  fechaCotizacion: string;
  estadoCotizacion: string;
  origenCotizacion: string;
  observacion: string | null;

  subtotal: number;
  descuento: number;
  igv: number;
  total: number;

  detalles: CotizacionWebDetalleItem[];

  IdCotizacion?: number;
  CodigoCotizacion?: string;
  FechaCotizacion?: string;
  EstadoCotizacion?: string;
  OrigenCotizacion?: string;
  Observacion?: string | null;
  Subtotal?: number;
  Descuento?: number;
  Igv?: number;
  Total?: number;
  Detalles?: CotizacionWebDetalleItem[];
}

export interface CotizacionWebDetalleItem {
  idDetalleCotizacion: number;
  idElementoCatalogo: number;
  idProducto: number | null;

  codigoProducto: string;
  nombreProducto: string;
  imagenUrl: string;

  cantidad: number;
  precioUnitario: number;
  subtotal: number;

  observacion: string | null;

  IdDetalleCotizacion?: number;
  IdElementoCatalogo?: number;
  IdProducto?: number | null;
  CodigoProducto?: string;
  NombreProducto?: string;
  ImagenUrl?: string;
  Cantidad?: number;
  PrecioUnitario?: number;
  Subtotal?: number;
  Observacion?: string | null;
}

export interface ResultadoPaginadoWeb<T> {
  items: T[];
  pagina: number;
  tamanioPagina: number;
  totalRegistros: number;
  totalPaginas?: number;
  hayMas?: boolean;

  Items?: T[];
  Pagina?: number;
  TamanioPagina?: number;
  TotalRegistros?: number;
  TotalPaginas?: number;
  HayMas?: boolean;
}