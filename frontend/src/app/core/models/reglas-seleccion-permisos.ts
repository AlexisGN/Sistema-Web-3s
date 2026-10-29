import { Permiso } from './permiso.model';

export const MINIMO_MODULO_VISIBLE = 'El rol debe tener al menos un módulo habilitado para visualización.';

export function baseRequerida(nombre: string): string | null {
  const codigo = nombre.trim().toUpperCase();
  const granular = /^([A-Z][A-Z0-9]*)_([A-Z][A-Z0-9_]*)$/.exec(codigo);
  if (granular) return granular[2] === 'VER' ? null : `${granular[1]}_VER`;
  const heredado = /^GESTIONAR ([A-Z][A-Z0-9]*)$/.exec(codigo);
  return heredado ? `${heredado[1]}_VER` : null;
}

export function esVisualizacionFuncional(nombre: string): boolean {
  const codigo = nombre.trim().toUpperCase();
  return codigo !== 'INICIO_VER' && /^[A-Z][A-Z0-9]*_VER$/.test(codigo);
}

export function validarSeleccionPermisos(catalogo: Permiso[]): string | null {
  const seleccionados = catalogo.filter(p => p.asignado);
  if (seleccionados.some(p => !p.estado)) return 'Uno o más permisos seleccionados no existen o están inactivos.';
  const nombres = new Set(seleccionados.map(p => p.nombre.trim().toUpperCase()));
  for (const nombre of nombres) {
    const requerido = baseRequerida(nombre);
    if (requerido && !nombres.has(requerido)) return `Debe activar ${requerido} antes de asignar otros permisos del módulo.`;
  }
  return [...nombres].some(esVisualizacionFuncional) ? null : MINIMO_MODULO_VISIBLE;
}
