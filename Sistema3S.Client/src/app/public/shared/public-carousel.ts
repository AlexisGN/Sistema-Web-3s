import { NgTemplateOutlet } from '@angular/common';
import { AfterViewInit, Component, ContentChild, ElementRef, Input, NgZone, OnDestroy, TemplateRef, ViewChild } from '@angular/core';
import { UiIconComponent } from '../../shared/ui-icon/ui-icon';

@Component({
  selector: 's3s-public-carousel', standalone: true, imports: [NgTemplateOutlet, UiIconComponent],
  template: `<section class="public-carousel" [attr.aria-label]="label" aria-roledescription="carrusel">
    <div class="carousel-controls">
      <span>Explora y selecciona · Desliza para ver más</span>
      <button type="button" (click)="move(-1)" [attr.aria-label]="'Anterior: ' + label"><s3s-icon name="left" /></button>
      <button type="button" (click)="move(1)" [attr.aria-label]="'Siguiente: ' + label"><s3s-icon name="right" /></button>
      <button type="button" (click)="paused = !paused" [attr.aria-pressed]="paused" [disabled]="reduced">{{ paused || reduced ? 'Reanudar' : 'Pausar' }}</button>
    </div>
    <div #track class="carousel-track" tabindex="0" [attr.aria-label]="label + '. Usa las flechas o desliza para explorar.'"
      (keydown.arrowright)="move(1); $event.preventDefault()" (keydown.arrowleft)="move(-1); $event.preventDefault()">
      <div #group class="carousel-group"><ng-container [ngTemplateOutlet]="content" /></div>
      <div #copy class="carousel-group carousel-copy" aria-hidden="true"><ng-container [ngTemplateOutlet]="content" /></div>
    </div>
  </section>`,
  styles: [':host{display:block;min-width:0}']
})
export class PublicCarouselComponent implements AfterViewInit, OnDestroy {
  @Input() label = 'Catálogo';
  @Input() duration = 16;
  @ContentChild(TemplateRef) content!: TemplateRef<unknown>;
  @ViewChild('track') track!: ElementRef<HTMLElement>;
  @ViewChild('group') group!: ElementRef<HTMLElement>;
  @ViewChild('copy') copy!: ElementRef<HTMLElement>;
  paused = false;
  reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  private frame = 0; private previous = 0; private fraction = 0; private cycle = 0;
  private resize?: ResizeObserver;
  private media = window.matchMedia('(prefers-reduced-motion: reduce)');
  private motionChanged = () => { this.reduced = this.media.matches; };
  constructor(private zone: NgZone) {}

  ngAfterViewInit(): void {
    // Ambas copias son vistas Angular: todos los enlaces y botones conservan sus acciones.
    // La repetición visual no duplica el orden de navegación con teclado.
    this.copy.nativeElement.querySelectorAll<HTMLElement>('a,button,input,select,[tabindex]').forEach(el => el.tabIndex = -1);
    this.resize = new ResizeObserver(() => {
      this.cycle = this.copy.nativeElement.offsetLeft - this.group.nativeElement.offsetLeft;
    });
    this.resize.observe(this.group.nativeElement);
    this.media.addEventListener('change', this.motionChanged);
    this.zone.runOutsideAngular(() => this.frame = requestAnimationFrame(time => this.tick(time)));
  }
  ngOnDestroy(): void { cancelAnimationFrame(this.frame); this.resize?.disconnect(); this.media.removeEventListener('change', this.motionChanged); }
  move(direction: number): void {
    const el = this.track.nativeElement;
    const card = this.group.nativeElement.firstElementChild as HTMLElement | null;
    const next = el.scrollLeft + direction * ((card?.offsetWidth || el.clientWidth * .8) + 20);
    el.scrollLeft = this.cycle > 0 ? ((next % this.cycle) + this.cycle) % this.cycle : Math.max(0, next);
  }
  private tick(time: number): void {
    const delta = Math.min(time - (this.previous || time), 48); this.previous = time;
    const el = this.track.nativeElement;
    // El puntero y el foco no interrumpen el movimiento ni la selección de tarjetas.
    if (!this.paused && !this.reduced && !document.hidden && this.cycle > 0) {
      this.fraction += delta * this.cycle / (Math.max(1, this.duration) * 1000);
      const pixels = Math.floor(this.fraction); this.fraction -= pixels;
      el.scrollLeft += pixels;
      if (el.scrollLeft >= this.cycle) el.scrollLeft -= this.cycle;
    }
    this.frame = requestAnimationFrame(next => this.tick(next));
  }
}
