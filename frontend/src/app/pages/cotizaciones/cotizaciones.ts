import { AvisoComponent } from '../../shared/mensajes/aviso';
import { MensajesService } from '../../shared/mensajes/mensajes.service';
import { UiIconComponent } from '../../shared/ui-icon/ui-icon';
import { SessionService } from '../../core/services/session.service';
import { CommonModule } from '@angular/common';
import { inject, Component, ChangeDetectorRef, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';

import { CotizacionService } from '../../core/services/cotizacion';
import {
  ClienteSelector,
  CotizacionCrear,
  CotizacionDetalleCrear,
  CotizacionListado,
  CotizacionWhatsApp,
  ElementoCotizable,
  EstadoCotizacion
} from '../../core/models/cotizacion.model';

@Component({
  selector: 'app-cotizaciones',
  standalone: true,
  imports: [AvisoComponent, UiIconComponent, CommonModule, FormsModule],
  templateUrl: './cotizaciones.html',
  styleUrl: './cotizaciones.scss'
})
export class CotizacionesComponent implements OnInit {
  private readonly mensajes = inject(MensajesService);
  readonly permisos = inject(SessionService);
  private readonly sesionAnulacion = inject(SessionService);
  private readonly rutaModuloVentas = '/admin/ventas';

  cotizaciones: CotizacionListado[] = [];

  clientes: ClienteSelector[] = [];
  elementosCotizables: ElementoCotizable[] = [];
  estados: EstadoCotizacion[] = [];

  origenesCotizacion = ['Manual', 'Web', 'WhatsApp', 'Correo'];
  tiposElementoDetalle: Array<'Producto' | 'Servicio'> = ['Producto', 'Servicio'];

  tipoElementoDetalle: 'Producto' | 'Servicio' = 'Producto';

  cargando = false;
  cargandoCombos = false;
  guardando = false;

  procesandoAccion = false;
  idAccionActual: number | null = null;
  accionActual = '';

  buscarCliente = '';
  buscarElemento = '';
  buscar = '';
  estadoFiltro = '';
  origenFiltro = '';

  mensaje = '';
  error = '';

  pagina = 1;
  tamanioPagina = 8;
  totalRegistros = 0;
  totalPaginas = 0;
  opcionesTamanioPagina = [5, 8, 10, 20];

  cotizacion: CotizacionCrear = this.nuevaCotizacion();
  detalleTemporal: CotizacionDetalleCrear = this.nuevoDetalle();

  cotizacionSeleccionada: CotizacionListado | null = null;
  detalleVisible = false;

  whatsappPendiente: CotizacionWhatsApp | null = null;
  cotizacionWhatsAppPendiente: CotizacionListado | null = null;
  modalWhatsappVisible = false;

  constructor(
    private cotizacionService: CotizacionService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.subirArriba();
    this.cargarCombos();
    this.cargarCotizaciones();
  }

  cargarCombos(): void {
    this.cargandoCombos = true;
    this.error = '';

    forkJoin({
      clientes: this.permisos.tienePermiso('COTIZACIONES_CREAR') ? this.cotizacionService.listarClientes() : of([]),
      elementos: this.permisos.tienePermiso('COTIZACIONES_CREAR') ? this.cotizacionService.listarElementosCotizables() : of([]),
      estados: this.cotizacionService.listarEstados()
    }).subscribe({
      next: (data) => {
        this.clientes = data.clientes ?? [];
        this.elementosCotizables = data.elementos ?? [];
        this.estados = this.normalizarEstados(data.estados ?? []);

        this.establecerValoresIniciales();

        this.cargandoCombos = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error cargando datos de cotización:', err);
        this.error = this.obtenerMensajeError(
          err,
          'No se pudieron cargar clientes, estados o elementos cotizables.'
        );
        this.cargandoCombos = false;
        this.cdr.detectChanges();
      }
    });
  }

  cargarCotizaciones(): void {
    this.cargando = true;
    this.error = '';

    this.cotizacionService
      .listar(
        this.buscar,
        this.estadoFiltro,
        this.origenFiltro,
        this.pagina,
        this.tamanioPagina
      )
      .subscribe({
        next: (data) => {
          this.cotizaciones = data.items ?? [];
          this.pagina = data.pagina;
          this.tamanioPagina = data.tamanioPagina;
          this.totalRegistros = data.totalRegistros;
          this.totalPaginas = (data as any).totalPaginas ??
            Math.ceil(this.totalRegistros / this.tamanioPagina);

          if (this.cotizaciones.length === 0 && this.totalRegistros > 0 && this.pagina > 1) {
            this.pagina--;
            this.cargando = false;
            this.cargarCotizaciones();
            return;
          }

          this.cargando = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Error cargando cotizaciones:', err);
          this.error = this.obtenerMensajeError(err, 'No se pudieron cargar las cotizaciones.');
          this.cargando = false;
          this.cdr.detectChanges();
        }
      });
  }

  guardarCotizacion(): void {
    if (!(this.permisos.tienePermiso('COTIZACIONES_CREAR'))) return;
    this.mensaje = '';
    this.error = '';

    const validacion = this.validarCotizacion();

    if (validacion) {
      this.error = validacion;
      return;
    }

    const payload: CotizacionCrear = {
      idCliente: Number(this.cotizacion.idCliente),
      idUsuarioRegistro: this.cotizacion.idUsuarioRegistro ?? 1,
      origenCotizacion: 'Manual',
      descuento: 0,
      observacion: null,
      detalles: this.cotizacion.detalles.map((detalle) => ({
        idElementoCatalogo: Number(detalle.idElementoCatalogo),
        cantidad: Number(detalle.cantidad),
        precioUnitario: Number(detalle.precioUnitario),
        observacion: this.normalizarTexto(detalle.observacion)
      }))
    };

    this.guardando = true;

    this.cotizacionService.crear(payload).subscribe({
      next: (data) => {
        this.guardando = false;
        this.limpiarFormulario();
        this.mensaje = `${data.codigoCotizacion || 'Cotización'} registrada correctamente. El PDF se generó automáticamente.`;
        this.pagina = 1;
        this.cargarCotizaciones();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.error = this.obtenerMensajeError(err, 'No se pudo registrar la cotización.');
        this.guardando = false;
        this.cdr.detectChanges();
      }
    });
  }

  verDetalle(item: CotizacionListado): void {
    this.mensaje = '';
    this.error = '';
    this.cotizacionSeleccionada = null;
    this.detalleVisible = true;

    this.iniciarAccion(item.idCotizacion, 'detalle');

    this.cotizacionService.obtenerPorId(item.idCotizacion).subscribe({
      next: (data) => {
        this.cotizacionSeleccionada = data;
        this.finalizarAccion();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.error = this.obtenerMensajeError(err, 'No se pudo cargar el detalle de la cotización.');
        this.detalleVisible = false;
        this.finalizarAccion();
        this.cdr.detectChanges();
      }
    });
  }

  cerrarDetalle(): void {
    this.detalleVisible = false;
    this.cotizacionSeleccionada = null;
    this.cdr.detectChanges();
  }

  generarPdf(item: CotizacionListado, abrirDespues = false): void {
    if (!(this.permisos.tienePermiso('COTIZACIONES_VER'))) return;
    this.mensaje = '';
    this.error = '';

    const ventanaPdf = abrirDespues
      ? this.abrirVentanaPreparacion('Preparando PDF de cotización...')
      : null;

    this.iniciarAccion(item.idCotizacion, 'pdf');

    this.cotizacionService.generarPdf(item.idCotizacion).subscribe({
      next: (data) => {
        this.mensaje = data.mensaje || 'PDF generado correctamente.';

        if (abrirDespues && data.archivoPdf) {
          const urlPdf = this.cotizacionService.obtenerUrlArchivo(data.archivoPdf);
          this.redirigirVentana(ventanaPdf, urlPdf);
        } else {
          this.cerrarVentanaPreparacion(ventanaPdf);
        }

        this.cargarCotizaciones();
        this.finalizarAccion();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.error = this.obtenerMensajeError(err, 'No se pudo generar el PDF.');
        this.cerrarVentanaPreparacion(ventanaPdf);
        this.finalizarAccion();
        this.cdr.detectChanges();
      }
    });
  }

  abrirPdf(item: CotizacionListado): void {
    this.mensaje = '';
    this.error = '';

    if (item.archivoPdf?.endsWith('-v2.pdf')) {
      window.open(this.cotizacionService.obtenerUrlArchivo(item.archivoPdf), '_blank');
      return;
    }

    this.generarPdf(item, true);
  }

  descargarPdf(item: CotizacionListado): void {
    this.mensaje = '';
    this.error = '';

    if (!item.archivoPdf?.endsWith('-v2.pdf')) {
      this.generarPdf(item, true);
      return;
    }

    const url = this.cotizacionService.obtenerUrlArchivo(item.archivoPdf);
    const link = document.createElement('a');

    link.href = url;
    link.target = '_blank';
    link.download = `${this.obtenerCodigoCotizacion(item)}.pdf`;

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }

  async enviarCorreo(item: CotizacionListado): Promise<void> {
    if (!(this.permisos.tienePermiso('COTIZACIONES_ENVIAR_CORREO'))) return;
    this.mensaje = '';
    this.error = '';

    if (!this.puedeEnviarCorreo(item)) {
      this.error = 'Esta cotización no permite envío por correo en su estado actual.';
      return;
    }

    if (!item.correoCliente) {
      this.error = 'El cliente no tiene correo registrado.';
      return;
    }

    const confirmar = (await this.mensajes.confirmar(`Se enviará ${this.obtenerCodigoCotizacion(item)} al correo ${item.correoCliente} con el PDF adjunto. ¿Deseas continuar?`, {"titulo":"Enviar cotización por correo","aceptar":"Enviar cotización","tipo":"info","icono":"mail"}));

    if (!confirmar) {
      return;
    }

    this.iniciarAccion(item.idCotizacion, 'correo');

    this.cotizacionService.enviarCorreo(item.idCotizacion, 1).subscribe({
      next: (data) => {
        this.mensaje = data.mensaje || 'Cotización enviada por correo correctamente.';
        this.cargarCotizaciones();
        this.finalizarAccion();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.error = this.obtenerMensajeError(err, 'No se pudo enviar el correo.');
        this.finalizarAccion();
        this.cdr.detectChanges();
      }
    });
  }

  enviarWhatsApp(item: CotizacionListado): void {
    if (!(this.permisos.tienePermiso('COTIZACIONES_ENVIAR_WHATSAPP'))) return;
    this.mensaje = '';
    this.error = '';

    if (!this.puedeEnviarWhatsApp(item)) {
      this.error = 'Esta cotización no permite envío por WhatsApp en su estado actual.';
      return;
    }

    if (!item.telefonoCliente) {
      this.error = 'El cliente no tiene teléfono registrado.';
      return;
    }

    const ventanaWhatsapp = this.abrirVentanaPreparacion('Preparando WhatsApp Web...');
    const ventanaPdf = this.abrirVentanaPreparacion('Preparando PDF para adjuntar...');

    if (!ventanaWhatsapp) {
      this.cerrarVentanaPreparacion(ventanaPdf);
      this.error = 'El navegador bloqueó la ventana de WhatsApp. Permite ventanas emergentes para esta página.';
      return;
    }

    this.iniciarAccion(item.idCotizacion, 'whatsapp');

    this.cotizacionService.enviarWhatsApp(item.idCotizacion, 1).subscribe({
      next: (data) => {
        const resultado = data.resultado;

        if (!resultado.requiereConfirmacionRespondida) {
          this.cerrarVentanaPreparacion(ventanaWhatsapp);
          this.cerrarVentanaPreparacion(ventanaPdf);

          this.mensaje = data.mensaje || 'Cotización enviada por WhatsApp correctamente.';
          this.cargarCotizaciones();
          this.finalizarAccion();
          this.cdr.detectChanges();
          return;
        }

        this.whatsappPendiente = resultado;
        this.cotizacionWhatsAppPendiente = item;
        this.modalWhatsappVisible = true;

        const urlWhatsappWeb = this.construirUrlWebWhatsApp(resultado);

        if (urlWhatsappWeb) {
          this.redirigirVentana(ventanaWhatsapp, urlWhatsappWeb);
        } else {
          this.cerrarVentanaPreparacion(ventanaWhatsapp);
          this.error = 'No se pudo construir el enlace de WhatsApp Web.';
        }

        if (resultado.archivoPdf) {
          const urlPdf = this.cotizacionService.obtenerUrlArchivo(resultado.archivoPdf);
          this.redirigirVentana(ventanaPdf, urlPdf);
        } else {
          this.cerrarVentanaPreparacion(ventanaPdf);
        }

        this.mensaje =
          'WhatsApp Web fue abierto con el mensaje preparado. Adjunta manualmente el PDF y confirma el envío en el sistema.';

        this.finalizarAccion();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.error = this.obtenerMensajeError(err, 'No se pudo preparar el envío por WhatsApp.');
        this.cerrarVentanaPreparacion(ventanaWhatsapp);
        this.cerrarVentanaPreparacion(ventanaPdf);
        this.finalizarAccion();
        this.cdr.detectChanges();
      }
    });
  }

  async confirmarWhatsAppEnviado(): Promise<void> {
    if (!(this.permisos.tienePermiso('COTIZACIONES_EDITAR'))) return;
    if (!this.cotizacionWhatsAppPendiente) {
      this.error = 'No hay una cotización pendiente de confirmación por WhatsApp.';
      return;
    }

    const item = this.cotizacionWhatsAppPendiente;

    const confirmar = (await this.mensajes.confirmar(`Confirma solo si ya enviaste ${this.obtenerCodigoCotizacion(item)} por WhatsApp y adjuntaste el PDF al cliente.`, {"titulo":"Confirmar envío por WhatsApp","aceptar":"Confirmar envío","tipo":"info","icono":"check"}));

    if (!confirmar) {
      return;
    }

    this.iniciarAccion(item.idCotizacion, 'confirmar-whatsapp');

    this.cotizacionService.marcarRespondidaWhatsApp(item.idCotizacion, 1).subscribe({
      next: (data) => {
        this.mensaje = data.mensaje || 'Cotización marcada como respondida por WhatsApp.';
        this.modalWhatsappVisible = false;
        this.whatsappPendiente = null;
        this.cotizacionWhatsAppPendiente = null;
        this.cargarCotizaciones();
        this.finalizarAccion();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.error = this.obtenerMensajeError(err, 'No se pudo marcar como respondida por WhatsApp.');
        this.finalizarAccion();
        this.cdr.detectChanges();
      }
    });
  }

  cancelarConfirmacionWhatsApp(): void {
    this.modalWhatsappVisible = false;
    this.whatsappPendiente = null;
    this.cotizacionWhatsAppPendiente = null;
    this.cdr.detectChanges();
  }

  async aprobarCotizacion(item: CotizacionListado): Promise<void> {
    if (!(this.permisos.tienePermiso('COTIZACIONES_APROBAR'))) return;
    this.mensaje = '';
    this.error = '';

    if (!this.puedeAprobar(item)) {
      this.error = 'Solo una cotización respondida puede marcarse como aprobada.';
      return;
    }

    const confirmar = (await this.mensajes.confirmar(`¿Confirmas que el cliente aprobó ${this.obtenerCodigoCotizacion(item)}?`, {"titulo":"Confirmar aprobación","aceptar":"Aprobar cotización","tipo":"success","icono":"check"}));

    if (!confirmar) {
      return;
    }

    this.iniciarAccion(item.idCotizacion, 'aprobar');

    this.cotizacionService.aprobar(item.idCotizacion, 1).subscribe({
      next: (data) => {
        this.mensaje = data.mensaje || 'Cotización aprobada correctamente.';
        this.cargarCotizaciones();
        this.finalizarAccion();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.error = this.obtenerMensajeError(err, 'No se pudo aprobar la cotización.');
        this.finalizarAccion();
        this.cdr.detectChanges();
      }
    });
  }

  async cancelarCotizacion(item: CotizacionListado): Promise<void> {
    if (!(this.permisos.tienePermiso('COTIZACIONES_CANCELAR'))) return;
    if (!this.sesionAnulacion.esAdministrador()) return;
    this.mensaje = '';
    this.error = '';

    if (!this.puedeCancelar(item)) {
      this.error = 'Esta cotización ya no puede cancelarse.';
      return;
    }

    const confirmar = (await this.mensajes.confirmar(`¿Deseas cancelar ${this.obtenerCodigoCotizacion(item)}? Esta acción cambiará su estado a Cancelada.`, {"titulo":"Cancelar cotización","aceptar":"Cancelar cotización","tipo":"warning","icono":"warning"}));

    if (!confirmar) {
      return;
    }

    this.iniciarAccion(item.idCotizacion, 'cancelar');

    this.cotizacionService.cancelar(item.idCotizacion, 1).subscribe({
      next: (data) => {
        this.mensaje = data.mensaje || 'Cotización cancelada correctamente.';
        this.cargarCotizaciones();
        this.finalizarAccion();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.error = this.obtenerMensajeError(err, 'No se pudo cancelar la cotización.');
        this.finalizarAccion();
        this.cdr.detectChanges();
      }
    });
  }

  prepararVenta(item: CotizacionListado): void {
    this.mensaje = '';
    this.error = '';

    if (!this.puedeConvertirEnVenta(item)) {
      this.error = 'Solo una cotización aprobada y no convertida puede pasar a venta.';
      return;
    }

    this.iniciarAccion(item.idCotizacion, 'venta');

    this.cotizacionService.prepararParaVenta(item.idCotizacion).subscribe({
      next: (data) => {
        const cotizacionParaVenta = {
          idCotizacion: data.idCotizacion,
          codigoCotizacion: data.codigoCotizacion || this.obtenerCodigoCotizacion(data),

          idCliente: data.idCliente,
          cliente: data.cliente,
          tipoCliente: data.tipoCliente,
          tipoDocumentoCliente: data.tipoDocumentoCliente,
          documentoCliente: data.documentoCliente,
          correoCliente: data.correoCliente || null,
          telefonoCliente: data.telefonoCliente || null,
          direccionCliente: data.direccionCliente || null,

          subtotal: Number(data.subtotal || 0),
          descuento: Number(data.descuento || 0),
          igv: Number(data.igv || 0),
          total: Number(data.total || 0),

          observacion: data.observacion || null,

          detalles: (data.detalles || []).map((detalle: any) => ({
            idElementoCatalogo: Number(detalle.idElementoCatalogo || 0),
            elementoNombre:
              detalle.elementoNombre ||
              detalle.elemento ||
              detalle.nombre ||
              'Elemento cotizado',
            tipoElemento: detalle.tipoElemento || 'Producto',
            codigo:
              detalle.codigo ||
              detalle.codigoProducto ||
              detalle.codigoServicio ||
              null,
            cantidad: Number(detalle.cantidad || 0),
            precioUnitario: Number(detalle.precioUnitario || 0),
            subtotal: Number(detalle.subtotal || 0),
            observacion: detalle.observacion || null,
            stockActual: detalle.stockActual ?? null,
            aplicaInventario: detalle.aplicaInventario ?? null
          }))
        };

        sessionStorage.setItem(
          'cotizacionParaVenta',
          JSON.stringify(cotizacionParaVenta)
        );

        this.mensaje = `${cotizacionParaVenta.codigoCotizacion} preparada para venta.`;

        this.finalizarAccion();
        this.cdr.detectChanges();

        this.router.navigate([this.rutaModuloVentas], {
          queryParams: {
            idCotizacion: data.idCotizacion
          }
        });
      },
      error: (err) => {
        console.error(err);
        this.error = this.obtenerMensajeError(
          err,
          'No se pudo preparar la cotización para venta.'
        );
        this.finalizarAccion();
        this.cdr.detectChanges();
      }
    });
  }

  verVenta(item: CotizacionListado): void {
    const idVenta = item.idVentaAsociada ?? item.idVentaGenerada;

    if (!idVenta) {
      this.error = 'Esta cotización figura como convertida, pero no se encontró la venta asociada.';
      return;
    }

    this.router.navigate([this.rutaModuloVentas], {
      queryParams: {
        idVenta
      }
    });
  }

  cambiarTipoElementoDetalle(): void {
    this.detalleTemporal.idElementoCatalogo = 0;
    this.detalleTemporal.precioUnitario = 0;
    this.detalleTemporal.observacion = '';
    this.cdr.detectChanges();
  }

  seleccionarElementoDetalle(): void {
    const elemento = this.obtenerElementoSeleccionadoDetalle();

    if (elemento?.precioReferencial !== null && elemento?.precioReferencial !== undefined) {
      this.detalleTemporal.precioUnitario = Number(elemento.precioReferencial);
    } else {
      this.detalleTemporal.precioUnitario = 0;
    }

    this.cdr.detectChanges();
  }

  async agregarDetalle(): Promise<void> {
    this.mensaje = '';
    this.error = '';

    if (!this.detalleTemporal.idElementoCatalogo || this.detalleTemporal.idElementoCatalogo <= 0) {
      this.error = 'Selecciona un producto o servicio para el detalle.';
      return;
    }

    const elementoSeleccionado = this.obtenerElementoSeleccionadoDetalle();

    if (!elementoSeleccionado) {
      this.error = 'No se encontró el producto o servicio seleccionado.';
      return;
    }

    if (!this.detalleTemporal.cantidad || this.detalleTemporal.cantidad <= 0) {
      this.error = 'La cantidad del detalle debe ser mayor que cero.';
      return;
    }

    const precioUnitario = Number(
      this.detalleTemporal.precioUnitario ||
      elementoSeleccionado.precioReferencial ||
      0
    );

    if (precioUnitario <= 0) {
      this.error = 'El producto o servicio seleccionado debe tener un precio mayor que cero.';
      return;
    }

    const yaExiste = this.cotizacion.detalles.some(
      (detalle) => detalle.idElementoCatalogo === this.detalleTemporal.idElementoCatalogo
    );

    if (yaExiste) {
      const confirmar = (await this.mensajes.confirmar('Este producto o servicio ya fue agregado. ¿Deseas agregarlo nuevamente como otra línea?', {"titulo":"Producto o servicio repetido","aceptar":"Agregar otra línea","tipo":"info","icono":"plus"}));

      if (!confirmar) {
        return;
      }
    }

    this.cotizacion.detalles.push({
      idElementoCatalogo: Number(this.detalleTemporal.idElementoCatalogo),
      cantidad: Number(this.detalleTemporal.cantidad),
      precioUnitario,
      observacion: this.normalizarTexto(this.detalleTemporal.observacion)
    });

    this.detalleTemporal = this.nuevoDetalle();
    this.cdr.detectChanges();
  }

  quitarDetalle(index: number): void {
    this.cotizacion.detalles.splice(index, 1);

    if (this.cotizacion.descuento > this.calcularSubtotalGeneral()) {
      this.cotizacion.descuento = this.calcularSubtotalGeneral();
    }

    this.cdr.detectChanges();
  }

  limpiarFormulario(): void {
    this.buscarCliente = '';
    this.buscarElemento = '';
    this.cotizacion = this.nuevaCotizacion();
    this.detalleTemporal = this.nuevoDetalle();
    this.tipoElementoDetalle = 'Producto';
    this.establecerValoresIniciales();

    this.mensaje = '';
    this.error = '';

    this.cdr.detectChanges();
  }

  buscarCotizaciones(): void {
    this.pagina = 1;
    this.cargarCotizaciones();
  }

  limpiarFiltros(): void {
    this.buscar = '';
    this.estadoFiltro = '';
    this.origenFiltro = '';
    this.pagina = 1;
    this.cargarCotizaciones();
  }

  paginaAnterior(): void {
    if (this.pagina <= 1) {
      return;
    }

    this.pagina--;
    this.cargarCotizaciones();
  }

  paginaSiguiente(): void {
    if (this.pagina >= this.totalPaginas) {
      return;
    }

    this.pagina++;
    this.cargarCotizaciones();
  }

  cambiarTamanioPagina(): void {
    this.pagina = 1;
    this.cargarCotizaciones();
  }

  clientesFiltrados(): ClienteSelector[] {
    const search = this.normalizar(this.buscarCliente);
    return this.clientes.filter(item => this.normalizar(item.textoMostrar + ' ' + item.cliente + ' ' + item.numeroDocumento).includes(search));
  }

  clienteSeleccionado(): ClienteSelector | undefined {
    return this.clientes.find(item => item.idCliente === this.cotizacion.idCliente);
  }

  elementosCotizablesFiltrados(): ElementoCotizable[] {
    return this.elementosCotizables.filter((item) =>
      this.normalizar(item.tipoElemento) === this.normalizar(this.tipoElementoDetalle) &&
      this.normalizar(item.nombre + ' ' + item.textoMostrar).includes(this.normalizar(this.buscarElemento))
    );
  }

  obtenerElementoSeleccionadoDetalle(): ElementoCotizable | undefined {
    return this.elementosCotizables.find(
      (item) => item.idElementoCatalogo === Number(this.detalleTemporal.idElementoCatalogo)
    );
  }

  obtenerPrecioReferencialDetalle(): number {
    const elemento = this.obtenerElementoSeleccionadoDetalle();

    if (!elemento || elemento.precioReferencial === null || elemento.precioReferencial === undefined) {
      return 0;
    }

    return Number(elemento.precioReferencial);
  }

  obtenerNombreElemento(idElementoCatalogo: number): string {
    const elemento = this.elementosCotizables.find(
      (item) => item.idElementoCatalogo === Number(idElementoCatalogo)
    );

    return elemento ? elemento.nombre : 'Elemento no encontrado';
  }

  obtenerTipoElemento(idElementoCatalogo: number): string {
    const elemento = this.elementosCotizables.find(
      (item) => item.idElementoCatalogo === Number(idElementoCatalogo)
    );

    return elemento ? elemento.tipoElemento : 'Elemento';
  }

  calcularSubtotal(detalle: CotizacionDetalleCrear): number {
    return Number(detalle.cantidad || 0) * Number(detalle.precioUnitario || 0);
  }

  calcularSubtotalTemporal(): number {
    return Number(this.detalleTemporal.cantidad || 0) * Number(this.detalleTemporal.precioUnitario || 0);
  }

  calcularSubtotalGeneral(): number {
    return this.cotizacion.detalles.reduce((total, detalle) => {
      return total + this.calcularSubtotal(detalle);
    }, 0);
  }

  calcularTotal(): number {
    const subtotalConIgv = this.calcularSubtotalGeneral();
    const descuento = Number(this.cotizacion.descuento || 0);

    return Math.max(subtotalConIgv - descuento, 0);
  }

  calcularValorVenta(): number {
    return this.calcularTotal() / 1.18;
  }

  calcularIgv(): number {
    return this.calcularTotal() - this.calcularValorVenta();
  }

  obtenerCodigoCotizacion(item: CotizacionListado): string {
    if (item.codigoCotizacion && item.codigoCotizacion.trim().length > 0) {
      return item.codigoCotizacion;
    }

    return `COT-${this.formatearCodigo(item.idCotizacion)}`;
  }

  formatearCodigo(id: number): string {
    return String(id).padStart(5, '0');
  }

  obtenerClaseEstado(estado: string): string {
    const valor = this.normalizar(estado);

    if (valor.includes('pendiente')) {
      return 'estado-pendiente';
    }

    if (valor.includes('respondida')) {
      return 'estado-respondida';
    }

    if (valor.includes('aprobada')) {
      return 'estado-aprobada';
    }

    if (valor.includes('convertida')) {
      return 'estado-convertida';
    }

    if (valor.includes('cancelada')) {
      return 'estado-cancelada';
    }

    return 'estado-general';
  }

  obtenerClaseOrigen(origen: string): string {
    const valor = this.normalizar(origen);

    if (valor === 'web') {
      return 'origen-web';
    }

    if (valor === 'whatsapp') {
      return 'origen-whatsapp';
    }

    if (valor === 'correo') {
      return 'origen-correo';
    }

    return 'origen-manual';
  }

  obtenerTextoPdf(item: CotizacionListado): string {
    return item.pdfGenerado || !!item.archivoPdf ? 'PDF generado' : 'PDF pendiente';
  }

  obtenerTextoCorreo(item: CotizacionListado): string {
    return item.correoEnviado ? 'Correo enviado' : 'Correo pendiente';
  }

  obtenerTextoWhatsApp(item: CotizacionListado): string {
    return item.whatsappEnviado ? 'WhatsApp enviado' : 'WhatsApp pendiente';
  }

  obtenerTextoBotonCorreo(item: CotizacionListado): string {
    return item.correoEnviado ? 'Reenviar correo' : 'Correo';
  }

  obtenerTextoBotonWhatsApp(item: CotizacionListado): string {
    return item.whatsappEnviado ? 'Reenviar WhatsApp' : 'WhatsApp';
  }

  esEstado(estadoActual: string, estadoBuscado: string): boolean {
    return this.normalizar(estadoActual).includes(this.normalizar(estadoBuscado));
  }

  puedeGenerarPdf(item: CotizacionListado): boolean {
    if (!this.permisos.tienePermiso('COTIZACIONES_VER')) return false;
    return !this.esEstado(item.estadoCotizacion, 'Convertida en venta') &&
      !this.esEstado(item.estadoCotizacion, 'Cancelada');
  }

  puedeEnviarCorreo(item: CotizacionListado): boolean {
    if (!this.permisos.tienePermiso('COTIZACIONES_ENVIAR_CORREO')) return false;
    return this.esEstado(item.estadoCotizacion, 'Pendiente') ||
      this.esEstado(item.estadoCotizacion, 'Respondida');
  }

  puedeEnviarWhatsApp(item: CotizacionListado): boolean {
    if (!this.permisos.tienePermiso('COTIZACIONES_ENVIAR_WHATSAPP')) return false;
    return this.esEstado(item.estadoCotizacion, 'Pendiente') ||
      this.esEstado(item.estadoCotizacion, 'Respondida');
  }

  puedeAprobar(item: CotizacionListado): boolean {
    if (!this.permisos.tienePermiso('COTIZACIONES_APROBAR')) return false;
    return this.esEstado(item.estadoCotizacion, 'Respondida');
  }

  puedeCancelar(item: CotizacionListado): boolean {
    if (!this.permisos.tienePermiso('COTIZACIONES_CANCELAR')) return false;
    if (!this.sesionAnulacion.esAdministrador()) return false;
    return this.esEstado(item.estadoCotizacion, 'Pendiente') ||
      this.esEstado(item.estadoCotizacion, 'Respondida') ||
      this.esEstado(item.estadoCotizacion, 'Aprobada');
  }

  puedeConvertirEnVenta(item: CotizacionListado): boolean {
    if (!this.permisos.tienePermiso('COTIZACIONES_CONVERTIR_VENTA') || !this.permisos.tienePermiso('VENTAS_CREAR') || !this.permisos.tienePermiso('VENTAS_VER')) return false;
    if (item.puedeConvertirVenta) {
      return true;
    }

    const yaTieneVenta = !!item.idVentaAsociada || !!item.idVentaGenerada;

    return this.esEstado(item.estadoCotizacion, 'Aprobada') && !yaTieneVenta;
  }

  puedeVerVenta(item: CotizacionListado): boolean {
    if (!this.permisos.tienePermiso('VENTAS_VER')) return false;
    return this.esEstado(item.estadoCotizacion, 'Convertida') ||
      !!item.idVentaAsociada ||
      !!item.idVentaGenerada;
  }

  estaProcesando(item: CotizacionListado, accion?: string): boolean {
    if (!this.procesandoAccion || this.idAccionActual !== item.idCotizacion) {
      return false;
    }

    if (!accion) {
      return true;
    }

    return this.accionActual === accion;
  }

  trackByCotizacion(_: number, item: CotizacionListado): number {
    return item.idCotizacion;
  }

  trackByDetalle(index: number): number {
    return index;
  }

  private validarCotizacion(): string {
    this.cotizacion.origenCotizacion = this.cotizacion.origenCotizacion || 'Manual';
    this.cotizacion.descuento = Number(this.cotizacion.descuento || 0);
    this.cotizacion.observacion = this.normalizarTexto(this.cotizacion.observacion);

    if (!this.cotizacion.idCliente || this.cotizacion.idCliente <= 0) {
      return 'Selecciona un cliente válido.';
    }

    if (!this.cotizacion.origenCotizacion) {
      return 'Selecciona el origen de la cotización.';
    }

    if (this.cotizacion.detalles.length === 0) {
      return 'Agrega al menos un producto o servicio a la cotización.';
    }

    if (this.cotizacion.descuento < 0) {
      return 'El descuento no puede ser negativo.';
    }

    if (this.cotizacion.descuento > this.calcularSubtotalGeneral()) {
      return 'El descuento no puede ser mayor al subtotal con IGV.';
    }

    for (const detalle of this.cotizacion.detalles) {
      if (!detalle.idElementoCatalogo || detalle.idElementoCatalogo <= 0) {
        return 'Existe un detalle sin producto o servicio seleccionado.';
      }

      if (!detalle.cantidad || detalle.cantidad <= 0) {
        return 'La cantidad debe ser mayor que cero.';
      }

      if (!detalle.precioUnitario || detalle.precioUnitario <= 0) {
        return 'El precio unitario debe ser mayor que cero.';
      }
    }

    return '';
  }

  private establecerValoresIniciales(): void {


    if (!this.cotizacion.origenCotizacion) {
      this.cotizacion.origenCotizacion = 'Manual';
    }
  }

  private nuevaCotizacion(): CotizacionCrear {
    return {
      idCliente: 0,
      idUsuarioRegistro: 1,
      origenCotizacion: 'Manual',
      descuento: 0,
      observacion: '',
      detalles: []
    };
  }

  private nuevoDetalle(): CotizacionDetalleCrear {
    return {
      idElementoCatalogo: 0,
      cantidad: 1,
      precioUnitario: 0,
      observacion: ''
    };
  }

  private normalizarTexto(valor?: string | null): string | null {
    if (!valor || valor.trim().length === 0) {
      return null;
    }

    return valor.trim();
  }

  private normalizar(valor?: string | null): string {
    return (valor ?? '')
      .toString()
      .trim()
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '');
  }

  private normalizarEstados(estados: EstadoCotizacion[]): EstadoCotizacion[] {
    const orden = [
      'pendiente',
      'respondida',
      'aprobada',
      'cancelada',
      'convertida en venta'
    ];

    return estados
      .filter((estado) => {
        const nombre = this.normalizar(estado.nombre);

        return orden.some((item) => nombre === item);
      })
      .sort((a, b) => {
        const indexA = orden.indexOf(this.normalizar(a.nombre));
        const indexB = orden.indexOf(this.normalizar(b.nombre));

        return indexA - indexB;
      });
  }

  private iniciarAccion(idCotizacion: number, accion: string): void {
    this.procesandoAccion = true;
    this.idAccionActual = idCotizacion;
    this.accionActual = accion;
  }

  private finalizarAccion(): void {
    this.procesandoAccion = false;
    this.idAccionActual = null;
    this.accionActual = '';
  }

  private obtenerMensajeError(err: any, mensajeDefecto: string): string {
    return err?.error?.mensaje ??
      err?.error?.message ??
      err?.message ??
      mensajeDefecto;
  }

  private construirUrlWebWhatsApp(resultado: CotizacionWhatsApp): string {
    const telefono = this.limpiarTelefonoWhatsApp(resultado.telefono);

    if (!telefono) {
      return '';
    }

    const mensaje = resultado.mensaje || '';

    return `https://web.whatsapp.com/send?phone=${telefono}&text=${encodeURIComponent(mensaje)}`;
  }

  private limpiarTelefonoWhatsApp(telefono?: string | null): string {
    if (!telefono) {
      return '';
    }

    let limpio = telefono.replace(/\D/g, '');

    if (limpio.length === 9) {
      limpio = `51${limpio}`;
    }

    return limpio;
  }

  private abrirVentanaPreparacion(titulo: string): Window | null {
    const ventana = window.open('about:blank', '_blank');

    if (!ventana) {
      return null;
    }

    this.escribirVentanaPreparacion(ventana, titulo);

    return ventana;
  }

  private escribirVentanaPreparacion(ventana: Window, titulo: string): void {
    try {
      ventana.document.open();
      ventana.document.write(`
        <!doctype html>
        <html lang="es">
          <head>
            <meta charset="utf-8">
            <title>${titulo}</title>
            <style>
              body {
                margin: 0;
                min-height: 100vh;
                display: flex;
                align-items: center;
                justify-content: center;
                font-family: Arial, sans-serif;
                background: #f4f7fa;
                color: #0f172a;
              }

              .card {
                padding: 24px 28px;
                border-radius: 18px;
                background: #ffffff;
                box-shadow: 0 16px 42px rgba(15, 23, 42, 0.16);
                text-align: center;
                border: 1px solid #e2e8f0;
              }

              .spinner {
                width: 34px;
                height: 34px;
                margin: 0 auto 14px;
                border-radius: 999px;
                border: 4px solid #e2e8f0;
                border-top-color: #d81920;
                animation: spin 0.8s linear infinite;
              }

              h1 {
                margin: 0;
                font-size: 18px;
              }

              p {
                margin: 8px 0 0;
                color: #64748b;
                font-size: 13px;
              }

              @keyframes spin {
                to {
                  transform: rotate(360deg);
                }
              }
            </style>
          </head>
          <body>
            <div class="card">
              <div class="spinner"></div>
              <h1>${titulo}</h1>
              <p>Espera un momento...</p>
            </div>
          </body>
        </html>
      `);
      ventana.document.close();
    } catch {
      // Si el navegador no permite escribir en la ventana, simplemente se redirigirá luego.
    }
  }

  private redirigirVentana(ventana: Window | null, url: string): void {
    if (!url) {
      this.cerrarVentanaPreparacion(ventana);
      return;
    }

    if (ventana && !ventana.closed) {
      ventana.location.href = url;
      return;
    }

    window.open(url, '_blank');
  }

  private cerrarVentanaPreparacion(ventana: Window | null): void {
    if (ventana && !ventana.closed) {
      ventana.close();
    }
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