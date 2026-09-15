export interface ServicioListado {
  idServicio: number;
  idElementoCatalogo: number;

  nombre: string;
  descripcion?: string;
  precioReferencial?: number | null;
  imagenUrl?: string | null;

  sectorAplicacion?: string | null;
  mensajeWhatsApp?: string | null;
  requiereVisitaTecnica: boolean;

  estado: boolean;
}

export interface ServicioCrear {
  nombre: string;
  descripcion?: string | null;
  precioReferencial?: number | null;
  imagenUrl?: string | null;

  sectorAplicacion?: string | null;
  mensajeWhatsApp?: string | null;
  requiereVisitaTecnica: boolean;
}

export interface ServicioActualizar {
  nombre: string;
  descripcion?: string | null;
  precioReferencial?: number | null;
  imagenUrl?: string | null;

  sectorAplicacion?: string | null;
  mensajeWhatsApp?: string | null;
  requiereVisitaTecnica: boolean;

  estado: boolean;
}