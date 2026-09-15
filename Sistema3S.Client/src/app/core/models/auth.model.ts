export interface LoginRequest {
  correo: string;
  contrasena: string;
}

export interface PermisoSesion {
  idPermiso?: number;
  nombre?: string;
  descripcion?: string | null;
  asignado?: boolean;
}

export interface LoginResponse {
  idUsuario?: number;
  idRol?: number;
  correo?: string;
  rol?: string;
  token?: string;
  expira?: string;
  fechaExpiracion?: string;
  expiracion?: string;
  permisos?: Array<string | PermisoSesion>;
  permisosDetalle?: PermisoSesion[];

  IdUsuario?: number;
  IdRol?: number;
  Correo?: string;
  Rol?: string;
  Token?: string;
  Expira?: string;
  FechaExpiracion?: string;
  Expiracion?: string;
  Permisos?: Array<string | PermisoSesion>;
  PermisosDetalle?: PermisoSesion[];
}

export interface SesionUsuario {
  idUsuario: number;
  idRol: number;
  correo: string;
  rol: string;
  token: string;
  expira: string;
  permisos: string[];
}