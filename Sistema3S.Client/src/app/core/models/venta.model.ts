export interface ResultadoPaginado<T> {
  items: T[];
  pagina: number;
  tamanioPagina: number;
  totalRegistros: number;
  totalPaginas?: number;
}

export interface ClienteVenta {
  idCliente: number;
  tipoDocumento?: string | null;
  numeroDocumento: string;

  nombres?: string | null;
  apellidoPaterno?: string | null;
  apellidoMaterno?: string | null;

  razonSocial?: string | null;
  nombreComercial?: string | null;

  cliente?: string | null;
  nombreCompleto?: string | null;

  correo?: string | null;
  telefono?: string | null;
  direccion?: string | null;

  estado?: boolean;
}

export interface ElementoVentaDisponible {
  idElementoCatalogo?: number | null;

  idProducto?: number | null;
  idServicio?: number | null;

  codigoProducto?: string | null;
  codigoServicio?: string | null;
  codigo?: string | null;

  nombre?: string | null;
  producto?: string | null;
  servicio?: string | null;
  nombreProducto?: string | null;
  nombreServicio?: string | null;
  descripcion?: string | null;

  categoria?: string | null;
  marca?: string | null;
  unidadMedida?: string | null;

  precioVenta?: number | null;
  precio?: number | null;
  precioUnitario?: number | null;

  precioConIgv?: number | null;
  precioVentaConIgv?: number | null;
  precioUnitarioConIgv?: number | null;
  precioFinal?: number | null;
  montoVenta?: number | null;

  stockActual?: number | null;
  stockMinimo?: number | null;
  aplicaInventario?: boolean | null;

  tipoElemento?: string | null;
  estado?: boolean;

  [key: string]: any;
}

export interface VentaDetalleCrear {
  idElementoCatalogo: number;
  cantidad: number;
  precioUnitario: number;

  codigo?: string | null;
  elemento?: string | null;
  tipoElemento?: string | null;
  stockActual?: number | null;
  aplicaInventario?: boolean | null;
}

export interface CuotaVentaCrear {
  numeroCuota: number;
  fechaVencimiento: string | null;
  montoCuota: number;
}

export interface VentaCrear {
  idCliente: number;
  idCotizacion?: number | null;
  idUsuarioRegistro: number;

  tipoComprobante: string;
  serie: string;
  numero: string;
  fechaEmision?: string | null;

  observacion?: string | null;

  tipoPago: string;
  metodoPago: string;
  montoPagado: number;

  numeroCuotas?: number | null;
  fechaPrimerVencimiento?: string | null;

  detalles: VentaDetalleCrear[];

  /**
   * Cronograma editable de cuotas.
   * Se envía cuando tipoPago = 'Cuotas'.
   */
  cuotas: CuotaVentaCrear[];
}

export interface VentaRegistroResultado {
  idVenta: number;
  subtotal: number;
  igv: number;
  total: number;
  totalPagado: number;
  saldoPendiente: number;
  estadoPago: string;
  mensaje: string;
}

export interface VentaListado {
  idVenta: number;
  idCliente: number;
  idCotizacion?: number | null;

  fechaVenta: string;

  tipoComprobante: string;
  serie: string;
  numero: string;
  documentoCompleto: string;

  documentoCliente: string;
  cliente: string;

  origenVenta: string;

  subtotal: number;
  igv: number;
  total: number;
  totalPagado: number;
  saldoPendiente: number;

  estadoVenta: string;
  estadoPago: string;
}

export interface VentaDetalleCompleto {
  idVenta: number;
  idCliente: number;
  idCotizacion?: number | null;

  fechaVenta: string;

  tipoComprobante: string;
  serie: string;
  numero: string;
  documentoCompleto: string;
  fechaEmision: string;

  tipoDocumentoCliente: string;
  documentoCliente: string;
  cliente: string;
  correoCliente?: string | null;
  telefonoCliente?: string | null;
  direccionCliente?: string | null;

  subtotal: number;
  igv: number;
  total: number;
  totalPagado: number;
  saldoPendiente: number;

  estadoVenta: string;
  estadoPago: string;
  origenVenta: string;

  observacion?: string | null;

  detalles: VentaDetalleItem[];
  pagos: PagoVentaDetalle[];
  cuotas: CuotaVentaDetalle[];
}

export interface VentaDetalleItem {
  idDetalleVenta: number;
  idElementoCatalogo: number;
  elemento: string;
  tipoElemento: string;

  idProducto?: number | null;
  codigoProducto?: string | null;

  idServicio?: number | null;

  cantidad: number;
  precioUnitario: number;
  subtotal: number;
}

export interface PagoVentaDetalle {
  idPagoVenta: number;
  idVenta: number;
  metodoPago: string;
  montoPagado: number;
  fechaPago: string;
  observacion?: string | null;
}

export interface CuotaVentaDetalle {
  idCuotaVenta: number;
  idVenta: number;
  numeroCuota: number;
  fechaVencimiento: string;
  montoCuota: number;
  montoPagado: number;
  estadoCuota: string;
}

export interface PagoVentaCrear {
  idVenta: number;
  idUsuarioRegistro: number;
  metodoPago: string;
  montoPagado: number;
  observacion?: string | null;

  /**
   * Para ventas en cuotas:
   * aquí se envían las cuotas seleccionadas desde el modal de cobro.
   */
  idsCuotasCobradas: number[];
}

export interface AnularVenta {
  idUsuarioRegistro: number;
  motivo: string;
}

export interface SiguienteComprobante {
  serie: string;
  numero: string;
  documentoCompleto: string;
}