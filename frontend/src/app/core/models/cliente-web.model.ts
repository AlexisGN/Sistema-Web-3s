export interface ClienteWebLoginRequest {
  correo: string;
  contrasena: string;
}

export interface ClienteWebRegistroRequest {
  tipoDocumento: 'DNI' | 'RUC';
  numeroDocumento: string;

  correo: string;
  telefono: string;
  direccion?: string | null;

  contrasena: string;
  confirmarContrasena: string;

  nombres?: string | null;
  apellidoPaterno?: string | null;
  apellidoMaterno?: string | null;

  razonSocial?: string | null;
  nombreComercial?: string | null;
}

export interface ClienteWebSesion {
  idCliente: number;
  idUsuario: number;
  idRol: number;

  correo: string;
  rol: string;

  tipoDocumento: string;
  numeroDocumento: string;
  tipoCliente: string;
  nombreCliente: string;

  esEmpresa: boolean;

  token: string;
  expira: string;

  mensaje: string;

  IdCliente?: number;
  IdUsuario?: number;
  IdRol?: number;
  Correo?: string;
  Rol?: string;
  TipoDocumento?: string;
  NumeroDocumento?: string;
  TipoCliente?: string;
  NombreCliente?: string;
  EsEmpresa?: boolean;
  Token?: string;
  Expira?: string;
  Mensaje?: string;
}
export interface ClienteWebConsultaDocumentoResponse {
  tipoDocumento: 'DNI' | 'RUC' | string;
  numeroDocumento: string;

  exitoso: boolean;
  clienteYaExiste: boolean;
  cuentaWebVinculada: boolean;

  idClienteExistente: number | null;

  correoExistente: string | null;
  telefonoExistente: string | null;
  direccionExistente: string | null;

  nombres: string | null;
  apellidoPaterno: string | null;
  apellidoMaterno: string | null;
  nombreCompleto: string | null;

  razonSocial: string | null;
  nombreComercial: string | null;

  estadoSunat: string | null;
  condicionSunat: string | null;

  codigoUbigeo: string | null;
  idUbigeo: number | null;
  ubicacion: string | null;

  mensaje: string;

  TipoDocumento?: string;
  NumeroDocumento?: string;
  Exitoso?: boolean;
  ClienteYaExiste?: boolean;
  CuentaWebVinculada?: boolean;
  IdClienteExistente?: number | null;
  CorreoExistente?: string | null;
  TelefonoExistente?: string | null;
  DireccionExistente?: string | null;
  Nombres?: string | null;
  ApellidoPaterno?: string | null;
  ApellidoMaterno?: string | null;
  NombreCompleto?: string | null;
  RazonSocial?: string | null;
  NombreComercial?: string | null;
  EstadoSunat?: string | null;
  CondicionSunat?: string | null;
  CodigoUbigeo?: string | null;
  IdUbigeo?: number | null;
  Ubicacion?: string | null;
  Mensaje?: string;
}