import {Component,OnInit,OnChanges,OnDestroy,Input,Output,EventEmitter,inject,signal} from '@angular/core';
import {Subscription,timer,switchMap,catchError,of} from 'rxjs';
import {SessionService} from '../../core/services/session.service';
import {IaService} from './ia.service';
@Component({selector:'s3s-ia-avisos',standalone:true,
 template:`@if(puede()&&pendientes()>0&&!cerrado()){<aside aria-label="Avisos de inventario de IA"><button type="button" class="abrir" (click)="revisar.emit()"><strong>Abastecimiento · {{pendientes()}} por revisar</strong><small>{{procedencia==='referencia'?'Demostración identificada':'Ver productos y planes de reposición'}}</small></button><button type="button" aria-label="Cerrar aviso de inventario" (click)="cerrado.set(true)">×</button></aside>}`,
 styles:[`aside{display:flex;align-items:center;justify-content:space-between;gap:12px;background:#fff6e9;border:1px solid #edd7b1;border-radius:10px;margin-bottom:14px;padding:1px 6px}button{border:0;background:transparent;padding:10px 12px;cursor:pointer;color:#76531f;font:inherit}.abrir{text-align:left;display:flex;gap:15px;align-items:center;flex-wrap:wrap}strong{font-size:12px}small{font-size:12px;color:#715f44}button:focus-visible{outline:2px solid #76531f;outline-offset:-3px}`]})
export class IaAvisosComponent implements OnInit,OnChanges,OnDestroy {
 @Input() procedencia='operativo';@Output() revisar=new EventEmitter<void>();
 private readonly session=inject(SessionService);private readonly service=inject(IaService);
 readonly pendientes=signal(0);readonly cerrado=signal(false);private sub?:Subscription;private iniciada=false;
 puede(){return this.session.tienePermiso('IA_VER')&&this.session.tienePermiso('IA_ALERTAS_VER')&&(this.procedencia!=='referencia'||this.session.tienePermiso('BI_REFERENCIA'));}
 ngOnInit(){this.iniciada=true;this.iniciar();}
 ngOnChanges(){if(this.iniciada)this.iniciar();}
 private iniciar(){this.sub?.unsubscribe();this.pendientes.set(0);this.cerrado.set(false);this.sub=timer(0,60000).pipe(switchMap(()=>this.puede()?this.service.avisos(this.procedencia).pipe(catchError(()=>of({pendientes:0}))):of({pendientes:0}))).subscribe(r=>{if(r.pendientes!==this.pendientes())this.cerrado.set(false);this.pendientes.set(r.pendientes);});}
 ngOnDestroy(){this.sub?.unsubscribe();}
}
