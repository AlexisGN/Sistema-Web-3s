import { Component, EventEmitter, Input, Output, ViewEncapsulation } from '@angular/core';
import { UiIconComponent } from '../ui-icon/ui-icon';
import { TipoMensaje } from './mensajes.service';

@Component({
  selector: 's3s-aviso', standalone: true, imports: [UiIconComponent],
  encapsulation: ViewEncapsulation.None, styleUrl: './aviso.scss',
  host: { class: 's3s-aviso', '[class.s3s-aviso--compacto]': 'compacto',
    '[attr.data-tipo]': 'tipo', '[attr.role]': "tipo === 'error' ? 'alert' : 'status'" },
  template: `<div class="s3s-aviso__fila"><span class="s3s-aviso__icono"><s3s-icon [name]="icono" /></span>
    <div class="s3s-aviso__contenido">
      @if (!compacto && mostrarTitulo) { <strong class="s3s-aviso__titulo">{{ titulo || tituloPredeterminado }}</strong> }
      <div class="s3s-aviso__texto"><ng-content /></div>
    </div>
    @if (cerrable) { <button type="button" class="s3s-aviso__cerrar" aria-label="Cerrar mensaje" (click)="cerrar.emit()"><s3s-icon name="close" /></button> }</div>`
})
export class AvisoComponent {
  @Input() tipo: TipoMensaje = 'info';
  @Input() titulo = '';
  @Input() compacto = false;
  @Input() mostrarTitulo = true;
  @Input() cerrable = false;
  @Output() cerrar = new EventEmitter<void>();
  get icono(): string { return { success: 'check', error: 'error', warning: 'warning', info: 'info' }[this.tipo]; }
  get tituloPredeterminado(): string {
    return { success: 'Operación completada', error: 'No se pudo completar', warning: 'Ten en cuenta', info: 'Información' }[this.tipo];
  }
}
