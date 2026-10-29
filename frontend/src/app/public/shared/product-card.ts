import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ProductoPublico } from '../../core/models/publico.model';
import { ImagenCatalogoComponent } from '../../shared/imagen-catalogo/imagen-catalogo';
import { UiIconComponent } from '../../shared/ui-icon/ui-icon';

@Component({
  selector: 's3s-product-card', standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, ImagenCatalogoComponent, UiIconComponent],
  template: `<article class="product-card">
    <a class="product-image" [routerLink]="['/productos', producto.idProducto || producto.id]" [attr.aria-label]="'Ver ' + producto.nombre">
      <app-imagen-catalogo ajuste="cover" [ruta]="producto.imagenUrl" [descripcion]="producto.nombre" />
      <span class="new-badge" *ngIf="producto.nuevo">Nuevo</span>
    </a>
    <div class="product-body"><div class="product-meta"><span>{{ producto.codigo }}</span><span>{{ producto.marca || '3S' }}</span></div>
      <h3><a [routerLink]="['/productos', producto.idProducto || producto.id]">{{ producto.nombre }}</a></h3>
      <p class="category-name">{{ producto.categoria }}</p>
      <p class="description" *ngIf="descripcion">{{ producto.descripcion }}</p>
      <button type="button" class="technical-link" *ngIf="ficha && producto.tieneFichaTecnica && producto.fichaTecnicaPdf" (click)="verFicha.emit(producto)"><s3s-icon name="file" /> Ficha técnica</button>
      <div class="product-actions">
        <label class="qty-control" *ngIf="cantidad">Cantidad<input type="number" min="1" step="1" [(ngModel)]="producto.cantidad" (change)="normalizar.emit(producto)" [attr.aria-label]="'Cantidad de ' + producto.nombre" /></label>
        <a class="btn-secondary" [routerLink]="['/productos', producto.idProducto || producto.id]">Ver producto</a>
        <button type="button" class="btn-primary" *ngIf="cotizable" (click)="cotizar.emit(producto)"><s3s-icon [name]="logueado ? 'cart' : 'file'" />{{ logueado ? 'Agregar al carrito' : 'Cotizar' }}</button>
      </div>
    </div>
  </article>`, styles: [':host{display:block;min-width:0;height:100%}']
})
export class PublicProductCardComponent {
  @Input({ required: true }) producto!: ProductoPublico;
  @Input() logueado = false;
  @Input() cantidad = false;
  @Input() descripcion = false;
  @Input() cotizable = true;
  @Input() ficha = false;
  @Output() cotizar = new EventEmitter<ProductoPublico>();
  @Output() normalizar = new EventEmitter<ProductoPublico>();
  @Output() verFicha = new EventEmitter<ProductoPublico>();
}
