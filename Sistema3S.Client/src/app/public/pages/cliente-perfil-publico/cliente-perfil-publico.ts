import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

import { ClienteWebSesion } from '../../../core/models/cliente-web.model';
import { ClienteWebService } from '../../../core/services/cliente-web.service';

@Component({
  selector: 'app-cliente-perfil-publico',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './cliente-perfil-publico.html',
  styleUrl: './cliente-perfil-publico.scss'
})
export class ClientePerfilPublicoComponent implements OnInit {
  sesion: ClienteWebSesion | null = null;

  constructor(
    private clienteWebService: ClienteWebService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.sesion = this.clienteWebService.obtenerSesion();

    if (!this.sesion) {
      this.router.navigate(['/cliente/login']);
    }
  }

  cerrarSesion(): void {
    this.clienteWebService.cerrarSesion();
    this.router.navigate(['/']);
  }
}