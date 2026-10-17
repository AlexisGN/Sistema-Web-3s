import {IaRestockComponent} from './ia-restock';
import {IaRankingComponent} from './ia-ranking';
import {IaDialogoComponent} from './ia-dialogo';
import {IaComparacionComponent} from './ia-comparacion';
import {NO_ERRORS_SCHEMA} from '@angular/core';
import {TestBed,ComponentFixture} from '@angular/core/testing';
import {provideRouter} from '@angular/router';
import {provideHttpClient} from '@angular/common/http';
import {HttpTestingController,provideHttpClientTesting} from '@angular/common/http/testing';
import {of,Subject} from 'rxjs';
import {IaComponent} from './ia';
import {IaGraficoComponent} from './ia-grafico';
import {BiGraficoComponent} from '../bi/bi-grafico';
import {IaService,IaRespuesta} from './ia.service';
import {SessionService} from '../../core/services/session.service';
import {environment} from '../../../environments/environment';

describe('IA calendario: contrato HTTP',()=>{
 afterEach(()=>{TestBed.inject(HttpTestingController).verify();TestBed.resetTestingModule();});
 beforeEach(()=>TestBed.configureTestingModule({providers:[provideHttpClient(),provideHttpClientTesting()]}));
 for(const view of ['resumen','pronostico','proyeccion','inventario','compras','seguimiento']){
  it(`${view} envía la fecha ISO sin alterar producto ni procedencia`,()=>{
   TestBed.inject(IaService).consultar(view,'operativo',7,'2026-11-20').subscribe();
   const req=TestBed.inject(HttpTestingController).expectOne(r=>r.url===environment.apiUrl+'/ia/'+view);
   expect(req.request.params.get('fechaObjetivo')).toBe('2026-11-20');
   expect(req.request.params.get('producto')).toBe('7');expect(req.request.params.get('procedencia')).toBe('operativo');req.flush({});
  });
 }
 it('sin calendario conserva el contrato de horizontes existente',()=>{
  TestBed.inject(IaService).consultar('pronostico','referencia',null).subscribe();
  const req=TestBed.inject(HttpTestingController).expectOne(r=>r.url.endsWith('/ia/pronostico'));
  expect(req.request.params.has('fechaObjetivo')).toBe(false);expect(req.request.params.has('producto')).toBe(false);req.flush({});
 });
});

describe('IA calendario en las seis vistas',()=>{
 let fixture:ComponentFixture<IaComponent>;
 const today=()=>new Date().toLocaleDateString('sv-SE',{timeZone:'America/Lima'});
 const day=(n:number)=>{const d=new Date(today()+'T00:00:00Z');d.setUTCDate(d.getUTCDate()+n);return d.toISOString().slice(0,10);};
 const amount=(q:number)=>({horizonte:10,cantidad:q,ingresoNeto:q*10,costoReferencia:q*6,margenBruto:q*4,porcentajeMargen:40,estado:'Estimado',recomendacion:'Revisar'});
 const product={idProducto:1,nombre:'Producto QA',unidad:'UND',suficiente:true,cantidades:{'30':30,'60':60,'90':90},periodoPersonalizado:amount(10),mesSiguienteFinanciero:amount(31),horizontes:[{...amount(30),horizonte:30},{...amount(60),horizonte:60},{...amount(90),horizonte:90}],historial:{dias:120,diasConVenta:8,diasFaltantes:0,diasConVentaFaltantes:0,desde:'2026-06-01'}};
 const reply=(view:string):IaRespuesta=>({procedencia:'operativo',mensaje:'Disponible',actualizado:null,historialOperativoVerificado:true,
  periodo:{desde:day(1),hasta:day(10),dias:10,disponibleDesde:day(1),disponibleHasta:day(90),personalizado:true},productos:view==='resumen'?1:[product],conEstimacion:1,recomendaciones:[],confiabilidad:{mensaje:'Sólo períodos cumplidos',resultados:[]}});
 const service={avisos:vi.fn(()=>of({pendientes:0})),consultar:vi.fn((v:string,_s:string,_p:number|null,_d?:string)=>of(reply(v))),productos:vi.fn(()=>of([product])),solicitud:vi.fn(()=>of({id:1}))};
 beforeEach(()=>{service.consultar.mockReset().mockImplementation((v:string)=>of(reply(v)));service.solicitud.mockClear();
  TestBed.configureTestingModule({imports:[IaComponent],providers:[provideRouter([]),{provide:IaService,useValue:service},{provide:SessionService,useValue:{tienePermiso:()=>true}}]});
  TestBed.overrideComponent(IaComponent,{remove:{imports:[IaRestockComponent,IaRankingComponent,IaDialogoComponent,IaComparacionComponent,IaGraficoComponent,BiGraficoComponent]},add:{schemas:[NO_ERRORS_SCHEMA]}});
  fixture=TestBed.createComponent(IaComponent);fixture.detectChanges();
 });
 afterEach(()=>{fixture.destroy();TestBed.resetTestingModule();vi.restoreAllMocks();});

 it('el calendario real cambia importes y demanda sin entrenar ni navegar',async()=>{
  await fixture.whenStable();const c=fixture.componentInstance;
  const input=fixture.nativeElement.querySelector('input[aria-label="Fecha objetivo del análisis"]') as HTMLInputElement;
  expect(input.min).toBe(day(1));expect(input.max).toBe(day(90));
  input.value=day(10);input.dispatchEvent(new Event('input'));input.dispatchEvent(new Event('change'));
  await fixture.whenStable();fixture.detectChanges();
  expect(c.fechaObjetivo).toBe(day(10));expect(service.consultar).toHaveBeenCalledWith('resumen','operativo',null,day(10));
  expect(c.ingresosMes).toBe(100);expect(c.demandaAgrupada).toEqual([{unidad:'UND',cantidad:10}]);
  expect(c.graficoIngresoResumen[0].Importe).toBe(100);expect(c.diasSeleccionados).toBe(10);
  expect(fixture.nativeElement.textContent).toContain('hasta');expect(service.solicitud).not.toHaveBeenCalled();
 });

 it('la fecha y el producto se conservan entre pestañas; volver limpia sólo el producto',()=>{
  const c=fixture.componentInstance;c.fechaObjetivo=day(10);c.producto=1;
  for(const view of ['pronostico','proyeccion','inventario','compras','seguimiento','resumen']){
   c.cambiarVista(view);expect(service.consultar).toHaveBeenCalledWith(view,'operativo',1,day(10));expect(c.fechaObjetivo).toBe(day(10));
  }
  c.volver();expect(c.producto).toBeNull();expect(c.fechaObjetivo).toBe(day(10));expect(c.horizonte).toBe(30);
 });

 it('volver a días quita la fecha del contrato y conserva las cantidades originales',()=>{
  const c=fixture.componentInstance;c.fechaObjetivo=day(10);c.cargar();c.horizonte=60;service.consultar.mockClear();c.usarHorizonte();
  expect(c.fechaObjetivo).toBe('');expect(service.consultar).toHaveBeenCalledWith('resumen','operativo',null);
  expect(c.demandaAgrupada[0].cantidad).toBe(60);expect(c.ingresosMes).toBe(600);
 });

 it('una fecha pasada o mayor que 90 días no dispara consultas ni conserva cifras anteriores',()=>{
  const c=fixture.componentInstance;
  for(const date of [today(),day(91)]){
   service.consultar.mockClear();c.fechaObjetivo=date;c.cargar();expect(service.consultar).not.toHaveBeenCalled();
   expect(c.respuesta()).toBeNull();expect(c.error()).toContain('fecha futura');expect(c.cargando()).toBe(false);
  }
 });

 it('la proyección personalizada usa su cantidad; el mes siguiente completo permanece disponible al limpiar fecha',()=>{
  const c=fixture.componentInstance;c.fechaObjetivo=day(10);c.cambiarVista('proyeccion');
  expect(c.finanzasTotales).toEqual({ingreso:100,costo:60,margen:40});
  c.usarHorizonte();c.periodoFinanciero='mes';expect(c.finanzasTotales.ingreso).toBe(310);const next=new Date(today()+'T00:00:00Z');next.setUTCMonth(next.getUTCMonth()+1,1);expect(c.rangoConsulta.startsWith(c.fecha(next.toISOString().slice(0,10)))).toBe(true);
  expect(c.cantidadSeleccionada(product)).toBe(30);
 });

 it('el diagnóstico por producto informa el historial usado sin exigir esperar 90 días desde la actualización',async()=>{
  await fixture.whenStable();const c=fixture.componentInstance;c.cambiarVista('pronostico');await fixture.whenStable();fixture.detectChanges();
  expect(fixture.nativeElement.textContent).toContain('120 días de historial');expect(fixture.nativeElement.textContent).toContain('8 días con ventas');
 });
 it('si las fechas cumplen pero falta entrenamiento no muestra cero días como causa pendiente',async()=>{
  await fixture.whenStable();const c=fixture.componentInstance;c.vista='pronostico';
  c.respuesta.set({...reply('pronostico'),productos:[{...product,suficiente:false,motivo:'Faltan observaciones diarias útiles'}]});
  await fixture.whenStable();fixture.detectChanges();
  expect(fixture.nativeElement.textContent).toContain('Las fechas cumplen los mínimos');
  expect(fixture.nativeElement.textContent).not.toContain('Faltan 0 días');
 });

});
