import { AvisoComponent } from '../../../shared/mensajes/aviso';
import { PublicAccountNavComponent } from '../../shared/account-nav';
import { ImagenCatalogoComponent } from '../../../shared/imagen-catalogo/imagen-catalogo';
import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnInit
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import {
  CotizacionWebDetalle,
  CotizacionWebDetalleItem
} from '../../../core/models/cotizacion-web.model';
import { ClienteWebSesion } from '../../../core/models/cliente-web.model';
import { ClienteWebService } from '../../../core/services/cliente-web.service';
import { CotizacionWebService } from '../../../core/services/cotizacion-web.service';

@Component({
  selector: 'app-cliente-cotizacion-detalle-publico',
  standalone: true,
  imports: [AvisoComponent, PublicAccountNavComponent, ImagenCatalogoComponent, CommonModule, RouterLink],
  templateUrl: './cliente-cotizacion-detalle-publico.html',
  styleUrl: './cliente-cotizacion-detalle-publico.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ClienteCotizacionDetallePublicoComponent implements OnInit {
  sesion: ClienteWebSesion | null = null;
  cotizacion: CotizacionWebDetalle | null = null;

  cargando = false;
  error = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private clienteWebService: ClienteWebService,
    private cotizacionWebService: CotizacionWebService,
    private cd: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.sesion = this.clienteWebService.obtenerSesion();

    if (!this.sesion) {
      this.router.navigate(['/cliente/login'], {
        queryParams: {
          returnUrl: this.router.url
        }
      });

      return;
    }

    const id = Number(this.route.snapshot.paramMap.get('id'));

    if (!id || id <= 0) {
      this.router.navigate(['/cliente/historial-cotizaciones']);
      return;
    }

    this.cargarDetalle(id);
  }

  cargarDetalle(idCotizacion: number): void {
    this.cargando = true;
    this.error = '';
    this.cotizacion = null;
    this.cd.markForCheck();

    this.cotizacionWebService.obtenerCotizacion(idCotizacion).subscribe({
      next: response => {
        this.cotizacion = response;
        this.cargando = false;
        this.cd.markForCheck();
      },
      error: error => {
        this.error =
          error?.error?.mensaje ||
          'No pudimos cargar el detalle de la solicitud.';

        this.cargando = false;
        this.cd.markForCheck();
      }
    });
  }

  volverHistorial(): void {
    this.router.navigate(['/cliente/historial-cotizaciones']);
  }

  seguirCotizando(): void {
    this.router.navigate(['/productos']);
  }

  contactarWhatsApp(): void {
    const telefono = '51948327667';

    const mensaje = [
      'Hola, deseo consultar sobre una solicitud de cotización enviada desde mi cuenta web.',
      '',
      this.sesion ? `Cliente: ${this.sesion.nombreCliente}` : '',
      this.sesion ? `${this.sesion.tipoDocumento}: ${this.sesion.numeroDocumento}` : '',
      '',
      'Quedo atento a la atención del equipo comercial.'
    ]
      .filter(linea => linea !== '')
      .join('\n');

    const url = `https://wa.me/${telefono}?text=${encodeURIComponent(mensaje)}`;

    window.open(url, '_blank');
  }

  obtenerEstadoCliente(estado: string): string { return estado || 'Sin estado'; }

  obtenerClaseEstado(estado: string): string {
    const valor = (estado || '').trim().toLowerCase();

    if (valor === 'respondida') {
      return 'respondida';
    }

    if (valor === 'aprobada' || valor === 'convertida en venta') {
      return 'atendida';
    }

    if (valor === 'cancelada') {
      return 'cancelada';
    }

    return 'revision';
  }

  trackByDetalle(_: number, item: CotizacionWebDetalleItem): number {
    return item.idDetalleCotizacion;
  }
}