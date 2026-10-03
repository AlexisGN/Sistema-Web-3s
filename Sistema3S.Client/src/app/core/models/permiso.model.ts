export interface Permiso {
  idPermiso: number;
  nombre: string;
  descripcion?: string | null;
  estado: boolean;
  asignado: boolean;
}