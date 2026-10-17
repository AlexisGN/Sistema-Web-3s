import {IaRestockComponent} from './ia-restock';
import {IaRankingComponent} from './ia-ranking';
import {IaDialogoComponent} from './ia-dialogo';
import {NO_ERRORS_SCHEMA} from '@angular/core';
import {TestBed} from '@angular/core/testing';
import {provideRouter} from '@angular/router';
import {of} from 'rxjs';
import {IaComponent} from './ia';
import {IaComparacionComponent} from './ia-comparacion';
import {IaGraficoComponent} from './ia-grafico';
import {BiGraficoComponent} from '../bi/bi-grafico';
import {IaService,IaRespuesta} from './ia.service';
import {SessionService} from '../../core/services/session.service';

describe('IA gerencial: tres secciones con decisiones conservadas',()=>{
 const reply:IaRespuesta={procedencia:'operativo',mensaje:'Disponible',actualizado:null,productos:[],recomendaciones:[]};
 const service={avisos:()=>of({pendientes:0}),productos:()=>of([]),consultar:vi.fn(()=>of(reply)),solicitud:vi.fn()};
 let allowed=(p:string)=>true;
 beforeEach(()=>{allowed=()=>true;service.consultar.mockClear();TestBed.configureTestingModule({imports:[IaComponent],providers:[provideRouter([]),{provide:IaService,useValue:service},{provide:SessionService,useValue:{tienePermiso:(p:string)=>allowed(p)}}]});TestBed.overrideComponent(IaComponent,{remove:{imports:[IaRestockComponent,IaRankingComponent,IaDialogoComponent,IaComparacionComponent,IaGraficoComponent,BiGraficoComponent]},add:{schemas:[NO_ERRORS_SCHEMA]}});});
 afterEach(()=>TestBed.resetTestingModule());
 it('concentra la navegación y conserva acceso a revisión e historial sin perder filtros',async()=>{
  const f=TestBed.createComponent(IaComponent);f.detectChanges();await f.whenStable();
  const c=f.componentInstance;c.producto=4;c.horizonte=60;
  const buttons=[...f.nativeElement.querySelectorAll('nav button')] as HTMLButtonElement[];expect(buttons).toHaveLength(3);buttons[2].click();f.detectChanges();
  expect(c.vista).toBe('inventario');expect(c.producto).toBe(4);expect(c.horizonte).toBe(60);expect(service.consultar).toHaveBeenCalledWith('inventario','operativo',4);
  c.cambiarVista('seguimiento');expect(c.seccionActiva('inventario')).toBe(true);expect(c.vista).toBe('seguimiento');expect(service.solicitud).not.toHaveBeenCalled();f.destroy();
 });
 it('un usuario sólo autorizado a proyecciones puede entrar por Resumen sin acceder al resto',()=>{
  allowed=p=>p==='IA_PROYECCIONES_VER';const f=TestBed.createComponent(IaComponent);const c=f.componentInstance;c.abrirSeccion('resumen');expect(c.vista).toBe('proyeccion');expect(c.seccionesVisibles.map(x=>x.id)).toEqual(['resumen']);c.abrirSeccion('inventario');expect(c.vista).toBe('proyeccion');f.destroy();
 });
 it('tarjetas y comparación cambian al horizonte seleccionado y mantienen litros separados',()=>{
  const f=TestBed.createComponent(IaComponent);const c=f.componentInstance;c.vista='pronostico';
  const amount=(h:number,q:number)=>({horizonte:h,cantidad:q,ingresoNeto:q*100,costoReferencia:q*70,margenBruto:q*30,porcentajeMargen:30,estado:'Estimado',recomendacion:''});
  const p={idProducto:1,nombre:'Contactor',unidad:'UND',stock:4,cantidades:{'30':12.01,'60':24.02},horizontes:[amount(30,12),amount(60,24)]};
  c.respuesta.set({...reply,productos:[p,{...p,idProducto:2,unidad:'L',stock:2},{...p,idProducto:3,cantidades:{}}]});c.resumenFinanzas.set([p]);
  expect(c.comparacionStock).toEqual([{id:1,nombre:'Contactor',actual:4,previsto:12}]);expect(c.stockEnRiesgo).toBe(1);expect(c.ingresosMes).toBe(1200);
  c.horizonte=60;expect(c.comparacionStock[0].previsto).toBe(24);expect(c.ingresosMes).toBe(2400);expect(c.margenResumen).toBe(720);c.unidadComparacion='L';expect(c.comparacionStock[0].previsto).toBe(24.02);expect(c.comparacionStock).toHaveLength(1);f.destroy();
 });
 it('el gráfico conserva ceros reales, etiqueta ambas series y nunca mezcla el stock con las ventas',()=>{
  const c=new IaComparacionComponent();c.filas=[{id:1,nombre:'A',actual:0,previsto:12},{id:2,nombre:'B',actual:4,previsto:0}];
  const o=c.opciones() as any;expect(o.series.map((s:any)=>s.name)).toEqual(['Stock actual','Ventas previstas']);expect(o.series[0].data).toEqual([0,4]);expect(o.series[1].data).toEqual([12,0]);expect(o.xAxis.minInterval).toBe(1);expect(o.tooltip.valueFormatter(12.2)).toBe('12 UND');c.unidad='L';expect((c.opciones() as any).tooltip.valueFormatter(12.2)).toBe('12.2 L');
 });
});
