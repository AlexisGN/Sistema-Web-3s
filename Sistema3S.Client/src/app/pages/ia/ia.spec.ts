import {IaRestockComponent} from './ia-restock';
import {IaRankingComponent} from './ia-ranking';
import {IaDialogoComponent} from './ia-dialogo';
import {IaComparacionComponent} from './ia-comparacion';
import {NO_ERRORS_SCHEMA} from '@angular/core';
import {TestBed,ComponentFixture} from '@angular/core/testing';
import {provideRouter,ActivatedRoute} from '@angular/router';
import {of,Subject,throwError} from 'rxjs';
import {IaComponent} from './ia';
import {IaGraficoComponent} from './ia-grafico';
import {BiGraficoComponent} from '../bi/bi-grafico';
import {IaService,IaRespuesta} from './ia.service';
import {SessionService} from '../../core/services/session.service';
import {ComprasComponent} from '../compras/compras';
import {CompraService} from '../../core/services/compra';
import {init,setPlatformAPI} from 'echarts/core';

describe('IA interacción y autorización',()=>{
 let fixture:ComponentFixture<IaComponent>;
 const reply:IaRespuesta={procedencia:'operativo',mensaje:'Historial insuficiente',actualizado:null,productos:[],recomendaciones:[],confiabilidad:{mensaje:'Aún no hay períodos cumplidos',resultados:[]}};
 const service={avisos:vi.fn(()=>of({pendientes:0})),consultar:vi.fn((_view:string,_source:string,_product:number|null)=>of(reply)),productos:vi.fn(()=>of([{idProducto:1,nombre:'Producto QA',unidad:'UND'}])),proveedores:vi.fn(()=>of([])),revisar:vi.fn(()=>of({})),solicitud:vi.fn(()=>of({id:2}))};
 let allowed=(p:string)=>true;
 beforeEach(()=>{service.consultar.mockReset().mockImplementation(()=>of(reply));service.solicitud.mockClear();allowed=()=>true;TestBed.configureTestingModule({imports:[IaComponent],providers:[provideRouter([]),{provide:IaService,useValue:service},{provide:SessionService,useValue:{tienePermiso:(p:string)=>allowed(p)}}]});TestBed.overrideComponent(IaComponent,{remove:{imports:[IaRestockComponent,IaRankingComponent,IaDialogoComponent,IaComparacionComponent,IaGraficoComponent,BiGraficoComponent]},add:{schemas:[NO_ERRORS_SCHEMA]}});fixture=TestBed.createComponent(IaComponent);fixture.detectChanges();});
 afterEach(()=>{fixture.destroy();TestBed.resetTestingModule();vi.restoreAllMocks();});
 it('muestra tres secciones y el retorno limpia únicamente la selección sin navegar ni entrenar',()=>{expect(fixture.nativeElement.querySelectorAll('nav button')).toHaveLength(3);const c=fixture.componentInstance;c.producto=1;c.horizonte=90;c.volver();expect(c.producto).toBeNull();expect(c.horizonte).toBe(90);expect(service.consultar).toHaveBeenCalledWith('resumen','operativo',null);expect(fixture.nativeElement.textContent).not.toContain('Entrenar');});
 it('oculta y rechaza vistas sin permiso específico',()=>{allowed=p=>p==='IA_VER';fixture.detectChanges();expect(fixture.nativeElement.querySelectorAll('nav button')).toHaveLength(1);const c=fixture.componentInstance;c.cambiarVista('proyeccion');expect(c.vista).toBe('resumen');});
 it('cancela consultas anteriores, maneja errores y no conserva resultados obsoletos',()=>{const a=new Subject<IaRespuesta>(),b=new Subject<IaRespuesta>();let primera=true;service.consultar.mockImplementation((view:string)=>{if(view==='pronostico'&&primera){primera=false;return a;}return view==='inventario'?b:of(reply);});const c=fixture.componentInstance;c.vista='pronostico';c.cargar();c.cambiarVista('inventario');expect(a.observed).toBe(false);b.error({error:{mensaje:'Error controlado'}});fixture.detectChanges();expect(c.respuesta()).toBeNull();expect(c.error()).toBe('Error controlado');});
 it('el resumen no solicita proyecciones ni riesgos sin permisos y mantiene la selección al explorar referencia',()=>{const c=fixture.componentInstance;allowed=p=>p==='IA_VER'||p==='BI_REFERENCIA';service.consultar.mockClear();c.cargar();expect(service.consultar.mock.calls.map(x=>x[0])).toEqual(['resumen']);c.horizonte=60;c.verDemostracion();expect(c.procedencia).toBe('referencia');expect(c.horizonte).toBe(60);expect(c.producto).toBeNull();});
 it('historial verificado sin pronósticos no muestra el aviso de demostración',async()=>{
  await fixture.whenStable();const c=fixture.componentInstance;
  c.respuesta.set({...reply,historialOperativoVerificado:true,productos:14,conEstimacion:0});
  await fixture.whenStable();fixture.detectChanges();
  expect(fixture.nativeElement.textContent).not.toContain('Demostración comercial disponible');
  expect(fixture.nativeElement.textContent).not.toContain('El historial operativo está pendiente de verificación');
  expect(fixture.nativeElement.textContent).toContain('Información insuficiente');
  expect(fixture.nativeElement.textContent).toContain('0 / 14');
  expect(c.procedencia).toBe('operativo');
 });
 it('historial pendiente conserva el aviso para explorar la demostración identificada',async()=>{
  await fixture.whenStable();fixture.componentInstance.respuesta.set({...reply,historialOperativoVerificado:false,conEstimacion:0});
  await fixture.whenStable();fixture.detectChanges();
  expect(fixture.nativeElement.textContent).toContain('Demostración comercial disponible');
  expect(fixture.nativeElement.textContent).toContain('El historial operativo está pendiente de verificación');
 });
 it('la fuente de referencia no muestra el aviso de verificación operativa',async()=>{
  await fixture.whenStable();const c=fixture.componentInstance;c.procedencia='referencia';
  c.respuesta.set({...reply,procedencia:'referencia',historialOperativoVerificado:false,conEstimacion:0});
  await fixture.whenStable();fixture.detectChanges();
  expect(fixture.nativeElement.textContent).not.toContain('Demostración comercial disponible');
 });
 it('el resumen suma sólo importes conocidos y separa unidades de demanda',()=>{const c=fixture.componentInstance;c.resumenFinanzas.set([{idProducto:1,nombre:'A',unidad:'UND',horizontes:[{horizonte:30,ingresoNeto:120} as any]},{idProducto:2,nombre:'B',unidad:'L',horizontes:[{horizonte:30,ingresoNeto:null} as any]}]);c.resumenVentas.set([{idProducto:1,nombre:'A',unidad:'UND',cantidades:{'30':4}},{idProducto:2,nombre:'B',unidad:'L',cantidades:{'30':3}}]);expect(c.ingresosMes).toBe(120);expect(c.demandaAgrupada).toEqual([{unidad:'UND',cantidad:4},{unidad:'L',cantidad:3}]);c.resumenFinanzas.set([]);expect(c.ingresosMes).toBeNull();});
 it('mantiene insuficiencia en lugar de cifras inventadas y prohíbe solicitud de referencia',()=>{const c=fixture.componentInstance;c.cambiarVista('pronostico');fixture.detectChanges();expect(fixture.nativeElement.textContent).toContain('Historial insuficiente');expect(c.numero(null)).toBe('Información insuficiente');c.procedencia='referencia';c.revision={Id:1} as any;c.motivo='Revisión';c.prepararCompra();expect(service.solicitud).not.toHaveBeenCalled();});
 it('la proyección mantiene pronósticos y rangos sin formulario ni botón de simulación',()=>{const c=fixture.componentInstance;c.cambiarVista('proyeccion');fixture.detectChanges();const text=fixture.nativeElement.textContent;expect(text).toContain('Proyección comercial');expect(text).not.toContain('Escenario hipotético');expect(text).not.toContain('Simular escenario');expect(fixture.nativeElement.querySelector('input[type=number]')).toBeNull();});
 it('cambia al próximo mes financiero sin recargar y conserva las series mensuales sin convertir importes desconocidos en cero',async()=>{await fixture.whenStable();const c=fixture.componentInstance;const amount={horizonte:30,cantidad:30,ingresoNeto:1500,costoReferencia:900,margenBruto:600,porcentajeMargen:40,estado:'Margen positivo',recomendacion:'Revisar condiciones'};c.respuesta.set({...reply,productos:[{idProducto:1,nombre:'Producto QA',unidad:'UND',precioNeto:50,intervalos:{'30':{lower:20,upper:40,windows:5}},horizontes:[amount],mesSiguienteFinanciero:{...amount,horizonte:null,mes:'2026-11',ingresoNeto:1600},historicoMensual:[{mes:'2026-09',ingresoNeto:100}],estimadoMensual:[{mes:'2026-11',cantidad:32,ingresoNeto:1600,desde:'2026-11-01',hasta:'2026-11-30'},{mes:'2026-12',cantidad:3,ingresoNeto:null,desde:'2026-12-01',hasta:'2026-12-03'}]}]});c.vista='proyeccion';await fixture.whenStable();fixture.detectChanges();expect(c.graficoFinanciero[0].Importe).toBe(1500);expect(fixture.nativeElement.textContent).toContain('Conservador: S/ 1,000.00');expect(fixture.nativeElement.textContent).toContain('Optimista: S/ 2,000.00');const calls=service.consultar.mock.calls.length;const selector=[...fixture.nativeElement.querySelectorAll('select')].find((s:any)=>s.textContent.includes('Horizonte seleccionado')) as HTMLSelectElement;selector.value='mes';selector.dispatchEvent(new Event('change'));await fixture.whenStable();fixture.detectChanges();expect(c.graficoFinanciero[0].Importe).toBe(1600);expect(fixture.nativeElement.textContent).toContain('Próximo mes calendario');expect(service.consultar.mock.calls.length).toBe(calls);expect(c.graficoMensual.estimado).toEqual([{date:'2026-11-01',quantity:1600}]);expect(c.graficoMensual.historico).toEqual([{date:'2026-09-01',quantity:100}]);const series=c.graficoMensual;c.horizonte=90;expect(c.graficoMensual).toBe(series);c.respuesta.set(reply);expect(c.graficoMensual.estimado).toEqual([]);});
});

describe('IA gráfico real y formulario de Compras',()=>{
 afterEach(()=>TestBed.resetTestingModule());
 it('SVG distingue observado y estimado sin inventar ceros fuera de cada serie',()=>{setPlatformAPI({measureText:t=>({width:String(t).length*7})});const c=new IaGraficoComponent();c.unidad='UND';c.historico=[{date:'2026-10-13',quantity:2}];c.estimado=[{date:'2026-10-15',quantity:3}];const opt=c.opciones();expect((opt['series'] as any[]).map(x=>x.data)).toEqual([[2,null],[null,3]]);const chart=init(null,undefined,{renderer:'svg',ssr:true,width:800,height:300});try{chart.setOption(opt);const svg=chart.renderToSVGString();expect(svg).toContain('Ventas observadas');expect(svg).toContain('Ventas estimadas');expect(svg).toContain('#CF1827');expect(svg).toContain('UND');}finally{chart.dispose();}});
 it('solicitud sólo prellena; no registra una compra ni calcula un pago automáticamente',()=>{const purchase={registrar:vi.fn()};const ia={obtenerSolicitud:vi.fn(()=>of({Id:2,IdProducto:1,IdProveedor:1,Cantidad:4,PrecioConIgv:118,Producto:'Producto QA',Proveedor:'Proveedor QA',Unidad:'UND',CodigoProducto:'QA',Justificacion:'Verificado',IdCompra:null}))};TestBed.configureTestingModule({imports:[ComprasComponent],providers:[{provide:CompraService,useValue:purchase},{provide:IaService,useValue:ia},{provide:SessionService,useValue:{tienePermiso:()=>true}},{provide:ActivatedRoute,useValue:{snapshot:{queryParamMap:new Map([['solicitudIA','2']])}}}]});const fixture=TestBed.createComponent(ComprasComponent);const c=fixture.componentInstance;(c as any).cargarSolicitudIA();expect(c.solicitudIA).toBe(2);expect(c.compra.detalles).toMatchObject([{idProducto:1,cantidad:4,precioCompra:118}]);expect(c.compra.montoPagado).toBe(0);expect(purchase.registrar).not.toHaveBeenCalled();fixture.destroy();});
});
