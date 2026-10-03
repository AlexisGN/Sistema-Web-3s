import { UiIconComponent } from '../../shared/ui-icon/ui-icon';
import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';

import {
  CajaActiva,
  CajaReporte,
  CajaResumen,
  MovimientoCaja,
  MovimientoCajaManual
} from '../../core/models/caja.model';
import { CajaService } from '../../core/services/caja.service';

type AccionCaja = 'abrir' | 'movimiento' | 'cerrar' | 'reporte' | null;

interface ResumenMetodoPago {
  metodoPago: string;
  ingresos: number;
  egresos: number;
  neto: number;
  cantidadMovimientos: number;
  cantidadIngresos: number;
  cantidadEgresos: number;
}

@Component({
  selector: 'app-caja',
  standalone: true,
  imports: [UiIconComponent, CommonModule, FormsModule],
  templateUrl: './caja.html',
  styleUrls: ['./caja.scss']
})
export class CajaComponent implements OnInit {
  idUsuarioActual = 1;
  usuarioActual = 'Administrador';
  rolActual = 'Administrador';

  esAdministrador = true;

  cajaActiva: CajaActiva | null = null;
  resumen: CajaResumen | null = null;
  movimientos: MovimientoCaja[] = [];
  reporte: CajaReporte[] = [];

  cargando = false;
  cargandoMovimientos = false;
  procesando = false;

  mensaje = '';
  error = '';

  accionModal: AccionCaja = null;

  filtroFechaInicio = '';
  filtroFechaFin = '';

  reporteFechaInicio = '';
  reporteFechaFin = '';

  abrirForm = {
    saldoInicial: 0,
    observacionApertura: ''
  };

  movimientoForm: MovimientoCajaManual = {
    idUsuarioRegistro: 1,
    tipoMovimiento: 'Ingreso manual',
    metodoPago: 'Efectivo',
    monto: 0,
    descripcion: ''
  };

  cerrarForm = {
    saldoContado: 0,
    observacionCierre: ''
  };

  tiposMovimientoManual = [
    'Ingreso manual',
    'Egreso manual',
    'Ajuste ingreso',
    'Ajuste egreso'
  ];

  metodosPago = [
    'Efectivo',
    'Yape',
    'Plin',
    'Transferencia',
    'Tarjeta',
    'Depósito',
    'Otro'
  ];

  constructor(
    private cajaService: CajaService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.cargarUsuarioActual();

    if (!this.esAdministrador) {
      this.error = 'Acceso denegado. Solo el administrador puede acceder al módulo Caja.';
      return;
    }

    this.cargarTodo();
  }

  cargarTodo(): void {
    this.mensaje = '';
    this.error = '';
    this.cargando = true;

    this.cajaService.obtenerCajaActiva(this.idUsuarioActual).subscribe({
      next: (caja) => {
        this.cajaActiva = caja;

        if (caja && caja.idCaja > 0) {
          this.cerrarForm.saldoContado = Number(caja.saldoSistema || 0);
          this.cargarResumen(caja.idCaja);
          this.cargarMovimientos(caja.idCaja);
        } else {
          this.resumen = null;
          this.movimientos = [];
        }

        this.cargarReporte();

        this.cargando = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.error = this.obtenerMensajeError(err, 'No se pudo cargar la caja.');
        this.cargando = false;
        this.cdr.detectChanges();
      }
    });
  }

  cargarResumen(idCaja?: number): void {
    const cajaId = idCaja || this.cajaActiva?.idCaja || null;

    if (!cajaId) {
      this.resumen = null;
      return;
    }

    this.cajaService.obtenerResumen(this.idUsuarioActual, cajaId).subscribe({
      next: (resumen) => {
        this.resumen = resumen;
        this.cerrarForm.saldoContado = Number(resumen.saldoSistema || 0);
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.error = this.obtenerMensajeError(err, 'No se pudo obtener el resumen de caja.');
        this.cdr.detectChanges();
      }
    });
  }

  cargarMovimientos(idCaja?: number): void {
    const cajaId = idCaja || this.cajaActiva?.idCaja || null;

    if (!cajaId) {
      this.movimientos = [];
      return;
    }

    this.cargandoMovimientos = true;

    this.cajaService.listarMovimientos(
      this.idUsuarioActual,
      cajaId,
      this.filtroFechaInicio || null,
      this.filtroFechaFin || null
    ).subscribe({
      next: (movimientos) => {
        this.movimientos = movimientos;
        this.cargandoMovimientos = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.error = this.obtenerMensajeError(err, 'No se pudieron cargar los movimientos.');
        this.cargandoMovimientos = false;
        this.cdr.detectChanges();
      }
    });
  }

  cargarReporte(): void {
    this.cajaService.obtenerReporte(
      this.idUsuarioActual,
      this.reporteFechaInicio || null,
      this.reporteFechaFin || null
    ).subscribe({
      next: (reporte) => {
        this.reporte = reporte;
        this.cdr.detectChanges();
      },
      error: () => {
        this.reporte = [];
        this.cdr.detectChanges();
      }
    });
  }

  abrirModalApertura(): void {
    this.limpiarMensajes();

    this.abrirForm = {
      saldoInicial: 0,
      observacionApertura: ''
    };

    this.accionModal = 'abrir';
  }

  abrirModalMovimiento(tipo: string = 'Ingreso manual'): void {
    this.limpiarMensajes();

    this.movimientoForm = {
      idUsuarioRegistro: this.idUsuarioActual,
      tipoMovimiento: tipo,
      metodoPago: 'Efectivo',
      monto: 0,
      descripcion: ''
    };

    this.accionModal = 'movimiento';
  }

  abrirModalCierre(): void {
    this.limpiarMensajes();

    this.cerrarForm = {
      saldoContado: Number(this.resumen?.saldoSistema || this.cajaActiva?.saldoSistema || 0),
      observacionCierre: ''
    };

    this.accionModal = 'cerrar';
  }

  abrirModalReporte(): void {
    this.limpiarMensajes();
    this.cargarReporte();
    this.accionModal = 'reporte';
  }

  cerrarModal(): void {
    if (this.procesando) {
      return;
    }

    this.accionModal = null;
  }

  abrirCaja(): void {
    this.limpiarMensajes();

    if (this.abrirForm.saldoInicial < 0) {
      this.error = 'El saldo inicial no puede ser negativo.';
      return;
    }

    this.procesando = true;

    this.cajaService.abrirCaja({
      idUsuarioApertura: this.idUsuarioActual,
      saldoInicial: Number(this.abrirForm.saldoInicial || 0),
      observacionApertura: this.abrirForm.observacionApertura || null
    }).subscribe({
      next: (caja) => {
        this.cajaActiva = caja;
        this.mensaje = caja.mensaje || 'Caja abierta correctamente.';
        this.accionModal = null;
        this.procesando = false;
        this.cargarTodo();
      },
      error: (err) => {
        this.error = this.obtenerMensajeError(err, 'No se pudo abrir la caja.');
        this.procesando = false;
        this.cdr.detectChanges();
      }
    });
  }

  registrarMovimientoManual(): void {
    this.limpiarMensajes();

    if (!this.cajaActiva) {
      this.error = 'No existe una caja abierta.';
      return;
    }

    if (!this.movimientoForm.tipoMovimiento) {
      this.error = 'Selecciona el tipo de movimiento.';
      return;
    }

    if (!this.movimientoForm.metodoPago) {
      this.error = 'Selecciona el método de pago.';
      return;
    }

    if (!this.movimientoForm.monto || this.movimientoForm.monto <= 0) {
      this.error = 'El monto debe ser mayor a 0.';
      return;
    }

    if (!this.movimientoForm.descripcion || this.movimientoForm.descripcion.trim().length < 5) {
      this.error = 'Ingresa una descripción válida.';
      return;
    }

    this.procesando = true;

    this.cajaService.registrarMovimientoManual({
      ...this.movimientoForm,
      idUsuarioRegistro: this.idUsuarioActual,
      metodoPago: this.normalizarMetodoPago(this.movimientoForm.metodoPago),
      monto: Number(this.movimientoForm.monto || 0),
      descripcion: this.movimientoForm.descripcion.trim()
    }).subscribe({
      next: (resultado) => {
        this.mensaje = resultado.mensaje || 'Movimiento registrado correctamente.';
        this.accionModal = null;
        this.procesando = false;
        this.cargarTodo();
      },
      error: (err) => {
        this.error = this.obtenerMensajeError(err, 'No se pudo registrar el movimiento.');
        this.procesando = false;
        this.cdr.detectChanges();
      }
    });
  }

  cerrarCaja(): void {
    this.limpiarMensajes();

    if (!this.cajaActiva) {
      this.error = 'No existe una caja abierta.';
      return;
    }

    if (this.cerrarForm.saldoContado < 0) {
      this.error = 'El saldo contado no puede ser negativo.';
      return;
    }

    this.procesando = true;

    this.cajaService.cerrarCaja({
      idUsuarioCierre: this.idUsuarioActual,
      idCaja: this.cajaActiva.idCaja,
      saldoContado: Number(this.cerrarForm.saldoContado || 0),
      observacionCierre: this.cerrarForm.observacionCierre || null
    }).subscribe({
      next: (caja) => {
        this.mensaje = caja.mensaje || 'Caja cerrada correctamente.';
        this.accionModal = null;
        this.procesando = false;
        this.cajaActiva = null;
        this.resumen = null;
        this.movimientos = [];
        this.cargarReporte();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.error = this.obtenerMensajeError(err, 'No se pudo cerrar la caja.');
        this.procesando = false;
        this.cdr.detectChanges();
      }
    });
  }

  aplicarFiltrosMovimientos(): void {
    this.cargarMovimientos();
  }

  limpiarFiltrosMovimientos(): void {
    this.filtroFechaInicio = '';
    this.filtroFechaFin = '';
    this.cargarMovimientos();
  }

  aplicarFiltrosReporte(): void {
    this.cargarReporte();
  }

  limpiarFiltrosReporte(): void {
    this.reporteFechaInicio = '';
    this.reporteFechaFin = '';
    this.cargarReporte();
  }

  diferenciaCierre(): number {
    const saldoContado = Number(this.cerrarForm.saldoContado || 0);
    const saldoSistema = Number(this.resumen?.saldoSistema || this.cajaActiva?.saldoSistema || 0);

    return Number((saldoContado - saldoSistema).toFixed(2));
  }

  totalMovimientosIngreso(): number {
    return this.movimientos.reduce((total, item) => total + Number(item.ingreso || 0), 0);
  }

  totalMovimientosEgreso(): number {
    return this.movimientos.reduce((total, item) => total + Number(item.egreso || 0), 0);
  }

  totalNetoMovimientos(): number {
    return Number((this.totalMovimientosIngreso() - this.totalMovimientosEgreso()).toFixed(2));
  }

  totalMetodosIngreso(): number {
    return this.resumenPorMetodoPago().reduce((total, item) => total + Number(item.ingresos || 0), 0);
  }

  totalMetodosEgreso(): number {
    return this.resumenPorMetodoPago().reduce((total, item) => total + Number(item.egresos || 0), 0);
  }

  totalMetodosNeto(): number {
    return Number((this.totalMetodosIngreso() - this.totalMetodosEgreso()).toFixed(2));
  }

  resumenPorMetodoPago(): ResumenMetodoPago[] {
    const mapa = new Map<string, ResumenMetodoPago>();

    for (const metodo of this.metodosPago) {
      const metodoNormalizado = this.normalizarMetodoPago(metodo);

      mapa.set(this.claveMetodoPago(metodoNormalizado), {
        metodoPago: metodoNormalizado,
        ingresos: 0,
        egresos: 0,
        neto: 0,
        cantidadMovimientos: 0,
        cantidadIngresos: 0,
        cantidadEgresos: 0
      });
    }

    for (const movimiento of this.movimientos) {
      const metodoNormalizado = this.normalizarMetodoPago(movimiento.metodoPago || 'Otro');
      const clave = this.claveMetodoPago(metodoNormalizado);

      if (!mapa.has(clave)) {
        mapa.set(clave, {
          metodoPago: metodoNormalizado,
          ingresos: 0,
          egresos: 0,
          neto: 0,
          cantidadMovimientos: 0,
          cantidadIngresos: 0,
          cantidadEgresos: 0
        });
      }

      const item = mapa.get(clave)!;
      const ingreso = Number(movimiento.ingreso || 0);
      const egreso = Number(movimiento.egreso || 0);

      item.ingresos = Number((item.ingresos + ingreso).toFixed(2));
      item.egresos = Number((item.egresos + egreso).toFixed(2));
      item.neto = Number((item.ingresos - item.egresos).toFixed(2));

      if (ingreso > 0 || egreso > 0) {
        item.cantidadMovimientos += 1;
      }

      if (ingreso > 0) {
        item.cantidadIngresos += 1;
      }

      if (egreso > 0) {
        item.cantidadEgresos += 1;
      }
    }

    return Array.from(mapa.values())
      .map(item => ({
        ...item,
        ingresos: Number(item.ingresos.toFixed(2)),
        egresos: Number(item.egresos.toFixed(2)),
        neto: Number((item.ingresos - item.egresos).toFixed(2))
      }))
      .sort((a, b) => {
        const ordenA = this.ordenMetodoPago(a.metodoPago);
        const ordenB = this.ordenMetodoPago(b.metodoPago);

        if (ordenA !== ordenB) {
          return ordenA - ordenB;
        }

        return a.metodoPago.localeCompare(b.metodoPago);
      });
  }

  resumenPorMetodoPagoConMovimiento(): ResumenMetodoPago[] {
    return this.resumenPorMetodoPago().filter(item =>
      item.cantidadMovimientos > 0 ||
      item.ingresos > 0 ||
      item.egresos > 0 ||
      item.neto !== 0
    );
  }

  tieneMovimientosPorMetodo(): boolean {
    return this.resumenPorMetodoPagoConMovimiento().length > 0;
  }

  efectivoEsperado(): number {
    const efectivo = this.resumenPorMetodoPago().find(
      item => this.claveMetodoPago(item.metodoPago) === this.claveMetodoPago('Efectivo')
    );

    const netoEfectivo = Number(efectivo?.neto || 0);
    const saldoInicial = Number(this.resumen?.saldoInicial || this.cajaActiva?.saldoInicial || 0);

    return Number((saldoInicial + netoEfectivo).toFixed(2));
  }

  netoMetodoPago(metodoPago: string): number {
    const item = this.resumenPorMetodoPago().find(
      metodo => this.claveMetodoPago(metodo.metodoPago) === this.claveMetodoPago(metodoPago)
    );

    return Number((item?.neto || 0).toFixed(2));
  }

  claseMetodoPago(metodoPago: string): string {
    const clave = this.claveMetodoPago(metodoPago);

    if (clave === 'efectivo') {
      return 'metodo-efectivo';
    }

    if (clave === 'yape') {
      return 'metodo-yape';
    }

    if (clave === 'plin') {
      return 'metodo-plin';
    }

    if (clave === 'transferencia') {
      return 'metodo-transferencia';
    }

    if (clave === 'tarjeta') {
      return 'metodo-tarjeta';
    }

    if (clave === 'deposito') {
      return 'metodo-deposito';
    }

    return 'metodo-otro';
  }

  claseNetoMetodo(neto: number): string {
    if (neto > 0) {
      return 'neto-positivo';
    }

    if (neto < 0) {
      return 'neto-negativo';
    }

    return 'neto-cero';
  }

  textoVerificacionMetodoPago(item: ResumenMetodoPago): string {
    const clave = this.claveMetodoPago(item.metodoPago);

    if (clave === 'efectivo') {
      return `Verificar efectivo esperado: S/ ${(this.efectivoEsperado()).toFixed(2)}`;
    }

    if (clave === 'yape') {
      return 'Verificar abonos recibidos en Yape.';
    }

    if (clave === 'plin') {
      return 'Verificar abonos recibidos en Plin.';
    }

    if (clave === 'transferencia') {
      return 'Verificar transferencias bancarias.';
    }

    if (clave === 'tarjeta') {
      return 'Verificar voucher o reporte de POS.';
    }

    if (clave === 'deposito') {
      return 'Verificar depósitos registrados.';
    }

    return 'Verificar soporte del movimiento.';
  }

  claseTipoMovimiento(tipo: string): string {
    const valor = (tipo || '').toLowerCase();

    if (valor.includes('ingreso')) {
      return 'tipo-ingreso';
    }

    if (valor.includes('egreso')) {
      return 'tipo-egreso';
    }

    return 'tipo-neutral';
  }

  claseEstadoCaja(estado: string): string {
    const valor = (estado || '').toLowerCase();

    if (valor.includes('abierta')) {
      return 'estado-abierta';
    }

    if (valor.includes('cerrada')) {
      return 'estado-cerrada';
    }

    if (valor.includes('anulada')) {
      return 'estado-anulada';
    }

    return 'estado-general';
  }

  formatearFecha(valor?: string | null): string {
    if (!valor) {
      return '-';
    }

    const fecha = new Date(valor);

    if (Number.isNaN(fecha.getTime())) {
      return '-';
    }

    return fecha.toLocaleString('es-PE', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  formatearFechaCorta(valor?: string | null): string {
    if (!valor) {
      return '-';
    }

    const fecha = new Date(valor);

    if (Number.isNaN(fecha.getTime())) {
      return '-';
    }

    return fecha.toLocaleDateString('es-PE', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    });
  }

  obtenerReferencia(item: MovimientoCaja): string {
    if (item.idVenta) {
      return `Venta N° ${item.idVenta}`;
    }

    if (item.idCompra) {
      return `Compra N° ${item.idCompra}`;
    }

    return 'Movimiento manual';
  }

  obtenerIconoMetodoPago(metodoPago: string): string {
    const clave = this.claveMetodoPago(metodoPago);

    if (clave === 'efectivo') {
      return 'EF';
    }

    if (clave === 'yape') {
      return 'YA';
    }

    if (clave === 'plin') {
      return 'PL';
    }

    if (clave === 'transferencia') {
      return 'TR';
    }

    if (clave === 'tarjeta') {
      return 'TJ';
    }

    if (clave === 'deposito') {
      return 'DP';
    }

    return 'OT';
  }

  private cargarUsuarioActual(): void {
    const claves = [
      'usuario',
      'usuarioActual',
      'authUser',
      'user',
      'sesionUsuario'
    ];

    let data: any = null;

    for (const clave of claves) {
      const raw = localStorage.getItem(clave);

      if (!raw) {
        continue;
      }

      try {
        data = JSON.parse(raw);
        break;
      } catch {
        data = null;
      }
    }

    const idUsuario = Number(
      data?.idUsuario ??
      data?.usuario?.idUsuario ??
      data?.id ??
      1
    );

    const rol = String(
      data?.rol ??
      data?.rolNombre ??
      data?.nombreRol ??
      data?.usuario?.rol ??
      data?.usuario?.rolNombre ??
      'Administrador'
    );

    const nombre = String(
      data?.correo ??
      data?.nombre ??
      data?.usuario ??
      data?.usuario?.correo ??
      'Administrador'
    );

    this.idUsuarioActual = idUsuario > 0 ? idUsuario : 1;
    this.usuarioActual = nombre;
    this.rolActual = rol;

    const rolNormalizado = rol.trim().toLowerCase();

    this.esAdministrador =
      this.idUsuarioActual === 1 ||
      rolNormalizado === 'administrador' ||
      rolNormalizado === 'admin';
  }

  private limpiarMensajes(): void {
    this.mensaje = '';
    this.error = '';
  }

  private obtenerMensajeError(err: any, mensajeDefault: string): string {
    return err?.error?.mensaje ||
      err?.error?.message ||
      err?.message ||
      mensajeDefault;
  }

  private normalizarMetodoPago(valor?: string | null): string {
    const texto = (valor || '').trim();

    if (!texto) {
      return 'Otro';
    }

    const clave = this.claveMetodoPago(texto);

    if (clave === 'efectivo') {
      return 'Efectivo';
    }

    if (clave === 'yape') {
      return 'Yape';
    }

    if (clave === 'plin') {
      return 'Plin';
    }

    if (clave === 'transferencia') {
      return 'Transferencia';
    }

    if (clave === 'tarjeta') {
      return 'Tarjeta';
    }

    if (clave === 'deposito') {
      return 'Depósito';
    }

    return 'Otro';
  }

  private claveMetodoPago(valor?: string | null): string {
    return (valor || '')
      .trim()
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/\s+/g, ' ');
  }

  private ordenMetodoPago(metodoPago: string): number {
    const clave = this.claveMetodoPago(metodoPago);

    const orden: Record<string, number> = {
      efectivo: 1,
      yape: 2,
      plin: 3,
      transferencia: 4,
      tarjeta: 5,
      deposito: 6,
      otro: 99
    };

    return orden[clave] ?? 99;
  }
}