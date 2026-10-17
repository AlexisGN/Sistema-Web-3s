import {Component,EventEmitter,Input,Output} from '@angular/core';
import {UiIconComponent} from '../../shared/ui-icon/ui-icon';
import {IaProducto} from './ia.service';
import {cantidadVisible,unidadEntera} from './ia-cantidades';
export interface IaStockTarjeta{p:IaProducto;stock:number|null;cantidad:number|null|undefined;porcentaje:number|null;nivel:string;estado:string;cobertura?:number|null;}
@Component({selector:'s3s-ia-restock',standalone:true,imports:[UiIconComponent],template:`<div class="lista-restock">@for(a of filas;track a.p.idProducto){<div class="restock-item" [class.critico]="a.nivel==='critico'"><div class="restock-icono"><s3s-icon name="box" /></div><div class="restock-info"><div class="restock-titulo"><strong>{{a.p.nombre}}</strong><span class="estado-stock" [class.critico]="a.nivel==='critico'">{{a.estado}}</span></div><p><b>{{cantidad(a.stock,a.p.unidad)}} {{a.p.unidad}}</b> disponibles @if(a.cobertura!=null){· para {{numero(a.cobertura)}} días}</p>@if(a.porcentaje!=null){<div class="stock-medidor"><div class="stock-pista" role="progressbar" [attr.aria-valuenow]="a.porcentaje" aria-valuemin="0" aria-valuemax="100" aria-label="Stock frente a ventas previstas"><span [style.width.%]="a.porcentaje"></span></div><small>{{numero(a.porcentaje)}} % de lo previsto · {{periodo}}</small></div>}@else{<small>Revisa el historial de ventas para estimar cobertura.</small>}</div><div class="restock-accion">@if(configurable){<button type="button" (click)="planificar.emit(a.p)">{{a.p.politica?'Planificar compra':'Completar plan'}}</button>}@else{<button type="button" (click)="ver.emit(a.p.idProducto)">Ver cobertura</button>}</div></div>}@empty{<p>No hay productos para este filtro.</p>}</div>`,styleUrl:'./ia-restock.scss'})
export class IaRestockComponent{
 @Input() filas:IaStockTarjeta[]=[];@Input() periodo='30 días';@Input() configurable=false;
 @Output() planificar=new EventEmitter<IaProducto>();@Output() ver=new EventEmitter<number>();
 cantidad(v:number|null|undefined,u:string){const n=cantidadVisible(v,u);return n==null?'—':n.toLocaleString('es-PE',{maximumFractionDigits:unidadEntera(u)?0:2});}
 numero(n:number){return n.toLocaleString('es-PE',{maximumFractionDigits:0});}
}
