import { AvisoComponent } from '../../shared/mensajes/aviso';
import { UiIconComponent } from '../../shared/ui-icon/ui-icon';
import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Subscription, timeout } from 'rxjs';
import { environment } from '../../../environments/environment';

interface Cambio { campo: string; antes: string; despues: string; }
interface Detalle { registro: string; accion: string; cambios: Cambio[]; }
interface Actividad { id: number; fecha: string; responsable: string; modulo: string; titulo: string; resultado: 'completado' | 'error' | 'denegado'; resumen: string; detalles: Detalle[]; }
interface Opciones { modulos: string[]; responsables: {id: number; nombre: string; correo: string}[]; }

@Component({
  selector: 'app-auditoria', standalone: true, imports: [AvisoComponent, UiIconComponent, CommonModule, FormsModule],
  templateUrl: './auditoria.html', styleUrl: './auditoria.scss', changeDetection: ChangeDetectionStrategy.OnPush
})
export class AuditoriaComponent implements OnInit, OnDestroy {
  private http = inject(HttpClient);
  private cdr = inject(ChangeDetectorRef);
  private consulta?: Subscription;
  private opcionesConsulta?: Subscription;
  items: Actividad[] = [];
  opciones: Opciones = {modulos: [], responsables: []};
  total=0; pagina=1; tamanoPagina=20; modulo=''; usuario: number|null=null; resultado=''; desde=''; hasta='';
  cargando=false; error=''; errorOpciones=''; abiertos = new Set<number>();
  get paginas(): number { return Math.max(1, Math.ceil(this.total/this.tamanoPagina)); }
  ngOnInit(): void { this.cargarOpciones(); this.cargar(); }
  ngOnDestroy(): void { this.consulta?.unsubscribe(); this.opcionesConsulta?.unsubscribe(); }
  cargarOpciones(): void {
    this.errorOpciones='';
    this.opcionesConsulta?.unsubscribe();
    this.opcionesConsulta=this.http.get<Opciones>(environment.apiUrl+'/auditoria/opciones').pipe(timeout(30000)).subscribe({
      next: r=>{this.opciones=r;this.cdr.markForCheck();},
      error: ()=>{this.errorOpciones='No se pudieron cargar las opciones de búsqueda.';this.cdr.markForCheck();}
    });
  }
  buscar(): void { this.pagina=1;this.cargar(); }
  limpiar(): void { this.modulo='';this.usuario=null;this.resultado='';this.desde='';this.hasta='';this.buscar(); }
  cambiarPagina(pagina: number): void { this.pagina=pagina;this.cargar(); }
  alternar(id: number): void { this.abiertos.has(id) ? this.abiertos.delete(id) : this.abiertos.add(id); }
  cargar(): void {
    if(this.desde && this.hasta && this.desde>this.hasta){this.error='La fecha inicial debe ser anterior o igual a la fecha final.';return;}
    this.consulta?.unsubscribe();
    this.cargando=true;this.error='';this.items=[];this.abiertos.clear();
    let params=new HttpParams().set('pagina',this.pagina);
    if(this.modulo)params=params.set('modulo',this.modulo);
    if(this.usuario)params=params.set('idUsuario',this.usuario);
    if(this.resultado)params=params.set('resultado',this.resultado);
    if(this.desde)params=params.set('desde',this.desde);
    if(this.hasta)params=params.set('hasta',this.hasta);
    this.consulta=this.http.get<{items:Actividad[];total:number;tamanoPagina:number}>(environment.apiUrl+'/auditoria',{params}).pipe(timeout(30000)).subscribe({
      next:r=>{this.items=r.items;this.total=r.total;this.tamanoPagina=r.tamanoPagina;this.cargando=false;this.cdr.markForCheck();},
      error:e=>{this.total=0;this.cargando=false;this.error=e.error?.mensaje||'No se pudo cargar la actividad. Intenta nuevamente.';this.cdr.markForCheck();}
    });
  }
}
