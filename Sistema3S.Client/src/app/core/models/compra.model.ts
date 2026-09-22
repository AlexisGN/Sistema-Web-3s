export interface ResultadoPaginado<T> {
  items: T[];
  pagina: number;
  tamanioPagina: number;
  totalRegistros: number;
  totalPaginas?: number;
}

export interface ProveedorCompra {
  idProveedor: number;
  ruc: string;
  razonSocial: string;
  nombreComercial?: string | null;
  correo?: string | null;
  telefono?: string | null;
  direccion?: string | null;
  estado?: boolean;
}

export interface ProductoCompraDisponible {
  idProducto: number;
  idElementoCatalogo?: number | null;

  codigoProducto: string;
  codigo?: string | null;

  nombre?: string | null;
  producto?: string | null;
  nombreProducto?: string | null;
  descripcion?: string | null;

  categoria?: string | null;
  marca?: string | null;
  unidadMedida?: string | null;

  precioReferencial?: number | null;
  precioVenta?: number | null;
  precioConIgv?: number | null;
  precioUnitario?: number | null;
  precio?: number | null;

  stockActual?: number | null;
  stockMinimo?: number | null;
  aplicaInventario?: boolean | null;

  estado?: boolean;

  [key: string]: any;
}

export interface GuiaRemisionCompraCrear {
  tieneGuia: boolean;

  numeroGuia?: string | null;
  fechaEmision?: string | null;
  fechaTraslado?: string | null;

  puntoPartida?: string | null;
  puntoLlegada?: string | null;

  transportista?: string | null;
  rucTransportista?: string | null;
  placaVehiculo?: string | null;

  observacion?: string | null;
}

export interface CompraDetalleCrear {
  idProducto: number;
  cantidad: number;

  /**
   * Precio unitario de compra CON IGV incluido.
   * Ejemplo: si el proveedor factura S/ 118.00, aquí va 118.00.
   */
  precioCompra: number;

  codigoProducto?: string;
  producto?: string;
  categoria?: string | null;
  marca?: string | null;
  unidadMedida?: string | null;
}

/**
 * NUEVO:
 * Cuota que se envía al backend cuando el tipo de pago es "Cuotas".
 * La fecha puede editarse en el formulario.
 */
export interface CuotaCompraCrear {
  numeroCuota: number;
  fechaVencimiento: string | null;
  montoCuota: number;
}

export interface CompraCrear {
  idProveedor: number;
  idUsuarioRegistro: number;

  tipoComprobanteProveedor: string;
  serieComprobante: string;
  numeroComprobante: string;
  fechaEmisionComprobante?: string | null;

  observacionCompra?: string | null;

  guiaRemision: GuiaRemisionCompraCrear;

  tipoPago: string;
  metodoPago: string;

  /**
   * Monto pagado sobre el total, donde el total ya incluye IGV.
   * En tipo "Cuotas", este campo funciona como pago inicial.
   */
  montoPagado: number;

  numeroCuotas?: number | null;
  fechaPrimerVencimiento?: string | null;

  observacionPago?: string | null;

  detalles: CompraDetalleCrear[];

  /**
   * NUEVO:
   * Cronograma editable de cuotas.
   * Si está vacío, el backend/SQL puede generar las fechas mensuales
   * desde fechaPrimerVencimiento.
   */
  cuotas: CuotaCompraCrear[];
}

export interface CompraRegistroResultado {
  idCompra: number;
  subtotal: number;
  igv: number;
  total: number;
  totalPagado: number;
  saldoPendiente: number;
  estadoPago?: string;
  mensaje: string;
}

export interface CompraListado {
  idCompra: number;

  fechaCompra: string;
  fechaEmisionComprobante?: string | null;

  tipoComprobanteProveedor: string;
  serieComprobante: string;
  numeroComprobante: string;
  documentoCompleto: string;

  idProveedor: number;
  rucProveedor: string;
  razonSocialProveedor: string;

  subtotal: number;
  igv: number;
  total: number;
  totalPagado: number;
  saldoPendiente: number;

  estadoCompra: string;
  estadoPago: string;

  tieneGuia: boolean;
}

export interface ReporteCompra {
  idCompra: number;
  fechaCompra: string;

  tipoComprobanteProveedor: string;
  serieComprobante: string;
  numeroComprobante: string;
  documentoCompleto: string;

  rucProveedor: string;
  razonSocialProveedor: string;

  subtotal: number;
  igv: number;
  total: number;
  totalPagado: number;
  saldoPendiente: number;

  estadoCompra: string;
  estadoPago: string;

  tieneGuia: boolean;
}

export interface PagoCompraCrear {
  idCompra: number;
  idUsuarioRegistro: number;
  metodoPago: string;
  montoPagado: number;
  observacion?: string | null;

  /**
   * Para compras en cuotas:
   * aquí se envían las cuotas seleccionadas en el modal de pago.
   */
  idsCuotasPagadas: number[];
}

export interface AnularCompra {
  idUsuarioRegistro: number;
  motivo: string;
}

export interface CompraDetalleCompleto {
  idCompra: number;

  fechaCompra: string;
  fechaEmisionComprobante?: string | null;

  tipoComprobanteProveedor: string;
  serieComprobante: string;
  numeroComprobante: string;
  documentoCompleto?: string | null;

  subtotal: number;
  igv: number;
  total: number;
  totalPagado: number;
  saldoPendiente: number;

  observacion?: string | null;

  estadoCompra: string;
  estadoPago: string;

  idProveedor: number;
  rucProveedor: string;
  razonSocialProveedor: string;
  nombreComercialProveedor?: string | null;
  correoProveedor?: string | null;
  telefonoProveedor?: string | null;
  direccionProveedor?: string | null;

  guiaRemision?: GuiaRemisionCompraDetalle | null;

  detalles: DetalleCompraDetalle[];
  pagos: PagoCompraDetalle[];
  cuotas: CuotaCompraDetalle[];
}

export interface GuiaRemisionCompraDetalle {
  idGuiaRemisionCompra: number;
  numeroGuia: string;
  fechaEmision: string;
  fechaTraslado: string;
  puntoPartida: string;
  puntoLlegada: string;
  transportista?: string | null;
  rucTransportista?: string | null;
  placaVehiculo?: string | null;
  observacion?: string | null;
}

export interface DetalleCompraDetalle {
  idDetalleCompra: number;
  idProducto: number;
  codigoProducto: string;
  producto: string;
  cantidad: number;

  /**
   * Precio unitario de compra CON IGV incluido.
   */
  precioCompra: number;

  /**
   * Importe de línea: cantidad * precioCompra.
   * Este importe también incluye IGV.
   */
  subtotal: number;
}

export interface PagoCompraDetalle {
  idPagoCompra: number;
  metodoPago: string;
  montoPagado: number;
  fechaPago: string;
  observacion?: string | null;
}

export interface CuotaCompraDetalle {
  idCuotaCompra: number;
  numeroCuota: number;
  fechaVencimiento: string;
  montoCuota: number;
  montoPagado: number;
  estadoCuota: string;
}