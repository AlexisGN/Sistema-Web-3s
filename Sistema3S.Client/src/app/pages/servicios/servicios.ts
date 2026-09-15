import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { ServicioService } from '../../core/services/servicio';
import {
  ServicioActualizar,
  ServicioCrear,
  ServicioListado
} from '../../core/models/servicio.model';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-servicios',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './servicios.html',
  styleUrl: './servicios.scss'
})
export class ServiciosComponent implements OnInit {
  servicios: ServicioListado[] = [];

  cargando = false;
  guardando = false;

  buscar = '';
  verInactivos = false;
  mensaje = '';
  error = '';

  erroresCampo: Record<string, string> = {};

  pagina = 1;
  tamanioPagina = 5;
  totalRegistros = 0;
  totalPaginas = 0;
  opcionesTamanioPagina = [5, 10, 20];

  editando = false;
  idServicioEditando: number | null = null;
  estadoServicioEditando = true;

  servicio: ServicioCrear = this.nuevoServicio();
  imagenArchivo: File | null = null;
  imagenVistaPrevia = '';

  private readonly baseArchivosUrl = environment.apiUrl.replace(/\/api\/?$/, '');

  constructor(
    private servicioService: ServicioService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.subirArriba();
    this.cargarServicios();
  }

  seleccionarImagen(event: Event): void {
    const input = event.target as HTMLInputElement;
    const archivo = input.files?.[0] ?? null;

    if (!archivo) {
      return;
    }

    const tiposPermitidos = ['image/jpeg', 'image/png', 'image/webp'];

    if (!tiposPermitidos.includes(archivo.type)) {
      input.value = '';
      this.imagenArchivo = null;
      this.imagenVistaPrevia = '';
      this.erroresCampo['imagenUrl'] = 'Selecciona una imagen JPG, PNG o WebP.';
      return;
    }

    if (archivo.size > 5 * 1024 * 1024) {
      input.value = '';
      this.imagenArchivo = null;
      this.imagenVistaPrevia = '';
      this.erroresCampo['imagenUrl'] = 'La imagen no puede superar 5 MB.';
      return;
    }

    this.imagenArchivo = archivo;
    delete this.erroresCampo['imagenUrl'];

    const reader = new FileReader();
    reader.onload = () => {
      this.imagenVistaPrevia = typeof reader.result === 'string' ? reader.result : '';
      this.cdr.detectChanges();
    };
    reader.readAsDataURL(archivo);
  }

  quitarImagenSeleccionada(input: HTMLInputElement): void {
    input.value = '';
    this.imagenArchivo = null;
    this.imagenVistaPrevia = '';
  }

  obtenerImagenFormulario(): string {
    return this.imagenVistaPrevia || this.resolverImagen(this.servicio.imagenUrl);
  }

  cargarServicios(): void {
    this.cargando = true;
    this.error = '';

    this.servicioService.listar(this.buscar, this.pagina, this.tamanioPagina, this.verInactivos ? false : undefined).subscribe({
      next: (data) => {
        this.servicios = data.items;
        this.pagina = data.pagina;
        this.tamanioPagina = data.tamanioPagina;
        this.totalRegistros = data.totalRegistros;
        this.totalPaginas = data.totalPaginas;

        if (this.servicios.length === 0 && this.totalRegistros > 0 && this.pagina > 1) {
          this.pagina--;
          this.cargando = false;
          this.cargarServicios();
          return;
        }

        this.cargando = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error cargando servicios:', err);
        this.error = 'No se pudieron cargar los servicios.';
        this.cargando = false;
        this.cdr.detectChanges();
      }
    });
  }

  guardarServicio(): void {
    this.mensaje = '';
    this.error = '';
    this.erroresCampo = {};

    this.servicio.nombre = (this.servicio.nombre ?? '').trim();

    this.servicio.descripcion = this.normalizarTexto(this.servicio.descripcion);
    this.servicio.imagenUrl = this.normalizarTexto(this.servicio.imagenUrl);
    this.servicio.sectorAplicacion = this.normalizarTexto(this.servicio.sectorAplicacion);
    this.servicio.mensajeWhatsApp = this.normalizarTexto(this.servicio.mensajeWhatsApp);

    const precio = this.obtenerPrecioReferencialComoNumero();
    this.servicio.precioReferencial = precio;

    const errorValidacion = this.validarFormulario();

    if (errorValidacion) {
      this.error = '';
      this.cdr.detectChanges();
      return;
    }

    if (this.editando) {
      this.actualizarServicio();
    } else {
      this.registrarServicio();
    }
  }

  registrarServicio(): void {
    this.guardando = true;

    this.servicioService.crear(this.servicio, this.imagenArchivo!).subscribe({
      next: () => {
        const mensajeOk = 'Servicio registrado correctamente.';

        this.guardando = false;
        this.limpiarFormulario();
        this.mensaje = mensajeOk;
        this.pagina = 1;
        this.cargarServicios();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.error = err.error?.mensaje ?? 'No se pudo registrar el servicio.';
        this.guardando = false;
        this.cdr.detectChanges();
      }
    });
  }

  actualizarServicio(): void {
    if (this.idServicioEditando === null) {
      this.error = 'No se encontró el servicio a editar.';
      return;
    }

    const servicioActualizar: ServicioActualizar = {
      nombre: (this.servicio.nombre ?? '').trim(),
      descripcion: this.normalizarTexto(this.servicio.descripcion),
      precioReferencial: this.obtenerPrecioReferencialComoNumero(),
      imagenUrl: this.normalizarTexto(this.servicio.imagenUrl),
      sectorAplicacion: this.normalizarTexto(this.servicio.sectorAplicacion),
      mensajeWhatsApp: this.normalizarTexto(this.servicio.mensajeWhatsApp),
      requiereVisitaTecnica: this.servicio.requiereVisitaTecnica,
      estado: this.estadoServicioEditando
    };

    this.guardando = true;

    this.servicioService.actualizar(
      this.idServicioEditando,
      servicioActualizar,
      this.imagenArchivo
    ).subscribe({
      next: () => {
        const mensajeOk = 'Servicio actualizado correctamente.';

        this.guardando = false;
        this.limpiarFormulario();
        this.mensaje = mensajeOk;
        this.cargarServicios();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.error = err.error?.mensaje ?? 'No se pudo actualizar el servicio.';
        this.guardando = false;
        this.cdr.detectChanges();
      }
    });
  }

  editarServicio(item: ServicioListado): void {
    this.mensaje = '';
    this.error = '';
    this.erroresCampo = {};

    this.editando = true;
    this.idServicioEditando = item.idServicio;

    // Al editar un servicio inactivo, al actualizar volverá a estar activo
    this.estadoServicioEditando = true;
    this.imagenArchivo = null;
    this.imagenVistaPrevia = '';

    this.servicio = {
      nombre: item.nombre,
      descripcion: item.descripcion ?? '',
      precioReferencial: item.precioReferencial ?? null,
      imagenUrl: item.imagenUrl ?? '',
      sectorAplicacion: item.sectorAplicacion ?? '',
      mensajeWhatsApp: item.mensajeWhatsApp ?? '',
      requiereVisitaTecnica: item.requiereVisitaTecnica
    };

    window.scrollTo({
      top: 0,
      behavior: 'smooth'
    });

    this.cdr.detectChanges();
  }

  eliminarServicio(idServicio: number): void {
    const confirmar = confirm('¿Deseas eliminar este servicio?');

    if (!confirmar) {
      return;
    }

    this.servicioService.eliminar(idServicio).subscribe({
      next: () => {
        this.mensaje = 'Servicio eliminado correctamente.';
        this.cargarServicios();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.error = 'No se pudo eliminar el servicio.';
        this.cdr.detectChanges();
      }
    });
  }

  limpiarFormulario(): void {
    this.servicio = this.nuevoServicio();
    this.imagenArchivo = null;
    this.imagenVistaPrevia = '';

    this.editando = false;
    this.idServicioEditando = null;
    this.estadoServicioEditando = true;

    this.mensaje = '';
    this.error = '';
    this.erroresCampo = {};

    this.cdr.detectChanges();
  }

  alternarInactivos(): void {
    this.verInactivos = !this.verInactivos;
    this.pagina = 1;
    this.cargarServicios();
  }

  buscarServicios(): void {
    this.pagina = 1;
    this.cargarServicios();
  }

  paginaAnterior(): void {
    if (this.pagina <= 1) {
      return;
    }

    this.pagina--;
    this.cargarServicios();
  }

  paginaSiguiente(): void {
    if (this.pagina >= this.totalPaginas) {
      return;
    }

    this.pagina++;
    this.cargarServicios();
  }

  cambiarTamanioPagina(): void {
    this.pagina = 1;
    this.cargarServicios();
  }

  resolverImagen(url?: string | null): string {
    if (!url || url.trim().length === 0) {
      return '';
    }

    const ruta = url.trim();

    if (ruta.startsWith('http://') || ruta.startsWith('https://')) {
      return ruta;
    }

    return ruta.startsWith('/')
      ? `${this.baseArchivosUrl}${ruta}`
      : `${this.baseArchivosUrl}/${ruta}`;
  }

  marcarImagenFallida(item: ServicioListado): void {
    item.imagenUrl = '';
    this.cdr.detectChanges();
  }

  formularioServicioValido(): boolean {
    const precio = this.obtenerPrecioReferencialComoNumero();

    return (
      this.campoTextoLleno(this.servicio.nombre) &&
      this.campoTextoLleno(this.servicio.sectorAplicacion) &&
      precio !== null &&
      precio >= 0 &&
      (this.imagenArchivo !== null || this.campoTextoLleno(this.servicio.imagenUrl)) &&
      this.campoTextoLleno(this.servicio.descripcion)
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
    if (!this.campoTextoLleno(this.servicio.nombre)) {
      return this.marcarErrorCampo('nombre', 'Ingresa el nombre del servicio.');
    }

    if (!this.campoTextoLleno(this.servicio.sectorAplicacion)) {
      return this.marcarErrorCampo('sectorAplicacion', 'Ingresa el sector de aplicación.');
    }

    const precio = this.obtenerPrecioReferencialComoNumero();

    if (precio === null) {
      return this.marcarErrorCampo('precioReferencial', 'Ingresa el precio referencial.');
    }

    if (precio < 0) {
      return this.marcarErrorCampo('precioReferencial', 'El precio referencial no puede ser negativo.');
    }

    if (this.imagenArchivo === null && !this.campoTextoLleno(this.servicio.imagenUrl)) {
      return this.marcarErrorCampo('imagenUrl', 'Selecciona una imagen desde tu equipo.');
    }

    if (!this.campoTextoLleno(this.servicio.descripcion)) {
      return this.marcarErrorCampo('descripcion', 'Ingresa la descripción del servicio.');
    }

    return null;
  }

  private obtenerMensajeErrorCampo(campo: string): string | null {
    switch (campo) {
      case 'nombre':
        if (!this.campoTextoLleno(this.servicio.nombre)) {
          return 'Ingresa el nombre del servicio.';
        }

        return null;

      case 'sectorAplicacion':
        if (!this.campoTextoLleno(this.servicio.sectorAplicacion)) {
          return 'Ingresa el sector de aplicación.';
        }

        return null;

      case 'precioReferencial': {
        const precio = this.obtenerPrecioReferencialComoNumero();

        if (precio === null) {
          return 'Ingresa el precio referencial.';
        }

        if (precio < 0) {
          return 'El precio referencial no puede ser negativo.';
        }

        return null;
      }

      case 'imagenUrl':
        if (this.imagenArchivo === null && !this.campoTextoLleno(this.servicio.imagenUrl)) {
          return 'Selecciona una imagen desde tu equipo.';
        }

        return null;

      case 'descripcion':
        if (!this.campoTextoLleno(this.servicio.descripcion)) {
          return 'Ingresa la descripción del servicio.';
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

  private campoTextoLleno(valor?: string | null): boolean {
    return !!valor && valor.trim().length > 0;
  }

  private obtenerPrecioReferencialComoNumero(): number | null {
    const valor = this.servicio.precioReferencial as unknown;

    if (valor === null || valor === undefined || valor === '') {
      return null;
    }

    const precio = Number(valor);

    if (!Number.isFinite(precio)) {
      return null;
    }

    return precio;
  }

  private normalizarTexto(valor?: string | null): string | undefined {
    if (!valor || valor.trim().length === 0) {
      return undefined;
    }

    return valor.trim();
  }

  private nuevoServicio(): ServicioCrear {
    return {
      nombre: '',
      descripcion: '',
      precioReferencial: null,
      imagenUrl: '',
      sectorAplicacion: '',
      mensajeWhatsApp: '',
      requiereVisitaTecnica: true
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
