import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { ClienteWebService } from '../../../core/services/cliente-web.service';

@Component({
  selector: 'app-cliente-login-publico',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './cliente-login-publico.html',
  styleUrl: './cliente-login-publico.scss'
})
export class ClienteLoginPublicoComponent {
  correo = '';
  contrasena = '';

  cargando = false;
  error = '';

  constructor(
    private clienteWebService: ClienteWebService,
    private router: Router
  ) {}

  iniciarSesion(): void {
    this.error = '';

    const correo = this.correo.trim().toLowerCase();
    const contrasena = this.contrasena;

    if (!correo || !contrasena) {
      this.error = 'Ingresa tu correo y contraseña para continuar.';
      return;
    }

    this.cargando = true;

    this.clienteWebService.login({
      correo,
      contrasena
    }).subscribe({
      next: () => {
        this.cargando = false;
        this.router.navigate(['/productos']);
      },
      error: error => {
        this.cargando = false;
        this.error = error?.error?.mensaje || 'No pudimos iniciar sesión. Revisa tus datos e intenta nuevamente.';
      }
    });
  }
}