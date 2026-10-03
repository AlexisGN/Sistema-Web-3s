export interface ContactoProveedor {
  idContactoProveedor?: number | null;

  nombres: string;
  apellidoPaterno: string;
  apellidoMaterno?: string | null;

  cargo?: string | null;
  correo?: string | null;
  telefono?: string | null;

  estado?: boolean;
}

export interface ProveedorListado {
  idProveedor: number;

  idUbigeo?: number | null;
  ubicacion?: string | null;
  departamento?: string | null;
  provincia?: string | null;
  distrito?: string | null;

  ruc: string;
  razonSocial: string;
  nombreComercial?: string | null;

  correo?: string | null;
  telefono?: string | null;
  direccion?: string | null;

  estado: boolean;

  contactoPrincipal?: ContactoProveedor | null;
}

export interface ProveedorCrear {
  idUbigeo?: number | null;

  ruc: string;
  razonSocial: string;
  nombreComercial?: string | null;

  correo: string;
  telefono: string;
  direccion: string;

  registrarContactoPrincipal: boolean;
  contactoPrincipal?: ContactoProveedor | null;
}

export interface ProveedorActualizar {
  idUbigeo?: number | null;

  ruc: string;
  razonSocial: string;
  nombreComercial?: string | null;

  correo: string;
  telefono: string;
  direccion: string;

  registrarContactoPrincipal: boolean;
  contactoPrincipal?: ContactoProveedor | null;

  estado: boolean;
}

export interface ConsultaRucProveedorResultado {
  exitoso: boolean;
  proveedorYaExiste: boolean;
  idProveedorExistente?: number | null;

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

export interface Ubigeo {
  idUbigeo: number;
  departamento: string;
  provincia: string;
  distrito: string;
  codigoUbigeo?: string | null;
  ubicacion: string;
}

export interface ResultadoPaginado<T> {
  items: T[];
  pagina: number;
  tamanioPagina: number;
  totalRegistros: number;
  totalPaginas: number;
}