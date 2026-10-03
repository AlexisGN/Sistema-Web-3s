import { UiIconComponent } from '../../shared/ui-icon/ui-icon';
import { RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { Permiso } from '../../core/models/permiso.model';
import { RolListado } from '../../core/models/rol.model';
import { UsuarioListado } from '../../core/models/usuario.model';
import { RolService } from '../../core/services/rol.service';
import { UsuarioService } from '../../core/services/usuario.service';
import { SessionService } from '../../core/services/session.service';

type TabUsuariosRoles = 'usuarios' | 'clientes' | 'roles' | 'permisos';

interface GrupoPermisos {
  modulo: string;
  titulo: string;
  permisos: Permiso[];
}

@Component({
  selector: 'app-usuarios-roles',
  standalone: true,
  imports: [UiIconComponent, CommonModule, FormsModule, RouterLink],
  templateUrl: './usuarios-roles.html',
  styleUrl: './usuarios-roles.scss'
})
export class UsuariosRolesComponent implements OnInit, OnDestroy {
  mostrarFormulario = false;
  paginaUsuarios = 1;
  private consultaUsuarios?: Subscription;
  get usuariosVisibles(): UsuarioListado[] { return this.usuarios.slice((this.paginaUsuarios-1)*10, this.paginaUsuarios*10); }
  get paginasUsuarios(): number { return Math.max(1, Math.ceil(this.usuarios.length/10)); }
  get rolesDisponibles(): RolListado[] { return this.roles.filter(r => r.estado); }
  puedeVerClientes(): boolean { return this.sessionService.tienePermiso('CLIENTES_VER'); }
  ngOnDestroy(): void { this.consultaUsuarios?.unsubscribe(); }
  cerrarFormulario(): void { this.mostrarFormulario=false; this.usuarioForm.contrasena=''; }
  tabActiva: TabUsuariosRoles = 'usuarios';

  usuarios: UsuarioListado[] = [];
  roles: RolListado[] = [];
  permisosRol: Permiso[] = [];

  buscarUsuario = '';
  filtroSoloActivos: boolean | null = null;

  idRolPermisosSeleccionado = 0;

  cargando = false;
  procesando = false;

  mensaje = '';
  error = '';

  usuarioForm = {
    idUsuario: 0,
    idRol: 0,
    correo: '',
    contrasena: '',
    estado: true,
    editando: false
  };

  rolForm = {
    idRol: 0,
    nombre: '',
    descripcion: '',
    estado: true,
    editando: false
  };

  contrasenaForm = {
    idUsuario: 0,
    correo: '',
    nuevaContrasena: ''
  };

  constructor(
    private usuarioService: UsuarioService,
    private rolService: RolService,
    private sessionService: SessionService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.cargarTodo();
  }

  cargarTodo(): void {
    this.limpiarMensajes();
    this.cargando = true;

    this.cargarRoles(() => {
      this.cargarUsuarios(() => {
        if (this.roles.length > 0 && this.idRolPermisosSeleccionado === 0) {
          this.idRolPermisosSeleccionado = this.roles[0].idRol;
        }

        if (this.idRolPermisosSeleccionado > 0) {
          this.cargarPermisosRol(this.idRolPermisosSeleccionado);
        }

        this.cargando = false;
      });
    });
  }

  cambiarTab(tab: TabUsuariosRoles): void {
    if (this.procesando) return;
    this.tabActiva = tab;
    this.cerrarFormulario();
    this.cancelarCambioContrasena();
    if (tab === 'usuarios' || tab === 'clientes') { this.buscarUsuario=''; this.filtroSoloActivos=null; this.cargarUsuarios(); }
    this.limpiarMensajes();

    if (tab === 'permisos' && this.idRolPermisosSeleccionado > 0) {
      this.cargarPermisosRol(this.idRolPermisosSeleccionado);
    }
  }

  cargarUsuarios(callback?: () => void): void {
    this.consultaUsuarios?.unsubscribe();
    this.cargando = true;
    this.error = '';
    this.usuarios = [];
    this.paginaUsuarios = 1;
    this.consultaUsuarios = this.usuarioService.listar(this.buscarUsuario, this.filtroSoloActivos, this.tabActiva === 'clientes').subscribe({
      next: usuarios => {
        this.cdr.markForCheck(); this.usuarios=usuarios; this.cargando=false; callback?.(); this.cdr.markForCheck(); },
      error: err => {
        this.cdr.markForCheck(); this.error=this.obtenerMensajeError(err, 'No se pudieron cargar las cuentas.'); this.cargando=false; this.cdr.markForCheck(); }
    });
  }

  cargarRoles(callback?: () => void): void {
    this.rolService.listar(null).subscribe({
      next: (roles) => {
        this.cdr.markForCheck();
        this.roles = roles.filter(r => !['cliente', 'cliente web'].includes(r.nombre.trim().toLowerCase()));

        if (this.roles.length > 0 && this.usuarioForm.idRol === 0) {
          const rolVendedor = this.rolesDisponibles.find(r => r.nombre.toLowerCase() === 'vendedor');
          this.usuarioForm.idRol = rolVendedor?.idRol || 0;
        }

        if (callback) {
          callback();
        }
      },
      error: (err) => {
        this.cdr.markForCheck();
        this.error = this.obtenerMensajeError(err, 'No se pudieron cargar los roles.');
        this.cargando = false;
      }
    });
  }

  cargarPermisosRol(idRol: number): void {
    if (!idRol) {
      this.permisosRol = [];
      return;
    }

    this.idRolPermisosSeleccionado = Number(idRol);

    this.rolService.obtenerPermisosPorRol(this.idRolPermisosSeleccionado).subscribe({
      next: (permisos) => {
        this.cdr.markForCheck();
        this.permisosRol = permisos;
      },
      error: (err) => {
        this.cdr.markForCheck();
        this.error = this.obtenerMensajeError(err, 'No se pudieron cargar los permisos del rol.');
      }
    });
  }

  aplicarBusquedaUsuarios(): void {
    this.cargarUsuarios();
  }

  limpiarBusquedaUsuarios(): void {
    this.buscarUsuario = '';
    this.filtroSoloActivos = null;
    this.cargarUsuarios();
  }

  nuevoUsuario(): void {
    this.mostrarFormulario = true;
    this.cancelarCambioContrasena();
    this.limpiarMensajes();

    const rolDefault = this.rolesDisponibles.find(r => r.nombre.toLowerCase() === 'vendedor');

    this.usuarioForm = {
      idUsuario: 0,
      idRol: rolDefault?.idRol || 0,
      correo: '',
      contrasena: '',
      estado: true,
      editando: false
    };
  }

  editarUsuario(usuario: UsuarioListado): void {
    if (usuario.esCliente) return;
    this.mostrarFormulario = true;
    this.cancelarCambioContrasena();
    window.scrollTo({top: 0, behavior: 'smooth'});
    this.limpiarMensajes();

    this.usuarioForm = {
      idUsuario: usuario.idUsuario,
      idRol: usuario.idRol,
      correo: usuario.correo,
      contrasena: '',
      estado: usuario.estado,
      editando: true
    };
  }

  guardarUsuario(): void {
    this.limpiarMensajes();

    if (!this.usuarioForm.idRol) {
      this.error = 'Selecciona un rol para el usuario.';
      return;
    }

    if (!this.usuarioForm.correo.trim()) {
      this.error = 'Ingresa el correo del usuario.';
      return;
    }

    if (!this.usuarioForm.editando && !this.usuarioForm.contrasena.trim()) {
      this.error = 'Ingresa una contraseña para el nuevo usuario.';
      return;
    }

    this.procesando = true;

    if (this.usuarioForm.editando) {
      this.usuarioService.actualizar(this.usuarioForm.idUsuario, {
        idRol: Number(this.usuarioForm.idRol),
        correo: this.usuarioForm.correo.trim().toLowerCase(),
        estado: this.usuarioForm.estado
      }).subscribe({
        next: () => {
        this.cdr.markForCheck();
          this.nuevoUsuario();
          this.cerrarFormulario();
          this.mensaje = 'Usuario actualizado correctamente.';
          this.procesando = false;
          this.cargarUsuarios();
          this.cargarRoles();
        },
        error: (err) => {
        this.cdr.markForCheck();
          this.error = this.obtenerMensajeError(err, 'No se pudo actualizar el usuario.');
          this.procesando = false;
        }
      });

      return;
    }

    this.usuarioService.crear({
      idRol: Number(this.usuarioForm.idRol),
      correo: this.usuarioForm.correo.trim().toLowerCase(),
      contrasena: this.usuarioForm.contrasena,
      estado: this.usuarioForm.estado
    }).subscribe({
      next: () => {
        this.cdr.markForCheck();
        this.nuevoUsuario();
        this.cerrarFormulario();
        this.mensaje = 'Usuario creado correctamente.';
        this.procesando = false;
        this.cargarUsuarios();
        this.cargarRoles();
      },
      error: (err) => {
        this.cdr.markForCheck();
        this.error = this.obtenerMensajeError(err, 'No se pudo crear el usuario.');
        this.procesando = false;
      }
    });
  }

  prepararCambioContrasena(usuario: UsuarioListado): void {
    this.cerrarFormulario();
    window.scrollTo({top: 0, behavior: 'smooth'});
    this.limpiarMensajes();

    this.contrasenaForm = {
      idUsuario: usuario.idUsuario,
      correo: usuario.correo,
      nuevaContrasena: ''
    };
  }

  cancelarCambioContrasena(): void {
    this.contrasenaForm = {
      idUsuario: 0,
      correo: '',
      nuevaContrasena: ''
    };
  }

  cambiarContrasena(): void {
    this.limpiarMensajes();

    if (!this.contrasenaForm.idUsuario) {
      this.error = 'Selecciona un usuario.';
      return;
    }

    if (!this.contrasenaForm.nuevaContrasena.trim()) {
      this.error = 'Ingresa la nueva contraseña.';
      return;
    }

    this.procesando = true;

    this.usuarioService.cambiarContrasena(this.contrasenaForm.idUsuario, {
      nuevaContrasena: this.contrasenaForm.nuevaContrasena
    }).subscribe({
      next: (resultado) => {
        this.cdr.markForCheck();
        this.mensaje = resultado.mensaje || 'Contraseña actualizada correctamente.';
        this.procesando = false;
        this.cancelarCambioContrasena();
      },
      error: (err) => {
        this.cdr.markForCheck();
        this.error = this.obtenerMensajeError(err, 'No se pudo cambiar la contraseña.');
        this.procesando = false;
      }
    });
  }

  desactivarUsuario(usuario: UsuarioListado): void {
    this.limpiarMensajes();

    if (usuario.idUsuario === this.sessionService.obtenerIdUsuario()) {
      this.error = 'No puedes desactivar tu propio usuario desde esta sesión.';
      return;
    }

    const confirmar = window.confirm(`¿Deseas desactivar el usuario ${usuario.correo}?`);

    if (!confirmar) {
      return;
    }

    this.procesando = true;

    this.usuarioService.desactivar(usuario.idUsuario).subscribe({
      next: (resultado) => {
        this.cdr.markForCheck();
        this.mensaje = resultado.mensaje || 'Usuario desactivado correctamente.';
        this.procesando = false;
        this.cargarUsuarios();
        this.cargarRoles();
      },
      error: (err) => {
        this.cdr.markForCheck();
        this.error = this.obtenerMensajeError(err, 'No se pudo desactivar el usuario.');
        this.procesando = false;
      }
    });
  }

  nuevoRol(): void {
    this.limpiarMensajes();

    this.rolForm = {
      idRol: 0,
      nombre: '',
      descripcion: '',
      estado: true,
      editando: false
    };
  }

  editarRol(rol: RolListado): void {
    this.limpiarMensajes();

    this.rolForm = {
      idRol: rol.idRol,
      nombre: rol.nombre,
      descripcion: rol.descripcion || '',
      estado: rol.estado,
      editando: true
    };
  }

  guardarRol(): void {
    this.limpiarMensajes();

    if (!this.rolForm.nombre.trim()) {
      this.error = 'Ingresa el nombre del rol.';
      return;
    }

    this.procesando = true;

    if (this.rolForm.editando) {
      this.rolService.actualizar(this.rolForm.idRol, {
        nombre: this.rolForm.nombre.trim(),
        descripcion: this.rolForm.descripcion?.trim() || null,
        estado: this.rolForm.estado
      }).subscribe({
        next: (respuesta) => {
        this.cdr.markForCheck();
          this.roles = respuesta.roles.filter(r => !['cliente', 'cliente web'].includes(r.nombre.trim().toLowerCase()));
          this.mensaje = respuesta.mensaje || 'Rol actualizado correctamente.';
          this.procesando = false;
          this.nuevoRol();
          this.cargarUsuarios();
        },
        error: (err) => {
        this.cdr.markForCheck();
          this.error = this.obtenerMensajeError(err, 'No se pudo actualizar el rol.');
          this.procesando = false;
        }
      });

      return;
    }

    this.rolService.crear({
      nombre: this.rolForm.nombre.trim(),
      descripcion: this.rolForm.descripcion?.trim() || null
    }).subscribe({
      next: (respuesta) => {
        this.cdr.markForCheck();
        this.roles = respuesta.roles.filter(r => !['cliente', 'cliente web'].includes(r.nombre.trim().toLowerCase()));
        this.mensaje = respuesta.mensaje || 'Rol creado correctamente.';
        this.procesando = false;
        this.nuevoRol();
      },
      error: (err) => {
        this.cdr.markForCheck();
        this.error = this.obtenerMensajeError(err, 'No se pudo crear el rol.');
        this.procesando = false;
      }
    });
  }

  desactivarRol(rol: RolListado): void {
    this.limpiarMensajes();

    const confirmar = window.confirm(`¿Deseas desactivar el rol ${rol.nombre}?`);

    if (!confirmar) {
      return;
    }

    this.procesando = true;

    this.rolService.desactivar(rol.idRol).subscribe({
      next: (resultado) => {
        this.cdr.markForCheck();
        this.mensaje = resultado.mensaje || 'Rol desactivado correctamente.';
        this.procesando = false;
        this.cargarRoles();
      },
      error: (err) => {
        this.cdr.markForCheck();
        this.error = this.obtenerMensajeError(err, 'No se pudo desactivar el rol.');
        this.procesando = false;
      }
    });
  }

  togglePermiso(permiso: Permiso): void {
    if (this.rolSeleccionadoEsAdministrador()) {
      return;
    }

    permiso.asignado = !permiso.asignado;
  }

  guardarPermisosRol(): void {
    this.limpiarMensajes();

    if (!this.idRolPermisosSeleccionado) {
      this.error = 'Selecciona un rol.';
      return;
    }

    const idsPermisos = this.permisosRol
      .filter(p => p.asignado)
      .map(p => p.idPermiso);

    this.procesando = true;

    this.rolService.asignarPermisos(this.idRolPermisosSeleccionado, {
      idsPermisos
    }).subscribe({
      next: (resultado) => {
        this.cdr.markForCheck();
        this.mensaje = resultado.mensaje || 'Permisos actualizados correctamente.';
        this.procesando = false;
        this.cargarPermisosRol(this.idRolPermisosSeleccionado);
        this.cargarRoles();
      },
      error: (err) => {
        this.cdr.markForCheck();
        this.error = this.obtenerMensajeError(err, 'No se pudieron actualizar los permisos.');
        this.procesando = false;
      }
    });
  }

  gruposPermisos(): GrupoPermisos[] {
    const mapa = new Map<string, Permiso[]>();

    for (const permiso of this.permisosRol) {
      const modulo = this.obtenerModuloPermiso(permiso.nombre);

      if (!mapa.has(modulo)) {
        mapa.set(modulo, []);
      }

      mapa.get(modulo)!.push(permiso);
    }

    return Array.from(mapa.entries()).map(([modulo, permisos]) => ({
      modulo,
      titulo: this.tituloModuloPermiso(modulo),
      permisos
    }));
  }

  rolSeleccionadoEsAdministrador(): boolean {
    const rol = this.roles.find(r => r.idRol === Number(this.idRolPermisosSeleccionado));
    const nombre = (rol?.nombre || '').trim().toLowerCase();

    return nombre === 'administrador' || nombre === 'admin';
  }

  esRolAdministrador(rol: RolListado): boolean {
    const nombre = rol.nombre.trim().toLowerCase();
    return nombre === 'administrador' || nombre === 'admin';
  }

  formatearFecha(fecha: string): string {
    if (!fecha) {
      return '-';
    }

    const date = new Date(fecha);

    if (Number.isNaN(date.getTime())) {
      return '-';
    }

    return date.toLocaleString('es-PE', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  private obtenerModuloPermiso(nombre: string): string {
    const valor = (nombre || '').split('_')[0] || 'OTROS';
    return valor.toUpperCase();
  }

  private tituloModuloPermiso(modulo: string): string {
    const titulos: Record<string, string> = {
      INICIO: 'Inicio',
      PRODUCTOS: 'Productos',
      SERVICIOS: 'Servicios',
      CLIENTES: 'Clientes',
      COTIZACIONES: 'Cotizaciones',
      VENTAS: 'Ventas internas',
      PROVEEDORES: 'Proveedores',
      COMPRAS: 'Compras',
      INVENTARIO: 'Inventario / Stock',
      CAJA: 'Caja',
      USUARIOS: 'Usuarios',
      ROLES: 'Roles y permisos'
    };

    return titulos[modulo] || modulo;
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
}
