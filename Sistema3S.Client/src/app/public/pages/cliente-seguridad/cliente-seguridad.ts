import { AvisoComponent } from '../../../shared/mensajes/aviso';
import { CommonModule, Location } from '@angular/common';
import { ChangeDetectorRef, Component } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ClienteCuentaService, mensajeCuenta } from '../../../core/services/cliente-cuenta.service';
import { ClienteWebService } from '../../../core/services/cliente-web.service';
import { PublicAccountNavComponent } from '../../shared/account-nav';
import { UiIconComponent } from '../../../shared/ui-icon/ui-icon';

@Component({ selector: 'app-cliente-seguridad', standalone: true,
  imports: [AvisoComponent, CommonModule, FormsModule, RouterLink, PublicAccountNavComponent, UiIconComponent],
  templateUrl: './cliente-seguridad.html' })
export class ClienteSeguridadComponent {
  modo: 'recuperar' | 'restablecer' | 'cambiar';
  correo = ''; actual = ''; nueva = ''; confirmar = ''; token = '';
  mostrar = false; enviando = false; completado = false; mensaje = ''; error = '';
  constructor(route: ActivatedRoute, location: Location, router: Router, private cuenta: ClienteCuentaService,
    private sesion: ClienteWebService, private cd: ChangeDetectorRef) {
    this.modo = route.snapshot.data['modo'];
    if (this.modo === 'cambiar' && !sesion.estaLogueado()) router.navigate(['/cliente/login']);
    if (this.modo === 'restablecer') {
      this.token = new URLSearchParams(route.snapshot.fragment || '').get('token') || '';
      location.replaceState(router.url.split('#')[0]);
      if (!/^[a-f0-9]{64}$/i.test(this.token)) this.error = 'El enlace es inválido. Solicita un nuevo enlace de recuperación.';
    }
  }
  get titulo() { return this.modo === 'recuperar' ? 'Recupera tu cuenta' : this.modo === 'restablecer' ? 'Crea una nueva contraseña' : 'Seguridad de tu cuenta'; }
  enviar(form: NgForm): void {
    if (this.enviando) return;
    form.control.markAllAsTouched(); if (form.invalid) return;
    this.error = '';
    if (this.modo !== 'recuperar' && this.nueva !== this.confirmar) { this.error = 'Las contraseñas no coinciden.'; return; }
    if (this.modo === 'restablecer' && !/^[a-f0-9]{64}$/i.test(this.token)) { this.error = 'El enlace es inválido. Solicita uno nuevo.'; return; }
    this.enviando = true;
    const request = this.modo === 'recuperar' ? this.cuenta.recuperar(this.correo.trim().toLowerCase()) : this.modo === 'restablecer'
      ? this.cuenta.restablecer(this.token, this.nueva, this.confirmar) : this.cuenta.cambiar(this.actual, this.nueva, this.confirmar);
    request.pipe(finalize(() => { this.enviando = false; this.cd.markForCheck(); })).subscribe({
      next: result => { this.completado = true; this.mensaje = result.mensaje; this.actual = this.nueva = this.confirmar = this.token = '';
        if (this.modo !== 'recuperar') this.sesion.cerrarSesion(); },
      error: error => { this.error = mensajeCuenta(error); }
    });
  }
}
