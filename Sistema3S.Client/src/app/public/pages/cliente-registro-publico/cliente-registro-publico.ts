import { CommonModule } from '@angular/common';
import { Component, OnDestroy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import {
  ClienteWebConsultaDocumentoResponse,
  ClienteWebRegistroRequest
} from '../../../core/models/cliente-web.model';
import { ClienteWebService } from '../../../core/services/cliente-web.service';

@Component({
  selector: 'app-cliente-registro-publico',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './cliente-registro-publico.html',
  styleUrl: './cliente-registro-publico.scss'
})
export class ClienteRegistroPublicoComponent implements OnDestroy {
  data: ClienteWebRegistroRequest = {
    tipoDocumento: 'DNI',
    numeroDocumento: '',
    correo: '',
    telefono: '',
    direccion: '',
    contrasena: '',
    confirmarContrasena: '',
    nombres: '',
    apellidoPaterno: '',
    apellidoMaterno: '',
    razonSocial: '',
    nombreComercial: ''
  };

  cargando = false;
  consultandoDocumento = false;

  error = '';
  mensaje = '';
  avisoDocumento = '';

  documentoConsultado = false;
  clienteYaExiste = false;
  cuentaWebVinculada = false;

  private consultaTimeout?: number;

  constructor(
    private clienteWebService: ClienteWebService,
    private router: Router
  ) { }

  ngOnDestroy(): void {
    if (this.consultaTimeout) {
      window.clearTimeout(this.consultaTimeout);
    }
  }

  get esEmpresa(): boolean {
    return this.data.tipoDocumento === 'RUC';
  }

  get longitudDocumento(): number {
    return this.esEmpresa ? 11 : 8;
  }

  cambiarTipoDocumento(tipo: 'DNI' | 'RUC'): void {
    this.data.tipoDocumento = tipo;
    this.data.numeroDocumento = '';

    this.limpiarMensajes();
    this.limpiarDatosDocumento();

    if (tipo === 'DNI') {
      this.data.razonSocial = '';
      this.data.nombreComercial = '';
    } else {
      this.data.nombres = '';
      this.data.apellidoPaterno = '';
      this.data.apellidoMaterno = '';
    }
  }

  alCambiarDocumento(): void {
    this.data.numeroDocumento = this.data.numeroDocumento
      .replace(/\D/g, '')
      .slice(0, this.longitudDocumento);

    this.limpiarMensajes();
    this.documentoConsultado = false;
    this.clienteYaExiste = false;
    this.cuentaWebVinculada = false;

    if (this.consultaTimeout) {
      window.clearTimeout(this.consultaTimeout);
    }

    if (this.data.numeroDocumento.length !== this.longitudDocumento) {
      return;
    }

    this.consultaTimeout = window.setTimeout(() => {
      this.consultarDocumento();
    }, 420);
  }

  consultarDocumento(): void {
    if (this.data.numeroDocumento.length !== this.longitudDocumento) {
      return;
    }

    this.consultandoDocumento = true;
    this.avisoDocumento = '';
    this.error = '';

    this.clienteWebService
      .consultarDocumento(this.data.tipoDocumento, this.data.numeroDocumento)
      .subscribe({
        next: response => {
          this.consultandoDocumento = false;
          this.documentoConsultado = true;

          this.aplicarDatosConsulta(response);
        },
        error: () => {
          this.consultandoDocumento = false;
          this.documentoConsultado = false;

          this.avisoDocumento = this.esEmpresa
            ? 'No se encontró información del RUC o el servicio no respondió correctamente.'
            : 'No se encontró información del DNI o el servicio no respondió correctamente.';
        }
      });
  }

  aplicarDatosConsulta(response: ClienteWebConsultaDocumentoResponse): void {
    this.clienteYaExiste = response.clienteYaExiste;
    this.cuentaWebVinculada = response.cuentaWebVinculada;

    if (response.correoExistente && !this.data.correo) {
      this.data.correo = response.correoExistente;
    }

    if (response.telefonoExistente && !this.data.telefono) {
      this.data.telefono = response.telefonoExistente;
    }

    if (response.direccionExistente && !this.data.direccion) {
      this.data.direccion = response.direccionExistente;
    }

    if (this.data.tipoDocumento === 'DNI') {
      this.data.nombres = response.nombres || this.data.nombres || '';
      this.data.apellidoPaterno = response.apellidoPaterno || this.data.apellidoPaterno || '';
      this.data.apellidoMaterno = response.apellidoMaterno || this.data.apellidoMaterno || '';
    }

    if (this.data.tipoDocumento === 'RUC') {
      this.data.razonSocial = response.razonSocial || this.data.razonSocial || '';
      this.data.nombreComercial = response.nombreComercial || this.data.nombreComercial || '';
      this.data.direccion = response.direccionExistente || this.data.direccion || '';
    }

    if (response.cuentaWebVinculada) {
      this.avisoDocumento = 'Este documento ya tiene una cuenta web registrada. Inicia sesión para continuar.';
      return;
    }

    if (response.exitoso || response.clienteYaExiste) {
      this.avisoDocumento = 'Datos encontrados correctamente. Revisa la información antes de crear tu cuenta.';
      return;
    }

    this.avisoDocumento = this.esEmpresa
      ? 'No se encontró información del RUC o el servicio no respondió correctamente.'
      : 'No se encontró información del DNI o el servicio no respondió correctamente.';
  }
  registrar(): void {
    this.limpiarMensajes();

    const validacion = this.validarFormulario();

    if (validacion) {
      this.error = validacion;
      return;
    }

    if (this.cuentaWebVinculada) {
      this.error = 'Este cliente ya tiene una cuenta web. Inicia sesión para continuar.';
      return;
    }

    this.cargando = true;

    const payload: ClienteWebRegistroRequest = {
      ...this.data,
      numeroDocumento: this.data.numeroDocumento.trim(),
      correo: this.data.correo.trim().toLowerCase(),
      telefono: this.data.telefono.trim(),
      direccion: this.data.direccion?.trim() || null,
      nombres: this.data.nombres?.trim() || null,
      apellidoPaterno: this.data.apellidoPaterno?.trim() || null,
      apellidoMaterno: this.data.apellidoMaterno?.trim() || null,
      razonSocial: this.data.razonSocial?.trim() || null,
      nombreComercial: this.data.nombreComercial?.trim() || null
    };

    this.clienteWebService.registrar(payload).subscribe({
      next: response => {
        this.cargando = false;
        this.mensaje = response.mensaje || 'Cuenta creada correctamente.';

        window.setTimeout(() => {
          this.router.navigate(['/productos']);
        }, 900);
      },
      error: error => {
        this.cargando = false;
        this.error =
          error?.error?.mensaje ||
          'No pudimos registrar tu cuenta. Revisa los datos e intenta nuevamente.';
      }
    });
  }

  irLogin(): void {
    this.router.navigate(['/cliente/login']);
  }

  private validarFormulario(): string {
    const numeroDocumento = this.data.numeroDocumento.trim();
    const correo = this.data.correo.trim();
    const telefono = this.data.telefono.trim();

    if (numeroDocumento.length !== this.longitudDocumento) {
      return this.esEmpresa
        ? 'El RUC debe tener 11 dígitos.'
        : 'El DNI debe tener 8 dígitos.';
    }

    if (!/^\d+$/.test(numeroDocumento)) {
      return 'El número de documento solo debe contener dígitos.';
    }

    if (!correo || !correo.includes('@') || !correo.includes('.')) {
      return 'Ingresa un correo válido.';
    }

    if (!telefono) {
      return 'Ingresa un número de contacto.';
    }

    if (!this.esEmpresa) {
      if (!this.data.nombres?.trim() || !this.data.apellidoPaterno?.trim()) {
        return 'Ingresa nombres y apellido paterno.';
      }
    }

    if (this.esEmpresa) {
      if (!this.data.razonSocial?.trim()) {
        return 'Ingresa la razón social de la empresa.';
      }
    }

    if (!this.data.contrasena || this.data.contrasena.length < 8) {
      return 'La contraseña debe tener como mínimo 8 caracteres.';
    }

    if (this.data.contrasena !== this.data.confirmarContrasena) {
      return 'Las contraseñas no coinciden.';
    }

    return '';
  }

  private limpiarDatosDocumento(): void {
    this.documentoConsultado = false;
    this.clienteYaExiste = false;
    this.cuentaWebVinculada = false;
    this.avisoDocumento = '';
  }

  private limpiarMensajes(): void {
    this.error = '';
    this.mensaje = '';
  }
}