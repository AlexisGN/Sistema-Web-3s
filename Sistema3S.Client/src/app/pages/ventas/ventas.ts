import { UiIconComponent } from '../../shared/ui-icon/ui-icon';
import { SessionService } from '../../core/services/session.service';
import { CommonModule } from '@angular/common';
import { inject, Component, ChangeDetectorRef, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';

import { VentaService } from '../../core/services/venta';
import {
  AnularVenta,
  ClienteVenta,
  ElementoVentaDisponible,
  PagoVentaCrear,
  CuotaVentaCrear,
  CuotaVentaDetalle,
  VentaCrear,
  VentaDetalleCompleto,
  VentaDetalleCrear,
  VentaListado
} from '../../core/models/venta.model';

@Component({
  selector: 'app-ventas',
  standalone: true,
  imports: [UiIconComponent, CommonModule, FormsModule],
  templateUrl: './ventas.html',
  styleUrl: './ventas.scss'
})
export class VentasComponent implements OnInit {
  readonly permisos = inject(SessionService);
  private readonly sesionAnulacion = inject(SessionService);
  ventas: VentaListado[] = [];
  clientes: ClienteVenta[] = [];
  productos: ElementoVentaDisponible[] = [];
  servicios: ElementoVentaDisponible[] = [];

  venta: VentaCrear = this.nuevaVenta();

  clienteSeleccionado: ClienteVenta | null = null;
  elementoSeleccionado: ElementoVentaDisponible | null = null;

  tipoElementoFormulario: 'Producto' | 'Servicio' = 'Producto';

  detalleFormulario = {
    cantidad: null as number | null,
    precioUnitario: null as number | null
  };

  buscarVenta = '';
  estadoPagoFiltro = '';
  tipoComprobanteFiltro = '';
  origenVentaFiltro = '';
  fechaInicioFiltro = '';
  fechaFinFiltro = '';

  buscarCliente = '';
  buscarProducto = '';
  buscarServicio = '';

  pagina = 1;
  tamanioPagina = 8;
  totalRegistros = 0;
  totalPaginas = 0;
  opcionesTamanioPagina = [8, 12, 20];

  cargando = false;
  guardando = false;
  buscandoClientes = false;
  buscandoProductos = false;
  buscandoServicios = false;

  generandoPdfVentaId: number | null = null;
  generandoReportePdf = false;
  generandoReporteExcel = false;
  cargandoSiguienteComprobante = false;

  cargandoCotizacion = false;
  idCotizacionOrigen: number | null = null;
  cotizacionOrigenTexto = '';

  mensaje = '';
  error = '';
  erroresCampo: Record<string, string> = {};

  ventaPagoSeleccionada: VentaListado | null = null;
  ventaDetallePago: VentaDetalleCompleto | null = null;
  cargandoDetallePago = false;
  cuotasSeleccionadasCobro: number[] = [];
  pago: PagoVentaCrear = this.nuevoPago();

  ventaAnularSeleccionada: VentaListado | null = null;
  anularDto: AnularVenta = this.nuevaAnulacion();

  readonly tiposComprobante = [
    'Factura',
    'Boleta',
    'Nota de venta'
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

  readonly origenesVenta = [
    'Directa',
    'Cotización'
  ];

  constructor(
    private ventaService: VentaService,
    private route: ActivatedRoute,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.subirArriba();
    this.cargarVentas();
    this.cargarClientes();
    this.cargarProductos();
    this.cargarServicios();

    this.route.queryParamMap.subscribe(params => {
      const idCotizacion = Number(
        params.get('idCotizacion') ??
        params.get('cotizacion') ??
        0
      );

      const cargadoDesdeSessionStorage = this.cargarCotizacionDesdeSessionStorage(idCotizacion);

      if (!cargadoDesdeSessionStorage && idCotizacion > 0) {
        this.cargarCotizacionParaVenta(idCotizacion);
      }
    });
  }

  cargarVentas(): void {
    this.cargando = true;
    this.error = '';

    this.ventaService
      .listar(
        this.buscarVenta,
        this.estadoPagoFiltro,
        this.tipoComprobanteFiltro,
        this.origenVentaFiltro,
        this.fechaInicioFiltro,
        this.fechaFinFiltro,
        this.pagina,
        this.tamanioPagina
      )
      .subscribe({
        next: (data) => {
          this.ventas = data.items ?? [];
          this.pagina = data.pagina;
          this.tamanioPagina = data.tamanioPagina;
          this.totalRegistros = data.totalRegistros;
          this.totalPaginas = data.totalPaginas ?? Math.max(1, Math.ceil(this.totalRegistros / this.tamanioPagina));
          this.cargando = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error(err);
          this.error = 'No se pudieron cargar las ventas.';
          this.cargando = false;
          this.cdr.detectChanges();
        }
      });
  }

  cargarClientes(): void {
    if (!(this.permisos.tienePermiso('VENTAS_CREAR'))) return;
    this.buscandoClientes = true;

    this.ventaService.listarClientes(this.buscarCliente, 1, 10).subscribe({
      next: (data) => {
        this.clientes = data.items ?? [];
        this.buscandoClientes = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.clientes = [];
        this.buscandoClientes = false;
        this.cdr.detectChanges();
      }
    });
  }

  cargarProductos(): void {
    if (!(this.permisos.tienePermiso('VENTAS_CREAR'))) return;
    this.buscandoProductos = true;

    this.ventaService.listarProductos(this.buscarProducto, 1, 10).subscribe({
      next: (data) => {
        this.productos = (data.items ?? []).map(item =>
          this.normalizarElementoDisponible(item, 'Producto')
        );

        this.buscandoProductos = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.productos = [];
        this.buscandoProductos = false;
        this.cdr.detectChanges();
      }
    });
  }

  cargarServicios(): void {
    if (!(this.permisos.tienePermiso('VENTAS_CREAR'))) return;
    this.buscandoServicios = true;

    this.ventaService.listarServicios(this.buscarServicio, 1, 10).subscribe({
      next: (data) => {
        this.servicios = (data.items ?? []).map(item =>
          this.normalizarElementoDisponible(item, 'Servicio')
        );

        this.buscandoServicios = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.servicios = [];
        this.buscandoServicios = false;
        this.cdr.detectChanges();
      }
    });
  }

  cargarCotizacionParaVenta(idCotizacion: number): void {
    if (!(this.permisos.tienePermiso('COTIZACIONES_CONVERTIR_VENTA') && this.permisos.tienePermiso('VENTAS_CREAR'))) return;
    this.cargandoCotizacion = true;
    this.error = '';
    this.mensaje = '';

    this.ventaService.obtenerCotizacionParaVenta(idCotizacion).subscribe({
      next: (data) => {
        this.aplicarCotizacionEnFormularioVenta(data, idCotizacion);
      },
      error: (err) => {
        console.error(err);
        this.cargandoCotizacion = false;
        this.error = 'No se pudo cargar la cotización para convertirla en venta.';
        this.cdr.detectChanges();
      }
    });
  }
  private cargarCotizacionDesdeSessionStorage(idCotizacionUrl: number = 0): boolean {
    const raw = sessionStorage.getItem('cotizacionParaVenta');

    if (!raw) {
      return false;
    }

    try {
      const data = JSON.parse(raw);

      if (!data || !data.idCotizacion) {
        return false;
      }

      if (idCotizacionUrl > 0 && Number(data.idCotizacion) !== idCotizacionUrl) {
        return false;
      }

      this.aplicarCotizacionEnFormularioVenta(data, Number(data.idCotizacion));

      sessionStorage.removeItem('cotizacionParaVenta');

      return true;
    } catch (error) {
      console.error('Error leyendo cotización desde sessionStorage:', error);
      sessionStorage.removeItem('cotizacionParaVenta');
      return false;
    }
  }

  private aplicarCotizacionEnFormularioVenta(data: any, idCotizacionFallback: number): void {
    this.limpiarFormulario(false);

    const idCotizacion = Number(
      data.idCotizacion ??
      data.cotizacion?.idCotizacion ??
      idCotizacionFallback ??
      0
    );

    this.idCotizacionOrigen = idCotizacion;
    this.venta.idCotizacion = idCotizacion;

    const codigoCotizacion =
      data.codigoCotizacion ||
      data.cotizacion?.codigoCotizacion ||
      `COT-${String(idCotizacion).padStart(5, '0')}`;

    const idCliente = Number(
      data.idCliente ??
      data.cliente?.idCliente ??
      data.cotizacion?.idCliente ??
      0
    );

    if (idCliente > 0) {
      this.clienteSeleccionado = {
        idCliente,
        numeroDocumento:
          data.numeroDocumentoCliente ??
          data.documentoCliente ??
          data.cliente?.numeroDocumento ??
          '',
        tipoDocumento:
          data.tipoDocumentoCliente ??
          data.cliente?.tipoDocumento ??
          '',
        cliente:
          data.clienteNombre ??
          data.nombreCliente ??
          data.cliente ??
          data.cliente?.cliente ??
          data.cliente?.nombreCompleto ??
          data.cliente?.razonSocial ??
          '',
        razonSocial: data.cliente?.razonSocial ?? null,
        nombres: data.cliente?.nombres ?? null,
        apellidoPaterno: data.cliente?.apellidoPaterno ?? null,
        apellidoMaterno: data.cliente?.apellidoMaterno ?? null,
        correo: data.cliente?.correo ?? data.correoCliente ?? null,
        telefono: data.cliente?.telefono ?? data.telefonoCliente ?? null,
        direccion: data.cliente?.direccion ?? data.direccionCliente ?? null
      };

      this.venta.idCliente = idCliente;
      this.buscarCliente = this.obtenerNombreCliente(this.clienteSeleccionado);
    }

    const detallesOrigen =
      data.detalles ??
      data.detalleCotizaciones ??
      data.items ??
      data.cotizacion?.detalles ??
      [];

    this.venta.detalles = detallesOrigen
      .map((detalle: any) => this.mapearDetalleCotizacionAVenta(detalle))
      .filter((detalle: VentaDetalleCrear) => detalle.idElementoCatalogo > 0);

    this.cotizacionOrigenTexto = `Venta generada desde ${codigoCotizacion}`;

    this.venta.observacion = `${this.cotizacionOrigenTexto}. ${data.observacion || ''}`.trim();

    this.seleccionarComprobanteInicialDesdeCotizacion();

    this.ajustarPagoPorTipo();

    delete this.erroresCampo['cliente'];
    delete this.erroresCampo['detalles'];
    delete this.erroresCampo['cotizacion'];

    this.mensaje = `${codigoCotizacion} cargada en ventas internas. Puedes editar cantidades y precios antes de registrar la venta.`;

    this.cargandoCotizacion = false;
    this.cdr.detectChanges();
  }

  private mapearDetalleCotizacionAVenta(detalle: any): VentaDetalleCrear {
    const idElementoCatalogo = this.extraerIdElementoCatalogo(detalle);
    const cantidad = Number(detalle.cantidad ?? detalle.Cantidad ?? 1);
    const precioUnitario = this.extraerPrecioCotizacion(detalle);
    const stockActual = this.extraerStockElemento(detalle);

    return {
      idElementoCatalogo,
      cantidad,
      precioUnitario,
      codigo:
        detalle.codigoProducto ??
        detalle.CodigoProducto ??
        detalle.codigoServicio ??
        detalle.CodigoServicio ??
        detalle.codigo ??
        detalle.Codigo ??
        null,
      elemento:
        detalle.elementoNombre ??
        detalle.ElementoNombre ??
        detalle.elemento ??
        detalle.Elemento ??
        detalle.producto ??
        detalle.Producto ??
        detalle.servicio ??
        detalle.Servicio ??
        detalle.nombre ??
        detalle.Nombre ??
        detalle.descripcion ??
        detalle.Descripcion ??
        'Elemento cotizado',
      tipoElemento:
        detalle.tipoElemento ??
        detalle.TipoElemento ??
        (detalle.idServicio || detalle.IdServicio ? 'Servicio' : 'Producto'),
      stockActual,
      aplicaInventario:
        detalle.aplicaInventario ??
        detalle.AplicaInventario ??
        null
    } as VentaDetalleCrear;
  }

  private extraerPrecioCotizacion(item: any): number {
    const camposPrecioCotizacion = [
      'precioUnitario',
      'PrecioUnitario',
      'precioCotizado',
      'PrecioCotizado',
      'precioVentaCotizado',
      'PrecioVentaCotizado',
      'precioFinal',
      'PrecioFinal',
      'precio',
      'Precio'
    ];

    const precioDirecto = this.leerNumeroDesdeObjeto(item, camposPrecioCotizacion) ?? 0;

    if (precioDirecto > 0) {
      return Number(precioDirecto.toFixed(2));
    }

    const cantidad = Number(item?.cantidad ?? item?.Cantidad ?? 0);
    const subtotal = Number(item?.subtotal ?? item?.Subtotal ?? 0);

    if (cantidad > 0 && subtotal > 0) {
      return Number((subtotal / cantidad).toFixed(2));
    }

    return Number(this.extraerPrecioElemento(item).toFixed(2));
  }

  private seleccionarComprobanteInicialDesdeCotizacion(): void {
    if (this.clienteEsEmpresaConRuc()) {
      this.venta.tipoComprobante = 'Factura';
    } else {
      this.venta.tipoComprobante = 'Boleta';
    }

    if (!this.comprobantePermitidoParaCliente(this.venta.tipoComprobante)) {
      this.venta.tipoComprobante = 'Nota de venta';
    }

    this.cambiarTipoComprobante();
  }
  seleccionarCliente(cliente: ClienteVenta): void {
    this.clienteSeleccionado = cliente;
    this.venta.idCliente = cliente.idCliente;
    this.buscarCliente = this.obtenerNombreCliente(cliente);

    this.normalizarTipoComprobantePorCliente();

    this.limpiarErrorCampo('cliente');
    this.limpiarErrorCampo('tipoComprobante');
    this.cdr.detectChanges();
  }

  seleccionarTipoElemento(tipo: 'Producto' | 'Servicio'): void {
    this.tipoElementoFormulario = tipo;
    this.elementoSeleccionado = null;

    this.detalleFormulario = {
      cantidad: null,
      precioUnitario: null
    };

    delete this.erroresCampo['elementoDetalle'];
    delete this.erroresCampo['cantidadDetalle'];
    delete this.erroresCampo['precioDetalle'];
  }

  seleccionarElemento(elemento: ElementoVentaDisponible): void {
    const elementoNormalizado = this.normalizarElementoDisponible(
      elemento,
      this.tipoElementoFormulario
    );

    const precio = this.obtenerPrecioElemento(elementoNormalizado);

    this.elementoSeleccionado = elementoNormalizado;
    this.detalleFormulario.precioUnitario = precio > 0 ? precio : null;

    if (this.tipoElementoFormulario === 'Producto') {
      this.buscarProducto = this.obtenerNombreElemento(elementoNormalizado);
    } else {
      this.buscarServicio = this.obtenerNombreElemento(elementoNormalizado);
    }

    this.limpiarErrorCampo('elementoDetalle');
    this.limpiarErrorCampo('precioDetalle');

    this.cdr.detectChanges();
  }

  agregarDetalle(): void {
    const errorDetalle = this.validarDetalleFormulario();

    if (errorDetalle) {
      this.cdr.detectChanges();
      return;
    }

    const elemento = this.elementoSeleccionado!;
    const idElementoCatalogo = this.obtenerIdElementoCatalogo(elemento);

    if (this.venta.detalles.some(d => d.idElementoCatalogo === idElementoCatalogo)) {
      this.erroresCampo['elementoDetalle'] = 'Este elemento ya fue agregado a la venta.';
      this.cdr.detectChanges();
      return;
    }

    const precio = Number(this.detalleFormulario.precioUnitario ?? this.obtenerPrecioElemento(elemento));
    const stockActual = this.obtenerStockElemento(elemento);

    const detalle: VentaDetalleCrear = {
      idElementoCatalogo,
      cantidad: Number(this.detalleFormulario.cantidad),
      precioUnitario: precio,
      codigo: this.obtenerCodigoElemento(elemento),
      elemento: this.obtenerNombreElemento(elemento),
      tipoElemento: this.tipoElementoFormulario,
      stockActual,
      aplicaInventario: this.tipoElementoFormulario === 'Producto'
        ? Boolean((elemento as any).aplicaInventario ?? (elemento as any).AplicaInventario ?? true)
        : false
    };

    this.venta.detalles.push(detalle);

    this.elementoSeleccionado = null;
    this.buscarProducto = '';
    this.buscarServicio = '';

    this.detalleFormulario = {
      cantidad: null,
      precioUnitario: null
    };

    delete this.erroresCampo['detalles'];
    delete this.erroresCampo['elementoDetalle'];
    delete this.erroresCampo['cantidadDetalle'];
    delete this.erroresCampo['precioDetalle'];
    delete this.erroresCampo['cronogramaCuotas'];

    this.ajustarPagoPorTipo();
    this.cdr.detectChanges();
  }

  eliminarDetalle(index: number): void {
    this.venta.detalles.splice(index, 1);
    delete this.erroresCampo['cronogramaCuotas'];
    this.ajustarPagoPorTipo();
    this.cdr.detectChanges();
  }
  cambiarCantidadDetalleVenta(index: number): void {
    const detalle = this.venta.detalles[index];

    if (!detalle) {
      return;
    }

    detalle.cantidad = Number(detalle.cantidad || 0);

    if (detalle.cantidad <= 0) {
      this.erroresCampo['detalles'] = 'La cantidad debe ser mayor a 0.';
    } else {
      delete this.erroresCampo['detalles'];
    }

    delete this.erroresCampo['cronogramaCuotas'];

    this.ajustarPagoPorTipo();
    this.cdr.detectChanges();
  }

  cambiarPrecioDetalleVenta(index: number): void {
    const detalle = this.venta.detalles[index];

    if (!detalle) {
      return;
    }

    detalle.precioUnitario = Number(detalle.precioUnitario || 0);

    if (detalle.precioUnitario <= 0) {
      this.erroresCampo['detalles'] = 'El precio de venta debe ser mayor a 0.';
    } else {
      delete this.erroresCampo['detalles'];
    }

    delete this.erroresCampo['cronogramaCuotas'];

    this.ajustarPagoPorTipo();
    this.cdr.detectChanges();
  }
  guardarVenta(): void {
    if (!(this.permisos.tienePermiso('VENTAS_CREAR'))) return;
    this.mensaje = '';
    this.error = '';
    this.erroresCampo = {};

    this.normalizarVenta();

    const errorValidacion = this.validarVenta();

    if (errorValidacion) {
      this.cdr.detectChanges();
      return;
    }

    this.guardando = true;

    this.ventaService.registrar(this.venta).subscribe({
      next: (data) => {
        this.mensaje = data.mensaje || 'Venta registrada correctamente.';
        this.guardando = false;

        this.cargarVentas();
        this.cargarProductos();
        this.limpiarFormulario(false);

        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.asignarErrorBackend(err, 'No se pudo registrar la venta.');
        this.guardando = false;
        this.cdr.detectChanges();
      }
    });
  }

  verPdfVenta(item: VentaListado): void {
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
        <head><title>Generando PDF</title></head>
        <body style="font-family: Arial; padding: 30px;">
          <h2>Generando PDF de venta...</h2>
          <p>Espera un momento.</p>
        </body>
      </html>
    `);

    ventanaPdf.document.close();

    this.generandoPdfVentaId = item.idVenta;

    this.ventaService.obtenerPdf(item.idVenta).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        ventanaPdf.location.href = url;

        setTimeout(() => {
          window.URL.revokeObjectURL(url);
        }, 60000);

        this.generandoPdfVentaId = null;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        ventanaPdf.close();

        this.error = err.error?.mensaje || 'No se pudo generar el PDF de la venta.';
        this.generandoPdfVentaId = null;
        this.cdr.detectChanges();
      }
    });
  }

  exportarReportePdf(): void {
    if (!(this.permisos.tienePermiso('VENTAS_REPORTE'))) return;
    this.error = '';
    this.mensaje = '';
    this.generandoReportePdf = true;

    const ventanaPdf = window.open('', '_blank');

    if (!ventanaPdf) {
      this.generandoReportePdf = false;
      this.error = 'El navegador bloqueó la ventana del reporte PDF.';
      this.cdr.detectChanges();
      return;
    }

    ventanaPdf.document.write(`
      <html>
        <head><title>Generando reporte</title></head>
        <body style="font-family: Arial; padding: 30px;">
          <h2>Generando reporte PDF de ventas...</h2>
          <p>Espera un momento.</p>
        </body>
      </html>
    `);

    ventanaPdf.document.close();

    this.ventaService
      .obtenerReportePdf(
        this.buscarVenta,
        this.estadoPagoFiltro,
        this.tipoComprobanteFiltro,
        this.origenVentaFiltro,
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

          this.error = 'No se pudo generar el reporte PDF de ventas.';
          this.generandoReportePdf = false;
          this.cdr.detectChanges();
        }
      });
  }

  exportarReporteExcel(): void {
    if (!(this.permisos.tienePermiso('VENTAS_REPORTE'))) return;
    this.error = '';
    this.mensaje = '';
    this.generandoReporteExcel = true;

    this.ventaService
      .obtenerReporteExcel(
        this.buscarVenta,
        this.estadoPagoFiltro,
        this.tipoComprobanteFiltro,
        this.origenVentaFiltro,
        this.fechaInicioFiltro,
        this.fechaFinFiltro
      )
      .subscribe({
        next: (blob) => {
          const url = window.URL.createObjectURL(blob);
          const link = document.createElement('a');

          link.href = url;
          link.download = `reporte-ventas-${new Date().toISOString().slice(0, 10)}.xlsx`;
          link.click();

          window.URL.revokeObjectURL(url);

          this.generandoReporteExcel = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error(err);
          this.error = 'No se pudo generar el reporte Excel de ventas.';
          this.generandoReporteExcel = false;
          this.cdr.detectChanges();
        }
      });
  }

  abrirPago(venta: VentaListado): void {
    if (!(this.permisos.tienePermiso('VENTAS_COBRAR'))) return;
    this.ventaPagoSeleccionada = venta;
    this.ventaDetallePago = null;
    this.cargandoDetallePago = false;
    this.cuotasSeleccionadasCobro = [];

    this.pago = {
      idVenta: venta.idVenta,
      idUsuarioRegistro: 1,
      metodoPago: '',
      montoPagado: this.esVentaEnCuotas(venta) ? 0 : Number(venta.saldoPendiente || 0),
      observacion: '',
      idsCuotasCobradas: []
    };

    this.mensaje = '';
    this.error = '';
    this.erroresCampo = {};

    if (this.esVentaEnCuotas(venta)) {
      this.cargarDetallePago(venta.idVenta);
    }

    this.cdr.detectChanges();
  }

  private cargarDetallePago(idVenta: number): void {
    this.cargandoDetallePago = true;

    this.ventaService.obtenerDetalle(idVenta).subscribe({
      next: (detalle) => {
        this.ventaDetallePago = detalle;
        this.cargandoDetallePago = false;

        if (this.cuotasPendientesCobro().length === 0) {
          this.erroresCampo['cuotasCobro'] = 'No hay cuotas pendientes para esta venta.';
        }

        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.cargandoDetallePago = false;
        this.ventaDetallePago = null;
        this.erroresCampo['cuotasCobro'] = 'No se pudieron cargar las cuotas de la venta.';
        this.cdr.detectChanges();
      }
    });
  }

  cerrarPago(): void {
    this.ventaPagoSeleccionada = null;
    this.ventaDetallePago = null;
    this.cargandoDetallePago = false;
    this.cuotasSeleccionadasCobro = [];
    this.pago = this.nuevoPago();
    this.erroresCampo = {};
  }

  registrarPago(): void {
    if (!(this.permisos.tienePermiso('VENTAS_COBRAR'))) return;
    this.mensaje = '';
    this.error = '';
    this.erroresCampo = {};

    this.prepararCobroAntesDeEnviar();

    const errorValidacion = this.validarPago();

    if (errorValidacion) {
      this.cdr.detectChanges();
      return;
    }

    this.guardando = true;

    this.ventaService.registrarPago(this.pago).subscribe({
      next: (data) => {
        this.mensaje = data.mensaje || 'Cobro registrado correctamente.';
        this.guardando = false;

        this.cerrarPago();
        this.cargarVentas();

        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.asignarErrorBackend(err, 'No se pudo registrar el cobro.');
        this.guardando = false;
        this.cdr.detectChanges();
      }
    });
  }

  private prepararCobroAntesDeEnviar(): void {
    if (!this.ventaPagoSeleccionada) {
      return;
    }

    this.pago.idVenta = this.ventaPagoSeleccionada.idVenta;
    this.pago.idUsuarioRegistro = 1;
    this.pago.metodoPago = this.pago.metodoPago?.trim() || '';
    this.pago.observacion = this.pago.observacion?.trim() || null;

    if (this.esCobroEnCuotas()) {
      this.pago.idsCuotasCobradas = [...this.cuotasSeleccionadasCobro];
      this.recalcularMontoCobroCuotas();
      return;
    }

    this.pago.idsCuotasCobradas = [];
    this.pago.montoPagado = Number(this.ventaPagoSeleccionada.saldoPendiente || 0);
  }

  abrirAnular(venta: VentaListado): void {
    if (!this.puedeAnular(venta)) return;
    this.ventaAnularSeleccionada = venta;
    this.anularDto = this.nuevaAnulacion();
    this.mensaje = '';
    this.error = '';
    this.erroresCampo = {};
    this.cdr.detectChanges();
  }

  cerrarAnular(): void {
    this.ventaAnularSeleccionada = null;
    this.anularDto = this.nuevaAnulacion();
    this.erroresCampo = {};
  }

  anularVenta(): void {
    if (!(this.permisos.tienePermiso('VENTAS_ANULAR'))) return;
    if (!this.sesionAnulacion.esAdministrador()) return;
    this.mensaje = '';
    this.error = '';
    this.erroresCampo = {};

    if (!this.ventaAnularSeleccionada) {
      this.erroresCampo['anular'] = 'Selecciona una venta para anular.';
      this.cdr.detectChanges();
      return;
    }

    if (!this.anularDto.motivo || this.anularDto.motivo.trim().length < 5) {
      this.erroresCampo['motivoAnulacion'] = 'Ingresa un motivo de anulación válido.';
      this.cdr.detectChanges();
      return;
    }

    this.guardando = true;

    this.ventaService
      .anular(this.ventaAnularSeleccionada.idVenta, this.anularDto)
      .subscribe({
        next: (data) => {
          this.mensaje = data.mensaje || 'Venta anulada correctamente.';
          this.guardando = false;

          this.cerrarAnular();
          this.cargarVentas();
          this.cargarProductos();

          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error(err);
          this.asignarErrorBackend(err, 'No se pudo anular la venta.');
          this.guardando = false;
          this.cdr.detectChanges();
        }
      });
  }

  buscarVentas(): void {
    this.pagina = 1;
    this.cargarVentas();
  }

  limpiarFiltros(): void {
    this.buscarVenta = '';
    this.estadoPagoFiltro = '';
    this.tipoComprobanteFiltro = '';
    this.origenVentaFiltro = '';
    this.fechaInicioFiltro = '';
    this.fechaFinFiltro = '';
    this.pagina = 1;
    this.cargarVentas();
  }

  cambiarTipoComprobante(): void {
    this.limpiarErrorCampo('tipoComprobante');

    this.venta.serie = '';
    this.venta.numero = '';

    if (!this.venta.tipoComprobante) {
      return;
    }

    if (!this.comprobantePermitidoParaCliente(this.venta.tipoComprobante)) {
      this.erroresCampo['tipoComprobante'] = 'Para persona natural con DNI solo se permite Boleta o Nota de venta.';
      this.venta.tipoComprobante = '';
      this.venta.serie = '';
      this.venta.numero = '';
      this.cdr.detectChanges();
      return;
    }

    // Muestra la serie al instante sin esperar al backend.
    this.venta.serie = this.obtenerSeriePorTipoComprobante(this.venta.tipoComprobante);
    this.venta.numero = 'Generando...';

    this.cargandoSiguienteComprobante = true;
    this.cdr.detectChanges();

    this.ventaService.obtenerSiguienteComprobante(this.venta.tipoComprobante).subscribe({
      next: (data) => {
        this.venta.serie = data.serie || this.obtenerSeriePorTipoComprobante(this.venta.tipoComprobante);
        this.venta.numero = data.numero || '';

        this.cargandoSiguienteComprobante = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);

        this.venta.numero = '';
        this.error = err.error?.mensaje || 'No se pudo obtener el siguiente número de comprobante.';

        this.cargandoSiguienteComprobante = false;
        this.cdr.detectChanges();
      }
    });
  }
  private obtenerSeriePorTipoComprobante(tipoComprobante: string): string {
    const tipo = (tipoComprobante || '').trim().toLowerCase();

    if (tipo === 'factura') {
      return 'F001';
    }

    if (tipo === 'boleta') {
      return 'B001';
    }

    if (tipo === 'nota de venta') {
      return 'NV01';
    }

    return '';
  }
  cambiarTipoPago(): void {
    this.venta.numeroCuotas = null;
    this.venta.fechaPrimerVencimiento = null;
    this.venta.cuotas = [];

    this.ajustarPagoPorTipo();
    this.limpiarErrorCampo('tipoPago');
    this.limpiarErrorCampo('montoPagado');

    delete this.erroresCampo['numeroCuotas'];
    delete this.erroresCampo['fechaPrimerVencimiento'];
    delete this.erroresCampo['cronogramaCuotas'];

    this.cdr.detectChanges();
  }

  cambiarMontoPagado(): void {
    this.limpiarErrorCampo('montoPagado');
    this.ajustarPagoPorTipo();
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

  paginaAnterior(): void {
    if (this.pagina <= 1) {
      return;
    }

    this.pagina--;
    this.cargarVentas();
  }

  paginaSiguiente(): void {
    if (this.pagina >= this.totalPaginas) {
      return;
    }

    this.pagina++;
    this.cargarVentas();
  }

  cambiarTamanioPagina(): void {
    this.pagina = 1;
    this.cargarVentas();
  }

  limpiarFormulario(limpiarMensaje: boolean = true): void {
    this.venta = this.nuevaVenta();

    this.clienteSeleccionado = null;
    this.elementoSeleccionado = null;
    this.idCotizacionOrigen = null;
    this.cotizacionOrigenTexto = '';

    this.buscarCliente = '';
    this.buscarProducto = '';
    this.buscarServicio = '';

    this.tipoElementoFormulario = 'Producto';

    this.detalleFormulario = {
      cantidad: null,
      precioUnitario: null
    };

    this.erroresCampo = {};

    if (limpiarMensaje) {
      this.mensaje = '';
      this.error = '';
    }

    this.cdr.detectChanges();
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

  calcularSubtotalDetalle(detalle: VentaDetalleCrear): number {
    return Number(detalle.cantidad || 0) * Number(detalle.precioUnitario || 0);
  }

  calcularTotal(): number {
    const total = this.venta.detalles.reduce((acumulado, detalle) => {
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
    const saldo = this.calcularTotal() - Number(this.venta.montoPagado || 0);
    return Number(saldo.toFixed(2));
  }

  calcularTotalCuotas(): number {
    const total = (this.venta.cuotas ?? []).reduce((acumulado, cuota) => {
      return acumulado + Number(cuota.montoCuota || 0);
    }, 0);

    return Number(total.toFixed(2));
  }

  obtenerSaldoFinanciar(): number {
    const saldo = this.calcularSaldo();
    return saldo > 0 ? Number(saldo.toFixed(2)) : 0;
  }

  tiposComprobanteDisponibles(): string[] {
    if (this.clienteEsPersonaNaturalDni()) {
      return ['Boleta', 'Nota de venta'];
    }

    return [...this.tiposComprobante];
  }

  seleccionarCuotaCobro(cuota: CuotaVentaDetalle): void {
    const idCuota = Number(cuota.idCuotaVenta);

    if (idCuota <= 0) {
      return;
    }

    if (this.estaCuotaCobroSeleccionada(idCuota)) {
      this.cuotasSeleccionadasCobro = this.cuotasSeleccionadasCobro.filter(id => id !== idCuota);
    } else {
      this.cuotasSeleccionadasCobro.push(idCuota);
    }

    delete this.erroresCampo['cuotasCobro'];
    delete this.erroresCampo['montoPago'];

    this.recalcularMontoCobroCuotas();
    this.cdr.detectChanges();
  }

  seleccionarTodasCuotasCobro(): void {
    this.cuotasSeleccionadasCobro = this.cuotasPendientesCobro().map(c => c.idCuotaVenta);
    delete this.erroresCampo['cuotasCobro'];
    delete this.erroresCampo['montoPago'];
    this.recalcularMontoCobroCuotas();
    this.cdr.detectChanges();
  }

  limpiarCuotasCobro(): void {
    this.cuotasSeleccionadasCobro = [];
    this.pago.idsCuotasCobradas = [];
    this.pago.montoPagado = 0;
    delete this.erroresCampo['cuotasCobro'];
    delete this.erroresCampo['montoPago'];
    this.cdr.detectChanges();
  }

  estaCuotaCobroSeleccionada(idCuotaVenta: number): boolean {
    return this.cuotasSeleccionadasCobro.includes(Number(idCuotaVenta));
  }

  cuotasPendientesCobro(): CuotaVentaDetalle[] {
    const cuotas = this.ventaDetallePago?.cuotas ?? [];

    return cuotas
      .filter(c => this.calcularSaldoCuota(c) > 0 && (c.estadoCuota || '').toLowerCase() !== 'pagada')
      .sort((a, b) => Number(a.numeroCuota) - Number(b.numeroCuota));
  }

  calcularSaldoCuota(cuota: CuotaVentaDetalle): number {
    const saldo = Number(cuota.montoCuota || 0) - Number(cuota.montoPagado || 0);
    return Number(Math.max(0, saldo).toFixed(2));
  }

  calcularMontoCuotasSeleccionadas(): number {
    const cuotas = this.cuotasPendientesCobro();
    const total = cuotas
      .filter(c => this.estaCuotaCobroSeleccionada(c.idCuotaVenta))
      .reduce((acumulado, cuota) => acumulado + this.calcularSaldoCuota(cuota), 0);

    return Number(total.toFixed(2));
  }

  recalcularMontoCobroCuotas(): void {
    if (!this.esCobroEnCuotas()) {
      return;
    }

    this.pago.idsCuotasCobradas = [...this.cuotasSeleccionadasCobro];
    this.pago.montoPagado = this.calcularMontoCuotasSeleccionadas();
  }

  esVentaEnCuotas(venta: VentaListado | null = this.ventaPagoSeleccionada): boolean {
    if (!venta) {
      return false;
    }

    return (venta.estadoPago || '').toLowerCase().includes('cuota');
  }

  esCobroEnCuotas(): boolean {
    return this.esVentaEnCuotas(this.ventaPagoSeleccionada);
  }

  calcularSaldoDespuesCobro(): number {
    const saldoActual = Number(this.ventaPagoSeleccionada?.saldoPendiente || 0);
    const monto = Number(this.pago.montoPagado || 0);
    return Number(Math.max(0, saldoActual - monto).toFixed(2));
  }

  puedePagar(venta: VentaListado): boolean {
    if (!this.permisos.tienePermiso('VENTAS_COBRAR')) return false;
    return venta.estadoVenta !== 'Anulada' && Number(venta.saldoPendiente) > 0;
  }

  puedeAnular(venta: VentaListado): boolean {
    if (!this.permisos.tienePermiso('VENTAS_ANULAR')) return false;
    if (!this.sesionAnulacion.esAdministrador()) return false;
    return venta.estadoVenta !== 'Anulada';
  }

  obtenerNombreCliente(cliente: ClienteVenta | null): string {
    if (!cliente) {
      return '';
    }

    const nombreNatural = `${cliente.nombres || ''} ${cliente.apellidoPaterno || ''} ${cliente.apellidoMaterno || ''}`.trim();

    return (
      cliente.cliente ||
      cliente.nombreCompleto ||
      cliente.razonSocial ||
      nombreNatural ||
      'Cliente sin nombre'
    );
  }

  obtenerDocumentoCliente(cliente: ClienteVenta | null): string {
    return cliente?.numeroDocumento || '';
  }

  obtenerNombreElemento(elemento: ElementoVentaDisponible): string {
    const item = elemento as any;

    const nombreDirecto = this.extraerTextoDesdeObjeto(item, [
      'nombre',
      'Nombre',
      'producto',
      'Producto',
      'servicio',
      'Servicio',
      'nombreProducto',
      'NombreProducto',
      'nombreServicio',
      'NombreServicio',
      'descripcion',
      'Descripcion'
    ]);

    if (nombreDirecto) {
      return nombreDirecto;
    }

    const nombreAnidado =
      this.extraerTextoDesdeObjeto(item.elementoCatalogo, ['nombre', 'Nombre', 'descripcion', 'Descripcion']) ||
      this.extraerTextoDesdeObjeto(item.producto, ['nombre', 'Nombre', 'descripcion', 'Descripcion']) ||
      this.extraerTextoDesdeObjeto(item.servicio, ['nombre', 'Nombre', 'descripcion', 'Descripcion']);

    return nombreAnidado || 'Elemento sin nombre';
  }

  obtenerCodigoElemento(elemento: ElementoVentaDisponible): string {
    const item = elemento as any;

    const codigoDirecto = this.extraerTextoDesdeObjeto(item, [
      'codigoProducto',
      'CodigoProducto',
      'codigoServicio',
      'CodigoServicio',
      'codigo',
      'Codigo'
    ]);

    if (codigoDirecto) {
      return codigoDirecto;
    }

    const codigoAnidado =
      this.extraerTextoDesdeObjeto(item.elementoCatalogo, ['codigo', 'Codigo']) ||
      this.extraerTextoDesdeObjeto(item.producto, ['codigoProducto', 'CodigoProducto', 'codigo', 'Codigo']) ||
      this.extraerTextoDesdeObjeto(item.servicio, ['codigoServicio', 'CodigoServicio', 'codigo', 'Codigo']);

    return codigoAnidado || '-';
  }

  obtenerPrecioElemento(elemento: ElementoVentaDisponible): number {
    return this.extraerPrecioElemento(elemento);
  }

  obtenerStockElemento(elemento: ElementoVentaDisponible): number | null {
    const stock = this.extraerStockElemento(elemento);

    if (stock === null || stock === undefined) {
      return null;
    }

    return stock;
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
    if (!this.elementoSeleccionado) {
      return this.marcarErrorCampo('elementoDetalle', 'Selecciona un producto o servicio.');
    }

    const idElementoCatalogo = this.obtenerIdElementoCatalogo(this.elementoSeleccionado);

    if (idElementoCatalogo <= 0) {
      return this.marcarErrorCampo(
        'elementoDetalle',
        'El elemento seleccionado no tiene IdElementoCatalogo. Revisa el DTO de productos o servicios.'
      );
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

    if (this.tipoElementoFormulario === 'Producto') {
      const stockActual = this.obtenerStockElemento(this.elementoSeleccionado);
      const item = this.elementoSeleccionado as any;
      const aplicaInventario = Boolean(item.aplicaInventario ?? item.AplicaInventario ?? true);

      if (aplicaInventario && stockActual !== null && Number(this.detalleFormulario.cantidad) > stockActual) {
        return this.marcarErrorCampo(
          'cantidadDetalle',
          `No hay stock suficiente. Stock actual: ${stockActual}.`
        );
      }
    }

    if (this.detalleFormulario.precioUnitario === null || this.detalleFormulario.precioUnitario === undefined) {
      return this.marcarErrorCampo('precioDetalle', 'Ingresa el precio de venta.');
    }

    if (Number(this.detalleFormulario.precioUnitario) <= 0) {
      return this.marcarErrorCampo('precioDetalle', 'El precio de venta debe ser mayor a 0.');
    }

    return null;
  }

  private validarVenta(): string | null {
    if (!this.clienteSeleccionado || this.venta.idCliente <= 0) {
      return this.marcarErrorCampo('cliente', 'Selecciona un cliente.');
    }

    if (!this.venta.tipoComprobante) {
      return this.marcarErrorCampo('tipoComprobante', 'Selecciona el tipo de comprobante.');
    }

    if (!this.comprobantePermitidoParaCliente(this.venta.tipoComprobante)) {
      return this.marcarErrorCampo('tipoComprobante', 'Para persona natural con DNI solo se permite Boleta o Nota de venta.');
    }

    if (this.venta.tipoComprobante === 'Factura' && !this.clienteEsEmpresaConRuc()) {
      return this.marcarErrorCampo('tipoComprobante', 'Para emitir factura, el cliente debe ser empresa con RUC.');
    }

    if (!this.venta.serie || !this.venta.numero) {
      return this.marcarErrorCampo(
        'tipoComprobante',
        'No se pudo generar el número automático del comprobante. Vuelve a seleccionar el tipo de comprobante.'
      );
    }

    if (!this.venta.fechaEmision) {
      return this.marcarErrorCampo('fechaEmision', 'Ingresa la fecha de emisión.');
    }

    if (this.venta.detalles.length === 0) {
      return this.marcarErrorCampo('detalles', 'Agrega al menos un producto o servicio.');
    }

    for (const detalle of this.venta.detalles) {
      if (detalle.aplicaInventario && detalle.stockActual !== null && detalle.stockActual !== undefined) {
        if (detalle.cantidad > Number(detalle.stockActual)) {
          return this.marcarErrorCampo(
            'detalles',
            `No hay stock suficiente para ${detalle.elemento}. Stock actual: ${detalle.stockActual}.`
          );
        }
      }
    }

    if (!this.venta.tipoPago) {
      return this.marcarErrorCampo('tipoPago', 'Selecciona el tipo de cobro.');
    }

    if (!this.venta.metodoPago && Number(this.venta.montoPagado || 0) > 0) {
      return this.marcarErrorCampo('metodoPago', 'Selecciona el método de cobro.');
    }

    const total = this.calcularTotal();

    if (this.venta.montoPagado === null || this.venta.montoPagado === undefined) {
      return this.marcarErrorCampo('montoPagado', 'Ingresa el monto cobrado.');
    }

    if (Number(this.venta.montoPagado) < 0) {
      return this.marcarErrorCampo('montoPagado', 'El monto cobrado no puede ser negativo.');
    }

    if (Number(this.venta.montoPagado) > total) {
      return this.marcarErrorCampo('montoPagado', 'El monto cobrado no puede ser mayor al total.');
    }

    if (this.venta.tipoPago === 'Total' && Number(this.venta.montoPagado) !== total) {
      return this.marcarErrorCampo('montoPagado', 'Para cobro total, el monto cobrado debe ser igual al total.');
    }

    if (
      this.venta.tipoPago === 'Parcial' &&
      (Number(this.venta.montoPagado) <= 0 || Number(this.venta.montoPagado) >= total)
    ) {
      return this.marcarErrorCampo(
        'montoPagado',
        'Para cobro parcial, el monto debe ser mayor a 0 y menor al total.'
      );
    }

    if (this.venta.tipoPago === 'Cuotas') {
      const numeroCuotas = Number(this.venta.numeroCuotas || 0);

      if (!this.venta.numeroCuotas || numeroCuotas <= 0) {
        return this.marcarErrorCampo('numeroCuotas', 'Ingresa un número de cuotas válido.');
      }

      if (!Number.isInteger(numeroCuotas)) {
        return this.marcarErrorCampo('numeroCuotas', 'El número de cuotas debe ser entero.');
      }

      if (numeroCuotas > 24) {
        return this.marcarErrorCampo('numeroCuotas', 'El número máximo permitido es 24 cuotas.');
      }

      if (!this.venta.fechaPrimerVencimiento) {
        return this.marcarErrorCampo('fechaPrimerVencimiento', 'Ingresa la fecha del primer vencimiento.');
      }

      if (Number(this.venta.montoPagado) >= total) {
        return this.marcarErrorCampo('montoPagado', 'Para venta en cuotas debe quedar saldo pendiente.');
      }

      this.generarCuotas(false);

      if (!this.venta.cuotas || this.venta.cuotas.length !== numeroCuotas) {
        return this.marcarErrorCampo('cronogramaCuotas', 'El cronograma de cuotas no coincide con el número de cuotas.');
      }

      for (let i = 0; i < this.venta.cuotas.length; i++) {
        const cuota = this.venta.cuotas[i];

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
    if (!this.ventaPagoSeleccionada) {
      return this.marcarErrorCampo('pago', 'Selecciona una venta.');
    }

    if (!this.pago.metodoPago) {
      return this.marcarErrorCampo('metodoPagoPago', 'Selecciona el método de cobro.');
    }

    if (this.esCobroEnCuotas()) {
      if (this.cargandoDetallePago) {
        return this.marcarErrorCampo('cuotasCobro', 'Espera a que se carguen las cuotas.');
      }

      if (!this.ventaDetallePago) {
        return this.marcarErrorCampo('cuotasCobro', 'No se pudo obtener el detalle de cuotas.');
      }

      if (this.cuotasPendientesCobro().length === 0) {
        return this.marcarErrorCampo('cuotasCobro', 'No hay cuotas pendientes para cobrar.');
      }

      if (this.cuotasSeleccionadasCobro.length === 0) {
        return this.marcarErrorCampo('cuotasCobro', 'Selecciona una o más cuotas pendientes.');
      }

      const montoCuotas = this.calcularMontoCuotasSeleccionadas();

      if (montoCuotas <= 0) {
        return this.marcarErrorCampo('cuotasCobro', 'El monto de las cuotas seleccionadas debe ser mayor a 0.');
      }

      if (Math.round(Number(this.pago.montoPagado || 0) * 100) !== Math.round(montoCuotas * 100)) {
        return this.marcarErrorCampo('montoPago', 'El monto debe coincidir con la suma de cuotas seleccionadas.');
      }

      return null;
    }

    const saldoPendiente = Number(this.ventaPagoSeleccionada.saldoPendiente || 0);

    if (Number(this.pago.montoPagado || 0) <= 0) {
      return this.marcarErrorCampo('montoPago', 'El monto cobrado debe ser mayor a 0.');
    }

    if (Math.round(Number(this.pago.montoPagado || 0) * 100) !== Math.round(saldoPendiente * 100)) {
      return this.marcarErrorCampo('montoPago', 'Para una venta parcial o pendiente debes cobrar el saldo completo.');
    }

    return null;
  }

  private obtenerMensajeErrorCampo(campo: string): string | null {
    switch (campo) {
      case 'cliente':
        return this.venta.idCliente <= 0 ? 'Selecciona un cliente.' : null;

      case 'tipoComprobante':
        if (!this.venta.tipoComprobante) {
          return 'Selecciona el tipo de comprobante.';
        }

        if (!this.comprobantePermitidoParaCliente(this.venta.tipoComprobante)) {
          return 'Para persona natural con DNI solo se permite Boleta o Nota de venta.';
        }

        if (this.venta.tipoComprobante === 'Factura' && !this.clienteEsEmpresaConRuc()) {
          return 'Para emitir factura, el cliente debe ser empresa con RUC.';
        }

        return null;

      case 'serie':
      case 'numero':
        return null;

      case 'fechaEmision':
        return !this.venta.fechaEmision ? 'Ingresa la fecha de emisión.' : null;

      case 'detalles':
        return this.venta.detalles.length === 0 ? 'Agrega al menos un producto o servicio.' : null;

      case 'tipoPago':
        return !this.venta.tipoPago ? 'Selecciona el tipo de cobro.' : null;

      case 'metodoPago':
        return !this.venta.metodoPago && Number(this.venta.montoPagado || 0) > 0
          ? 'Selecciona el método de cobro.'
          : null;

      case 'montoPagado':
        return this.validarMontoPagadoCampo();

      case 'numeroCuotas':
        if (this.venta.tipoPago !== 'Cuotas') {
          return null;
        }

        if (!this.venta.numeroCuotas || Number(this.venta.numeroCuotas) <= 0) {
          return 'Ingresa un número de cuotas válido.';
        }

        if (!Number.isInteger(Number(this.venta.numeroCuotas))) {
          return 'El número de cuotas debe ser entero.';
        }

        if (Number(this.venta.numeroCuotas) > 24) {
          return 'El número máximo permitido es 24 cuotas.';
        }

        return null;

      case 'fechaPrimerVencimiento':
        return this.venta.tipoPago === 'Cuotas' && !this.venta.fechaPrimerVencimiento
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

    if (this.venta.montoPagado === null || this.venta.montoPagado === undefined) {
      return 'Ingresa el monto cobrado.';
    }

    if (Number(this.venta.montoPagado) < 0) {
      return 'El monto cobrado no puede ser negativo.';
    }

    if (Number(this.venta.montoPagado) > total) {
      return 'El monto cobrado no puede ser mayor al total.';
    }

    if (this.venta.tipoPago === 'Total' && Number(this.venta.montoPagado) !== total) {
      return 'Para cobro total, el monto cobrado debe ser igual al total.';
    }

    if (
      this.venta.tipoPago === 'Parcial' &&
      (Number(this.venta.montoPagado) <= 0 || Number(this.venta.montoPagado) >= total)
    ) {
      return 'Para cobro parcial, el monto debe ser mayor a 0 y menor al total.';
    }

    if (this.venta.tipoPago === 'Cuotas' && Number(this.venta.montoPagado) >= total) {
      return 'Para venta en cuotas debe quedar saldo pendiente.';
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

    if (texto.includes('cliente')) {
      this.erroresCampo['cliente'] = mensajeBackend;
    } else if (texto.includes('factura') || texto.includes('comprobante') || texto.includes('serie') || texto.includes('número') || texto.includes('numero')) {
      this.erroresCampo['tipoComprobante'] = mensajeBackend;
    } else if (texto.includes('stock')) {
      this.erroresCampo['detalles'] = mensajeBackend;
    } else if (texto.includes('producto') || texto.includes('servicio') || texto.includes('detalle')) {
      this.erroresCampo['detalles'] = mensajeBackend;
    } else if (texto.includes('cobro') || texto.includes('pago') || texto.includes('monto')) {
      this.erroresCampo['montoPagado'] = mensajeBackend;
      this.erroresCampo['montoPago'] = mensajeBackend;
    } else if (texto.includes('cuota')) {
      this.erroresCampo['numeroCuotas'] = mensajeBackend;
      this.erroresCampo['cronogramaCuotas'] = mensajeBackend;
      this.erroresCampo['cuotasCobro'] = mensajeBackend;
    } else if (texto.includes('cotización') || texto.includes('cotizacion')) {
      this.erroresCampo['cotizacion'] = mensajeBackend;
    } else if (texto.includes('anulación') || texto.includes('anulacion')) {
      this.erroresCampo['motivoAnulacion'] = mensajeBackend;
    }
  }

  private normalizarVenta(): void {
    this.venta.idUsuarioRegistro = 1;

    this.venta.tipoComprobante = this.venta.tipoComprobante.trim();
    this.venta.serie = this.venta.serie.trim().toUpperCase();
    this.venta.numero = this.venta.numero.trim();

    this.venta.observacion = this.venta.observacion?.trim() || null;

    this.venta.metodoPago = this.venta.metodoPago.trim();
    this.venta.tipoPago = this.venta.tipoPago.trim();

    this.venta.montoPagado = Number(this.venta.montoPagado || 0);

    if (this.venta.tipoPago !== 'Cuotas') {
      this.venta.numeroCuotas = null;
      this.venta.fechaPrimerVencimiento = null;
      this.venta.cuotas = [];
    } else {
      this.generarCuotas(false);

      this.venta.cuotas = (this.venta.cuotas ?? []).map(c => ({
        numeroCuota: Number(c.numeroCuota),
        fechaVencimiento: c.fechaVencimiento,
        montoCuota: Number(c.montoCuota || 0)
      }));
    }

    this.venta.detalles = this.venta.detalles.map(d => ({
      ...d,
      cantidad: Number(d.cantidad),
      precioUnitario: Number(d.precioUnitario)
    }));
  }

  private ajustarPagoPorTipo(): void {
    if (this.venta.tipoPago === 'Total') {
      this.venta.montoPagado = this.calcularTotal();
      this.venta.cuotas = [];
      return;
    }

    if (this.venta.tipoPago === 'Parcial') {
      this.venta.cuotas = [];
      return;
    }

    if (this.venta.tipoPago === 'Cuotas') {
      if (this.venta.montoPagado < 0) {
        this.venta.montoPagado = 0;
      }

      this.generarCuotas(false);
    }
  }

  private generarCuotas(reiniciarFechas: boolean = false): void {
    if (this.venta.tipoPago !== 'Cuotas') {
      this.venta.cuotas = [];
      return;
    }

    const numeroCuotas = Number(this.venta.numeroCuotas || 0);

    if (!Number.isInteger(numeroCuotas) || numeroCuotas <= 0 || numeroCuotas > 24) {
      this.venta.cuotas = [];
      return;
    }

    const saldoFinanciar = this.obtenerSaldoFinanciar();

    if (saldoFinanciar <= 0) {
      this.venta.cuotas = [];
      return;
    }

    const saldoCentavos = Math.round(saldoFinanciar * 100);
    const baseCentavos = Math.floor(saldoCentavos / numeroCuotas);
    const diferenciaCentavos = saldoCentavos - (baseCentavos * numeroCuotas);

    const cuotasActuales = this.venta.cuotas ?? [];
    const cuotasNuevas: CuotaVentaCrear[] = [];

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

    this.venta.cuotas = cuotasNuevas;
  }

  private obtenerFechaCuotaDesdePrimerVencimiento(numeroCuota: number): string | null {
    if (!this.venta.fechaPrimerVencimiento) {
      return null;
    }

    return this.sumarMesesAFecha(this.venta.fechaPrimerVencimiento, numeroCuota - 1);
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
    if (this.venta.tipoPago !== 'Cuotas') {
      return null;
    }

    const numeroCuotas = Number(this.venta.numeroCuotas || 0);

    if (!Number.isInteger(numeroCuotas) || numeroCuotas <= 0 || numeroCuotas > 24) {
      return null;
    }

    if (!this.venta.cuotas || this.venta.cuotas.length !== numeroCuotas) {
      return 'El cronograma de cuotas no coincide con el número de cuotas.';
    }

    if (this.venta.cuotas.some(c => !c.fechaVencimiento)) {
      return 'Todas las cuotas deben tener fecha de vencimiento.';
    }

    const saldoFinanciarCentavos = Math.round(this.obtenerSaldoFinanciar() * 100);
    const totalCuotasCentavos = Math.round(this.calcularTotalCuotas() * 100);

    if (saldoFinanciarCentavos !== totalCuotasCentavos) {
      return 'La suma de las cuotas debe ser igual al saldo pendiente.';
    }

    return null;
  }

  private nuevaVenta(): VentaCrear {
    return {
      idCliente: 0,
      idCotizacion: null,
      idUsuarioRegistro: 1,

      tipoComprobante: '',
      serie: '',
      numero: '',
      fechaEmision: this.obtenerFechaActual(),

      observacion: '',

      tipoPago: 'Total',
      metodoPago: '',
      montoPagado: 0,

      numeroCuotas: null,
      fechaPrimerVencimiento: null,

      detalles: [],
      cuotas: []
    };
  }

  private nuevoPago(): PagoVentaCrear {
    return {
      idVenta: 0,
      idUsuarioRegistro: 1,
      metodoPago: '',
      montoPagado: 0,
      observacion: '',
      idsCuotasCobradas: []
    };
  }

  private nuevaAnulacion(): AnularVenta {
    return {
      idUsuarioRegistro: 1,
      motivo: ''
    };
  }

  private obtenerFechaActual(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private obtenerIdElementoCatalogo(elemento: ElementoVentaDisponible): number {
    return this.extraerIdElementoCatalogo(elemento);
  }

  private clienteEsEmpresaConRuc(): boolean {
    const documento = this.obtenerDocumentoCliente(this.clienteSeleccionado).replace(/\D/g, '');
    const tipoDocumento = (this.clienteSeleccionado?.tipoDocumento || '').toLowerCase();
    const tieneRazonSocial = !!this.clienteSeleccionado?.razonSocial;

    return documento.length === 11 || tipoDocumento.includes('ruc') || tieneRazonSocial;
  }

  private clienteEsPersonaNaturalDni(): boolean {
    const documento = this.obtenerDocumentoCliente(this.clienteSeleccionado).replace(/\D/g, '');
    const tipoDocumento = (this.clienteSeleccionado?.tipoDocumento || '').toLowerCase();

    return documento.length === 8 || tipoDocumento.includes('dni');
  }

  private comprobantePermitidoParaCliente(tipoComprobante: string): boolean {
    if (!tipoComprobante) {
      return true;
    }

    if (this.clienteEsPersonaNaturalDni()) {
      return tipoComprobante === 'Boleta' || tipoComprobante === 'Nota de venta';
    }

    return true;
  }

  private normalizarTipoComprobantePorCliente(): void {
    if (!this.venta.tipoComprobante) {
      return;
    }

    if (!this.comprobantePermitidoParaCliente(this.venta.tipoComprobante)) {
      this.venta.tipoComprobante = '';
      this.venta.serie = '';
      this.venta.numero = '';
      this.erroresCampo['tipoComprobante'] = 'Para persona natural con DNI solo se permite Boleta o Nota de venta.';
    }
  }

  private normalizarElementoDisponible(
    item: ElementoVentaDisponible,
    tipo: 'Producto' | 'Servicio'
  ): ElementoVentaDisponible {
    const itemAny = item as any;

    const precio = this.extraerPrecioElemento(itemAny);
    const idElementoCatalogo = this.extraerIdElementoCatalogo(itemAny);
    const stockActual = this.extraerStockElemento(itemAny);

    return {
      ...itemAny,
      idElementoCatalogo: idElementoCatalogo > 0 ? idElementoCatalogo : itemAny.idElementoCatalogo,
      precioVenta: precio,
      precio: precio,
      precioUnitario: precio,
      stockActual: stockActual ?? itemAny.stockActual ?? itemAny.StockActual ?? null,
      tipoElemento: tipo
    } as ElementoVentaDisponible;
  }

  private extraerIdElementoCatalogo(item: any): number {
    const campos = [
      'idElementoCatalogo',
      'IdElementoCatalogo',
      'elementoCatalogoId',
      'ElementoCatalogoId',
      'idCatalogo',
      'IdCatalogo',
      'idElemento',
      'IdElemento'
    ];

    const idDirecto = this.leerNumeroDesdeObjeto(item, campos) ?? 0;

    if (idDirecto > 0) {
      return idDirecto;
    }

    const objetosAnidados = [
      item?.elementoCatalogo,
      item?.ElementoCatalogo,
      item?.catalogo,
      item?.Catalogo,
      item?.producto,
      item?.Producto,
      item?.servicio,
      item?.Servicio
    ];

    for (const objeto of objetosAnidados) {
      const id = this.leerNumeroDesdeObjeto(objeto, campos) ?? 0;

      if (id > 0) {
        return id;
      }
    }

    return 0;
  }

  private extraerPrecioElemento(item: any): number {
    const camposPrecio = [
      'precioVentaConIgv',
      'PrecioVentaConIgv',
      'precioVentaConIGV',
      'PrecioVentaConIGV',
      'precioConIgv',
      'PrecioConIgv',
      'precioConIGV',
      'PrecioConIGV',
      'precioUnitarioConIgv',
      'PrecioUnitarioConIgv',
      'precioUnitarioConIGV',
      'PrecioUnitarioConIGV',
      'precioVenta',
      'PrecioVenta',
      'precioUnitario',
      'PrecioUnitario',
      'precioReferencial',
      'PrecioReferencial',
      'precioFinal',
      'PrecioFinal',
      'precio',
      'Precio',
      'montoVenta',
      'MontoVenta',
      'valorVenta',
      'ValorVenta',
      'tarifa',
      'Tarifa',
      'costo',
      'Costo'
    ];

    const precioDirecto = this.leerNumeroDesdeObjeto(item, camposPrecio) ?? 0;

    if (precioDirecto > 0) {
      return precioDirecto;
    }

    const objetosAnidados = [
      item?.elementoCatalogo,
      item?.ElementoCatalogo,
      item?.producto,
      item?.Producto,
      item?.servicio,
      item?.Servicio,
      item?.catalogo,
      item?.Catalogo,
      item?.item,
      item?.Item
    ];

    for (const objeto of objetosAnidados) {
      const precio = this.leerNumeroDesdeObjeto(objeto, camposPrecio) ?? 0;

      if (precio > 0) {
        return precio;
      }
    }

    return 0;
  }

  private extraerStockElemento(item: any): number | null {
    const camposStock = [
      'stockActual',
      'StockActual',
      'stock',
      'Stock',
      'cantidadStock',
      'CantidadStock',
      'existencia',
      'Existencia'
    ];

    const stockDirecto = this.leerNumeroDesdeObjeto(item, camposStock, true);

    if (stockDirecto !== null) {
      return stockDirecto;
    }

    const objetosAnidados = [
      item?.inventario,
      item?.Inventario,
      item?.producto,
      item?.Producto
    ];

    for (const objeto of objetosAnidados) {
      const stock = this.leerNumeroDesdeObjeto(objeto, camposStock, true);

      if (stock !== null) {
        return stock;
      }
    }

    return null;
  }

  private leerNumeroDesdeObjeto(
    objeto: any,
    campos: string[],
    aceptarCero: boolean = false
  ): number | null {
    if (!objeto || typeof objeto !== 'object') {
      return null;
    }

    for (const campo of campos) {
      const valor = objeto[campo];

      if (valor === null || valor === undefined || valor === '') {
        continue;
      }

      const numero = this.convertirNumero(valor);

      if (numero > 0 || (aceptarCero && numero === 0)) {
        return numero;
      }
    }

    return null;
  }

  private convertirNumero(valor: any): number {
    if (valor === null || valor === undefined || valor === '') {
      return 0;
    }

    if (typeof valor === 'number') {
      return Number.isFinite(valor) ? valor : 0;
    }

    if (typeof valor === 'string') {
      let texto = valor
        .replace(/S\/?/gi, '')
        .replace(/soles/gi, '')
        .replace(/\s/g, '')
        .trim();

      if (texto.includes(',') && texto.includes('.')) {
        texto = texto.replace(/,/g, '');
      } else if (texto.includes(',') && !texto.includes('.')) {
        texto = texto.replace(',', '.');
      }

      const numero = Number(texto);

      return Number.isFinite(numero) ? numero : 0;
    }

    const numero = Number(valor);

    return Number.isFinite(numero) ? numero : 0;
  }

  private extraerTextoDesdeObjeto(objeto: any, campos: string[]): string {
    if (!objeto || typeof objeto !== 'object') {
      return '';
    }

    for (const campo of campos) {
      const valor = objeto[campo];

      if (valor !== null && valor !== undefined && String(valor).trim().length > 0) {
        return String(valor).trim();
      }
    }

    return '';
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
