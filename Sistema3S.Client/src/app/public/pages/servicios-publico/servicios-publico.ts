import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';

import { ServicioPublico } from '../../../core/models/publico.model';
import { PublicoService } from '../../../core/services/publico.service';

@Component({
  selector: 'app-servicios-publico',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './servicios-publico.html',
  styleUrl: './servicios-publico.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ServiciosPublicoComponent implements OnInit, OnDestroy {
  cargando = false;
  error = '';

  busqueda = '';
  sectorSeleccionado = '';

  servicios: ServicioPublico[] = [];
  serviciosFiltrados: ServicioPublico[] = [];
  sectores: string[] = [];

  private destroy$ = new Subject<void>();

  constructor(
    private publicoService: PublicoService,
    private router: Router,
    private cd: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.cargarServicios();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  cargarServicios(): void {
    this.cargando = true;
    this.error = '';

    this.publicoService.obtenerServicios()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: servicios => {
          this.servicios = servicios || [];
          this.sectores = this.obtenerSectores(this.servicios);
          this.aplicarFiltros();

          this.cargando = false;
          this.cd.markForCheck();
        },
        error: () => {
          this.servicios = [];
          this.serviciosFiltrados = [];
          this.sectores = [];
          this.error = 'No pudimos cargar los servicios en este momento. Intenta nuevamente en unos instantes.';
          this.cargando = false;
          this.cd.markForCheck();
        }
      });
  }

  aplicarFiltros(): void {
    const texto = this.busqueda.trim().toLowerCase();
    const sector = this.sectorSeleccionado.trim().toLowerCase();

    this.serviciosFiltrados = this.servicios.filter(servicio => {
      const coincideTexto =
        !texto ||
        servicio.nombre.toLowerCase().includes(texto) ||
        servicio.descripcion.toLowerCase().includes(texto) ||
        (servicio.sectorAplicacion || '').toLowerCase().includes(texto);

      const coincideSector =
        !sector ||
        (servicio.sectorAplicacion || '').toLowerCase() === sector;

      return coincideTexto && coincideSector;
    });
  }

  buscarServicios(): void {
    this.aplicarFiltros();
  }

  seleccionarSector(sector: string): void {
    this.sectorSeleccionado = sector;
    this.aplicarFiltros();
  }

  limpiarFiltros(): void {
    this.busqueda = '';
    this.sectorSeleccionado = '';
    this.aplicarFiltros();
  }

  verDetalle(servicio: ServicioPublico): void {
    this.router.navigate(['/servicios', servicio.idServicio || servicio.id]);
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

  private obtenerSectores(servicios: ServicioPublico[]): string[] {
    const sectores = servicios
      .map(servicio => servicio.sectorAplicacion?.trim())
      .filter((sector): sector is string => !!sector);

    return Array.from(new Set(sectores)).sort((a, b) => a.localeCompare(b));
  }

  trackByServicio(_: number, servicio: ServicioPublico): number {
    return servicio.idServicio || servicio.id;
  }

  trackBySector(_: number, sector: string): string {
    return sector;
  }
}