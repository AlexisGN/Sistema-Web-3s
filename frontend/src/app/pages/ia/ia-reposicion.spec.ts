import {NO_ERRORS_SCHEMA} from '@angular/core';
import {TestBed} from '@angular/core/testing';
import {provideRouter} from '@angular/router';
import {of} from 'rxjs';
import {IaComponent} from './ia';
import {IaRankingComponent} from './ia-ranking';
import {IaComparacionComponent} from './ia-comparacion';
import {IaGraficoComponent} from './ia-grafico';
import {BiGraficoComponent} from '../bi/bi-grafico';
import {IaRestockComponent} from './ia-restock';
import {IaService,IaRespuesta} from './ia.service';
import {SessionService} from '../../core/services/session.service';

describe('Reposición: tarjeta, cálculo sin guardar y diálogo',()=>{
 const reply:IaRespuesta={procedencia:'operativo',mensaje:'Disponible',actualizado:null,productos:[],recomendaciones:[]};
 const service={avisos:()=>of({pendientes:0}),productos:()=>of([]),proveedores:()=>of([{IdProveedor:1,Nombre:'Proveedor existente'}]),consultar:()=>of(reply),abastecimiento:vi.fn(),previsualizar:vi.fn()};
 const original=Object.getOwnPropertyDescriptor(HTMLDialogElement.prototype,'showModal');
 beforeEach(()=>{service.abastecimiento.mockReset();service.previsualizar.mockReset();Object.defineProperty(HTMLDialogElement.prototype,'showModal',{configurable:true,value:function(this:HTMLDialogElement){this.setAttribute('open','');}});
  TestBed.configureTestingModule({imports:[IaComponent],providers:[provideRouter([]),{provide:IaService,useValue:service},{provide:SessionService,useValue:{tienePermiso:()=>true}}]});TestBed.overrideComponent(IaComponent,{remove:{imports:[IaRankingComponent,IaComparacionComponent,IaGraficoComponent,BiGraficoComponent]},add:{schemas:[NO_ERRORS_SCHEMA]}});
 });
 afterEach(()=>{TestBed.resetTestingModule();if(original)Object.defineProperty(HTMLDialogElement.prototype,'showModal',original);else delete (HTMLDialogElement.prototype as any).showModal;});
 it('abre el plan desde la tarjeta sin guardar y oculta una vista previa cuando cambian los datos',async()=>{
  const f=TestBed.createComponent(IaComponent);f.detectChanges();await f.whenStable();const c=f.componentInstance;
  const p={idProducto:1,nombre:'Válvula QA',unidad:'UND',idProveedor:1,stock:4,riesgo:{stock:4,coberturaDias:4,agotamiento:null,prioridad:'Verificar abastecimiento',cantidad:null,fechaPedido:null,idProveedor:1,explicacion:'Plazo pendiente'}};
  c.resumenRiesgos.set([p]);c.resumenVentas.set([{...p,cantidades:{'30':30}}]);await f.whenStable();f.detectChanges();
  const button=f.nativeElement.querySelector('s3s-ia-restock button') as HTMLButtonElement;button.click();await f.whenStable();f.detectChanges();expect(f.nativeElement.querySelector('dialog').open).toBe(true);expect(c.politica?.idProveedor).toBe(1);expect(service.abastecimiento).not.toHaveBeenCalled();
  c.politica!.plazoDias=10;service.previsualizar.mockReturnValue(of({riesgo:{cantidad:36,retrasoDias:6},demanda:40,stockDisponible:4,recepcion:'2026-10-26',costoNeto:2520,proveedor:'Proveedor existente',guardado:false}));
  c.calcularPlan();await f.whenStable();f.detectChanges();expect(service.previsualizar).toHaveBeenCalledOnce();expect(f.nativeElement.querySelector('.plan-resultado').textContent).toContain('36 UND');expect(f.nativeElement.textContent).toContain('Vista previa sin guardar');expect(service.abastecimiento).not.toHaveBeenCalled();
  c.politica!.coberturaDias=20;expect(c.vistaPreviaActual).toBeNull();c.cerrarPolitica();c.respuesta.set({...reply});await f.whenStable();f.detectChanges();expect(f.nativeElement.querySelector('dialog')).toBeNull();f.destroy();
 });
 it('advierte el error histórico sin confundirlo con cobertura de productos',async()=>{
  const f=TestBed.createComponent(IaComponent);f.detectChanges();await f.whenStable();const c=f.componentInstance;c.resumenVentas.set([{idProducto:1,nombre:'A',unidad:'UND',calidad:{'30':{predicted:50,actual:100,wape:.5}}} as any]);await f.whenStable();f.detectChanges();expect(f.nativeElement.textContent).toContain('Estimaciones orientativas');expect(c.calidadResumen[0].wape).toBe(50);f.destroy();
 });
 it('la tarjeta explica la base del porcentaje y emite únicamente el producto elegido',()=>{
  const f=TestBed.createComponent(IaRestockComponent);const producto={idProducto:2,nombre:'Contactor',unidad:'UND'};f.componentRef.setInput('filas',[{p:producto,stock:4,cantidad:20,porcentaje:20,nivel:'critico',estado:'Reponer pronto',cobertura:6}]);f.componentRef.setInput('configurable',true);f.detectChanges();
  expect(f.nativeElement.textContent).toContain('20 % de lo previsto');expect(f.nativeElement.querySelector('[role=progressbar]').getAttribute('aria-valuenow')).toBe('20');const received=vi.fn();f.componentInstance.planificar.subscribe(received);f.nativeElement.querySelector('button').click();expect(received).toHaveBeenCalledWith(producto);f.destroy();
 });
 it('los importes del gráfico quedan al final de la barra y no pierden céntimos',()=>{
  const c=new IaRankingComponent();c.filas=[{Nombre:'Equipo industrial',Importe:190784.17}];const o=c.opciones() as any;expect(o.series[0].label.position).toBe('right');expect(o.series[0].data).toEqual([190784.17]);expect(o.series[0].label.formatter({value:190784.17})).toBe('S/ 190,784.17');expect(o.grid.right).toBeGreaterThan(100);expect(o.aria.decal).toBeUndefined();
 });
});
