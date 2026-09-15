import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';

import {
  BusquedaPublicaResponse,
  CategoriaPublica,
  MarcaPublica,
  ProductoPublico,
  ServicioPublico
} from '../../../core/models/publico.model';
import { PublicoService } from '../../../core/services/publico.service';

@Component({
  selector: 'app-busqueda-publica',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './busqueda-publica.html',
  styleUrl: './busqueda-publica.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BusquedaPublicaComponent implements OnInit, OnDestroy {
  q = '';
  busquedaLocal = '';

  cargando = false;
  error = '';

  resultado: BusquedaPublicaResponse | null = null;

  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private publicoService: PublicoService,
    private cd: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.route.queryParamMap
      .pipe(takeUntil(this.destroy$))
      .subscribe(params => {
        this.q = (params.get('q') || '').trim();
        this.busquedaLocal = this.q;

        if (!this.q) {
          this.resultado = null;
          this.error = '';
          this.cargando = false;
          this.cd.markForCheck();
          return;
        }

        this.buscar();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  buscarDesdePagina(): void {
    const texto = this.busquedaLocal.trim();

    this.router.navigate(['/buscar'], {
      queryParams: texto ? { q: texto } : {}
    });
  }

  buscar(): void {
    this.cargando = true;
    this.error = '';
    this.resultado = null;
    this.cd.markForCheck();

    this.publicoService.buscarGlobal(this.q, 8)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: response => {
          this.resultado = response;
          this.cargando = false;
          this.cd.markForCheck();
        },
        error: () => {
          this.error = 'No pudimos realizar la búsqueda en este momento.';
          this.resultado = null;
          this.cargando = false;
          this.cd.markForCheck();
        }
      });
  }

  get hayResultados(): boolean {
    return !!this.resultado && this.resultado.totalResultados > 0;
  }

  trackByProducto(_: number, producto: ProductoPublico): number {
    return producto.idProducto || producto.id;
  }

  trackByServicio(_: number, servicio: ServicioPublico): number {
    return servicio.idServicio || servicio.id;
  }

  trackByCategoria(_: number, categoria: CategoriaPublica): number {
    return categoria.idCategoria || categoria.id;
  }

  trackByMarca(_: number, marca: MarcaPublica): number {
    return marca.idMarca || marca.id;
  }
}