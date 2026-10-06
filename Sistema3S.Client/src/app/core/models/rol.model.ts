export interface RolListado {
  idRol: number;
  nombre: string;
  descripcion?: string | null;
  estado: boolean;
  totalPermisos: number;
  totalUsuarios: number;
}

export interface RolCrear {
  nombre: string;
  descripcion?: string | null;
}

export interface RolActualizar {
  nombre: string;
  descripcion?: string | null;
  estado: boolean;
}

export interface RolPermisosActualizar {
  idsPermisos: number[];
}

export interface RolOperacionResultado {
  mensaje: string;
  idRol: number;
}

export interface RolRespuestaListado {
  mensaje: string;
  roles: RolListado[];
}