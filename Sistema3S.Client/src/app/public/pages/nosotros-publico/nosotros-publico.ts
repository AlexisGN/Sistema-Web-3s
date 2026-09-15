import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';

import {
  NosotrosPublicoResponse,
  SectorIndustrialPublico,
  ValorCorporativoPublico
} from '../../../core/models/publico.model';
import { PublicoService } from '../../../core/services/publico.service';

type MvHover = 'mision' | 'vision' | null;

@Component({
  selector: 'app-nosotros-publico',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './nosotros-publico.html',
  styleUrl: './nosotros-publico.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NosotrosPublicoComponent implements OnInit, OnDestroy {
  cargando = false;
  error = '';

  nosotros: NosotrosPublicoResponse | null = null;
  mvHover: MvHover = null;

  private destroy$ = new Subject<void>();

  constructor(
    private publicoService: PublicoService,
    private cd: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.cargarNosotros();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  cargarNosotros(): void {
    this.cargando = true;
    this.error = '';
    this.cd.markForCheck();

    this.publicoService.obtenerNosotros()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: response => {
          this.nosotros = response;
          this.cargando = false;
          this.cd.markForCheck();
        },
        error: () => {
          this.error = 'No pudimos cargar la información de la empresa en este momento.';
          this.cargando = false;
          this.cd.markForCheck();
        }
      });
  }

  cambiarHoverMv(valor: MvHover): void {
    this.mvHover = valor;
  }

  iconoIndustria(nombre: string): string {
    const valor = nombre.toLowerCase();

    if (valor.includes('miner')) {
      return '▣';
    }

    if (valor.includes('agro')) {
      return '⌁';
    }

    if (valor.includes('papel')) {
      return '▤';
    }

    if (valor.includes('textil')) {
      return '♧';
    }

    if (valor.includes('quím') || valor.includes('quim')) {
      return '△';
    }

    return '◈';
  }

  trackByIndustria(_: number, industria: SectorIndustrialPublico): number | string {
    return industria.idSectorIndustrial || industria.nombre;
  }

  trackByValor(_: number, valor: ValorCorporativoPublico): number | string {
    return valor.idValor || valor.nombre;
  }
}