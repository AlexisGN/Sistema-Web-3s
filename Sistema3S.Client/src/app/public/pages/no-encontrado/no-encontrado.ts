import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { UiIconComponent } from '../../../shared/ui-icon/ui-icon';
@Component({ selector:'app-no-encontrado', standalone:true, imports:[RouterLink,UiIconComponent],
  template:`<section class="empty-state"><div class="empty-icon"><s3s-icon name="search" /></div><span class="eyebrow">Error 404</span><h1>Página no encontrada</h1><p>Este enlace no está disponible. Explora el catálogo o vuelve al inicio.</p><div class="empty-actions"><a routerLink="/productos">Explorar productos</a><a routerLink="/">Ir al inicio</a></div></section>` })
export class NoEncontradoComponent {}
