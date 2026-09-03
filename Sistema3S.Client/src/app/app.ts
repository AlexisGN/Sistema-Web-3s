import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CentroMensajesComponent } from './shared/mensajes/centro-mensajes';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, CentroMensajesComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  title = 'Sistema3SClient';
}
