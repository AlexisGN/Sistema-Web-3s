export interface CotizacionDetalle {
  idDetalleCotizacion: number;
  idCotizacion?: number;
  idElementoCatalogo: number;

  elementoNombre: string;
  elemento?: string;
  tipoElemento: string;

  idProducto?: number | null;
  codigoProducto?: string | null;

  idServicio?: number | null;
  codigoServicio?: string | null;

  codigo?: string | null;

  cantidad: number;
  precioUnitario: number;
  subtotal: number;

  stockActual?: number | null;
  aplicaInventario?: boolean | null;

  observacion?: string | null;
}

export interface CotizacionDetalleCrear {
  idElementoCatalogo: number;
  cantidad: number;
  precioUnitario: number;
  observacion?: string | null;
}

export interface CotizacionListado {
  idCotizacion: number;
  codigoCotizacion: string;

  idCliente: number;
  cliente: string;

  tipoDocumentoCliente: string;
  documentoCliente: string;
  tipoCliente: string;

  correoCliente?: string | null;
  telefonoCliente?: string | null;
  direccionCliente?: string | null;

  idUsuarioRegistro?: number | null;
  idUsuarioAtencion?: number | null;

  idEstadoCotizacion: number;
  estadoCotizacion: string;

  origenCotizacion: string;

  fechaCotizacion: string;
  fechaRespuesta?: string | null;
  canalRespuesta?: string | null;

  subtotal: number;
  descuento: number;
  igv: number;
  total: number;
  totalReferencial: number;

  observacion?: string | null;
  archivoPdf?: string | null;

  correoEnviado: boolean;
  whatsappEnviado: boolean;
  pdfGenerado: boolean;

  cantidadDetalles: number;

  idVentaGenerada?: number | null;
  idVentaAsociada?: number | null;

  puedeConvertirVenta: boolean;
  puedeGestionar: boolean;

  detalles: CotizacionDetalle[];
}

export interface CotizacionCrear {
  idCliente: number;
  idUsuarioRegistro?: number | null;

  origenCotizacion: string;
  descuento: number;

  observacion?: string | null;

  detalles: CotizacionDetalleCrear[];
}

export interface CotizacionCambiarEstado {
  idEstadoCotizacion?: number | null;
  nuevoEstado?: string | null;
  idUsuarioAtencion?: number | null;
}

export interface CotizacionEnviar {
  idUsuarioAtencion?: number | null;
}

export interface CotizacionMarcarRespondida {
  idUsuarioAtencion?: number | null;
  canalEnvio: 'Correo' | 'WhatsApp';
}

export interface CotizacionConvertirVenta {
  idVenta: number;
  idUsuarioAtencion?: number | null;
}

export interface ClienteSelector {
  idCliente: number;
  cliente: string;
  tipoCliente: string;
  tipoDocumento: string;
  numeroDocumento: string;
  textoMostrar: string;
}

export interface ElementoCotizable {
  idElementoCatalogo: number;
  nombre: string;
  tipoElemento: string;
  precioReferencial?: number | null;
  textoMostrar: string;
}

export interface EstadoCotizacion {
  idEstadoCotizacion: number;
  nombre: string;
}

export interface CotizacionWhatsApp {
  idCotizacion: number;
  codigoCotizacion: string;

  telefono: string;
  mensaje: string;
  url: string;

  archivoPdf?: string | null;
  requiereConfirmacionRespondida: boolean;
}

export interface CotizacionEnviarWhatsAppResponse {
  mensaje: string;
  resultado: CotizacionWhatsApp;
}

export interface CotizacionPdfResponse {
  mensaje: string;
  archivoPdf: string;
}

export interface MensajeResponse {
  mensaje: string;
}