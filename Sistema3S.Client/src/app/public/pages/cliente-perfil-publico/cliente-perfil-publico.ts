import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { ClienteWebService } from '../../../core/services/cliente-web.service';
import { ClienteCuentaService, ClientePerfil, UbicacionCliente, mensajeCuenta } from '../../../core/services/cliente-cuenta.service';
import { TelefonoPeDirective, telefonoCompletoPeru } from '../../../shared/telefono-pe/telefono-pe';
import { PublicAccountNavComponent } from '../../shared/account-nav';
import { UiIconComponent } from '../../../shared/ui-icon/ui-icon';
@Component({selector:'app-cliente-perfil-publico', standalone:true,
 imports:[CommonModule, FormsModule, TelefonoPeDirective, PublicAccountNavComponent, UiIconComponent],
 templateUrl:'./cliente-perfil-publico.html', styleUrl:'./cliente-perfil-publico.scss'})
export class ClientePerfilPublicoComponent implements OnInit {
  perfil: ClientePerfil | null = null; original: ClientePerfil | null = null;
  cargando = false; guardando = false; editando = false; error = ''; mensaje = '';
  ubicaciones: UbicacionCliente[] = []; busquedaUbicacion = ''; buscandoUbicacion = false;
  constructor(private cuenta: ClienteCuentaService, private sesion: ClienteWebService, private router: Router, private cd: ChangeDetectorRef) {}
  ngOnInit(): void { if (!this.sesion.estaLogueado()) { this.router.navigate(['/cliente/login']); return; } this.cargar(); }
  cargar(): void { this.cargando = true; this.error = '';
    this.cuenta.perfil().pipe(finalize(()=>{this.cargando=false; this.cd.markForCheck();})).subscribe({
      next:p=>{this.original={...p};this.perfil={...p}; this.busquedaUbicacion=p.ubicacion||'';},
      error:e=>this.error=mensajeCuenta(e)
    });
  }
  editar(): void { this.editando=true; this.mensaje=''; this.error=''; }
  cancelar(): void { this.perfil=this.original?{...this.original}:null;this.busquedaUbicacion=this.perfil?.ubicacion||'';this.ubicaciones=[];this.editando=false;this.error=''; }
  buscarUbicacion(): void {
    if(this.busquedaUbicacion.trim().length<2 || this.buscandoUbicacion) return;
    this.buscandoUbicacion=true;
    this.cuenta.ubicaciones(this.busquedaUbicacion.trim()).pipe(finalize(()=>{this.buscandoUbicacion=false;this.cd.markForCheck();})).subscribe({
      next:r=>{this.ubicaciones=r;this.error=r.length?'':'No se encontraron ubicaciones. Prueba con el distrito o provincia.';},
      error:e=>this.error=mensajeCuenta(e)
    });
  }
  seleccionarUbicacion(u: UbicacionCliente): void { if(this.perfil){this.perfil.idUbigeo=u.idUbigeo;this.perfil.ubicacion=u.nombre;this.busquedaUbicacion=u.nombre;}this.ubicaciones=[]; }
  guardar(form:NgForm): void {
    form.control.markAllAsTouched(); if(form.invalid || !this.perfil || this.guardando) return;
    this.perfil.telefono=telefonoCompletoPeru(this.perfil.telefono);
    if(!/^\+51[0-9]{9}$/.test(this.perfil.telefono)) {this.error='Ingresa los 9 dígitos del teléfono.';return;}
    this.guardando=true;this.error='';this.mensaje='';
    this.cuenta.guardar(this.perfil).pipe(finalize(()=>{this.guardando=false;this.cd.markForCheck();})).subscribe({
      next:p=>{this.perfil={...p};this.original={...p};this.editando=false;this.mensaje='Tus datos de contacto se actualizaron correctamente.';},
      error:e=>this.error=mensajeCuenta(e)
    });
  }
  cerrarSesion():void {this.sesion.cerrarSesion();this.router.navigate(['/']);}
}

