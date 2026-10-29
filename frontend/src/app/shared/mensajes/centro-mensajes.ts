import { AfterViewChecked, Component, ElementRef, OnDestroy, ViewChild, inject } from '@angular/core';
import { UiIconComponent } from '../ui-icon/ui-icon';
import { AvisoComponent } from './aviso';
import { MensajesService } from './mensajes.service';

@Component({ selector: 's3s-centro-mensajes', standalone: true, imports: [UiIconComponent, AvisoComponent],
  templateUrl: './centro-mensajes.html', styleUrl: './centro-mensajes.scss' })
export class CentroMensajesComponent implements AfterViewChecked, OnDestroy {
  readonly mensajes = inject(MensajesService);
  @ViewChild('dialogo') private dialogo!: ElementRef<HTMLDialogElement>;
  @ViewChild('cancelar') private cancelar?: ElementRef<HTMLButtonElement>;
  private focoAnterior: HTMLElement | null = null;

  ngAfterViewChecked(): void {
    const elemento = this.dialogo.nativeElement;
    if (this.mensajes.dialogo() && !elemento.open) {
      this.focoAnterior = document.activeElement as HTMLElement | null;
      elemento.showModal();
      this.cancelar?.nativeElement.focus({ preventScroll: true });
    } else if (!this.mensajes.dialogo() && elemento.open) {
      elemento.close();
      if (this.focoAnterior?.isConnected) this.focoAnterior.focus({ preventScroll: true });
      this.focoAnterior = null;
    }
  }

  cancelarDialogo(event: Event): void { event.preventDefault(); this.mensajes.resolver(false); }
  ngOnDestroy(): void { this.mensajes.resolver(false); }
}
