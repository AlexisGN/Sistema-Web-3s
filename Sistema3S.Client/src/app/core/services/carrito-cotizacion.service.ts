import { Injectable } from '@angular/core';

import { ClienteWebService } from './cliente-web.service';

@Injectable({
  providedIn: 'root'
})
export class CarritoCotizacionService {
  private readonly claveBase = 'carritoCotizacion3S';

  constructor(private clienteWebService: ClienteWebService) {}

  private obtenerClaveCarrito(): string {
    const sesion = this.clienteWebService.obtenerSesion();

    if (!sesion) {
      return `${this.claveBase}_sin_sesion`;
    }

    const idCliente = Number(
      (sesion as any).idCliente ||
      (sesion as any).id ||
      (sesion as any).idClienteWeb ||
      0
    );

    if (idCliente > 0) {
      return `${this.claveBase}_cliente_${idCliente}`;
    }

    const documento = `${sesion.tipoDocumento || ''}_${sesion.numeroDocumento || ''}`
      .trim()
      .replace(/\s+/g, '_');

    return documento
      ? `${this.claveBase}_${documento}`
      : `${this.claveBase}_sin_sesion`;
  }

  obtenerItems<T>(): T[] {
    const clave = this.obtenerClaveCarrito();
    const data = localStorage.getItem(clave);

    if (!data) {
      return [];
    }

    try {
      const items = JSON.parse(data);
      return Array.isArray(items) ? items : [];
    } catch {
      return [];
    }
  }

  guardarItems<T>(
    items: T[],
    productoAgregado?: string,
    despacharEvento = true
  ): void {
    const clave = this.obtenerClaveCarrito();

    if (!items || items.length === 0) {
      localStorage.removeItem(clave);
    } else {
      localStorage.setItem(clave, JSON.stringify(items));
    }

    if (despacharEvento) {
      this.notificarActualizacion(productoAgregado);
    }
  }

  limpiarCarrito(despacharEvento = true): void {
    const clave = this.obtenerClaveCarrito();
    localStorage.removeItem(clave);

    if (despacharEvento) {
      this.notificarActualizacion();
    }
  }

  contarItems(): number {
    const items = this.obtenerItems<any>();

    return items.reduce((total, item) => {
      const cantidad = Number(item?.cantidad || 1);
      return total + (cantidad > 0 ? cantidad : 1);
    }, 0);
  }

  notificarActualizacion(productoAgregado?: string): void {
    window.dispatchEvent(
      new CustomEvent('carritoCotizacionActualizado', {
        detail: {
          mostrarToast: !!productoAgregado,
          producto: productoAgregado
        }
      })
    );
  }
}