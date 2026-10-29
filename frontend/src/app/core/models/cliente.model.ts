export interface ClienteContacto {
  idContactoCliente?: number | null;

  nombres?: string | null;
  apellidoPaterno?: string | null;
  apellidoMaterno?: string | null;

  cargo?: string | null;
  correo?: string | null;
  telefono?: string | null;

  estado?: boolean;
}

export interface ClienteListado {
  idCliente: number;

  idTipoCliente: number;
  tipoCliente: string;

  idTipoDocumento: number;
  tipoDocumento: string;

  idUbigeo?: number | null;
  ubicacion?: string | null;

  idUsuario?: number | null;
  cuentaWebVinculada: boolean;

  numeroDocumento: string;
  cliente: string;

  nombres?: string | null;
  apellidoPaterno?: string | null;
  apellidoMaterno?: string | null;

  razonSocial?: string | null;
  nombreComercial?: string | null;

  correo?: string | null;
  telefono?: string | null;
  direccion?: string | null;

  estado: boolean;
  fechaRegistro: string;

  tieneContactoPrincipal: boolean;
  contactoPrincipal?: ClienteContacto | null;
}

export interface ClienteCrear {
  idTipoCliente: number;
  idTipoDocumento: number;
  idUbigeo?: number | null;

  numeroDocumento: string;

  correo?: string | null;
  telefono?: string | null;
  direccion?: string | null;

  nombres?: string | null;
  apellidoPaterno?: string | null;
  apellidoMaterno?: string | null;

  razonSocial?: string | null;
  nombreComercial?: string | null;

  registrarContactoPrincipal: boolean;
  contactoPrincipal?: ClienteContacto | null;
}

export interface ClienteActualizar {
  idTipoCliente: number;
  idTipoDocumento: number;
  idUbigeo?: number | null;

  numeroDocumento: string;

  correo?: string | null;
  telefono?: string | null;
  direccion?: string | null;

  nombres?: string | null;
  apellidoPaterno?: string | null;
  apellidoMaterno?: string | null;

  razonSocial?: string | null;
  nombreComercial?: string | null;

  registrarContactoPrincipal: boolean;
  contactoPrincipal?: ClienteContacto | null;
}

export interface TipoCliente {
  idTipoCliente: number;
  nombre: string;
  descripcion?: string | null;
  requiereRuc: boolean;
  requiereFactura: boolean;
}

export interface TipoDocumento {
  idTipoDocumento: number;
  nombre: string;
  longitud: number;
}

export interface Ubigeo {
  idUbigeo: number;
  codigoUbigeo?: string | null;
  departamento: string;
  provincia: string;
  distrito: string;
  ubicacion: string;
}

export interface ResultadoPaginadoClientes {
  items: ClienteListado[];
  pagina: number;
  tamanioPagina: number;
  totalRegistros: number;
  totalPaginas?: number;
}
export interface ConsultaDniResultado {
  exitoso: boolean;
  clienteYaExiste: boolean;
  idClienteExistente?: number | null;

  numeroDocumento: string;

  nombres?: string | null;
  apellidoPaterno?: string | null;
  apellidoMaterno?: string | null;
  nombreCompleto?: string | null;

  mensaje: string;
}

export interface ConsultaRucResultado {
  exitoso: boolean;
  clienteYaExiste: boolean;
  idClienteExistente?: number | null;

  numeroDocumento: string;

  razonSocial?: string | null;
  nombreComercial?: string | null;

  estadoSunat?: string | null;
  condicionSunat?: string | null;

  direccion?: string | null;

  codigoUbigeo?: string | null;
  idUbigeo?: number | null;
  ubicacion?: string | null;

  departamento?: string | null;
  provincia?: string | null;
  distrito?: string | null;

  mensaje: string;
}