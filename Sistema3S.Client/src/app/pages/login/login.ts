import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { AuthService } from '../../core/services/auth.service';
import { SessionService } from '../../core/services/session.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class LoginComponent {
  correo = '';
  contrasena = '';

  mostrarContrasena = false;
  cargando = false;
  error = '';

  constructor(
    private authService: AuthService,
    private sessionService: SessionService,
    private router: Router
  ) {}

  ingresar(): void {
    this.error = '';

    const correoLimpio = this.correo.trim().toLowerCase();
    const contrasenaLimpia = this.contrasena.trim();

    if (!correoLimpio || !contrasenaLimpia) {
      this.error = 'Ingresa tu correo y contraseña.';
      return;
    }

    this.cargando = true;

    this.authService.login({
      correo: correoLimpio,
      contrasena: contrasenaLimpia
    }).subscribe({
      next: resultado => {
        this.sessionService.guardarSesion(resultado);

        const rutaInicial = this.sessionService.obtenerRutaInicial();

        this.cargando = false;

        this.router.navigateByUrl(rutaInicial).catch(() => {
          this.router.navigateByUrl('/login');
        });
      },
      error: error => {
        this.error = error?.error?.mensaje || 'No se pudo iniciar sesión.';
        this.cargando = false;
      }
    });
  }

  alternarContrasena(): void {
    this.mostrarContrasena = !this.mostrarContrasena;
  }
}