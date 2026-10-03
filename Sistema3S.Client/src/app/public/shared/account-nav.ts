import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { UiIconComponent } from '../../shared/ui-icon/ui-icon';
@Component({ selector: 's3s-account-nav', standalone: true, imports: [RouterLink, RouterLinkActive, UiIconComponent],
  template: `<nav class="account-tabs" aria-label="Mi cuenta">
    <a routerLink="/cliente/perfil" routerLinkActive="active" ariaCurrentWhenActive="page"><s3s-icon name="people" /> Mi perfil</a>
    <a routerLink="/cliente/historial-cotizaciones" routerLinkActive="active" ariaCurrentWhenActive="page"><s3s-icon name="file" /> Mis cotizaciones</a>
    <a routerLink="/cliente/seguridad" routerLinkActive="active" ariaCurrentWhenActive="page"><s3s-icon name="shield" /> Seguridad</a>
    <a routerLink="/cliente/carrito" routerLinkActive="active" ariaCurrentWhenActive="page"><s3s-icon name="cart" /> Carrito</a>
  </nav>` })
export class PublicAccountNavComponent {}
