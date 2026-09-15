import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';

import {
  ImagenPublica,
  ServicioDetallePublico,
  ServicioPublico
} from '../../../core/models/publico.model';
import { PublicoService } from '../../../core/services/publico.service';

@Component({
  selector: 'app-servicio-detalle-publico',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './servicio-detalle-publico.html',
  styleUrl: './servicio-detalle-publico.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ServicioDetallePublicoComponent implements OnInit, OnDestroy {
  servicio?: ServicioDetallePublico;
  imagenes: ImagenPublica[] = [];
  imagenSeleccionada = '';

  relacionados: ServicioPublico[] = [];

  cargando = false;
  error = '';

  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private publicoService: PublicoService,
    private cd: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.route.paramMap
      .pipe(takeUntil(this.destroy$))
      .subscribe(params => {
        const idServicio = Number(params.get('id') || 0);

        if (idServicio <= 0) {
          this.error = 'No pudimos identificar el servicio solicitado.';
          this.cd.markForCheck();
          return;
        }

        this.cargarServicio(idServicio);
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  cargarServicio(idServicio: number): void {
    this.cargando = true;
    this.error = '';
    this.servicio = undefined;
    this.imagenes = [];
    this.imagenSeleccionada = '';
    this.relacionados = [];
    this.cd.markForCheck();

    this.publicoService.obtenerServicioDetalle(idServicio)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: servicio => {
          this.servicio = servicio;

          this.imagenes = this.construirGaleria(servicio);
          this.imagenSeleccionada = this.imagenes[0]?.urlImagen || servicio.imagenUrl || '';

          this.cargando = false;
          this.cargarRelacionados(servicio);
          this.cd.markForCheck();
        },
        error: () => {
          this.error = 'El servicio solicitado no se encuentra disponible en el catálogo público.';
          this.cargando = false;
          this.cd.markForCheck();
        }
      });
  }

  cargarRelacionados(servicioActual: ServicioDetallePublico): void {
    this.publicoService.obtenerServicios()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: servicios => {
          const sector = servicioActual.sectorAplicacion?.trim().toLowerCase();

          this.relacionados = (servicios || [])
            .filter(servicio => (servicio.idServicio || servicio.id) !== (servicioActual.idServicio || servicioActual.id))
            .filter(servicio => {
              if (!sector) {
                return true;
              }

              return (servicio.sectorAplicacion || '').trim().toLowerCase() === sector;
            })
            .slice(0, 3);

          if (this.relacionados.length === 0) {
            this.relacionados = (servicios || [])
              .filter(servicio => (servicio.idServicio || servicio.id) !== (servicioActual.idServicio || servicioActual.id))
              .slice(0, 3);
          }

          this.cd.markForCheck();
        },
        error: () => {
          this.relacionados = [];
          this.cd.markForCheck();
        }
      });
  }

  construirGaleria(servicio: ServicioDetallePublico): ImagenPublica[] {
    const imagenes = servicio.imagenes || [];

    if (imagenes.length > 0) {
      return imagenes;
    }

    if (servicio.imagenUrl) {
      return [
        {
          idImagen: 0,
          urlImagen: servicio.imagenUrl,
          textoAlternativo: servicio.nombre,
          esPrincipal: true
        }
      ];
    }

    return [];
  }

  seleccionarImagen(imagen: ImagenPublica): void {
    this.imagenSeleccionada = imagen.urlImagen;
  }

  abrirWhatsAppServicio(servicio: ServicioPublico): void {
    const telefono = '51948327667';

    const mensajeBase = servicio.mensajeWhatsApp?.trim();

    const mensaje = mensajeBase
      ? mensajeBase
      : [
          'Hola, deseo solicitar información sobre este servicio:',
          '',
          `Servicio: ${servicio.nombre}`,
          servicio.sectorAplicacion ? `Sector de aplicación: ${servicio.sectorAplicacion}` : '',
          servicio.requiereVisitaTecnica ? 'Atención técnica: requiere evaluación o coordinación especializada.' : '',
          '',
          'Quedo atento a la atención del área comercial.'
        ]
          .filter(linea => linea !== '')
          .join('\n');

    const url = `https://wa.me/${telefono}?text=${encodeURIComponent(mensaje)}`;

    window.open(url, '_blank');
  }

  verServicioRelacionado(servicio: ServicioPublico): void {
    this.router.navigate(['/servicios', servicio.idServicio || servicio.id]);
  }

  volverServicios(): void {
    this.router.navigate(['/servicios']);
  }

  trackByImagen(_: number, imagen: ImagenPublica): number {
    return imagen.idImagen;
  }

  trackByServicio(_: number, servicio: ServicioPublico): number {
    return servicio.idServicio || servicio.id;
  }
}