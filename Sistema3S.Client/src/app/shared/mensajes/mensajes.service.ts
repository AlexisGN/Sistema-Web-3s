import { Injectable, OnDestroy, inject, signal } from '@angular/core';
import { NavigationStart, Router } from '@angular/router';
import { Subscription } from 'rxjs';

export type TipoMensaje = 'success' | 'error' | 'warning' | 'info';
export interface OpcionesConfirmacion {
  titulo?: string;
  aceptar?: string;
  tipo?: TipoMensaje;
  icono?: string;
}
export interface ConfirmacionVisual {
  mensaje: string;
  titulo: string;
  aceptar: string;
  tipo: TipoMensaje;
  icono: string;
}
export interface AvisoVisual { id: number; mensaje: string; tipo: TipoMensaje; titulo?: string; }

/** Sólo presentación: cada módulo conserva sus validaciones y decide qué hacer al aceptar. */
@Injectable({ providedIn: 'root' })
export class MensajesService implements OnDestroy {
  private readonly estadoDialogo = signal<ConfirmacionVisual | null>(null);
  private readonly estadoAvisos = signal<AvisoVisual[]>([]);
  readonly dialogo = this.estadoDialogo.asReadonly();
  readonly avisos = this.estadoAvisos.asReadonly();
  private resolverPendiente?: (aceptado: boolean) => void;
  private secuencia = 0;
  private readonly navegacion?: Subscription;

  constructor() {
    this.navegacion = inject(Router, { optional: true })?.events.subscribe(event => {
      if (event instanceof NavigationStart) {
        this.resolver(false);
        this.estadoAvisos.set([]);
      }
    });
  }

  confirmar(mensaje: string, opciones: OpcionesConfirmacion = {}): Promise<boolean> {
    // Evita dos acciones por un doble clic mientras se abre el mismo diálogo.
    if (this.resolverPendiente) return Promise.resolve(false);
    return new Promise<boolean>(resolve => {
      this.resolverPendiente = resolve;
      this.estadoDialogo.set({ mensaje, titulo: opciones.titulo || 'Confirmar operación',
        aceptar: opciones.aceptar || 'Confirmar', tipo: opciones.tipo || 'warning', icono: opciones.icono || 'help' });
    });
  }

  resolver(aceptado: boolean): void {
    const pendiente = this.resolverPendiente;
    this.resolverPendiente = undefined;
    this.estadoDialogo.set(null);
    pendiente?.(aceptado);
  }

  advertir(mensaje: string): void { this.notificar(mensaje, 'warning', 'Revisa la configuración'); }

  notificar(mensaje: string, tipo: TipoMensaje = 'info', titulo?: string): void {
    if (!this.avisos().some(aviso => aviso.mensaje === mensaje && aviso.tipo === tipo)) {
      this.estadoAvisos.update(avisos => [...avisos, { id: ++this.secuencia, mensaje, tipo, titulo }]);
    }
  }

  cerrarAviso(id: number): void { this.estadoAvisos.update(avisos => avisos.filter(aviso => aviso.id !== id)); }
  ngOnDestroy(): void { this.resolver(false); this.navegacion?.unsubscribe(); }
}
