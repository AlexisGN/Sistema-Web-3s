export interface InventarioListado {
  idInventario: number;
  idProducto: number;
  idElementoCatalogo: number;

  codigoProducto: string;
  producto: string;

  imagenUrl?: string | null;
  categoria?: string | null;
  marca?: string | null;
  unidadMedida?: string | null;

  stockActual: number;
  stockMinimo: number;

  estadoStock: string;
  tieneAlertaPendiente: boolean;
  mensajeAlerta?: string | null;

  fechaActualizacion: string;
}

export interface InventarioResumen {
  totalProductosInventario: number;
  totalStockNormal: number;
  totalStockBajo: number;
  totalSinStock: number;
  totalAlertasPendientes: number;
  totalMovimientos: number;
}

export interface MovimientoStockListado {
  idMovimientoStock: number;

  idProducto: number;
  codigoProducto: string;
  producto: string;

  tipoMovimiento: string;
  cantidad: number;

  fechaMovimiento: string;
  motivo?: string | null;

  idVenta?: number | null;
  idCompra?: number | null;

  usuarioRegistro: string;
}

export interface ActualizarStockMinimo {
  stockMinimo: number;
}

export interface RegistrarMovimientoStock {
  idProducto: number;
  tipoMovimiento: 'Entrada' | 'Salida' | 'Ajuste';
  cantidad?: number | null;
  nuevoStock?: number | null;
  motivo: string;
  idUsuarioRegistro?: number | null;
}

export interface ResultadoPaginado<T> {
  items: T[];
  pagina: number;
  tamanioPagina: number;
  totalRegistros: number;
  totalPaginas: number;
}