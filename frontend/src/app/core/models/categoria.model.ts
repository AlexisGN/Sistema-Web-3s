export interface CategoriaGestion {
  id: number;
  nombre: string;
  descripcion: string | null;
  estado: boolean;
  productosAsignados: number;
  productosActivos: number;
}

export interface CategoriaGuardar {
  nombre: string;
  descripcion: string | null;
}
