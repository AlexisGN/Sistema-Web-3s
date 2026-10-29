export interface UsuarioListado {
  esCliente: boolean;
  nombreCliente: string;
  numeroDocumento: string;
  idUsuario: number;
  idRol: number;
  rol: string;
  correo: string;
  estado: boolean;
  fechaRegistro: string;
  totalPermisos: number;
}

export interface UsuarioCrear {
  idRol: number;
  correo: string;
  contrasena: string;
  estado: boolean;
}

export interface UsuarioActualizar {
  idRol: number;
  correo: string;
  estado: boolean;
}

export interface UsuarioCambiarContrasena {
  nuevaContrasena: string;
}

export interface UsuarioOperacionResultado {
  mensaje: string;
  idUsuario: number;
}