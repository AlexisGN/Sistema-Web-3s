import { AvisoComponent } from '../../shared/mensajes/aviso';
import { inject } from '@angular/core';
import { SessionService } from '../../core/services/session.service';
import { ImagenCatalogoComponent } from '../../shared/imagen-catalogo/imagen-catalogo';
import { UiIconComponent } from '../../shared/ui-icon/ui-icon';
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { InventarioService } from '../../core/services/inventario';
import {
  InventarioListado,
  InventarioResumen,
  MovimientoStockListado,
  RegistrarMovimientoStock
} from '../../core/models/inventario.model';

@Component({
  selector: 'app-inventario',
  standalone: true,
  imports: [AvisoComponent, ImagenCatalogoComponent, UiIconComponent, CommonModule, FormsModule],
  templateUrl: './inventario.html',
  styleUrl: './inventario.scss'
})
export class InventarioComponent implements OnInit {
  readonly permisos = inject(SessionService);
  inventarios: InventarioListado[] = [];
  movimientosProducto: MovimientoStockListado[] = [];
  movimientosRecientes: MovimientoStockListado[] = [];

  resumen: InventarioResumen = {
    totalProductosInventario: 0,
    totalStockNormal: 0,
    totalStockBajo: 0,
    totalSinStock: 0,
    totalAlertasPendientes: 0,
    totalMovimientos: 0
  };

  productoSeleccionado: InventarioListado | null = null;

  cargando = false;
  guardandoStockMinimo = false;
  registrandoMovimiento = false;
  cargandoMovimientos = false;

  buscar = '';
  estadoStock = 'todos';

  pagina = 1;
  tamanioPagina = 6;
  totalRegistros = 0;
  totalPaginas = 0;
  opcionesTamanioPagina = [6, 12, 24];

  stockMinimoFormulario: number | null = null;

  movimiento: RegistrarMovimientoStock = this.nuevoMovimiento();

  mensaje = '';
  error = '';
  erroresCampo: Record<string, string> = {};

  constructor(
    private inventarioService: InventarioService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.subirArriba();
    this.cargarResumen();
    this.cargarInventario();
    this.cargarMovimientosRecientes();
  }

  cargarResumen(): void {
    this.inventarioService.resumen().subscribe({
      next: (data) => {
        this.resumen = data;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error cargando resumen de inventario:', err);
        this.cdr.detectChanges();
      }
    });
  }

  cargarInventario(): void {
    this.cargando = true;
    this.error = '';

    this.inventarioService
      .listar(this.buscar, this.estadoStock, this.pagina, this.tamanioPagina)
      .subscribe({
        next: (data) => {
          this.inventarios = data.items;
          this.pagina = data.pagina;
          this.tamanioPagina = data.tamanioPagina;
          this.totalRegistros = data.totalRegistros;
          this.totalPaginas = data.totalPaginas;

          if (this.inventarios.length === 0 && this.totalRegistros > 0 && this.pagina > 1) {
            this.pagina--;
            this.cargando = false;
            this.cargarInventario();
            return;
          }

          this.cargando = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Error cargando inventario:', err);
          this.error = 'No se pudo cargar el inventario.';
          this.cargando = false;
          this.cdr.detectChanges();
        }
      });
  }

  cargarMovimientosRecientes(): void {
    if (!(this.permisos.tienePermiso('INVENTARIO_REPORTE'))) return;
    this.inventarioService.listarMovimientosRecientes(5).subscribe({
      next: (data) => {
        this.movimientosRecientes = data;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error cargando movimientos recientes:', err);
        this.movimientosRecientes = [];
        this.cdr.detectChanges();
      }
    });
  }

  seleccionarProducto(item: InventarioListado): void {
    this.productoSeleccionado = item;
    this.stockMinimoFormulario = item.stockMinimo;
    this.movimiento = this.nuevoMovimiento(item.idProducto);

    this.mensaje = '';
    this.error = '';
    this.erroresCampo = {};

    this.cargarMovimientosProducto(item.idProducto);
    this.cdr.detectChanges();
  }

  cargarMovimientosProducto(idProducto: number): void {
    if (!(this.permisos.tienePermiso('INVENTARIO_REPORTE'))) return;
    this.cargandoMovimientos = true;

    this.inventarioService.listarMovimientosPorProducto(idProducto).subscribe({
      next: (data) => {
        this.movimientosProducto = data;
        this.cargandoMovimientos = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error cargando movimientos del producto:', err);
        this.movimientosProducto = [];
        this.cargandoMovimientos = false;
        this.cdr.detectChanges();
      }
    });
  }

  guardarStockMinimo(): void {
    if (!(this.permisos.tienePermiso('INVENTARIO_AJUSTAR'))) return;
    this.mensaje = '';
    this.error = '';
    this.erroresCampo = {};

    const errorValidacion = this.validarStockMinimo();

    if (errorValidacion) {
      this.cdr.detectChanges();
      return;
    }

    if (!this.productoSeleccionado) {
      this.marcarErrorCampo('producto', 'Selecciona un producto.');
      this.cdr.detectChanges();
      return;
    }

    this.guardandoStockMinimo = true;

    this.inventarioService
      .actualizarStockMinimo(this.productoSeleccionado.idProducto, {
        stockMinimo: Number(this.stockMinimoFormulario)
      })
      .subscribe({
        next: (data) => {
          this.productoSeleccionado = data;
          this.stockMinimoFormulario = data.stockMinimo;
          this.mensaje = 'Stock mínimo actualizado correctamente.';
          this.guardandoStockMinimo = false;

          this.cargarResumen();
          this.cargarInventario();
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error(err);
          this.asignarErrorBackend(err, 'No se pudo actualizar el stock mínimo.');
          this.guardandoStockMinimo = false;
          this.cdr.detectChanges();
        }
      });
  }

  registrarMovimiento(): void {
    if (!(this.permisos.tienePermiso('INVENTARIO_AJUSTAR'))) return;
    this.mensaje = '';
    this.error = '';
    this.erroresCampo = {};

    this.normalizarMovimiento();

    const errorValidacion = this.validarMovimiento();

    if (errorValidacion) {
      this.cdr.detectChanges();
      return;
    }

    this.registrandoMovimiento = true;

    this.inventarioService.registrarMovimientoManual(this.movimiento).subscribe({
      next: (data) => {
        this.productoSeleccionado = data;
        this.stockMinimoFormulario = data.stockMinimo;
        this.movimiento = this.nuevoMovimiento(data.idProducto);
        this.mensaje = 'Movimiento de stock registrado correctamente.';
        this.registrandoMovimiento = false;

        this.cargarResumen();
        this.cargarInventario();
        this.cargarMovimientosProducto(data.idProducto);
        this.cargarMovimientosRecientes();

        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.asignarErrorBackend(err, 'No se pudo registrar el movimiento de stock.');
        this.registrandoMovimiento = false;
        this.cdr.detectChanges();
      }
    });
  }

  cambiarTipoMovimiento(): void {
    this.erroresCampo = {};
    this.mensaje = '';
    this.error = '';

    this.movimiento.cantidad = null;
    this.movimiento.nuevoStock = null;
    this.movimiento.motivo = '';

    this.cdr.detectChanges();
  }

  buscarInventario(): void {
    this.pagina = 1;
    this.cargarInventario();
  }

  filtrarPorEstado(estado: string): void {
    this.estadoStock = estado;
    this.pagina = 1;
    this.cargarInventario();
  }

  paginaAnterior(): void {
    if (this.pagina <= 1) {
      return;
    }

    this.pagina--;
    this.cargarInventario();
  }

  paginaSiguiente(): void {
    if (this.pagina >= this.totalPaginas) {
      return;
    }

    this.pagina++;
    this.cargarInventario();
  }

  cambiarTamanioPagina(): void {
    this.pagina = 1;
    this.cargarInventario();
  }

  limpiarErrorCampo(campo: string): void {
    if (!this.erroresCampo[campo]) {
      return;
    }

    const mensajeActual = this.obtenerMensajeErrorCampo(campo);

    if (mensajeActual) {
      this.erroresCampo[campo] = mensajeActual;
      return;
    }

    delete this.erroresCampo[campo];
  }

  obtenerClaseEstado(item: InventarioListado): string {
    const estado = item.estadoStock.toLowerCase();

    if (estado.includes('sin')) {
      return 'sin-stock';
    }

    if (estado.includes('bajo')) {
      return 'stock-bajo';
    }

    return 'normal';
  }

  obtenerSignoMovimiento(tipo: string): string {
    const valor = tipo.toLowerCase();

    if (valor === 'entrada') {
      return '+';
    }

    if (valor === 'salida') {
      return '-';
    }

    return '±';
  }

  obtenerOrigenMovimiento(movimiento: MovimientoStockListado): string {
    if (movimiento.idCompra) {
      return 'Compra';
    }

    if (movimiento.idVenta) {
      return 'Venta';
    }

    return 'Manual';
  }

  private validarStockMinimo(): string | null {
    if (!this.productoSeleccionado) {
      return this.marcarErrorCampo('producto', 'Selecciona un producto.');
    }

    if (this.stockMinimoFormulario === null || this.stockMinimoFormulario === undefined) {
      return this.marcarErrorCampo('stockMinimo', 'Ingresa el stock mínimo.');
    }

    if (!Number.isInteger(Number(this.stockMinimoFormulario))) {
      return this.marcarErrorCampo('stockMinimo', 'El stock mínimo debe ser un número entero.');
    }

    if (Number(this.stockMinimoFormulario) < 0) {
      return this.marcarErrorCampo('stockMinimo', 'El stock mínimo no puede ser negativo.');
    }

    return null;
  }

  private validarMovimiento(): string | null {
    if (!this.productoSeleccionado || this.movimiento.idProducto <= 0) {
      return this.marcarErrorCampo('producto', 'Selecciona un producto.');
    }

    if (!this.movimiento.tipoMovimiento) {
      return this.marcarErrorCampo('tipoMovimiento', 'Selecciona el tipo de movimiento.');
    }

    if (this.movimiento.tipoMovimiento === 'Entrada' || this.movimiento.tipoMovimiento === 'Salida') {
      if (this.movimiento.cantidad === null || this.movimiento.cantidad === undefined) {
        return this.marcarErrorCampo('cantidad', 'Ingresa una cantidad.');
      }

      if (!Number.isInteger(Number(this.movimiento.cantidad))) {
        return this.marcarErrorCampo('cantidad', 'La cantidad debe ser un número entero.');
      }

      if (Number(this.movimiento.cantidad) <= 0) {
        return this.marcarErrorCampo('cantidad', 'La cantidad debe ser mayor a 0.');
      }

      if (
        this.movimiento.tipoMovimiento === 'Salida' &&
        Number(this.movimiento.cantidad) > this.productoSeleccionado.stockActual
      ) {
        return this.marcarErrorCampo(
          'cantidad',
          'No puedes registrar una salida mayor al stock actual.'
        );
      }
    }

    if (this.movimiento.tipoMovimiento === 'Ajuste') {
      if (this.movimiento.nuevoStock === null || this.movimiento.nuevoStock === undefined) {
        return this.marcarErrorCampo('nuevoStock', 'Ingresa el nuevo stock real.');
      }

      if (!Number.isInteger(Number(this.movimiento.nuevoStock))) {
        return this.marcarErrorCampo('nuevoStock', 'El nuevo stock debe ser un número entero.');
      }

      if (Number(this.movimiento.nuevoStock) < 0) {
        return this.marcarErrorCampo('nuevoStock', 'El nuevo stock no puede ser negativo.');
      }

      if (Number(this.movimiento.nuevoStock) === this.productoSeleccionado.stockActual) {
        return this.marcarErrorCampo(
          'nuevoStock',
          'El nuevo stock debe ser diferente al stock actual.'
        );
      }
    }

    if (!this.movimiento.motivo || this.movimiento.motivo.trim().length === 0) {
      return this.marcarErrorCampo('motivo', 'Ingresa el motivo del movimiento.');
    }

    if (this.movimiento.motivo.trim().length < 5) {
      return this.marcarErrorCampo('motivo', 'El motivo debe tener al menos 5 caracteres.');
    }

    return null;
  }

  private obtenerMensajeErrorCampo(campo: string): string | null {
    switch (campo) {
      case 'producto':
        if (!this.productoSeleccionado) {
          return 'Selecciona un producto.';
        }

        return null;

      case 'stockMinimo':
        return this.validarStockMinimoCampo();

      case 'cantidad':
        return this.validarCantidadCampo();

      case 'nuevoStock':
        return this.validarNuevoStockCampo();

      case 'motivo':
        if (!this.movimiento.motivo || this.movimiento.motivo.trim().length === 0) {
          return 'Ingresa el motivo del movimiento.';
        }

        if (this.movimiento.motivo.trim().length < 5) {
          return 'El motivo debe tener al menos 5 caracteres.';
        }

        return null;

      default:
        return null;
    }
  }

  private validarStockMinimoCampo(): string | null {
    if (this.stockMinimoFormulario === null || this.stockMinimoFormulario === undefined) {
      return 'Ingresa el stock mínimo.';
    }

    if (!Number.isInteger(Number(this.stockMinimoFormulario))) {
      return 'El stock mínimo debe ser un número entero.';
    }

    if (Number(this.stockMinimoFormulario) < 0) {
      return 'El stock mínimo no puede ser negativo.';
    }

    return null;
  }

  private validarCantidadCampo(): string | null {
    if (this.movimiento.tipoMovimiento !== 'Entrada' && this.movimiento.tipoMovimiento !== 'Salida') {
      return null;
    }

    if (this.movimiento.cantidad === null || this.movimiento.cantidad === undefined) {
      return 'Ingresa una cantidad.';
    }

    if (!Number.isInteger(Number(this.movimiento.cantidad))) {
      return 'La cantidad debe ser un número entero.';
    }

    if (Number(this.movimiento.cantidad) <= 0) {
      return 'La cantidad debe ser mayor a 0.';
    }

    if (
      this.movimiento.tipoMovimiento === 'Salida' &&
      this.productoSeleccionado &&
      Number(this.movimiento.cantidad) > this.productoSeleccionado.stockActual
    ) {
      return 'No puedes registrar una salida mayor al stock actual.';
    }

    return null;
  }

  private validarNuevoStockCampo(): string | null {
    if (this.movimiento.tipoMovimiento !== 'Ajuste') {
      return null;
    }

    if (this.movimiento.nuevoStock === null || this.movimiento.nuevoStock === undefined) {
      return 'Ingresa el nuevo stock real.';
    }

    if (!Number.isInteger(Number(this.movimiento.nuevoStock))) {
      return 'El nuevo stock debe ser un número entero.';
    }

    if (Number(this.movimiento.nuevoStock) < 0) {
      return 'El nuevo stock no puede ser negativo.';
    }

    if (
      this.productoSeleccionado &&
      Number(this.movimiento.nuevoStock) === this.productoSeleccionado.stockActual
    ) {
      return 'El nuevo stock debe ser diferente al stock actual.';
    }

    return null;
  }

  private marcarErrorCampo(campo: string, mensaje: string): string {
    this.erroresCampo[campo] = mensaje;
    return mensaje;
  }

  private asignarErrorBackend(err: any, mensajeDefecto: string): void {
    const mensajeBackend = err.error?.mensaje ?? mensajeDefecto;

    this.error = mensajeBackend;

    const texto = mensajeBackend.toLowerCase();

    if (texto.includes('producto')) {
      this.erroresCampo['producto'] = mensajeBackend;
    } else if (texto.includes('stock mínimo') || texto.includes('stock minimo')) {
      this.erroresCampo['stockMinimo'] = mensajeBackend;
    } else if (texto.includes('cantidad') || texto.includes('salida')) {
      this.erroresCampo['cantidad'] = mensajeBackend;
    } else if (texto.includes('nuevo stock')) {
      this.erroresCampo['nuevoStock'] = mensajeBackend;
    } else if (texto.includes('motivo')) {
      this.erroresCampo['motivo'] = mensajeBackend;
    } else if (texto.includes('tipo')) {
      this.erroresCampo['tipoMovimiento'] = mensajeBackend;
    }
  }

  private normalizarMovimiento(): void {
    this.movimiento.idProducto = this.productoSeleccionado?.idProducto ?? 0;
    this.movimiento.motivo = (this.movimiento.motivo ?? '').trim();
    this.movimiento.idUsuarioRegistro = 1;

    if (this.movimiento.tipoMovimiento === 'Ajuste') {
      this.movimiento.cantidad = null;
      this.movimiento.nuevoStock =
        this.movimiento.nuevoStock === null || this.movimiento.nuevoStock === undefined
          ? null
          : Number(this.movimiento.nuevoStock);
    } else {
      this.movimiento.cantidad =
        this.movimiento.cantidad === null || this.movimiento.cantidad === undefined
          ? null
          : Number(this.movimiento.cantidad);

      this.movimiento.nuevoStock = null;
    }
  }

  private nuevoMovimiento(idProducto: number = 0): RegistrarMovimientoStock {
    return {
      idProducto,
      tipoMovimiento: 'Entrada',
      cantidad: null,
      nuevoStock: null,
      motivo: '',
      idUsuarioRegistro: 1
    };
  }

  private subirArriba(): void {
    setTimeout(() => {
      window.scrollTo({
        top: 0,
        left: 0,
        behavior: 'auto'
      });
    }, 0);
  }
}