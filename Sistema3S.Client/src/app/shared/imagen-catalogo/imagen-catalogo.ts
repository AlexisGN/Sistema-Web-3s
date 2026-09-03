import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input, OnChanges } from '@angular/core';
import { environment } from '../../../environments/environment';

export function resolverImagenCatalogo(ruta: string | null | undefined, apiUrl = environment.apiUrl): string {
  const valor = (ruta || '').trim().replace(/\\/g, '/');
  if (!valor) return '';
  if (/^https?:\/\//i.test(valor)) return valor;
  if (/^(?:[a-z][a-z0-9+.-]*:|\/\/)/i.test(valor)) return '';
  const base = apiUrl.replace(/\/api\/?$/i, '').replace(/\/$/, '');
  if (/^\/?uploads\//i.test(valor)) return `${base}/${valor.replace(/^\//, '')}`;
  // Los recursos incluidos en Angular conservan su origen.
  return valor.startsWith('/') ? valor : `/${valor}`;
}

@Component({
  selector: 'app-imagen-catalogo', standalone: true, imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <img *ngIf="url && !fallo; else sinImagen" [src]="url" [alt]="descripcion" [style.object-fit]="ajuste"
      loading="lazy" decoding="async" (error)="fallo = true" />
    <ng-template #sinImagen><div class="placeholder" role="img" [attr.aria-label]="descripcion + ': imagen no disponible'">
      <strong>3S</strong><span>Imagen no disponible</span>
    </div></ng-template>`,
  styles: [`:host{display:block;width:100%;height:100%;min-width:0}img{display:block;width:100%;height:100%;object-fit:contain}.placeholder{height:100%;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:6px;background:#f4f7fa;color:#61728c;text-align:center;padding:10px;box-sizing:border-box}.placeholder strong{font-size:24px;color:#a61922}.placeholder span{font-size:12px}`]
})
export class ImagenCatalogoComponent implements OnChanges {
  @Input() ruta: string | null | undefined = '';
  @Input() descripcion = 'Producto';
  @Input() ajuste: 'contain' | 'cover' = 'contain';
  url = '';
  fallo = false;
  ngOnChanges(): void { this.url = resolverImagenCatalogo(this.ruta); this.fallo = false; }
}
