import { Injectable } from '@angular/core';

export interface SesionUsuario {
  idUsuario: number;
  idRol: number;
  correo: string;
  rol: string;
  token: string;
  expira: string;
  permisos: string[];
}

@Injectable({
  providedIn: 'root'
})
export class SessionService {
  private readonly storageKey = 'sistema3s_sesion';

  guardarSesion(data: any): void {
    this.cerrarSesion();

    const token = String(data?.token || data?.Token || '').trim();
    const payload = this.obtenerPayloadToken(token);

    const rol = String(
      data?.rol ||
      data?.Rol ||
      data?.rolNombre ||
      data?.RolNombre ||
      payload?.rol ||
      payload?.role ||
      payload?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
      ''
    ).trim();

    const correo = String(
      data?.correo ||
      data?.Correo ||
      payload?.correo ||
      payload?.email ||
      payload?.sub ||
      ''
    ).trim();

    const sesion: SesionUsuario = {
      idUsuario: Number(
        data?.idUsuario ||
        data?.IdUsuario ||
        payload?.idUsuario ||
        payload?.IdUsuario ||
        payload?.nameid ||
        payload?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ||
        0
      ),
      idRol: Number(
        data?.idRol ||
        data?.IdRol ||
        payload?.idRol ||
        payload?.IdRol ||
        0
      ),
      correo,
      rol,
      token,
      expira: String(
        data?.expira ||
        data?.Expira ||
        data?.fechaExpiracion ||
        data?.FechaExpiracion ||
        this.obtenerExpiracionDesdeToken(token) ||
        ''
      ),
      permisos: this.normalizarPermisos(data, payload)
    };

    localStorage.setItem(this.storageKey, JSON.stringify(sesion));

    localStorage.setItem('usuario', JSON.stringify({
      idUsuario: sesion.idUsuario,
      idRol: sesion.idRol,
      correo: sesion.correo,
      rol: sesion.rol,
      rolNombre: sesion.rol,
      permisos: sesion.permisos
    }));
  }

  obtenerSesion(): SesionUsuario | null {
    const raw = localStorage.getItem(this.storageKey);

    if (!raw) {
      return null;
    }

    try {
      const sesion = JSON.parse(raw) as SesionUsuario;

      if (!sesion || !sesion.token) {
        this.cerrarSesion();
        return null;
      }

      sesion.permisos = this.normalizarListaPermisos(sesion.permisos || []);

      return sesion;
    } catch {
      this.cerrarSesion();
      return null;
    }
  }

  obtenerToken(): string {
    return this.obtenerSesion()?.token || '';
  }

  obtenerCorreo(): string {
    return this.obtenerSesion()?.correo || '';
  }

  obtenerRol(): string {
    return this.obtenerSesion()?.rol || '';
  }

  obtenerIdUsuario(): number {
    return Number(this.obtenerSesion()?.idUsuario || 0);
  }

  obtenerPermisos(): string[] {
    return this.obtenerSesion()?.permisos || [];
  }

  estaAutenticado(): boolean {
    const sesion = this.obtenerSesion();

    if (!sesion || !sesion.token) {
      return false;
    }

    if (!sesion.expira) {
      return true;
    }

    const fechaExpiracion = new Date(sesion.expira).getTime();

    if (!Number.isNaN(fechaExpiracion) && fechaExpiracion <= Date.now()) {
      this.cerrarSesion();
      return false;
    }

    return true;
  }

  esAdministrador(): boolean {
    const rol = this.normalizarTexto(this.obtenerRol());

    return rol === 'administrador' || rol === 'admin';
  }

  tienePermiso(permiso: string): boolean {
    const permisoNormalizado = String(permiso || '').trim().toUpperCase();

    if (!permisoNormalizado) {
      return true;
    }

    if (this.esAdministrador()) {
      return true;
    }

    const permisos = this.obtenerPermisos();

    if (permisos.includes(permisoNormalizado)) {
      return true;
    }

    return this.tienePermisoPorRol(permisoNormalizado);
  }

  tieneAlgunPermiso(permisos: string[] | undefined | null): boolean {
    if (!permisos || permisos.length === 0) {
      return true;
    }

    if (this.esAdministrador()) {
      return true;
    }

    return permisos.some(permiso => this.tienePermiso(permiso));
  }

  obtenerRutaInicial(): string {
    if (!this.estaAutenticado()) {
      return '/login';
    }

    if (this.esAdministrador()) {
      return '/admin/inicio';
    }

    const rol = this.normalizarTexto(this.obtenerRol());

    if (rol.includes('compras')) {
      return '/admin/compras';
    }

    if (rol.includes('encargado de almacen') || rol === 'almacen' || rol.includes('almacen')) {
      return '/admin/inventario';
    }

    if (rol.includes('encargado de ventas')) {
      return '/admin/ventas';
    }

    if (rol.includes('vendedor')) {
      return '/admin/cotizaciones';
    }

    const rutasPorPermiso: Array<{ permiso: string; ruta: string }> = [
      { permiso: 'COMPRAS_VER', ruta: '/admin/compras' },
      { permiso: 'PROVEEDORES_VER', ruta: '/admin/proveedores' },
      { permiso: 'INVENTARIO_VER', ruta: '/admin/inventario' },
      { permiso: 'VENTAS_VER', ruta: '/admin/ventas' },
      { permiso: 'COTIZACIONES_VER', ruta: '/admin/cotizaciones' },
      { permiso: 'CLIENTES_VER', ruta: '/admin/clientes' },
      { permiso: 'PRODUCTOS_VER', ruta: '/admin/productos' },
      { permiso: 'SERVICIOS_VER', ruta: '/admin/servicios' },
      { permiso: 'CAJA_VER', ruta: '/admin/caja' },
      { permiso: 'USUARIOS_VER', ruta: '/admin/usuarios-roles' },
      { permiso: 'ROLES_VER', ruta: '/admin/usuarios-roles' },
      { permiso: 'INICIO_VER', ruta: '/admin/inicio' }
    ];

    const rutaPermitida = rutasPorPermiso.find(item => this.tienePermiso(item.permiso));

    return rutaPermitida?.ruta || '/admin/cotizaciones';
  }

  cerrarSesion(): void {
    localStorage.removeItem(this.storageKey);
    localStorage.removeItem('sistema3s_session');
    localStorage.removeItem('usuario');
    localStorage.removeItem('usuarioActual');
    localStorage.removeItem('authUser');
    localStorage.removeItem('user');
    localStorage.removeItem('sesionUsuario');
  }

  private tienePermisoPorRol(permiso: string): boolean {
    const rol = this.normalizarTexto(this.obtenerRol());

    const permisosPorRol: Record<string, string[]> = {
      administrador: ['*'],

      'compras / almacen': [
        'COMPRAS_VER',
        'COMPRAS_CREAR',
        'COMPRAS_EDITAR',
        'COMPRAS_ANULAR',
        'PROVEEDORES_VER',
        'PROVEEDORES_CREAR',
        'PROVEEDORES_EDITAR',
        'INVENTARIO_VER',
        'INVENTARIO_MOVIMIENTOS'
      ],

      'encargado de almacen': [
        'INVENTARIO_VER',
        'INVENTARIO_MOVIMIENTOS'
      ],

      'encargado de ventas': [
        'CLIENTES_VER',
        'CLIENTES_CREAR',
        'CLIENTES_EDITAR',
        'COTIZACIONES_VER',
        'COTIZACIONES_CREAR',
        'COTIZACIONES_EDITAR',
        'VENTAS_VER',
        'VENTAS_CREAR'
      ],

      vendedor: [
        'CLIENTES_VER',
        'CLIENTES_CREAR',
        'COTIZACIONES_VER',
        'COTIZACIONES_CREAR'
      ]
    };

    let permisosRol: string[] = [];

    if (rol.includes('compras')) {
      permisosRol = permisosPorRol['compras / almacen'];
    } else if (rol.includes('encargado de almacen') || rol.includes('almacen')) {
      permisosRol = permisosPorRol['encargado de almacen'];
    } else if (rol.includes('encargado de ventas')) {
      permisosRol = permisosPorRol['encargado de ventas'];
    } else if (rol.includes('vendedor')) {
      permisosRol = permisosPorRol['vendedor'];
    } else {
      permisosRol = permisosPorRol[rol] || [];
    }

    return permisosRol.includes('*') || permisosRol.includes(permiso);
  }

  private normalizarPermisos(data: any, payload: any): string[] {
    const permisosDirectos = data?.permisos || data?.Permisos || [];
    const permisosDetalle = data?.permisosDetalle || data?.PermisosDetalle || [];
    const permisosPayload = this.extraerPermisosPayload(payload);

    return this.normalizarListaPermisos([
      ...permisosDirectos,
      ...permisosDetalle,
      ...permisosPayload
    ]);
  }

  private extraerPermisosPayload(payload: any): any[] {
    if (!payload) {
      return [];
    }

    const posibles = [
      payload.permiso,
      payload.permisos,
      payload.Permiso,
      payload.Permisos
    ];

    const resultado: any[] = [];

    for (const item of posibles) {
      if (!item) {
        continue;
      }

      if (Array.isArray(item)) {
        resultado.push(...item);
      } else {
        resultado.push(item);
      }
    }

    return resultado;
  }

  private normalizarListaPermisos(lista: any[]): string[] {
    if (!Array.isArray(lista)) {
      return [];
    }

    const permisos = lista
      .map(item => {
        if (typeof item === 'string') {
          return item;
        }

        return item?.nombre ||
          item?.Nombre ||
          item?.permiso ||
          item?.Permiso ||
          '';
      })
      .filter(valor => !!valor)
      .map(valor => String(valor).trim().toUpperCase());

    return Array.from(new Set(permisos));
  }

  private obtenerPayloadToken(token: string): any {
    if (!token || !token.includes('.')) {
      return null;
    }

    try {
      const payloadBase64 = token.split('.')[1];
      const base64 = payloadBase64.replace(/-/g, '+').replace(/_/g, '/');
      const payloadJson = decodeURIComponent(
        atob(base64)
          .split('')
          .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      );

      return JSON.parse(payloadJson);
    } catch {
      return null;
    }
  }

  private obtenerExpiracionDesdeToken(token: string): string | null {
    const payload = this.obtenerPayloadToken(token);

    if (!payload?.exp) {
      return null;
    }

    return new Date(payload.exp * 1000).toISOString();
  }

  private normalizarTexto(valor: string): string {
    return String(valor || '')
      .trim()
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '');
  }
}