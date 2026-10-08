import { inject } from '@angular/core';
import { SessionService } from '../../core/services/session.service';
import { TelefonoPeDirective } from '../../shared/telefono-pe/telefono-pe';
import { UiIconComponent } from '../../shared/ui-icon/ui-icon';
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { ProveedorService } from '../../core/services/proveedor';
import {
  ContactoProveedor,
  ProveedorActualizar,
  ProveedorCrear,
  ProveedorListado,
  Ubigeo
} from '../../core/models/proveedor.model';

@Component({
  selector: 'app-proveedores',
  standalone: true,
  imports: [TelefonoPeDirective, UiIconComponent, CommonModule, FormsModule],
  templateUrl: './proveedores.html',
  styleUrl: './proveedores.scss'
})
export class ProveedoresComponent implements OnInit {
  readonly permisos = inject(SessionService);
  proveedores: ProveedorListado[] = [];
  ubigeos: Ubigeo[] = [];

  cargando = false;
  guardando = false;
  consultandoRuc = false;
  buscandoUbigeo = false;

  buscar = '';
  ubigeoBuscar = '';
  ubicacionSeleccionada = '';

  mensaje = '';
  error = '';
  erroresCampo: Record<string, string> = {};

  pagina = 1;
  tamanioPagina = 5;
  totalRegistros = 0;
  totalPaginas = 0;
  opcionesTamanioPagina = [5, 10, 20];

  editando = false;
  idProveedorEditando: number | null = null;
  estadoProveedorEditando = true;

  proveedor: ProveedorCrear = this.nuevoProveedor();

  constructor(
    private proveedorService: ProveedorService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.subirArriba();
    this.cargarProveedores();
  }

  cargarProveedores(): void {
    this.cargando = true;
    this.error = '';

    this.proveedorService.listar(this.buscar, this.pagina, this.tamanioPagina).subscribe({
      next: (data) => {
        this.proveedores = data.items;
        this.pagina = data.pagina;
        this.tamanioPagina = data.tamanioPagina;
        this.totalRegistros = data.totalRegistros;
        this.totalPaginas = data.totalPaginas;

        if (this.proveedores.length === 0 && this.totalRegistros > 0 && this.pagina > 1) {
          this.pagina--;
          this.cargando = false;
          this.cargarProveedores();
          return;
        }

        this.cargando = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error cargando proveedores:', err);
        this.error = 'No se pudieron cargar los proveedores.';
        this.cargando = false;
        this.cdr.detectChanges();
      }
    });
  }

  guardarProveedor(): void {
    if (!(this.editando ? this.permisos.tienePermiso('PROVEEDORES_EDITAR') : this.permisos.tienePermiso('PROVEEDORES_CREAR'))) return;
    this.mensaje = '';
    this.error = '';
    this.erroresCampo = {};

    this.normalizarProveedorAntesDeEnviar();

    const errorValidacion = this.validarFormulario();

    if (errorValidacion) {
      this.error = '';
      this.cdr.detectChanges();
      return;
    }

    if (this.editando) {
      this.actualizarProveedor();
    } else {
      this.registrarProveedor();
    }
  }

  registrarProveedor(): void {
    if (!this.permisos.tienePermiso('PROVEEDORES_CREAR')) return;
    this.guardando = true;

    this.proveedorService.crear(this.proveedor).subscribe({
      next: () => {
        const mensajeOk = 'Proveedor registrado correctamente.';

        this.guardando = false;
        this.limpiarFormulario();
        this.mensaje = mensajeOk;
        this.pagina = 1;
        this.cargarProveedores();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.asignarErrorBackend(err, 'No se pudo registrar el proveedor.');
        this.guardando = false;
        this.cdr.detectChanges();
      }
    });
  }

  actualizarProveedor(): void {
    if (!this.permisos.tienePermiso('PROVEEDORES_EDITAR')) return;
    if (this.idProveedorEditando === null) {
      this.error = 'No se encontró el proveedor a editar.';
      return;
    }

    const proveedorActualizar: ProveedorActualizar = {
      idUbigeo: this.proveedor.idUbigeo,
      ruc: this.proveedor.ruc,
      razonSocial: this.proveedor.razonSocial,
      nombreComercial: this.normalizarTexto(this.proveedor.nombreComercial),
      correo: this.proveedor.correo,
      telefono: this.proveedor.telefono,
      direccion: this.proveedor.direccion,
      registrarContactoPrincipal: this.proveedor.registrarContactoPrincipal,
      contactoPrincipal: this.proveedor.registrarContactoPrincipal
        ? this.proveedor.contactoPrincipal
        : null,
      estado: this.estadoProveedorEditando
    };

    this.guardando = true;

    this.proveedorService.actualizar(this.idProveedorEditando, proveedorActualizar).subscribe({
      next: () => {
        const mensajeOk = 'Proveedor actualizado correctamente.';

        this.guardando = false;
        this.limpiarFormulario();
        this.mensaje = mensajeOk;
        this.cargarProveedores();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.asignarErrorBackend(err, 'No se pudo actualizar el proveedor.');
        this.guardando = false;
        this.cdr.detectChanges();
      }
    });
  }

  editarProveedor(item: ProveedorListado): void {
    if (!(this.permisos.tienePermiso('PROVEEDORES_EDITAR'))) return;
    this.mensaje = '';
    this.error = '';
    this.erroresCampo = {};

    this.editando = true;
    this.idProveedorEditando = item.idProveedor;
    this.estadoProveedorEditando = true;

    const contacto = item.contactoPrincipal;

    this.proveedor = {
      idUbigeo: item.idUbigeo ?? null,
      ruc: item.ruc,
      razonSocial: item.razonSocial,
      nombreComercial: item.nombreComercial ?? '',
      correo: item.correo ?? '',
      telefono: item.telefono ?? '',
      direccion: item.direccion ?? '',
      registrarContactoPrincipal: !!contacto,
      contactoPrincipal: contacto
        ? {
            idContactoProveedor: contacto.idContactoProveedor ?? null,
            nombres: contacto.nombres ?? '',
            apellidoPaterno: contacto.apellidoPaterno ?? '',
            apellidoMaterno: contacto.apellidoMaterno ?? '',
            cargo: contacto.cargo ?? '',
            correo: contacto.correo ?? '',
            telefono: contacto.telefono ?? '',
            estado: contacto.estado ?? true
          }
        : this.nuevoContacto()
    };

    this.ubicacionSeleccionada = item.ubicacion ?? '';
    this.ubigeoBuscar = item.ubicacion ?? '';
    this.ubigeos = [];

    window.scrollTo({
      top: 0,
      behavior: 'smooth'
    });

    this.cdr.detectChanges();
  }

  eliminarProveedor(idProveedor: number): void {
    if (!(this.permisos.tienePermiso('PROVEEDORES_DESACTIVAR'))) return;
    const confirmar = confirm('¿Deseas eliminar este proveedor?');

    if (!confirmar) {
      return;
    }

    this.proveedorService.eliminar(idProveedor).subscribe({
      next: () => {
        this.mensaje = 'Proveedor eliminado correctamente.';
        this.cargarProveedores();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.error = 'No se pudo eliminar el proveedor.';
        this.cdr.detectChanges();
      }
    });
  }

  consultarRuc(): void {
    this.mensaje = '';
    this.error = '';
    this.erroresCampo = {};

    this.filtrarRucSoloNumeros();

    const errorRuc = this.validarRuc();

    if (errorRuc) {
      this.marcarErrorCampo('ruc', errorRuc);
      this.cdr.detectChanges();
      return;
    }

    this.consultandoRuc = true;

    this.proveedorService.consultarRuc(this.proveedor.ruc).subscribe({
      next: (data) => {
        this.consultandoRuc = false;

        if (data.proveedorYaExiste) {
          this.marcarErrorCampo('ruc', data.mensaje || 'Ya existe un proveedor registrado con ese RUC.');
          this.error = '';
          this.cdr.detectChanges();
          return;
        }

        this.proveedor.ruc = data.numeroDocumento ?? this.proveedor.ruc;
        this.proveedor.razonSocial = data.razonSocial ?? '';
        this.proveedor.nombreComercial = data.nombreComercial ?? data.razonSocial ?? '';
        this.proveedor.direccion = data.direccion ?? this.proveedor.direccion;

        if (data.idUbigeo && data.ubicacion) {
          this.proveedor.idUbigeo = data.idUbigeo;
          this.ubicacionSeleccionada = data.ubicacion;
          this.ubigeoBuscar = data.ubicacion;
          this.ubigeos = [];
        }

        this.limpiarErrorCampo('ruc');
        this.limpiarErrorCampo('razonSocial');
        this.limpiarErrorCampo('direccion');
        this.limpiarErrorCampo('idUbigeo');

        this.mensaje = data.mensaje || 'Datos SUNAT cargados correctamente.';
        this.error = '';
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.consultandoRuc = false;
        this.error = err.error?.mensaje ?? 'No se pudo consultar SUNAT.';
        this.cdr.detectChanges();
      }
    });
  }

  buscarUbigeo(): void {
    const texto = this.ubigeoBuscar.trim();

    this.proveedor.idUbigeo = null;
    this.ubicacionSeleccionada = '';

    if (texto.length < 3) {
      this.ubigeos = [];
      this.limpiarErrorCampo('idUbigeo');
      this.cdr.detectChanges();
      return;
    }

    this.buscandoUbigeo = true;

    this.proveedorService.listarUbigeos(texto).subscribe({
      next: (data) => {
        this.ubigeos = data;
        this.buscandoUbigeo = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.ubigeos = [];
        this.buscandoUbigeo = false;
        this.cdr.detectChanges();
      }
    });
  }

  seleccionarUbigeo(ubigeo: Ubigeo): void {
    this.proveedor.idUbigeo = ubigeo.idUbigeo;
    this.ubicacionSeleccionada = ubigeo.ubicacion;
    this.ubigeoBuscar = ubigeo.ubicacion;
    this.ubigeos = [];
    this.limpiarErrorCampo('idUbigeo');
    this.cdr.detectChanges();
  }

  activarContactoPrincipal(): void {
    if (!this.proveedor.contactoPrincipal) {
      this.proveedor.contactoPrincipal = this.nuevoContacto();
    }

    if (!this.proveedor.registrarContactoPrincipal) {
      this.limpiarErroresContacto();
    }

    this.cdr.detectChanges();
  }

  limpiarFormulario(): void {
    this.proveedor = this.nuevoProveedor();

    this.editando = false;
    this.idProveedorEditando = null;
    this.estadoProveedorEditando = true;

    this.ubigeos = [];
    this.ubigeoBuscar = '';
    this.ubicacionSeleccionada = '';

    this.mensaje = '';
    this.error = '';
    this.erroresCampo = {};

    this.cdr.detectChanges();
  }

  buscarProveedores(): void {
    this.pagina = 1;
    this.cargarProveedores();
  }

  paginaAnterior(): void {
    if (this.pagina <= 1) {
      return;
    }

    this.pagina--;
    this.cargarProveedores();
  }

  paginaSiguiente(): void {
    if (this.pagina >= this.totalPaginas) {
      return;
    }

    this.pagina++;
    this.cargarProveedores();
  }

  cambiarTamanioPagina(): void {
    this.pagina = 1;
    this.cargarProveedores();
  }

  filtrarRucSoloNumeros(): void {
    this.proveedor.ruc = this.limpiarNumeros(this.proveedor.ruc).slice(0, 11);
    this.limpiarErrorCampo('ruc');
  }

  formularioProveedorValido(): boolean {
    return (
      this.validarRuc() === null &&
      this.campoTextoLleno(this.proveedor.razonSocial) &&
      this.esCorreoValido(this.proveedor.correo) &&
      this.esTelefonoPeruValido(this.proveedor.telefono) &&
      this.campoTextoLleno(this.proveedor.direccion) &&
      !!this.proveedor.idUbigeo &&
      this.proveedor.idUbigeo > 0 &&
      this.validarContactoFormulario() === null
    );
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

  private validarFormulario(): string | null {
    const errorRuc = this.validarRuc();

    if (errorRuc) {
      return this.marcarErrorCampo('ruc', errorRuc);
    }

    if (!this.campoTextoLleno(this.proveedor.razonSocial)) {
      return this.marcarErrorCampo('razonSocial', 'Ingresa la razón social del proveedor.');
    }

    if (!this.campoTextoLleno(this.proveedor.correo)) {
      return this.marcarErrorCampo('correo', 'Ingresa el correo del proveedor.');
    }

    if (!this.esCorreoValido(this.proveedor.correo)) {
      return this.marcarErrorCampo('correo', 'Ingresa un correo válido. Ejemplo: proveedor@empresa.com');
    }

    if (!this.campoTextoLleno(this.proveedor.telefono)) {
      return this.marcarErrorCampo('telefono', 'Ingresa el teléfono del proveedor.');
    }

    if (!this.esTelefonoPeruValido(this.proveedor.telefono)) {
      return this.marcarErrorCampo(
        'telefono',
        'Formato válido: +51 seguido de 9 dígitos. Ejemplo: +51999999999'
      );
    }

    if (!this.campoTextoLleno(this.proveedor.direccion)) {
      return this.marcarErrorCampo('direccion', 'Ingresa la dirección del proveedor.');
    }

    if (!this.proveedor.idUbigeo || this.proveedor.idUbigeo <= 0) {
      return this.marcarErrorCampo('idUbigeo', 'Selecciona la ubicación del proveedor.');
    }

    const errorContacto = this.validarContactoFormulario();

    if (errorContacto) {
      return errorContacto;
    }

    return null;
  }

  private validarRuc(): string | null {
    const ruc = this.limpiarNumeros(this.proveedor.ruc);

    if (!ruc) {
      return 'Ingresa el RUC del proveedor.';
    }

    if (ruc.length !== 11) {
      return 'El RUC debe tener 11 dígitos.';
    }

    return null;
  }

  private validarContactoFormulario(): string | null {
    if (!this.proveedor.registrarContactoPrincipal) {
      return null;
    }

    const contacto = this.proveedor.contactoPrincipal;

    if (!contacto) {
      return this.marcarErrorCampo('contactoNombres', 'Ingresa los datos del contacto principal.');
    }

    if (!this.campoTextoLleno(contacto.nombres)) {
      return this.marcarErrorCampo('contactoNombres', 'Ingresa los nombres del contacto.');
    }

    if (!this.esTextoSoloLetras(contacto.nombres)) {
      return this.marcarErrorCampo(
        'contactoNombres',
        'Los nombres del contacto solo deben contener letras y espacios.'
      );
    }

    if (!this.campoTextoLleno(contacto.apellidoPaterno)) {
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

    if (
      contacto.correo &&
      contacto.correo.trim().length > 0 &&
      !this.esCorreoValido(contacto.correo)
    ) {
      return this.marcarErrorCampo(
        'contactoCorreo',
        'Ingresa un correo válido para el contacto.'
      );
    }

    if (
      contacto.telefono &&
      contacto.telefono.trim().length > 0 &&
      !this.esTelefonoPeruValido(contacto.telefono)
    ) {
      return this.marcarErrorCampo(
        'contactoTelefono',
        'Formato válido: +51 seguido de 9 dígitos. Ejemplo: +51999999999'
      );
    }

    return null;
  }

  private obtenerMensajeErrorCampo(campo: string): string | null {
    switch (campo) {
      case 'ruc':
        return this.validarRuc();

      case 'razonSocial':
        if (!this.campoTextoLleno(this.proveedor.razonSocial)) {
          return 'Ingresa la razón social del proveedor.';
        }

        return null;

      case 'correo':
        if (!this.campoTextoLleno(this.proveedor.correo)) {
          return 'Ingresa el correo del proveedor.';
        }

        if (!this.esCorreoValido(this.proveedor.correo)) {
          return 'Ingresa un correo válido. Ejemplo: proveedor@empresa.com';
        }

        return null;

      case 'telefono':
        if (!this.campoTextoLleno(this.proveedor.telefono)) {
          return 'Ingresa el teléfono del proveedor.';
        }

        if (!this.esTelefonoPeruValido(this.proveedor.telefono)) {
          return 'Formato válido: +51 seguido de 9 dígitos. Ejemplo: +51999999999';
        }

        return null;

      case 'direccion':
        if (!this.campoTextoLleno(this.proveedor.direccion)) {
          return 'Ingresa la dirección del proveedor.';
        }

        return null;

      case 'idUbigeo':
        if (!this.proveedor.idUbigeo || this.proveedor.idUbigeo <= 0) {
          return 'Selecciona la ubicación del proveedor.';
        }

        return null;

      case 'contactoNombres':
        if (this.proveedor.registrarContactoPrincipal) {
          const nombres = this.proveedor.contactoPrincipal?.nombres ?? '';

          if (!this.campoTextoLleno(nombres)) {
            return 'Ingresa los nombres del contacto.';
          }

          if (!this.esTextoSoloLetras(nombres)) {
            return 'Los nombres del contacto solo deben contener letras y espacios.';
          }
        }

        return null;

      case 'contactoApellidoPaterno':
        if (this.proveedor.registrarContactoPrincipal) {
          const apellido = this.proveedor.contactoPrincipal?.apellidoPaterno ?? '';

          if (!this.campoTextoLleno(apellido)) {
            return 'Ingresa el apellido paterno del contacto.';
          }

          if (!this.esTextoSoloLetras(apellido)) {
            return 'El apellido paterno del contacto solo debe contener letras y espacios.';
          }
        }

        return null;

      case 'contactoApellidoMaterno':
        if (this.proveedor.registrarContactoPrincipal) {
          const apellido = this.proveedor.contactoPrincipal?.apellidoMaterno ?? '';

          if (apellido.trim().length > 0 && !this.esTextoSoloLetras(apellido)) {
            return 'El apellido materno del contacto solo debe contener letras y espacios.';
          }
        }

        return null;

      case 'contactoCorreo':
        if (this.proveedor.registrarContactoPrincipal) {
          const correo = this.proveedor.contactoPrincipal?.correo ?? '';

          if (correo.trim().length > 0 && !this.esCorreoValido(correo)) {
            return 'Ingresa un correo válido para el contacto.';
          }
        }

        return null;

      case 'contactoTelefono':
        if (this.proveedor.registrarContactoPrincipal) {
          const telefono = this.proveedor.contactoPrincipal?.telefono ?? '';

          if (telefono.trim().length > 0 && !this.esTelefonoPeruValido(telefono)) {
            return 'Formato válido: +51 seguido de 9 dígitos. Ejemplo: +51999999999';
          }
        }

        return null;

      default:
        return null;
    }
  }

  private marcarErrorCampo(campo: string, mensaje: string): string {
    this.erroresCampo[campo] = mensaje;
    return mensaje;
  }

  private asignarErrorBackend(err: any, mensajeDefecto: string): void {
    const mensajeBackend = err.error?.mensaje ?? mensajeDefecto;

    this.error = mensajeBackend;

    const texto = mensajeBackend.toLowerCase();

    if (texto.includes('ruc')) {
      this.erroresCampo['ruc'] = mensajeBackend;
    } else if (texto.includes('razón social') || texto.includes('razon social')) {
      this.erroresCampo['razonSocial'] = mensajeBackend;
    } else if (texto.includes('correo') && texto.includes('contacto')) {
      this.erroresCampo['contactoCorreo'] = mensajeBackend;
    } else if (texto.includes('correo')) {
      this.erroresCampo['correo'] = mensajeBackend;
    } else if (texto.includes('teléfono') || texto.includes('telefono')) {
      if (texto.includes('contacto')) {
        this.erroresCampo['contactoTelefono'] = mensajeBackend;
      } else {
        this.erroresCampo['telefono'] = mensajeBackend;
      }
    } else if (texto.includes('dirección') || texto.includes('direccion')) {
      this.erroresCampo['direccion'] = mensajeBackend;
    } else if (texto.includes('ubicación') || texto.includes('ubicacion')) {
      this.erroresCampo['idUbigeo'] = mensajeBackend;
    } else if (texto.includes('nombres')) {
      this.erroresCampo['contactoNombres'] = mensajeBackend;
    } else if (texto.includes('apellido paterno')) {
      this.erroresCampo['contactoApellidoPaterno'] = mensajeBackend;
    } else if (texto.includes('apellido materno')) {
      this.erroresCampo['contactoApellidoMaterno'] = mensajeBackend;
    }
  }

  private normalizarProveedorAntesDeEnviar(): void {
    this.proveedor.ruc = this.limpiarNumeros(this.proveedor.ruc).slice(0, 11);
    this.proveedor.razonSocial = (this.proveedor.razonSocial ?? '').trim();
    this.proveedor.nombreComercial = this.normalizarTexto(this.proveedor.nombreComercial);
    this.proveedor.correo = (this.proveedor.correo ?? '').trim();
    this.proveedor.telefono = (this.proveedor.telefono ?? '').trim();
    this.proveedor.direccion = (this.proveedor.direccion ?? '').trim();

    if (this.proveedor.registrarContactoPrincipal) {
      if (!this.proveedor.contactoPrincipal) {
        this.proveedor.contactoPrincipal = this.nuevoContacto();
      }

      this.proveedor.contactoPrincipal.nombres =
        (this.proveedor.contactoPrincipal.nombres ?? '').trim();

      this.proveedor.contactoPrincipal.apellidoPaterno =
        (this.proveedor.contactoPrincipal.apellidoPaterno ?? '').trim();

      this.proveedor.contactoPrincipal.apellidoMaterno =
        this.normalizarTexto(this.proveedor.contactoPrincipal.apellidoMaterno);

      this.proveedor.contactoPrincipal.cargo =
        this.normalizarTexto(this.proveedor.contactoPrincipal.cargo);

      this.proveedor.contactoPrincipal.correo =
        this.normalizarTexto(this.proveedor.contactoPrincipal.correo);

      this.proveedor.contactoPrincipal.telefono =
        this.normalizarTexto(this.proveedor.contactoPrincipal.telefono);
    } else {
      this.proveedor.contactoPrincipal = null;
    }
  }

  private limpiarErroresContacto(): void {
    delete this.erroresCampo['contactoNombres'];
    delete this.erroresCampo['contactoApellidoPaterno'];
    delete this.erroresCampo['contactoApellidoMaterno'];
    delete this.erroresCampo['contactoCorreo'];
    delete this.erroresCampo['contactoTelefono'];
  }

  private campoTextoLleno(valor?: string | null): boolean {
    return !!valor && valor.trim().length > 0;
  }

  private esCorreoValido(correo?: string | null): boolean {
    if (!correo) {
      return false;
    }

    return /^[^@\s]+@[^@\s]+\.[^@\s]+$/i.test(correo.trim());
  }

  private esTelefonoPeruValido(telefono?: string | null): boolean {
    if (!telefono) {
      return false;
    }

    return /^\+51\d{9}$/.test(telefono.trim());
  }

  private esTextoSoloLetras(valor?: string | null): boolean {
    if (!valor) {
      return false;
    }

    return /^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$/.test(valor.trim());
  }

  private limpiarNumeros(valor?: string | null): string {
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

  private nuevoProveedor(): ProveedorCrear {
    return {
      idUbigeo: null,
      ruc: '',
      razonSocial: '',
      nombreComercial: '',
      correo: '',
      telefono: '+51',
      direccion: '',
      registrarContactoPrincipal: false,
      contactoPrincipal: this.nuevoContacto()
    };
  }

  private nuevoContacto(): ContactoProveedor {
    return {
      nombres: '',
      apellidoPaterno: '',
      apellidoMaterno: '',
      cargo: '',
      correo: '',
      telefono: '',
      estado: true
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