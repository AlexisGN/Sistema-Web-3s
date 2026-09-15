import { CommonModule } from '@angular/common';
import { Component, ChangeDetectorRef, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';

import { ClienteService } from '../../core/services/cliente';
import {
  ClienteActualizar,
  ClienteContacto,
  ClienteCrear,
  ClienteListado,
  ConsultaDniResultado,
  ConsultaRucResultado,
  TipoCliente,
  TipoDocumento,
  Ubigeo
} from '../../core/models/cliente.model';

@Component({
  selector: 'app-clientes',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './clientes.html',
  styleUrl: './clientes.scss'
})
export class ClientesComponent implements OnInit {
  clientes: ClienteListado[] = [];

  tiposCliente: TipoCliente[] = [];
  tiposDocumento: TipoDocumento[] = [];

  ubigeos: Ubigeo[] = [];
  ubigeoBuscar = '';
  ubicacionSeleccionada = '';

  cargando = false;
  cargandoCombos = false;
  guardando = false;
  consultandoDocumento = false;

  buscar = '';
  mensaje = '';
  error = '';

  erroresCampo: Record<string, string> = {};

  pagina = 1;
  tamanioPagina = 5;
  totalRegistros = 0;
  totalPaginas = 0;
  opcionesTamanioPagina = [5, 10, 20];

  editando = false;
  idClienteEditando: number | null = null;

  cliente: ClienteCrear = this.nuevoCliente();

  constructor(
    private clienteService: ClienteService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.subirArriba();
    this.cargarCombos();
    this.cargarClientes();
  }

  cargarCombos(): void {
    this.cargandoCombos = true;
    this.error = '';

    forkJoin({
      tiposCliente: this.clienteService.listarTiposCliente(),
      tiposDocumento: this.clienteService.listarTiposDocumento()
    }).subscribe({
      next: (data) => {
        this.tiposCliente = data.tiposCliente;
        this.tiposDocumento = data.tiposDocumento;

        this.establecerValoresIniciales();

        this.cargandoCombos = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.error = 'No se pudieron cargar los datos iniciales de clientes.';
        this.cargandoCombos = false;
        this.cdr.detectChanges();
      }
    });
  }

  cargarClientes(): void {
    this.cargando = true;
    this.error = '';

    this.clienteService.listar(this.buscar, this.pagina, this.tamanioPagina).subscribe({
      next: (data) => {
        this.clientes = data.items;
        this.pagina = data.pagina;
        this.tamanioPagina = data.tamanioPagina;
        this.totalRegistros = data.totalRegistros;
        this.totalPaginas = data.totalPaginas ?? Math.ceil(data.totalRegistros / data.tamanioPagina);

        if (this.clientes.length === 0 && this.totalRegistros > 0 && this.pagina > 1) {
          this.pagina--;
          this.cargando = false;
          this.cargarClientes();
          return;
        }

        this.cargando = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.error = 'No se pudieron cargar los clientes.';
        this.cargando = false;
        this.cdr.detectChanges();
      }
    });
  }

  guardarCliente(): void {
    this.mensaje = '';
    this.error = '';
    this.erroresCampo = {};

    const errorValidacion = this.validarFormulario();

    if (errorValidacion) {
      this.error = '';
      this.cdr.detectChanges();
      return;
    }

    if (this.editando) {
      this.actualizarCliente();
    } else {
      this.registrarCliente();
    }
  }

  registrarCliente(): void {
    this.guardando = true;

    const payload = this.prepararPayloadCrear();

    this.clienteService.crear(payload).subscribe({
      next: () => {
        this.mensaje = 'Cliente registrado correctamente.';
        this.guardando = false;
        this.limpiarFormulario();
        this.pagina = 1;
        this.cargarClientes();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.error = err.error?.mensaje ?? 'No se pudo registrar el cliente.';
        this.guardando = false;
        this.cdr.detectChanges();
      }
    });
  }

  actualizarCliente(): void {
    if (this.idClienteEditando === null) {
      this.error = 'No se encontró el cliente a editar.';
      return;
    }

    this.guardando = true;

    const payload = this.prepararPayloadActualizar();

    this.clienteService.actualizar(this.idClienteEditando, payload).subscribe({
      next: (data) => {
        this.mensaje = data.mensaje || 'Cliente actualizado correctamente.';
        this.guardando = false;
        this.limpiarFormulario();
        this.cargarClientes();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.error = err.error?.mensaje ?? 'No se pudo actualizar el cliente.';
        this.guardando = false;
        this.cdr.detectChanges();
      }
    });
  }

  editarCliente(item: ClienteListado): void {
    this.mensaje = '';
    this.error = '';
    this.erroresCampo = {};

    this.clienteService.obtenerPorId(item.idCliente).subscribe({
      next: (cliente) => {
        this.editando = true;
        this.idClienteEditando = cliente.idCliente;

        this.cliente = {
          idTipoCliente: cliente.idTipoCliente,
          idTipoDocumento: cliente.idTipoDocumento,
          idUbigeo: cliente.idUbigeo ?? null,

          numeroDocumento: cliente.numeroDocumento,

          correo: cliente.correo ?? '',
          telefono: cliente.telefono ?? '',
          direccion: cliente.direccion ?? '',

          nombres: cliente.nombres ?? '',
          apellidoPaterno: cliente.apellidoPaterno ?? '',
          apellidoMaterno: cliente.apellidoMaterno ?? '',

          razonSocial: cliente.razonSocial ?? '',
          nombreComercial: cliente.nombreComercial ?? '',

          registrarContactoPrincipal: cliente.tieneContactoPrincipal,
          contactoPrincipal: cliente.tieneContactoPrincipal
            ? {
              idContactoCliente: cliente.contactoPrincipal?.idContactoCliente ?? null,
              nombres: cliente.contactoPrincipal?.nombres ?? '',
              apellidoPaterno: cliente.contactoPrincipal?.apellidoPaterno ?? '',
              apellidoMaterno: cliente.contactoPrincipal?.apellidoMaterno ?? '',
              cargo: cliente.contactoPrincipal?.cargo ?? '',
              correo: cliente.contactoPrincipal?.correo ?? '',
              telefono: cliente.contactoPrincipal?.telefono ?? '',
              estado: true
            }
            : this.nuevoContacto()
        };

        this.ubicacionSeleccionada = cliente.ubicacion ?? '';
        this.ubigeoBuscar = cliente.ubicacion ?? '';
        this.ubigeos = [];

        window.scrollTo({
          top: 0,
          left: 0,
          behavior: 'smooth'
        });

        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.error = 'No se pudo cargar el cliente seleccionado.';
        this.cdr.detectChanges();
      }
    });
  }

  eliminarCliente(item: ClienteListado): void {
    const confirmar = confirm(`¿Deseas desactivar al cliente ${item.cliente}?`);

    if (!confirmar) {
      return;
    }

    this.clienteService.eliminar(item.idCliente).subscribe({
      next: (data) => {
        this.mensaje = data.mensaje || 'Cliente desactivado correctamente.';
        this.cargarClientes();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.error = err.error?.mensaje ?? 'No se pudo desactivar el cliente.';
        this.cdr.detectChanges();
      }
    });
  }

  buscarClientes(): void {
    this.pagina = 1;
    this.cargarClientes();
  }

  paginaAnterior(): void {
    if (this.pagina <= 1) {
      return;
    }

    this.pagina--;
    this.cargarClientes();
  }

  paginaSiguiente(): void {
    if (this.pagina >= this.totalPaginas) {
      return;
    }

    this.pagina++;
    this.cargarClientes();
  }

  cambiarTamanioPagina(): void {
    this.pagina = 1;
    this.cargarClientes();
  }

  cambiarTipoCliente(): void {
    this.cliente.numeroDocumento = '';
    this.mensaje = '';
    this.error = '';
    this.erroresCampo = {};

    if (this.esEmpresa()) {
      this.cliente.idTipoDocumento = this.obtenerIdTipoDocumentoPorNombre('RUC');

      this.cliente.nombres = '';
      this.cliente.apellidoPaterno = '';
      this.cliente.apellidoMaterno = '';

      if (!this.cliente.contactoPrincipal) {
        this.cliente.contactoPrincipal = this.nuevoContacto();
      }
    } else {
      this.cliente.idTipoDocumento = this.obtenerIdTipoDocumentoPorNombre('DNI');

      this.cliente.razonSocial = '';
      this.cliente.nombreComercial = '';
      this.cliente.registrarContactoPrincipal = false;
      this.cliente.contactoPrincipal = this.nuevoContacto();
    }

    this.cdr.detectChanges();
  }

  cambiarRegistroContacto(): void {
    if (this.cliente.registrarContactoPrincipal && !this.cliente.contactoPrincipal) {
      this.cliente.contactoPrincipal = this.nuevoContacto();
    }

    this.cdr.detectChanges();
  }

  esPersonaNatural(): boolean {
    const tipo = this.obtenerTipoClienteSeleccionado();

    return tipo?.nombre.toLowerCase() === 'persona natural';
  }

  esEmpresa(): boolean {
    const tipo = this.obtenerTipoClienteSeleccionado();

    return tipo?.nombre.toLowerCase() === 'empresa';
  }

  obtenerTipoClienteSeleccionado(): TipoCliente | undefined {
    return this.tiposCliente.find(
      (tipo) => tipo.idTipoCliente === Number(this.cliente.idTipoCliente)
    );
  }

  obtenerDocumentoSeleccionado(): TipoDocumento | undefined {
    return this.tiposDocumento.find(
      (doc) => doc.idTipoDocumento === Number(this.cliente.idTipoDocumento)
    );
  }

  obtenerIdTipoClientePorNombre(nombre: string): number {
    const tipo = this.tiposCliente.find(
      (item) => item.nombre.toLowerCase() === nombre.toLowerCase()
    );

    return tipo?.idTipoCliente ?? 0;
  }

  obtenerIdTipoDocumentoPorNombre(nombre: string): number {
    const tipo = this.tiposDocumento.find(
      (item) => item.nombre.toLowerCase() === nombre.toLowerCase()
    );

    return tipo?.idTipoDocumento ?? 0;
  }

  consultarDocumento(): void {
    this.mensaje = '';
    this.error = '';

    if (this.esPersonaNatural()) {
      this.consultarDni();
      return;
    }

    if (this.esEmpresa()) {
      this.consultarRuc();
      return;
    }

    this.error = 'Selecciona el tipo de cliente antes de consultar.';
  }

  consultarDni(): void {
    const dni = this.normalizarDocumento(this.cliente.numeroDocumento);
    this.cliente.numeroDocumento = dni.slice(0, 8);

    if (dni.length !== 8) {
      this.marcarErrorCampo('numeroDocumento', 'El DNI debe tener exactamente 8 dígitos.');
      this.error = '';
      this.cdr.detectChanges();
      return;
    }

    this.limpiarErrorCampo('numeroDocumento');
    this.consultandoDocumento = true;

    this.clienteService.consultarDni(dni).subscribe({
      next: (data) => {
        this.aplicarDatosDni(data);
        this.consultandoDocumento = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.error = err.error?.mensaje ?? 'No se pudo consultar RENIEC.';
        this.consultandoDocumento = false;
        this.cdr.detectChanges();
      }
    });
  }

  consultarRuc(): void {
    const ruc = this.normalizarDocumento(this.cliente.numeroDocumento);
    this.cliente.numeroDocumento = ruc.slice(0, 11);

    if (ruc.length !== 11) {
      this.marcarErrorCampo('numeroDocumento', 'El RUC debe tener exactamente 11 dígitos.');
      this.error = '';
      this.cdr.detectChanges();
      return;
    }

    this.limpiarErrorCampo('numeroDocumento');
    this.consultandoDocumento = true;

    this.clienteService.consultarRuc(ruc).subscribe({
      next: (data) => {
        this.aplicarDatosRuc(data);
        this.consultandoDocumento = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.error = err.error?.mensaje ?? 'No se pudo consultar SUNAT.';
        this.consultandoDocumento = false;
        this.cdr.detectChanges();
      }
    });
  }

  buscarUbigeos(): void {
    this.error = '';
    this.ubigeos = [];

    if (this.cliente.idUbigeo && this.ubigeoBuscar === this.ubicacionSeleccionada) {
      return;
    }

    if (this.cliente.idUbigeo && this.ubigeoBuscar !== this.ubicacionSeleccionada) {
      this.cliente.idUbigeo = null;
      this.ubicacionSeleccionada = '';
    }

    if (!this.ubigeoBuscar || this.ubigeoBuscar.trim().length < 2) {
      return;
    }

    this.clienteService.listarUbigeos(this.ubigeoBuscar).subscribe({
      next: (data) => {
        this.ubigeos = data;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.error = 'No se pudieron buscar ubicaciones.';
        this.cdr.detectChanges();
      }
    });
  }

  seleccionarUbigeo(ubigeo: Ubigeo): void {
  this.cliente.idUbigeo = ubigeo.idUbigeo;
  this.ubicacionSeleccionada = ubigeo.ubicacion;
  this.ubigeoBuscar = ubigeo.ubicacion;
  this.ubigeos = [];
  this.limpiarErrorCampo('idUbigeo');
  this.cdr.detectChanges();
}

  limpiarUbigeo(): void {
    this.cliente.idUbigeo = null;
    this.ubicacionSeleccionada = '';
    this.ubigeoBuscar = '';
    this.ubigeos = [];
    this.cdr.detectChanges();
  }

  limpiarFormulario(): void {
    this.cliente = this.nuevoCliente();
    this.editando = false;
    this.idClienteEditando = null;
    this.mensaje = '';
    this.error = '';
    this.erroresCampo = {};

    this.ubigeoBuscar = '';
    this.ubicacionSeleccionada = '';
    this.ubigeos = [];

    this.establecerValoresIniciales();

    this.cdr.detectChanges();
  }

  private aplicarDatosDni(data: ConsultaDniResultado): void {
    this.mensaje = data.mensaje || 'Datos RENIEC cargados correctamente.';

    this.cliente.numeroDocumento = data.numeroDocumento ?? this.cliente.numeroDocumento;
    this.cliente.nombres = data.nombres ?? '';
    this.cliente.apellidoPaterno = data.apellidoPaterno ?? '';
    this.cliente.apellidoMaterno = data.apellidoMaterno ?? '';

    this.cliente.razonSocial = '';
    this.cliente.nombreComercial = '';

    this.limpiarErrorCampo('numeroDocumento');
    this.limpiarErrorCampo('nombres');
    this.limpiarErrorCampo('apellidoPaterno');
    this.limpiarErrorCampo('apellidoMaterno');

    if (data.clienteYaExiste && data.idClienteExistente) {
      this.buscar = data.numeroDocumento;
      this.pagina = 1;
      this.cargarClientes();
    }
  }

  private aplicarDatosRuc(data: ConsultaRucResultado): void {
    this.mensaje = data.mensaje || 'Datos SUNAT cargados correctamente.';

    this.cliente.numeroDocumento = data.numeroDocumento ?? this.cliente.numeroDocumento;
    this.cliente.razonSocial = data.razonSocial ?? '';
    this.cliente.nombreComercial = data.nombreComercial ?? '';
    this.cliente.direccion = data.direccion ?? this.cliente.direccion;

    this.cliente.nombres = '';
    this.cliente.apellidoPaterno = '';
    this.cliente.apellidoMaterno = '';

    if (data.idUbigeo && data.ubicacion) {
      this.cliente.idUbigeo = data.idUbigeo;
      this.ubicacionSeleccionada = data.ubicacion;
      this.ubigeoBuscar = data.ubicacion;
      this.ubigeos = [];
    }

    this.limpiarErrorCampo('numeroDocumento');
    this.limpiarErrorCampo('razonSocial');

    if (data.clienteYaExiste && data.idClienteExistente) {
      this.buscar = data.numeroDocumento;
      this.pagina = 1;
      this.cargarClientes();
    }
  }

  private establecerValoresIniciales(): void {
    if (this.cliente.idTipoCliente === 0) {
      const personaNatural = this.tiposCliente.find(
        (tipo) => tipo.nombre.toLowerCase() === 'persona natural'
      );

      this.cliente.idTipoCliente =
        personaNatural?.idTipoCliente ??
        this.tiposCliente[0]?.idTipoCliente ??
        0;
    }

    if (this.cliente.idTipoDocumento === 0) {
      if (this.esEmpresa()) {
        this.cliente.idTipoDocumento = this.obtenerIdTipoDocumentoPorNombre('RUC');
      } else {
        this.cliente.idTipoDocumento = this.obtenerIdTipoDocumentoPorNombre('DNI');
      }
    }
  }

  private validarFormulario(): string | null {
    if (!this.cliente.idTipoCliente || this.cliente.idTipoCliente <= 0) {
      return this.marcarErrorCampo('idTipoCliente', 'Selecciona el tipo de cliente.');
    }

    if (!this.cliente.idTipoDocumento || this.cliente.idTipoDocumento <= 0) {
      return this.marcarErrorCampo('idTipoDocumento', 'Selecciona el tipo de documento.');
    }

    const errorDocumento = this.validarDocumento();

    if (errorDocumento) {
      return errorDocumento;
    }

    if (!this.cliente.idUbigeo || this.cliente.idUbigeo <= 0) {
      return this.marcarErrorCampo('idUbigeo', 'Selecciona la ubicación del cliente.');
    }

    if (this.esPersonaNatural()) {
      if (!this.cliente.nombres || this.cliente.nombres.trim().length === 0) {
        return this.marcarErrorCampo('nombres', 'Ingresa los nombres del cliente.');
      }

      if (!this.esTextoSoloLetras(this.cliente.nombres)) {
        return this.marcarErrorCampo('nombres', 'Los nombres solo deben contener letras y espacios.');
      }

      if (!this.cliente.apellidoPaterno || this.cliente.apellidoPaterno.trim().length === 0) {
        return this.marcarErrorCampo('apellidoPaterno', 'Ingresa el apellido paterno del cliente.');
      }

      if (!this.esTextoSoloLetras(this.cliente.apellidoPaterno)) {
        return this.marcarErrorCampo(
          'apellidoPaterno',
          'El apellido paterno solo debe contener letras y espacios.'
        );
      }

      if (!this.cliente.apellidoMaterno || this.cliente.apellidoMaterno.trim().length === 0) {
        return this.marcarErrorCampo('apellidoMaterno', 'Ingresa el apellido materno del cliente.');
      }

      if (!this.esTextoSoloLetras(this.cliente.apellidoMaterno)) {
        return this.marcarErrorCampo(
          'apellidoMaterno',
          'El apellido materno solo debe contener letras y espacios.'
        );
      }
    }

    if (this.esEmpresa()) {
      if (!this.cliente.razonSocial || this.cliente.razonSocial.trim().length === 0) {
        return this.marcarErrorCampo('razonSocial', 'Ingresa la razón social de la empresa.');
      }

      if (this.cliente.registrarContactoPrincipal) {
        const contacto = this.cliente.contactoPrincipal;

        if (!contacto) {
          return this.marcarErrorCampo('contactoNombres', 'Ingresa los datos del contacto principal.');
        }

        if (!contacto.nombres || contacto.nombres.trim().length === 0) {
          return this.marcarErrorCampo('contactoNombres', 'Ingresa los nombres del contacto.');
        }

        if (!this.esTextoSoloLetras(contacto.nombres)) {
          return this.marcarErrorCampo(
            'contactoNombres',
            'Los nombres del contacto solo deben contener letras y espacios.'
          );
        }

        if (!contacto.apellidoPaterno || contacto.apellidoPaterno.trim().length === 0) {
          return this.marcarErrorCampo(
            'contactoApellidoPaterno',
            'Ingresa el apellido paterno del contacto.'
          );
        }

        if (!this.esTextoSoloLetras(contacto.apellidoPaterno)) {
          return this.marcarErrorCampo(
            'contactoApellidoPaterno',
            'El apellido paterno del contacto solo debe contener letras y espacios.'
          );
        }

        if (
          contacto.apellidoMaterno &&
          contacto.apellidoMaterno.trim().length > 0 &&
          !this.esTextoSoloLetras(contacto.apellidoMaterno)
        ) {
          return this.marcarErrorCampo(
            'contactoApellidoMaterno',
            'El apellido materno del contacto solo debe contener letras y espacios.'
          );
        }

        if (contacto.correo && contacto.correo.trim().length > 0) {
          if (!this.esCorreoValido(contacto.correo)) {
            return this.marcarErrorCampo(
              'contactoCorreo',
              'Ingresa un correo válido. Ejemplo: contacto@empresa.com'
            );
          }
        }

        if (contacto.telefono && contacto.telefono.trim().length > 0) {
          if (!this.esTelefonoPeruValido(contacto.telefono)) {
            return this.marcarErrorCampo(
              'contactoTelefono',
              'Formato válido: +51 seguido de 9 dígitos. Ejemplo: +51999999999'
            );
          }
        }
      }
    }

    if (!this.cliente.correo || this.cliente.correo.trim().length === 0) {
      return this.marcarErrorCampo('correo', 'Ingresa el correo del cliente.');
    }

    if (!this.esCorreoValido(this.cliente.correo)) {
      return this.marcarErrorCampo(
        'correo',
        'Ingresa un correo válido. Ejemplo: cliente@empresa.com'
      );
    }

    if (!this.cliente.telefono || this.cliente.telefono.trim().length === 0) {
      return this.marcarErrorCampo('telefono', 'Ingresa el teléfono del cliente.');
    }

    if (!this.esTelefonoPeruValido(this.cliente.telefono)) {
      return this.marcarErrorCampo(
        'telefono',
        'Formato válido: +51 seguido de 9 dígitos. Ejemplo: +51999999999'
      );
    }

    if (!this.cliente.direccion || this.cliente.direccion.trim().length === 0) {
      return this.marcarErrorCampo('direccion', 'Ingresa la dirección del cliente.');
    }

    return null;
  }

  private prepararPayloadCrear(): ClienteCrear {
    return {
      idTipoCliente: Number(this.cliente.idTipoCliente),
      idTipoDocumento: Number(this.cliente.idTipoDocumento),
      idUbigeo: this.cliente.idUbigeo ? Number(this.cliente.idUbigeo) : null,

      numeroDocumento: this.normalizarDocumento(this.cliente.numeroDocumento),

      correo: this.normalizarTexto(this.cliente.correo),
      telefono: this.normalizarTexto(this.cliente.telefono),
      direccion: this.normalizarTexto(this.cliente.direccion),

      nombres: this.esPersonaNatural()
        ? this.normalizarTexto(this.cliente.nombres)
        : null,

      apellidoPaterno: this.esPersonaNatural()
        ? this.normalizarTexto(this.cliente.apellidoPaterno)
        : null,

      apellidoMaterno: this.esPersonaNatural()
        ? this.normalizarTexto(this.cliente.apellidoMaterno)
        : null,

      razonSocial: this.esEmpresa()
        ? this.normalizarTexto(this.cliente.razonSocial)
        : null,

      nombreComercial: this.esEmpresa()
        ? this.normalizarTexto(this.cliente.nombreComercial)
        : null,

      registrarContactoPrincipal: this.esEmpresa() && this.cliente.registrarContactoPrincipal,

      contactoPrincipal: this.esEmpresa() && this.cliente.registrarContactoPrincipal
        ? this.prepararContacto(this.cliente.contactoPrincipal)
        : null
    };
  }

  private prepararPayloadActualizar(): ClienteActualizar {
    return {
      idTipoCliente: Number(this.cliente.idTipoCliente),
      idTipoDocumento: Number(this.cliente.idTipoDocumento),
      idUbigeo: this.cliente.idUbigeo ? Number(this.cliente.idUbigeo) : null,

      numeroDocumento: this.normalizarDocumento(this.cliente.numeroDocumento),

      correo: this.normalizarTexto(this.cliente.correo),
      telefono: this.normalizarTexto(this.cliente.telefono),
      direccion: this.normalizarTexto(this.cliente.direccion),

      nombres: this.esPersonaNatural()
        ? this.normalizarTexto(this.cliente.nombres)
        : null,

      apellidoPaterno: this.esPersonaNatural()
        ? this.normalizarTexto(this.cliente.apellidoPaterno)
        : null,

      apellidoMaterno: this.esPersonaNatural()
        ? this.normalizarTexto(this.cliente.apellidoMaterno)
        : null,

      razonSocial: this.esEmpresa()
        ? this.normalizarTexto(this.cliente.razonSocial)
        : null,

      nombreComercial: this.esEmpresa()
        ? this.normalizarTexto(this.cliente.nombreComercial)
        : null,

      registrarContactoPrincipal: this.esEmpresa() && this.cliente.registrarContactoPrincipal,

      contactoPrincipal: this.esEmpresa() && this.cliente.registrarContactoPrincipal
        ? this.prepararContacto(this.cliente.contactoPrincipal)
        : null
    };
  }

  private prepararContacto(contacto?: ClienteContacto | null): ClienteContacto | null {
    if (!contacto) {
      return null;
    }

    return {
      idContactoCliente: contacto.idContactoCliente ?? null,
      nombres: this.normalizarTexto(contacto.nombres),
      apellidoPaterno: this.normalizarTexto(contacto.apellidoPaterno),
      apellidoMaterno: this.normalizarTexto(contacto.apellidoMaterno),
      cargo: this.normalizarTexto(contacto.cargo),
      correo: this.normalizarTexto(contacto.correo),
      telefono: this.normalizarTexto(contacto.telefono),
      estado: true
    };
  }

  private nuevoCliente(): ClienteCrear {
    return {
      idTipoCliente: 0,
      idTipoDocumento: 0,
      idUbigeo: null,

      numeroDocumento: '',

      correo: '',
      telefono: '',
      direccion: '',

      nombres: '',
      apellidoPaterno: '',
      apellidoMaterno: '',

      razonSocial: '',
      nombreComercial: '',

      registrarContactoPrincipal: false,
      contactoPrincipal: this.nuevoContacto()
    };
  }

  private nuevoContacto(): ClienteContacto {
    return {
      idContactoCliente: null,
      nombres: '',
      apellidoPaterno: '',
      apellidoMaterno: '',
      cargo: '',
      correo: '',
      telefono: '',
      estado: true
    };
  }

  private normalizarDocumento(valor?: string | null): string {
    if (!valor) {
      return '';
    }

    return valor.replace(/\D/g, '');
  }

  private normalizarTexto(valor?: string | null): string | null {
    if (!valor || valor.trim().length === 0) {
      return null;
    }

    return valor.trim();
  }

  filtrarDocumentoSoloNumeros(): void {
    const maximo = this.esEmpresa() ? 11 : 8;

    this.cliente.numeroDocumento = this.normalizarDocumento(this.cliente.numeroDocumento)
      .slice(0, maximo);

    this.limpiarErrorCampo('numeroDocumento');
    this.cdr.detectChanges();
  }

  private validarDocumento(): string | null {
    if (!this.cliente.numeroDocumento || this.cliente.numeroDocumento.trim().length === 0) {
      return this.marcarErrorCampo('numeroDocumento', 'Ingresa el número de documento.');
    }

    const documentoOriginal = this.cliente.numeroDocumento.trim();

    if (!/^\d+$/.test(documentoOriginal)) {
      return this.marcarErrorCampo('numeroDocumento', 'El documento solo debe contener números.');
    }

    const documento = this.normalizarDocumento(documentoOriginal);

    if (this.esPersonaNatural() && documento.length !== 8) {
      return this.marcarErrorCampo('numeroDocumento', 'El DNI debe tener exactamente 8 dígitos.');
    }

    if (this.esEmpresa() && documento.length !== 11) {
      return this.marcarErrorCampo('numeroDocumento', 'El RUC debe tener exactamente 11 dígitos.');
    }

    this.cliente.numeroDocumento = documento;
    return null;
  }

  private marcarErrorCampo(campo: string, mensaje: string): string {
    this.erroresCampo[campo] = mensaje;
    return mensaje;
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

  private obtenerMensajeErrorCampo(campo: string): string | null {
    switch (campo) {
      case 'numeroDocumento':
        return this.obtenerMensajeErrorDocumento();

      case 'idUbigeo':
        if (!this.cliente.idUbigeo || this.cliente.idUbigeo <= 0) {
          return 'Selecciona la ubicación del cliente.';
        }

        return null;

      case 'nombres':
        if (this.esPersonaNatural()) {
          if (!this.cliente.nombres || this.cliente.nombres.trim().length === 0) {
            return 'Ingresa los nombres del cliente.';
          }

          if (!this.esTextoSoloLetras(this.cliente.nombres)) {
            return 'Los nombres solo deben contener letras y espacios.';
          }
        }

        return null;

      case 'apellidoPaterno':
        if (this.esPersonaNatural()) {
          if (!this.cliente.apellidoPaterno || this.cliente.apellidoPaterno.trim().length === 0) {
            return 'Ingresa el apellido paterno del cliente.';
          }

          if (!this.esTextoSoloLetras(this.cliente.apellidoPaterno)) {
            return 'El apellido paterno solo debe contener letras y espacios.';
          }
        }

        return null;

      case 'apellidoMaterno':
        if (this.esPersonaNatural()) {
          if (!this.cliente.apellidoMaterno || this.cliente.apellidoMaterno.trim().length === 0) {
            return 'Ingresa el apellido materno del cliente.';
          }

          if (!this.esTextoSoloLetras(this.cliente.apellidoMaterno)) {
            return 'El apellido materno solo debe contener letras y espacios.';
          }
        }

        return null;

      case 'razonSocial':
        if (this.esEmpresa()) {
          if (!this.cliente.razonSocial || this.cliente.razonSocial.trim().length === 0) {
            return 'Ingresa la razón social de la empresa.';
          }
        }

        return null;

      case 'correo':
        if (!this.cliente.correo || this.cliente.correo.trim().length === 0) {
          return 'Ingresa el correo del cliente.';
        }

        if (!this.esCorreoValido(this.cliente.correo)) {
          return 'Ingresa un correo válido. Ejemplo: cliente@empresa.com';
        }

        return null;

      case 'telefono':
        if (!this.cliente.telefono || this.cliente.telefono.trim().length === 0) {
          return 'Ingresa el teléfono del cliente.';
        }

        if (!this.esTelefonoPeruValido(this.cliente.telefono)) {
          return 'Formato válido: +51 seguido de 9 dígitos. Ejemplo: +51999999999';
        }

        return null;

      case 'direccion':
        if (!this.cliente.direccion || this.cliente.direccion.trim().length === 0) {
          return 'Ingresa la dirección del cliente.';
        }

        return null;

      case 'contactoNombres': {
        const contacto = this.cliente.contactoPrincipal;

        if (this.esEmpresa() && this.cliente.registrarContactoPrincipal) {
          if (!contacto || !contacto.nombres || contacto.nombres.trim().length === 0) {
            return 'Ingresa los nombres del contacto.';
          }

          if (!this.esTextoSoloLetras(contacto.nombres)) {
            return 'Los nombres del contacto solo deben contener letras y espacios.';
          }
        }

        return null;
      }

      case 'contactoApellidoPaterno': {
        const contacto = this.cliente.contactoPrincipal;

        if (this.esEmpresa() && this.cliente.registrarContactoPrincipal) {
          if (!contacto || !contacto.apellidoPaterno || contacto.apellidoPaterno.trim().length === 0) {
            return 'Ingresa el apellido paterno del contacto.';
          }

          if (!this.esTextoSoloLetras(contacto.apellidoPaterno)) {
            return 'El apellido paterno del contacto solo debe contener letras y espacios.';
          }
        }

        return null;
      }

      case 'contactoApellidoMaterno': {
        const contacto = this.cliente.contactoPrincipal;

        if (
          contacto?.apellidoMaterno &&
          contacto.apellidoMaterno.trim().length > 0 &&
          !this.esTextoSoloLetras(contacto.apellidoMaterno)
        ) {
          return 'El apellido materno del contacto solo debe contener letras y espacios.';
        }

        return null;
      }

      case 'contactoCorreo': {
        const contacto = this.cliente.contactoPrincipal;

        if (contacto?.correo && contacto.correo.trim().length > 0) {
          if (!this.esCorreoValido(contacto.correo)) {
            return 'Ingresa un correo válido. Ejemplo: contacto@empresa.com';
          }
        }

        return null;
      }

      case 'contactoTelefono': {
        const contacto = this.cliente.contactoPrincipal;

        if (contacto?.telefono && contacto.telefono.trim().length > 0) {
          if (!this.esTelefonoPeruValido(contacto.telefono)) {
            return 'Formato válido: +51 seguido de 9 dígitos. Ejemplo: +51999999999';
          }
        }

        return null;
      }

      default:
        return null;
    }
  }

  private obtenerMensajeErrorDocumento(): string | null {
    if (!this.cliente.numeroDocumento || this.cliente.numeroDocumento.trim().length === 0) {
      return 'Ingresa el número de documento.';
    }

    const documento = this.normalizarDocumento(this.cliente.numeroDocumento);

    if (this.esPersonaNatural() && documento.length !== 8) {
      return 'El DNI debe tener exactamente 8 dígitos.';
    }

    if (this.esEmpresa() && documento.length !== 11) {
      return 'El RUC debe tener exactamente 11 dígitos.';
    }

    return null;
  }

  private esTextoSoloLetras(valor?: string | null): boolean {
    if (!valor || valor.trim().length === 0) {
      return false;
    }

    return /^[A-Za-zÁÉÍÓÚáéíóúÑñÜü\s]+$/.test(valor.trim());
  }

  private esCorreoValido(valor?: string | null): boolean {
    if (!valor || valor.trim().length === 0) {
      return false;
    }

    return /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(valor.trim());
  }

  private esTelefonoPeruValido(valor?: string | null): boolean {
    if (!valor || valor.trim().length === 0) {
      return false;
    }

    return /^\+51\d{9}$/.test(valor.trim());
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