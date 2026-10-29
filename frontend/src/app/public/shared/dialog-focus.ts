import { AfterViewInit, Directive, ElementRef, EventEmitter, HostListener, OnDestroy, Output } from '@angular/core';

/** Mantiene la navegación de teclado en la confirmación y devuelve el foco al cerrar. */
@Directive({ selector: '[s3sDialogFocus]', standalone: true, host: { role: 'dialog', 'aria-modal': 'true', tabindex: '-1' } })
export class PublicDialogFocusDirective implements AfterViewInit, OnDestroy {
  @Output() dialogEscape = new EventEmitter<void>();
  private previous = document.activeElement as HTMLElement | null;
  constructor(private element: ElementRef<HTMLElement>) {}
  private controls(): HTMLElement[] {
    return Array.from(this.element.nativeElement.querySelectorAll<HTMLElement>('button:not(:disabled),a[href],input:not(:disabled),[tabindex="0"]')).filter(e => e.getClientRects().length > 0);
  }
  ngAfterViewInit(): void { (this.controls()[0] || this.element.nativeElement).focus(); }
  @HostListener('keydown', ['$event']) onKey(event: KeyboardEvent): void {
    if (event.key === 'Escape') { event.preventDefault(); this.dialogEscape.emit(); }
    if (event.key !== 'Tab') return;
    const controls = this.controls();
    const first = controls[0]; const last = controls[controls.length - 1];
    if (!first) { event.preventDefault(); return; }
    if (event.shiftKey && (document.activeElement === first || document.activeElement === this.element.nativeElement)) { event.preventDefault(); last.focus(); }
    else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
  }
  ngOnDestroy(): void { if (this.previous?.isConnected) this.previous.focus(); }
}
