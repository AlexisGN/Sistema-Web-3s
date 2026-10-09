import { AvisoComponent } from '../../shared/mensajes/aviso';
import { UiIconComponent } from '../../shared/ui-icon/ui-icon';
import { SessionService } from '../../core/services/session.service';
import { CommonModule } from '@angular/common';
import { inject, Component, ChangeDetectorRef, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { CompraService } from '../../core/services/compra';
import {
  AnularCompra,
  CompraCrear,
  CompraDetalleCompleto,
  CompraDetalleCrear,
  CompraListado,
  CuotaCompraCrear,
  CuotaCompraDetalle,
  PagoCompraCrear,
  ProductoCompraDisponible,
  ProveedorCompra
} from '../../core/models/compra.model';

@Component({
  selector: 'app-compras',
  standalone: true,
  imports: [AvisoComponent, UiIconComponent, CommonModule, FormsModule],
  templateUrl: './compras.html',
  styleUrl: './compras.scss'
})
export class ComprasComponent implements OnInit {
  readonly permisos = inject(SessionService);
  private readonly sesionAnulacion = inject(SessionService);
  compras: CompraListado[] = [];
  proveedores: ProveedorCompra[] = [];
  productos: ProductoCompraDisponible[] = [];

  compra: CompraCrear = this.nuevaCompra();

  proveedorSeleccionado: ProveedorCompra | null = null;
  productoSeleccionado: ProductoCompraDisponible | null = null;

  detalleFormulario = {
    cantidad: null as number | null,
    precioCompra: null as number | null
  };

  buscarCompra = '';
  estadoPagoFiltro = '';
  fechaInicioFiltro = '';
  fechaFinFiltro = '';

  buscarProveedor = '';
  buscarProducto = '';

  pagina = 1;
  tamanioPagina = 8;
  totalRegistros = 0;
  totalPaginas = 0;
  opcionesTamanioPagina = [8, 12, 20];

  cargando = false;
  guardando = false;
  buscandoProveedores = false;
  buscandoProductos = false;
  generandoPdfCompraId: number | null = null;
  generandoReportePdf = false;
  generandoReporteExcel = false;

  mensaje = '';
  error = '';
  erroresCampo: Record<string, string> = {};

  compraPagoSeleccionada: CompraListado | null = null;
  compraDetallePago: CompraDetalleCompleto | null = null;
  cargandoDetallePago = false;
  cuotasSeleccionadasPago: number[] = [];
  pago: PagoCompraCrear = this.nuevoPago();

  compraAnularSeleccionada: CompraListado | null = null;
  anularDto: AnularCompra = this.nuevaAnulacion();

  readonly tiposComprobante = [
    'Factura',
    'Boleta',
    'Nota de venta',
    'Recibo',
    'Otro'
  ];

  readonly tiposPago = [
    'Total',
    'Parcial',
    'Cuotas'
  ];

  readonly metodosPago = [
    'Efectivo',
    'Yape',
    'Plin',
    'Transferencia',
    'Tarjeta',
    'Otro'
  ];

  readonly estadosPago = [
    'Pendiente',
    'Parcial',
    'En cuotas',
    'Pagada',
    'Anulada'
  ];

  constructor(
    private compraService: CompraService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.subirArriba();
    this.cargarCompras();
    this.cargarProveedores();
    this.cargarProductos();
  }

  cargarCompras(): void {
    this.cargando = true;
    this.error = '';

    this.compraService
      .listar(
        this.buscarCompra,
        this.estadoPagoFiltro,
        this.fechaInicioFiltro,
        this.fechaFinFiltro,
        this.pagina,
        this.tamanioPagina
      )
      .subscribe({
        next: (data) => {
          this.compras = data.items ?? [];
          this.pagina = data.pagina;
          this.tamanioPagina = data.tamanioPagina;
          this.totalRegistros = data.totalRegistros;
          this.totalPaginas = data.totalPaginas ?? Math.max(1, Math.ceil(this.totalRegistros / this.tamanioPagina));

          this.cargando = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Error cargando compras:', err);
          this.error = 'No se pudieron cargar las compras.';
          this.cargando = false;
          this.cdr.detectChanges();
        }
      });
  }

  cargarProveedores(): void {
    if (!(this.permisos.tienePermiso('COMPRAS_CREAR'))) return;
    this.buscandoProveedores = true;

    this.compraService
      .listarProveedores(this.buscarProveedor, 1, 10)
      .subscribe({
        next: (data) => {
          this.proveedores = data.items ?? [];
          this.buscandoProveedores = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Error cargando proveedores:', err);
          this.proveedores = [];
          this.buscandoProveedores = false;
          this.cdr.detectChanges();
        }
      });
  }

  cargarProductos(): void {
    if (!(this.permisos.tienePermiso('COMPRAS_CREAR'))) return;
    this.buscandoProductos = true;

    this.compraService
      .listarProductos(this.buscarProducto, 1, 10)
      .subscribe({
        next: (data) => {
          this.productos = data.items ?? [];
          this.buscandoProductos = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Error cargando productos:', err);
          this.productos = [];
          this.buscandoProductos = false;
          this.cdr.detectChanges();
        }
      });
  }

  buscarCompras(): void {
    this.pagina = 1;
    this.cargarCompras();
  }

  limpiarFiltros(): void {
    this.buscarCompra = '';
    this.estadoPagoFiltro = '';
    this.fechaInicioFiltro = '';
    this.fechaFinFiltro = '';
    this.pagina = 1;
    this.cargarCompras();
  }

  seleccionarProveedor(proveedor: ProveedorCompra): void {
    this.proveedorSeleccionado = proveedor;
    this.compra.idProveedor = proveedor.idProveedor;
    this.buscarProveedor = proveedor.razonSocial;
    this.limpiarErrorCampo('proveedor');
  }

  seleccionarProducto(producto: ProductoCompraDisponible): void {
    this.productoSeleccionado = producto;
    this.buscarProducto = this.obtenerNombreProducto(producto);
    this.limpiarErrorCampo('productoDetalle');
  }

  agregarDetalle(): void {
    const errorDetalle = this.validarDetalleFormulario();

    if (errorDetalle) {
      this.cdr.detectChanges();
      return;
    }

    const producto = this.productoSeleccionado!;

    if (this.compra.detalles.some(d => d.idProducto === producto.idProducto)) {
      this.erroresCampo['productoDetalle'] = 'Este producto ya fue agregado a la compra.';
      this.cdr.detectChanges();
      return;
    }

    const detalle: CompraDetalleCrear = {
      idProducto: producto.idProducto,
      cantidad: Number(this.detalleFormulario.cantidad),
      precioCompra: Number(this.detalleFormulario.precioCompra),
      codigoProducto: producto.codigoProducto,
      producto: this.obtenerNombreProducto(producto),
      categoria: producto.categoria,
      marca: producto.marca,
      unidadMedida: producto.unidadMedida
    };

    this.compra.detalles.push(detalle);

    this.productoSeleccionado = null;
    this.buscarProducto = '';

    this.detalleFormulario = {
      cantidad: null,
      precioCompra: null
    };

    delete this.erroresCampo['detalles'];
    delete this.erroresCampo['productoDetalle'];
    delete this.erroresCampo['cantidadDetalle'];
    delete this.erroresCampo['precioDetalle'];
    delete this.erroresCampo['cronogramaCuotas'];

    this.ajustarPagoPorTipo();
    this.cdr.detectChanges();
  }

  eliminarDetalle(index: number): void {
    this.compra.detalles.splice(index, 1);
    this.ajustarPagoPorTipo();
    this.cdr.detectChanges();
  }

  guardarCompra(): void {
    if (!(this.permisos.tienePermiso('COMPRAS_CREAR'))) return;
    this.mensaje = '';
    this.error = '';
    this.erroresCampo = {};

    this.normalizarCompra();

    const errorValidacion = this.validarCompra();

    if (errorValidacion) {
      this.cdr.detectChanges();
      return;
    }

    this.guardando = true;

    this.compraService.registrar(this.compra).subscribe({
      next: (data) => {
        this.mensaje = data.mensaje || 'Compra registrada correctamente.';
        this.guardando = false;

        this.cargarCompras();
        this.cargarProductos();
        this.limpiarFormulario(false);

        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.asignarErrorBackend(err, 'No se pudo registrar la compra.');
        this.guardando = false;
        this.cdr.detectChanges();
      }
    });
  }

  verPdfCompra(item: CompraListado): void {
    this.error = '';
    this.mensaje = '';

    const ventanaPdf = window.open('', '_blank');

    if (!ventanaPdf) {
      this.error = 'El navegador bloqueó la ventana del PDF. Permite ventanas emergentes para el sistema.';
      this.cdr.detectChanges();
      return;
    }

    ventanaPdf.document.write(`
      <html>
        <head>
          <title>Generando PDF</title>
        </head>
        <body style="font-family: Arial; padding: 30px;">
          <h2>Generando PDF...</h2>
          <p>Espera un momento.</p>
        </body>
      </html>
    `);

    ventanaPdf.document.close();

    this.generandoPdfCompraId = item.idCompra;

    this.compraService.obtenerPdf(item.idCompra).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        ventanaPdf.location.href = url;

        setTimeout(() => {
          window.URL.revokeObjectURL(url);
        }, 60000);

        this.generandoPdfCompraId = null;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        ventanaPdf.close();

        this.error = err.error?.mensaje || 'No se pudo generar el PDF de la compra.';
        this.generandoPdfCompraId = null;
        this.cdr.detectChanges();
      }
    });
  }

  exportarReportePdf(): void {
    if (!(this.permisos.tienePermiso('COMPRAS_REPORTE'))) return;
    this.error = '';
    this.mensaje = '';
    this.generandoReportePdf = true;

    const ventanaPdf = window.open('', '_blank');

    if (!ventanaPdf) {
      this.generandoReportePdf = false;
      this.error = 'El navegador bloqueó la ventana del reporte PDF. Permite ventanas emergentes para el sistema.';
      this.cdr.detectChanges();
      return;
    }

    ventanaPdf.document.write(`
      <html>
        <head>
          <title>Generando reporte PDF</title>
        </head>
        <body style="font-family: Arial; padding: 30px;">
          <h2>Generando reporte PDF de compras...</h2>
          <p>Espera un momento.</p>
        </body>
      </html>
    `);

    ventanaPdf.document.close();

    this.compraService
      .obtenerReportePdf(
        this.buscarCompra,
        this.estadoPagoFiltro,
        this.fechaInicioFiltro,
        this.fechaFinFiltro
      )
      .subscribe({
        next: (blob) => {
          const url = window.URL.createObjectURL(blob);
          ventanaPdf.location.href = url;

          setTimeout(() => {
            window.URL.revokeObjectURL(url);
          }, 60000);

          this.generandoReportePdf = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error(err);
          ventanaPdf.close();

          this.error = 'No se pudo generar el reporte PDF de compras.';
          this.generandoReportePdf = false;
          this.cdr.detectChanges();
        }
      });
  }

  exportarReporteExcel(): void {
    if (!(this.permisos.tienePermiso('COMPRAS_REPORTE'))) return;
    this.error = '';
    this.mensaje = '';
    this.generandoReporteExcel = true;

    this.compraService
      .obtenerReporteExcel(
        this.buscarCompra,
        this.estadoPagoFiltro,
        this.fechaInicioFiltro,
        this.fechaFinFiltro
      )
      .subscribe({
        next: (blob) => {
          const url = window.URL.createObjectURL(blob);
          const link = document.createElement('a');

          link.href = url;
          link.download = `reporte-compras-${new Date().toISOString().slice(0, 10)}.xlsx`;
          link.click();

          window.URL.revokeObjectURL(url);

          this.generandoReporteExcel = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error(err);
          this.error = 'No se pudo generar el reporte Excel de compras.';
          this.generandoReporteExcel = false;
          this.cdr.detectChanges();
        }
      });
  }

  abrirPago(compra: CompraListado): void {
    if (!(this.permisos.tienePermiso('COMPRAS_PAGAR'))) return;
    this.compraPagoSeleccionada = compra;
    this.compraDetallePago = null;
    this.cargandoDetallePago = false;
    this.cuotasSeleccionadasPago = [];

    this.pago = {
      idCompra: compra.idCompra,
      idUsuarioRegistro: 1,
      metodoPago: '',
      montoPagado: this.esCompraEnCuotas(compra) ? 0 : Number(compra.saldoPendiente || 0),
      observacion: '',
      idsCuotasPagadas: []
    };

    this.mensaje = '';
    this.error = '';
    this.erroresCampo = {};

    if (this.esCompraEnCuotas(compra)) {
      this.cargarDetallePago(compra.idCompra);
    }

    this.cdr.detectChanges();
  }

  private cargarDetallePago(idCompra: number): void {
    this.cargandoDetallePago = true;

    this.compraService.obtenerDetalle(idCompra).subscribe({
      next: (detalle) => {
        this.compraDetallePago = detalle;
        this.cargandoDetallePago = false;

        if (this.cuotasPendientesPago().length === 0) {
          this.erroresCampo['cuotasPago'] = 'No hay cuotas pendientes para esta compra.';
        }

        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.cargandoDetallePago = false;
        this.compraDetallePago = null;
        this.erroresCampo['cuotasPago'] = 'No se pudieron cargar las cuotas de la compra.';
        this.cdr.detectChanges();
      }
    });
  }

  cerrarPago(): void {
    this.compraPagoSeleccionada = null;
    this.compraDetallePago = null;
    this.cargandoDetallePago = false;
    this.cuotasSeleccionadasPago = [];
    this.pago = this.nuevoPago();
    this.erroresCampo = {};
  }

  registrarPago(): void {
    if (!(this.permisos.tienePermiso('COMPRAS_PAGAR'))) return;
    this.mensaje = '';
    this.error = '';
    this.erroresCampo = {};

    this.prepararPagoAntesDeEnviar();

    const errorValidacion = this.validarPago();

    if (errorValidacion) {
      this.cdr.detectChanges();
      return;
    }

    this.guardando = true;

    this.compraService.registrarPago(this.pago).subscribe({
      next: (data) => {
        this.mensaje = data.mensaje || 'Pago registrado correctamente.';
        this.guardando = false;

        this.cerrarPago();
        this.cargarCompras();

        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.asignarErrorBackend(err, 'No se pudo registrar el pago.');
        this.guardando = false;
        this.cdr.detectChanges();
      }
    });
  }

  private prepararPagoAntesDeEnviar(): void {
    if (!this.compraPagoSeleccionada) {
      return;
    }

    this.pago.idCompra = this.compraPagoSeleccionada.idCompra;
    this.pago.idUsuarioRegistro = 1;
    this.pago.metodoPago = this.pago.metodoPago?.trim() || '';
    this.pago.observacion = this.pago.observacion?.trim() || null;

    if (this.esPagoEnCuotas()) {
      this.pago.idsCuotasPagadas = [...this.cuotasSeleccionadasPago];
      this.recalcularMontoPagoCuotas();
      return;
    }

    this.pago.idsCuotasPagadas = [];
    this.pago.montoPagado = Number(this.compraPagoSeleccionada.saldoPendiente || 0);
  }

  abrirAnular(compra: CompraListado): void {
    if (!this.puedeAnular(compra)) return;
    this.compraAnularSeleccionada = compra;
    this.anularDto = this.nuevaAnulacion();
    this.mensaje = '';
    this.error = '';
    this.erroresCampo = {};
    this.cdr.detectChanges();
  }

  cerrarAnular(): void {
    this.compraAnularSeleccionada = null;
    this.anularDto = this.nuevaAnulacion();
    this.erroresCampo = {};
  }

  anularCompra(): void {
    if (!(this.permisos.tienePermiso('COMPRAS_ANULAR'))) return;
    if (!this.sesionAnulacion.esAdministrador()) return;
    this.mensaje = '';
    this.error = '';
    this.erroresCampo = {};

    if (!this.compraAnularSeleccionada) {
      this.erroresCampo['anular'] = 'Selecciona una compra para anular.';
      this.cdr.detectChanges();
      return;
    }

    if (!this.anularDto.motivo || this.anularDto.motivo.trim().length < 5) {
      this.erroresCampo['motivoAnulacion'] = 'Ingresa un motivo de anulación válido.';
      this.cdr.detectChanges();
      return;
    }

    this.guardando = true;

    this.compraService
      .anular(this.compraAnularSeleccionada.idCompra, this.anularDto)
      .subscribe({
        next: (data) => {
          this.mensaje = data.mensaje || 'Compra anulada correctamente.';
          this.guardando = false;

          this.cerrarAnular();
          this.cargarCompras();

          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error(err);
          this.asignarErrorBackend(err, 'No se pudo anular la compra.');
          this.guardando = false;
          this.cdr.detectChanges();
        }
      });
  }

  cambiarTipoPago(): void {
    this.compra.numeroCuotas = null;
    this.compra.fechaPrimerVencimiento = null;
    this.compra.observacionPago = '';
    this.compra.cuotas = [];

    this.ajustarPagoPorTipo();
    this.limpiarErrorCampo('tipoPago');
    this.limpiarErrorCampo('montoPagado');

    delete this.erroresCampo['numeroCuotas'];
    delete this.erroresCampo['fechaPrimerVencimiento'];
    delete this.erroresCampo['cronogramaCuotas'];

    this.cdr.detectChanges();
  }

  cambiarNumeroCuotas(): void {
    this.limpiarErrorCampo('numeroCuotas');
    this.limpiarErrorCampo('cronogramaCuotas');
    this.generarCuotas(false);
    this.cdr.detectChanges();
  }

  cambiarFechaPrimerVencimiento(): void {
    this.limpiarErrorCampo('fechaPrimerVencimiento');
    this.limpiarErrorCampo('cronogramaCuotas');
    this.generarCuotas(true);
    this.cdr.detectChanges();
  }

  cambiarFechaCuota(index: number): void {
    delete this.erroresCampo[`fechaCuota${index}`];
    delete this.erroresCampo['cronogramaCuotas'];
    this.cdr.detectChanges();
  }

  cambiarTieneGuia(): void {
    if (!this.compra.guiaRemision.tieneGuia) {
      this.compra.guiaRemision = {
        tieneGuia: false,
        numeroGuia: null,
        fechaEmision: null,
        fechaTraslado: null,
        puntoPartida: null,
        puntoLlegada: null,
        transportista: null,
        rucTransportista: null,
        placaVehiculo: null,
        observacion: null
      };

      delete this.erroresCampo['numeroGuia'];
      delete this.erroresCampo['fechaEmisionGuia'];
      delete this.erroresCampo['fechaTrasladoGuia'];
      delete this.erroresCampo['puntoPartida'];
      delete this.erroresCampo['puntoLlegada'];
    }
  }

  cambiarMontoPagado(): void {
    this.limpiarErrorCampo('montoPagado');
    this.ajustarPagoPorTipo();
    this.cdr.detectChanges();
  }

  seleccionarCuotaPago(cuota: CuotaCompraDetalle): void {
    const idCuota = Number(cuota.idCuotaCompra);

    if (idCuota <= 0) {
      return;
    }

    if (this.estaCuotaPagoSeleccionada(idCuota)) {
      this.cuotasSeleccionadasPago = this.cuotasSeleccionadasPago.filter(id => id !== idCuota);
    } else {
      this.cuotasSeleccionadasPago.push(idCuota);
    }

    delete this.erroresCampo['cuotasPago'];
    delete this.erroresCampo['montoPago'];

    this.recalcularMontoPagoCuotas();
    this.cdr.detectChanges();
  }

  seleccionarTodasCuotasPago(): void {
    this.cuotasSeleccionadasPago = this.cuotasPendientesPago().map(c => c.idCuotaCompra);
    delete this.erroresCampo['cuotasPago'];
    delete this.erroresCampo['montoPago'];
    this.recalcularMontoPagoCuotas();
    this.cdr.detectChanges();
  }

  limpiarCuotasPago(): void {
    this.cuotasSeleccionadasPago = [];
    this.pago.idsCuotasPagadas = [];
    this.pago.montoPagado = 0;
    delete this.erroresCampo['cuotasPago'];
    delete this.erroresCampo['montoPago'];
    this.cdr.detectChanges();
  }

  estaCuotaPagoSeleccionada(idCuotaCompra: number): boolean {
    return this.cuotasSeleccionadasPago.includes(Number(idCuotaCompra));
  }

  cuotasPendientesPago(): CuotaCompraDetalle[] {
    const cuotas = this.compraDetallePago?.cuotas ?? [];

    return cuotas
      .filter(c => this.calcularSaldoCuota(c) > 0 && (c.estadoCuota || '').toLowerCase() !== 'pagada')
      .sort((a, b) => Number(a.numeroCuota) - Number(b.numeroCuota));
  }

  calcularSaldoCuota(cuota: CuotaCompraDetalle): number {
    const saldo = Number(cuota.montoCuota || 0) - Number(cuota.montoPagado || 0);
    return Number(Math.max(0, saldo).toFixed(2));
  }

  calcularMontoCuotasSeleccionadas(): number {
    const cuotas = this.cuotasPendientesPago();
    const total = cuotas
      .filter(c => this.estaCuotaPagoSeleccionada(c.idCuotaCompra))
      .reduce((acumulado, cuota) => acumulado + this.calcularSaldoCuota(cuota), 0);

    return Number(total.toFixed(2));
  }

  recalcularMontoPagoCuotas(): void {
    if (!this.esPagoEnCuotas()) {
      return;
    }

    this.pago.idsCuotasPagadas = [...this.cuotasSeleccionadasPago];
    this.pago.montoPagado = this.calcularMontoCuotasSeleccionadas();
  }

  esCompraEnCuotas(compra: CompraListado | null = this.compraPagoSeleccionada): boolean {
    if (!compra) {
      return false;
    }

    return (compra.estadoPago || '').toLowerCase().includes('cuota');
  }

  esPagoEnCuotas(): boolean {
    return this.esCompraEnCuotas(this.compraPagoSeleccionada);
  }

  calcularSaldoDespuesPago(): number {
    const saldoActual = Number(this.compraPagoSeleccionada?.saldoPendiente || 0);
    const monto = Number(this.pago.montoPagado || 0);
    return Number(Math.max(0, saldoActual - monto).toFixed(2));
  }

  paginaAnterior(): void {
    if (this.pagina <= 1) {
      return;
    }

    this.pagina--;
    this.cargarCompras();
  }

  paginaSiguiente(): void {
    if (this.pagina >= this.totalPaginas) {
      return;
    }

    this.pagina++;
    this.cargarCompras();
  }

  cambiarTamanioPagina(): void {
    this.pagina = 1;
    this.cargarCompras();
  }

  limpiarFormulario(limpiarMensaje: boolean = true): void {
    this.compra = this.nuevaCompra();
    this.proveedorSeleccionado = null;
    this.productoSeleccionado = null;

    this.buscarProveedor = '';
    this.buscarProducto = '';

    this.detalleFormulario = {
      cantidad: null,
      precioCompra: null
    };

    this.erroresCampo = {};

    if (limpiarMensaje) {
      this.mensaje = '';
      this.error = '';
    }

    this.cdr.detectChanges();
  }

  abrirProductosEnNuevaPestana(): void {
    window.open('/admin/productos', '_blank');
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

  obtenerNombreProducto(producto: ProductoCompraDisponible | CompraDetalleCrear): string {
    return (
      (producto as ProductoCompraDisponible).nombre ||
      (producto as ProductoCompraDisponible).producto ||
      (producto as ProductoCompraDisponible).nombreProducto ||
      (producto as CompraDetalleCrear).producto ||
      'Producto sin nombre'
    );
  }

  calcularSubtotalDetalle(detalle: CompraDetalleCrear): number {
    return Number(detalle.cantidad || 0) * Number(detalle.precioCompra || 0);
  }

  calcularTotal(): number {
    const total = this.compra.detalles.reduce((acumulado, detalle) => {
      return acumulado + this.calcularSubtotalDetalle(detalle);
    }, 0);

    return Number(total.toFixed(2));
  }

  calcularSubtotal(): number {
    const total = this.calcularTotal();
    return Number((total / 1.18).toFixed(2));
  }

  calcularIgv(): number {
    const total = this.calcularTotal();
    const subtotal = this.calcularSubtotal();

    return Number((total - subtotal).toFixed(2));
  }

  calcularSaldo(): number {
    const saldo = this.calcularTotal() - Number(this.compra.montoPagado || 0);
    return Number(saldo.toFixed(2));
  }

  calcularTotalCuotas(): number {
    const total = (this.compra.cuotas ?? []).reduce((acumulado, cuota) => {
      return acumulado + Number(cuota.montoCuota || 0);
    }, 0);

    return Number(total.toFixed(2));
  }

  obtenerSaldoFinanciar(): number {
    const saldo = this.calcularSaldo();
    return saldo > 0 ? Number(saldo.toFixed(2)) : 0;
  }

  puedePagar(compra: CompraListado): boolean {
    if (!this.permisos.tienePermiso('COMPRAS_PAGAR')) return false;
    const estadoCompra = (compra.estadoCompra || '').toLowerCase();
    return !estadoCompra.includes('anulada') && Number(compra.saldoPendiente) > 0;
  }

  puedeAnular(compra: CompraListado): boolean {
    if (!this.permisos.tienePermiso('COMPRAS_ANULAR')) return false;
    if (!this.sesionAnulacion.esAdministrador()) return false;
    const estadoCompra = (compra.estadoCompra || '').toLowerCase();
    return !estadoCompra.includes('anulada');
  }

  obtenerClaseEstadoPago(estado: string): string {
    const valor = estado.toLowerCase();

    if (valor.includes('pagada')) {
      return 'pagada';
    }

    if (valor.includes('cuota')) {
      return 'cuotas';
    }

    if (valor.includes('parcial')) {
      return 'parcial';
    }

    if (valor.includes('anulada')) {
      return 'anulada';
    }

    return 'pendiente';
  }

  private validarDetalleFormulario(): string | null {
    if (!this.productoSeleccionado) {
      return this.marcarErrorCampo('productoDetalle', 'Selecciona un producto.');
    }

    if (this.detalleFormulario.cantidad === null || this.detalleFormulario.cantidad === undefined) {
      return this.marcarErrorCampo('cantidadDetalle', 'Ingresa la cantidad.');
    }

    if (!Number.isInteger(Number(this.detalleFormulario.cantidad))) {
      return this.marcarErrorCampo('cantidadDetalle', 'La cantidad debe ser un número entero.');
    }

    if (Number(this.detalleFormulario.cantidad) <= 0) {
      return this.marcarErrorCampo('cantidadDetalle', 'La cantidad debe ser mayor a 0.');
    }

    if (this.detalleFormulario.precioCompra === null || this.detalleFormulario.precioCompra === undefined) {
      return this.marcarErrorCampo('precioDetalle', 'Ingresa el precio de compra con IGV.');
    }

    if (Number(this.detalleFormulario.precioCompra) <= 0) {
      return this.marcarErrorCampo('precioDetalle', 'El precio de compra con IGV debe ser mayor a 0.');
    }

    return null;
  }

  private validarCompra(): string | null {
    if (!this.proveedorSeleccionado || this.compra.idProveedor <= 0) {
      return this.marcarErrorCampo('proveedor', 'Selecciona un proveedor.');
    }

    if (!this.compra.tipoComprobanteProveedor) {
      return this.marcarErrorCampo('tipoComprobanteProveedor', 'Selecciona el tipo de comprobante recibido.');
    }

    if (!this.compra.serieComprobante) {
      return this.marcarErrorCampo('serieComprobante', 'Ingresa la serie del comprobante.');
    }

    if (this.compra.serieComprobante.length < 2) {
      return this.marcarErrorCampo('serieComprobante', 'La serie debe tener al menos 2 caracteres.');
    }

    if (!this.compra.numeroComprobante) {
      return this.marcarErrorCampo('numeroComprobante', 'Ingresa el número del comprobante.');
    }

    if (!this.compra.fechaEmisionComprobante) {
      return this.marcarErrorCampo('fechaEmisionComprobante', 'Ingresa la fecha de emisión del comprobante.');
    }

    if (this.compra.guiaRemision.tieneGuia) {
      if (!this.compra.guiaRemision.numeroGuia) {
        return this.marcarErrorCampo('numeroGuia', 'Ingresa el número de guía de remisión.');
      }

      if (!this.compra.guiaRemision.fechaEmision) {
        return this.marcarErrorCampo('fechaEmisionGuia', 'Ingresa la fecha de emisión de la guía.');
      }

      if (!this.compra.guiaRemision.fechaTraslado) {
        return this.marcarErrorCampo('fechaTrasladoGuia', 'Ingresa la fecha de traslado de la guía.');
      }

      if (!this.compra.guiaRemision.puntoPartida) {
        return this.marcarErrorCampo('puntoPartida', 'Ingresa el punto de partida.');
      }

      if (!this.compra.guiaRemision.puntoLlegada) {
        return this.marcarErrorCampo('puntoLlegada', 'Ingresa el punto de llegada.');
      }
    }

    if (this.compra.detalles.length === 0) {
      return this.marcarErrorCampo('detalles', 'Agrega al menos un producto a la compra.');
    }

    if (!this.compra.tipoPago) {
      return this.marcarErrorCampo('tipoPago', 'Selecciona el tipo de pago.');
    }

    if (!this.compra.metodoPago) {
      return this.marcarErrorCampo('metodoPago', 'Selecciona el método de pago.');
    }

    const total = this.calcularTotal();

    if (this.compra.montoPagado === null || this.compra.montoPagado === undefined) {
      return this.marcarErrorCampo('montoPagado', 'Ingresa el monto pagado.');
    }

    if (Number(this.compra.montoPagado) < 0) {
      return this.marcarErrorCampo('montoPagado', 'El monto pagado no puede ser negativo.');
    }

    if (Number(this.compra.montoPagado) > total) {
      return this.marcarErrorCampo('montoPagado', 'El monto pagado no puede ser mayor al total.');
    }

    if (this.compra.tipoPago === 'Total' && Number(this.compra.montoPagado) !== total) {
      return this.marcarErrorCampo('montoPagado', 'Para pago total, el monto pagado debe ser igual al total.');
    }

    if (
      this.compra.tipoPago === 'Parcial' &&
      (Number(this.compra.montoPagado) <= 0 || Number(this.compra.montoPagado) >= total)
    ) {
      return this.marcarErrorCampo(
        'montoPagado',
        'Para pago parcial, el monto debe ser mayor a 0 y menor al total.'
      );
    }

    if (this.compra.tipoPago === 'Cuotas') {
      const numeroCuotas = Number(this.compra.numeroCuotas || 0);

      if (!this.compra.numeroCuotas || numeroCuotas <= 0) {
        return this.marcarErrorCampo('numeroCuotas', 'Ingresa un número de cuotas válido.');
      }

      if (!Number.isInteger(numeroCuotas)) {
        return this.marcarErrorCampo('numeroCuotas', 'El número de cuotas debe ser entero.');
      }

      if (numeroCuotas > 24) {
        return this.marcarErrorCampo('numeroCuotas', 'El número máximo permitido es 24 cuotas.');
      }

      if (!this.compra.fechaPrimerVencimiento) {
        return this.marcarErrorCampo('fechaPrimerVencimiento', 'Ingresa la fecha del primer vencimiento.');
      }

      if (Number(this.compra.montoPagado) >= total) {
        return this.marcarErrorCampo('montoPagado', 'Para compra en cuotas debe quedar saldo pendiente.');
      }

      this.generarCuotas(false);

      if (!this.compra.cuotas || this.compra.cuotas.length !== numeroCuotas) {
        return this.marcarErrorCampo('cronogramaCuotas', 'El cronograma de cuotas no coincide con el número de cuotas.');
      }

      for (let i = 0; i < this.compra.cuotas.length; i++) {
        const cuota = this.compra.cuotas[i];

        if (!cuota.fechaVencimiento) {
          return this.marcarErrorCampo(
            `fechaCuota${i}`,
            `Ingresa la fecha de vencimiento de la cuota ${cuota.numeroCuota}.`
          );
        }

        if (Number(cuota.montoCuota) <= 0) {
          return this.marcarErrorCampo('cronogramaCuotas', 'El monto de cada cuota debe ser mayor a 0.');
        }
      }

      const saldoFinanciarCentavos = Math.round(this.obtenerSaldoFinanciar() * 100);
      const totalCuotasCentavos = Math.round(this.calcularTotalCuotas() * 100);

      if (saldoFinanciarCentavos !== totalCuotasCentavos) {
        return this.marcarErrorCampo('cronogramaCuotas', 'La suma de las cuotas debe ser igual al saldo pendiente.');
      }
    }

    return null;
  }

  private validarPago(): string | null {
    if (!this.compraPagoSeleccionada) {
      return this.marcarErrorCampo('pago', 'Selecciona una compra.');
    }

    if (!this.pago.metodoPago) {
      return this.marcarErrorCampo('metodoPagoPago', 'Selecciona el método de pago.');
    }

    if (this.esPagoEnCuotas()) {
      if (this.cargandoDetallePago) {
        return this.marcarErrorCampo('cuotasPago', 'Espera a que se carguen las cuotas.');
      }

      if (!this.compraDetallePago) {
        return this.marcarErrorCampo('cuotasPago', 'No se pudo obtener el detalle de cuotas.');
      }

      if (this.cuotasPendientesPago().length === 0) {
        return this.marcarErrorCampo('cuotasPago', 'No hay cuotas pendientes para pagar.');
      }

      if (this.cuotasSeleccionadasPago.length === 0) {
        return this.marcarErrorCampo('cuotasPago', 'Selecciona una o más cuotas pendientes.');
      }

      const montoCuotas = this.calcularMontoCuotasSeleccionadas();

      if (montoCuotas <= 0) {
        return this.marcarErrorCampo('cuotasPago', 'El monto de las cuotas seleccionadas debe ser mayor a 0.');
      }

      if (Math.round(Number(this.pago.montoPagado || 0) * 100) !== Math.round(montoCuotas * 100)) {
        return this.marcarErrorCampo('montoPago', 'El monto debe coincidir con la suma de cuotas seleccionadas.');
      }

      return null;
    }

    const saldoPendiente = Number(this.compraPagoSeleccionada.saldoPendiente || 0);

    if (Number(this.pago.montoPagado || 0) <= 0) {
      return this.marcarErrorCampo('montoPago', 'El monto pagado debe ser mayor a 0.');
    }

    if (Math.round(Number(this.pago.montoPagado || 0) * 100) !== Math.round(saldoPendiente * 100)) {
      return this.marcarErrorCampo('montoPago', 'Para una compra parcial o pendiente debes cancelar el saldo completo.');
    }

    return null;
  }

  private obtenerMensajeErrorCampo(campo: string): string | null {
    switch (campo) {
      case 'proveedor':
        return this.compra.idProveedor <= 0 ? 'Selecciona un proveedor.' : null;

      case 'tipoComprobanteProveedor':
        return !this.compra.tipoComprobanteProveedor ? 'Selecciona el tipo de comprobante recibido.' : null;

      case 'serieComprobante':
        if (!this.compra.serieComprobante) {
          return 'Ingresa la serie del comprobante.';
        }

        if (this.compra.serieComprobante.length < 2) {
          return 'La serie debe tener al menos 2 caracteres.';
        }

        return null;

      case 'numeroComprobante':
        return !this.compra.numeroComprobante ? 'Ingresa el número del comprobante.' : null;

      case 'fechaEmisionComprobante':
        return !this.compra.fechaEmisionComprobante ? 'Ingresa la fecha de emisión del comprobante.' : null;

      case 'numeroGuia':
        return this.compra.guiaRemision.tieneGuia && !this.compra.guiaRemision.numeroGuia
          ? 'Ingresa el número de guía de remisión.'
          : null;

      case 'fechaEmisionGuia':
        return this.compra.guiaRemision.tieneGuia && !this.compra.guiaRemision.fechaEmision
          ? 'Ingresa la fecha de emisión de la guía.'
          : null;

      case 'fechaTrasladoGuia':
        return this.compra.guiaRemision.tieneGuia && !this.compra.guiaRemision.fechaTraslado
          ? 'Ingresa la fecha de traslado de la guía.'
          : null;

      case 'puntoPartida':
        return this.compra.guiaRemision.tieneGuia && !this.compra.guiaRemision.puntoPartida
          ? 'Ingresa el punto de partida.'
          : null;

      case 'puntoLlegada':
        return this.compra.guiaRemision.tieneGuia && !this.compra.guiaRemision.puntoLlegada
          ? 'Ingresa el punto de llegada.'
          : null;

      case 'detalles':
        return this.compra.detalles.length === 0 ? 'Agrega al menos un producto a la compra.' : null;

      case 'tipoPago':
        return !this.compra.tipoPago ? 'Selecciona el tipo de pago.' : null;

      case 'metodoPago':
        return !this.compra.metodoPago ? 'Selecciona el método de pago.' : null;

      case 'montoPagado':
        return this.validarMontoPagadoCampo();

      case 'numeroCuotas':
        if (this.compra.tipoPago !== 'Cuotas') {
          return null;
        }

        if (!this.compra.numeroCuotas || Number(this.compra.numeroCuotas) <= 0) {
          return 'Ingresa un número de cuotas válido.';
        }

        if (!Number.isInteger(Number(this.compra.numeroCuotas))) {
          return 'El número de cuotas debe ser entero.';
        }

        if (Number(this.compra.numeroCuotas) > 24) {
          return 'El número máximo permitido es 24 cuotas.';
        }

        return null;

      case 'fechaPrimerVencimiento':
        return this.compra.tipoPago === 'Cuotas' && !this.compra.fechaPrimerVencimiento
          ? 'Ingresa la fecha del primer vencimiento.'
          : null;

      case 'cronogramaCuotas':
        return this.validarCronogramaCuotasCampo();

      default:
        return null;
    }
  }

  private validarMontoPagadoCampo(): string | null {
    const total = this.calcularTotal();

    if (this.compra.montoPagado === null || this.compra.montoPagado === undefined) {
      return 'Ingresa el monto pagado.';
    }

    if (Number(this.compra.montoPagado) < 0) {
      return 'El monto pagado no puede ser negativo.';
    }

    if (Number(this.compra.montoPagado) > total) {
      return 'El monto pagado no puede ser mayor al total.';
    }

    if (this.compra.tipoPago === 'Total' && Number(this.compra.montoPagado) !== total) {
      return 'Para pago total, el monto pagado debe ser igual al total.';
    }

    if (
      this.compra.tipoPago === 'Parcial' &&
      (Number(this.compra.montoPagado) <= 0 || Number(this.compra.montoPagado) >= total)
    ) {
      return 'Para pago parcial, el monto debe ser mayor a 0 y menor al total.';
    }

    if (this.compra.tipoPago === 'Cuotas' && Number(this.compra.montoPagado) >= total) {
      return 'Para compra en cuotas debe quedar saldo pendiente.';
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

    if (texto.includes('proveedor')) {
      this.erroresCampo['proveedor'] = mensajeBackend;
    } else if (texto.includes('comprobante') || texto.includes('serie') || texto.includes('número') || texto.includes('numero')) {
      this.erroresCampo['numeroComprobante'] = mensajeBackend;
    } else if (texto.includes('guía') || texto.includes('guia')) {
      this.erroresCampo['numeroGuia'] = mensajeBackend;
    } else if (texto.includes('producto') || texto.includes('detalle')) {
      this.erroresCampo['detalles'] = mensajeBackend;
    } else if (texto.includes('pago') || texto.includes('monto')) {
      this.erroresCampo['montoPagado'] = mensajeBackend;
      this.erroresCampo['montoPago'] = mensajeBackend;
    } else if (texto.includes('cuota')) {
      this.erroresCampo['numeroCuotas'] = mensajeBackend;
      this.erroresCampo['cronogramaCuotas'] = mensajeBackend;
      this.erroresCampo['cuotasPago'] = mensajeBackend;
    } else if (texto.includes('anulación') || texto.includes('anulacion')) {
      this.erroresCampo['motivoAnulacion'] = mensajeBackend;
    }
  }

  private normalizarCompra(): void {
    this.compra.idUsuarioRegistro = 1;

    this.compra.tipoComprobanteProveedor = this.compra.tipoComprobanteProveedor.trim();
    this.compra.serieComprobante = this.compra.serieComprobante.trim().toUpperCase();
    this.compra.numeroComprobante = this.compra.numeroComprobante.trim();

    this.compra.observacionCompra = this.compra.observacionCompra?.trim() || null;

    this.compra.metodoPago = this.compra.metodoPago.trim();
    this.compra.tipoPago = this.compra.tipoPago.trim();

    this.compra.montoPagado = Number(this.compra.montoPagado || 0);

    if (this.compra.tipoPago !== 'Cuotas') {
      this.compra.numeroCuotas = null;
      this.compra.fechaPrimerVencimiento = null;
      this.compra.cuotas = [];
    } else {
      this.generarCuotas(false);

      this.compra.cuotas = (this.compra.cuotas ?? []).map(c => ({
        numeroCuota: Number(c.numeroCuota),
        fechaVencimiento: c.fechaVencimiento,
        montoCuota: Number(c.montoCuota || 0)
      }));
    }

    if (this.compra.guiaRemision.tieneGuia) {
      this.compra.guiaRemision.numeroGuia = this.compra.guiaRemision.numeroGuia?.trim() || null;
      this.compra.guiaRemision.puntoPartida = this.compra.guiaRemision.puntoPartida?.trim() || null;
      this.compra.guiaRemision.puntoLlegada = this.compra.guiaRemision.puntoLlegada?.trim() || null;
      this.compra.guiaRemision.transportista = this.compra.guiaRemision.transportista?.trim() || null;
      this.compra.guiaRemision.rucTransportista = this.compra.guiaRemision.rucTransportista?.trim() || null;
      this.compra.guiaRemision.placaVehiculo = this.compra.guiaRemision.placaVehiculo?.trim().toUpperCase() || null;
      this.compra.guiaRemision.observacion = this.compra.guiaRemision.observacion?.trim() || null;
    }

    this.compra.detalles = this.compra.detalles.map(d => ({
      ...d,
      cantidad: Number(d.cantidad),
      precioCompra: Number(d.precioCompra)
    }));
  }

  private ajustarPagoPorTipo(): void {
    if (this.compra.tipoPago === 'Total') {
      this.compra.montoPagado = this.calcularTotal();
      this.compra.cuotas = [];
      return;
    }

    if (this.compra.tipoPago === 'Parcial') {
      this.compra.cuotas = [];
      return;
    }

    if (this.compra.tipoPago === 'Cuotas') {
      if (this.compra.montoPagado < 0) {
        this.compra.montoPagado = 0;
      }

      this.generarCuotas(false);
    }
  }

  private generarCuotas(reiniciarFechas: boolean = false): void {
    if (this.compra.tipoPago !== 'Cuotas') {
      this.compra.cuotas = [];
      return;
    }

    const numeroCuotas = Number(this.compra.numeroCuotas || 0);

    if (!Number.isInteger(numeroCuotas) || numeroCuotas <= 0 || numeroCuotas > 24) {
      this.compra.cuotas = [];
      return;
    }

    const saldoFinanciar = this.obtenerSaldoFinanciar();

    if (saldoFinanciar <= 0) {
      this.compra.cuotas = [];
      return;
    }

    const saldoCentavos = Math.round(saldoFinanciar * 100);
    const baseCentavos = Math.floor(saldoCentavos / numeroCuotas);
    const diferenciaCentavos = saldoCentavos - (baseCentavos * numeroCuotas);

    const cuotasActuales = this.compra.cuotas ?? [];
    const cuotasNuevas: CuotaCompraCrear[] = [];

    for (let i = 1; i <= numeroCuotas; i++) {
      const cuotaActual = cuotasActuales.find(c => Number(c.numeroCuota) === i);

      const montoCentavos = i === numeroCuotas
        ? baseCentavos + diferenciaCentavos
        : baseCentavos;

      const fechaVencimiento = reiniciarFechas
        ? this.obtenerFechaCuotaDesdePrimerVencimiento(i)
        : cuotaActual?.fechaVencimiento || this.obtenerFechaCuotaDesdePrimerVencimiento(i);

      cuotasNuevas.push({
        numeroCuota: i,
        fechaVencimiento,
        montoCuota: Number((montoCentavos / 100).toFixed(2))
      });
    }

    this.compra.cuotas = cuotasNuevas;
  }

  private obtenerFechaCuotaDesdePrimerVencimiento(numeroCuota: number): string | null {
    if (!this.compra.fechaPrimerVencimiento) {
      return null;
    }

    return this.sumarMesesAFecha(this.compra.fechaPrimerVencimiento, numeroCuota - 1);
  }

  private sumarMesesAFecha(fechaBase: string, meses: number): string {
    const partes = fechaBase.split('-').map(valor => Number(valor));

    if (partes.length !== 3 || partes.some(valor => Number.isNaN(valor))) {
      return fechaBase;
    }

    const anio = partes[0];
    const mes = partes[1];
    const dia = partes[2];

    const fechaPrimerDia = new Date(anio, mes - 1 + meses, 1);
    const ultimoDiaMesDestino = new Date(
      fechaPrimerDia.getFullYear(),
      fechaPrimerDia.getMonth() + 1,
      0
    ).getDate();

    fechaPrimerDia.setDate(Math.min(dia, ultimoDiaMesDestino));

    return this.formatearFecha(fechaPrimerDia);
  }

  private formatearFecha(fecha: Date): string {
    const anio = fecha.getFullYear();
    const mes = String(fecha.getMonth() + 1).padStart(2, '0');
    const dia = String(fecha.getDate()).padStart(2, '0');

    return `${anio}-${mes}-${dia}`;
  }

  private validarCronogramaCuotasCampo(): string | null {
    if (this.compra.tipoPago !== 'Cuotas') {
      return null;
    }

    const numeroCuotas = Number(this.compra.numeroCuotas || 0);

    if (!Number.isInteger(numeroCuotas) || numeroCuotas <= 0 || numeroCuotas > 24) {
      return null;
    }

    if (!this.compra.cuotas || this.compra.cuotas.length !== numeroCuotas) {
      return 'El cronograma de cuotas no coincide con el número de cuotas.';
    }

    if (this.compra.cuotas.some(c => !c.fechaVencimiento)) {
      return 'Todas las cuotas deben tener fecha de vencimiento.';
    }

    const saldoFinanciarCentavos = Math.round(this.obtenerSaldoFinanciar() * 100);
    const totalCuotasCentavos = Math.round(this.calcularTotalCuotas() * 100);

    if (saldoFinanciarCentavos !== totalCuotasCentavos) {
      return 'La suma de las cuotas debe ser igual al saldo pendiente.';
    }

    return null;
  }

  private nuevaCompra(): CompraCrear {
    return {
      idProveedor: 0,
      idUsuarioRegistro: 1,

      tipoComprobanteProveedor: '',
      serieComprobante: '',
      numeroComprobante: '',
      fechaEmisionComprobante: this.obtenerFechaActual(),

      observacionCompra: '',

      guiaRemision: {
        tieneGuia: false,
        numeroGuia: null,
        fechaEmision: null,
        fechaTraslado: null,
        puntoPartida: null,
        puntoLlegada: null,
        transportista: null,
        rucTransportista: null,
        placaVehiculo: null,
        observacion: null
      },

      tipoPago: 'Total',
      metodoPago: '',
      montoPagado: 0,

      numeroCuotas: null,
      fechaPrimerVencimiento: null,

      observacionPago: '',

      detalles: [],
      cuotas: []
    };
  }

  private nuevoPago(): PagoCompraCrear {
    return {
      idCompra: 0,
      idUsuarioRegistro: 1,
      metodoPago: '',
      montoPagado: 0,
      observacion: '',
      idsCuotasPagadas: []
    };
  }

  private nuevaAnulacion(): AnularCompra {
    return {
      idUsuarioRegistro: 1,
      motivo: ''
    };
  }

  private obtenerFechaActual(): string {
    return new Date().toISOString().slice(0, 10);
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
