export interface CajaActiva {
  idCaja: number;

  idUsuarioApertura: number;
  usuarioApertura?: string | null;

  idUsuarioCierre?: number | null;
  usuarioCierre?: string | null;

  idEstadoCaja: number;
  estadoCaja: string;

  fechaApertura: string;
  fechaCierre?: string | null;

  saldoInicial: number;
  totalIngresos: number;
  totalEgresos: number;
  saldoSistema: number;

  saldoFinal?: number | null;
  saldoContado?: number | null;
  diferencia?: number | null;

  observacionApertura?: string | null;
  observacionCierre?: string | null;

  mensaje?: string | null;
}

export interface CajaAbrir {
  idUsuarioApertura: number;
  saldoInicial: number;
  observacionApertura?: string | null;
}

export interface CajaCerrar {
  idUsuarioCierre: number;
  idCaja: number;
  saldoContado: number;
  observacionCierre?: string | null;
}

export interface CajaResumen {
  idCaja: number;

  idEstadoCaja: number;
  estadoCaja: string;

  fechaApertura: string;
  fechaCierre?: string | null;

  saldoInicial: number;

  ingresosPorVenta: number;
  ingresosManuales: number;
  ajustesIngreso: number;

  egresosPorCompra: number;
  egresosManuales: number;
  ajustesEgreso: number;

  totalIngresos: number;
  totalEgresos: number;
  saldoSistema: number;

  saldoContado?: number | null;
  diferencia?: number | null;

  observacionApertura?: string | null;
  observacionCierre?: string | null;
}

export interface MovimientoCaja {
  idMovimientoCaja: number;

  idCaja: number;

  idTipoMovimientoCaja: number;
  tipoMovimiento: string;

  idVenta?: number | null;
  idCompra?: number | null;

  idPagoVenta?: number | null;
  idPagoCompra?: number | null;

  idUsuarioRegistro: number;
  usuarioRegistro: string;

  metodoPago?: string | null;

  monto: number;

  descripcion?: string | null;

  fechaMovimiento: string;

  origenMovimiento: string;

  esAutomatico: boolean;

  estado: boolean;

  ingreso: number;

  egreso: number;
}

export interface MovimientoCajaManual {
  idUsuarioRegistro: number;
  tipoMovimiento: string;
  metodoPago: string;
  monto: number;
  descripcion: string;
}

export interface CajaOperacionResultado {
  mensaje: string;
  idCaja: number;
}

export interface CajaReporte {
  idCaja: number;

  estadoCaja: string;

  fechaApertura: string;
  fechaCierre?: string | null;

  saldoInicial: number;

  totalIngresos: number;
  totalEgresos: number;
  saldoSistema: number;

  saldoFinal?: number | null;
  saldoContado?: number | null;
  diferencia?: number | null;

  usuarioApertura: string;
  usuarioCierre?: string | null;

  observacionApertura?: string | null;
  observacionCierre?: string | null;
}