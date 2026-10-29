import {IaRestockComponent} from './ia-restock';
import {IaRankingComponent} from './ia-ranking';
import {IaDialogoComponent} from './ia-dialogo';
import {IaComparacionComponent} from './ia-comparacion';
import {NO_ERRORS_SCHEMA} from '@angular/core';
import {TestBed,ComponentFixture} from '@angular/core/testing';
import {provideRouter} from '@angular/router';
import {of} from 'rxjs';
import {cantidadVisible,serieUnidades,unidadEntera} from './ia-cantidades';
import {IaComponent} from './ia';
import {IaService,IaRespuesta,IaRecomendacion} from './ia.service';
import {IaGraficoComponent} from './ia-grafico';
import {BiGraficoComponent} from '../bi/bi-grafico';
import {SessionService} from '../../core/services/session.service';

describe('IA unidades completas sin alterar el modelo',()=>{
 it('reconoce UND y conserva medidas continuas',()=>{expect(unidadEntera('und.')).toBe(true);expect(unidadEntera('UN')).toBe(true);expect(unidadEntera('L')).toBe(false);expect(cantidadVisible(72.01,'UND')).toBe(72);expect(cantidadVisible(0.8,'L')).toBe(0.8);expect(cantidadVisible(null,'UND')).toBeNull();});
 it('0,8 diarios conserva 24/48/72 unidades y cada día es entero',()=>{
  const raw=Array.from({length:90},(_,i)=>({date:String(i),quantity:0.8001}));const original=JSON.stringify(raw);
  const visible=serieUnidades(raw,'UND');expect(visible.every(d=>Number.isInteger(d.quantity)&&d.quantity>=0)).toBe(true);
  for(const n of [1,7,30,45,60,90])expect(visible.slice(0,n).reduce((a,b)=>a+b.quantity,0)).toBe(Math.round(raw.slice(0,n).reduce((a,b)=>a+b.quantity,0)));
  expect(visible.slice(0,30).reduce((a,b)=>a+b.quantity,0)).toBe(24);expect(JSON.stringify(raw)).toBe(original);
 });
 it('litros y ausencia de serie se conservan',()=>{expect(serieUnidades([{date:'a',quantity:0.85}],'L')[0].quantity).toBe(0.85);expect(serieUnidades([],'UND')).toEqual([]);});
 it('el gráfico de unidades no muestra ejes ni tooltips con fracciones',()=>{
  const c=new IaGraficoComponent();c.unidad='UND';c.estimado=serieUnidades([{date:'2026-10-16',quantity:0.8}],'UND');
  const o=c.opciones() as any;expect(o.yAxis.minInterval).toBe(1);expect(o.tooltip.valueFormatter(72.01)).toBe('72');expect(o.series[1].data).toEqual([1]);
  c.unidad='L';expect((c.opciones() as any).tooltip.valueFormatter(0.8)).toBe('0.8');
 });
 it('el gráfico compartido conserva el contrato anterior salvo que IA solicite unidades completas',()=>{
  const c=new BiGraficoComponent();const original=c.opciones() as any;expect(original.tooltip.valueFormatter(0.8)).toContain('0.8');
  c.cantidadesEnteras=true;expect((c.opciones() as any).tooltip.valueFormatter(72.01)).toContain('72');expect((c.opciones() as any).yAxis.minInterval).toBe(1);
 });
});

describe('IA para gerencia: pendientes y columnas útiles',()=>{
 let fixture:ComponentFixture<IaComponent>;
 const reply:IaRespuesta={procedencia:'operativo',mensaje:'Disponible',actualizado:null,productos:[],recomendaciones:[]};
 const service={avisos:()=>of({pendientes:0}),consultar:()=>of(reply),productos:()=>of([]),solicitud:vi.fn()};
 const rec=(id:number,qty:number|null,actual=true):IaRecomendacion=>({Id:id,IdProducto:1,Nombre:'Contactor QA',Unidad:'UND',Prioridad:'Verificar abastecimiento',Cantidad:qty,FechaPedido:null,Mensaje:'Confirma proveedor y plazo',Estado:'Pendiente',Creada:'2026-10-15',Motivo:null,IdProveedor:null,Proveedor:null,IdSolicitud:null,IdCompra:null,Stock:3,Demanda30:24.002,Actual:actual});
 beforeEach(()=>{TestBed.configureTestingModule({imports:[IaComponent],providers:[provideRouter([]),{provide:IaService,useValue:service},{provide:SessionService,useValue:{tienePermiso:()=>true}}]});TestBed.overrideComponent(IaComponent,{remove:{imports:[IaRestockComponent,IaRankingComponent,IaDialogoComponent,IaComparacionComponent,IaGraficoComponent,BiGraficoComponent]},add:{schemas:[NO_ERRORS_SCHEMA]}});fixture=TestBed.createComponent(IaComponent);fixture.detectChanges();});
 afterEach(()=>{fixture.destroy();TestBed.resetTestingModule();vi.restoreAllMocks();});
 it('oculta cantidad, proveedor y fecha cuando todos son avisos sin esos datos',async()=>{
  await fixture.whenStable();const c=fixture.componentInstance;c.vista='seguimiento';c.respuesta.set({...reply,recomendaciones:[rec(1,null)]});await fixture.whenStable();fixture.detectChanges();
  const headers=fixture.nativeElement.querySelector('table thead').textContent;
  expect(headers).not.toContain('Cantidad a comprar');expect(headers).not.toContain('Cantidad guardada');expect(headers).not.toContain('Proveedor');expect(headers).not.toContain('Fecha sugerida');
  expect(fixture.nativeElement.textContent).toContain('todavía no hay una cantidad de compra calculada');expect(fixture.nativeElement.textContent).toContain('Completar datos');
  expect(fixture.nativeElement.textContent).not.toContain('24.002');expect(service.solicitud).not.toHaveBeenCalled();
 });
 it('muestra cantidades calculadas y conserva los avisos anteriores al desplegarlos',async()=>{
  await fixture.whenStable();const c=fixture.componentInstance;c.vista='seguimiento';c.respuesta.set({...reply,recomendaciones:[rec(1,12),rec(2,null,false)]});await fixture.whenStable();fixture.detectChanges();
  expect(c.recomendaciones).toHaveLength(1);expect(fixture.nativeElement.textContent).toContain('Cantidad a comprar');expect(fixture.nativeElement.textContent).toContain('12 UND');expect(c.avisosAnteriores).toBe(1);
  const toggle=[...fixture.nativeElement.querySelectorAll('button')].find((b:any)=>b.textContent.includes('Ver avisos anteriores')) as HTMLButtonElement;toggle.click();fixture.detectChanges();
  expect(c.recomendaciones).toHaveLength(2);expect(fixture.nativeElement.textContent).toContain('Ocultar avisos anteriores');
 });
 it('falta de costo explica el registro necesario sin convertir el costo ausente a cero',async()=>{
  await fixture.whenStable();const c=fixture.componentInstance;c.vista='proyeccion';c.respuesta.set({...reply,productos:[{idProducto:1,nombre:'Contactor QA',unidad:'UND',precioNeto:100,costoNeto:null,horizontes:[{horizonte:30,cantidad:24.002,ingresoNeto:2400.2,costoReferencia:null,margenBruto:null,porcentajeMargen:null,estado:'Pendiente',recomendacion:'Revisa una compra vigente de este producto dentro de los últimos 90 días, incluido hoy.'}]}]});await fixture.whenStable();fixture.detectChanges();
  const text=fixture.nativeElement.textContent;expect(text).toContain('Falta costo de compra');expect(text).toContain('Revisar compras');expect(text).toContain('incluido hoy');expect(c.finanzasTotales.costo).toBeNull();expect(text).not.toContain('24.002');
 });
 it('distingue ausencia de pronóstico y de precio sin atribuir todos los pendientes al costo',()=>{
  const c=fixture.componentInstance;c.vista='proyeccion';
  const noForecast={idProducto:2,nombre:'QA',unidad:'UND',precioNeto:100,costoNeto:40,motivo:'Historial pendiente'};
  c.respuesta.set({...reply,productos:[noForecast]});expect(c.textoCosto).toBe('Falta pronóstico de ventas');expect(c.textoMargen).toBe('Falta pronóstico de ventas');expect(c.pendienteFinanciero(noForecast)).toBe('Historial pendiente');
  const noPrice={...noForecast,precioNeto:null,horizontes:[{horizonte:30,cantidad:12,ingresoNeto:null,costoReferencia:480,margenBruto:null,porcentajeMargen:null,estado:'Pendiente',recomendacion:'Genérico'}]};
  c.respuesta.set({...reply,productos:[noPrice]});expect(c.textoMargen).toBe('Falta precio de venta');expect(c.pendienteFinanciero(noPrice)).toContain('venta confirmada');
 });
 it('reutiliza el proveedor del producto sin volver a verificarlo ni guardar automáticamente',async()=>{
  await fixture.whenStable();const c=fixture.componentInstance;const abastecimiento=vi.fn();(service as any).abastecimiento=abastecimiento;(service as any).proveedores=()=>of([{IdProveedor:7,Nombre:'Proveedor registrado'}]);
  const p={idProducto:1,nombre:'QA',unidad:'UND',idProveedor:7,proveedor:'Proveedor registrado'};
  c.vista='inventario';c.respuesta.set({...reply,productos:[p]});c.abrirPolitica(p);fixture.detectChanges();await fixture.whenStable();
  expect(c.politica?.idProveedor).toBe(7);expect(c.politica?.plazoDias).toBe(0);expect(abastecimiento).not.toHaveBeenCalled();
  expect(fixture.nativeElement.textContent).not.toContain('Pendiente de verificar');expect(fixture.nativeElement.textContent).toContain('Planificar la próxima compra');
  const policy={idProducto:1,idProveedor:9,plazoDias:3,coberturaDias:7,seguridad:0,minimo:1,multiplo:1,reservado:0,entradaConfirmada:0,fechaEntrada:null,evidencia:'Condiciones registradas',verificadaHasta:'2026-12-01'};
  c.abrirPolitica({...p,politica:policy});expect(c.politica?.idProveedor).toBe(9);expect(c.politica).not.toBe(policy);expect(abastecimiento).not.toHaveBeenCalled();
 });
 it('corrige el texto de avisos existentes conservando su código e historial',()=>{
  const c=fixture.componentInstance;const item={...rec(1,null),IdProveedor:7,Mensaje:'Mensaje anterior guardado'};const original=JSON.stringify(item);
  expect(c.prioridadTexto(item.Prioridad)).toBe('Configurar reposición');expect(c.mensajeRecomendacion(item)).toContain('Proveedor identificado');expect(JSON.stringify(item)).toBe(original);
  expect(c.mensajeRecomendacion({...item,Actual:false})).toBe('Mensaje anterior guardado');
 });
 it('el plan separa opciones avanzadas y explica el error de una entrada sin fecha',async()=>{
  await fixture.whenStable();const c=fixture.componentInstance;(service as any).proveedores=()=>of([{IdProveedor:7,Nombre:'Proveedor registrado'}]);
  c.vista='inventario';c.respuesta.set({...reply,productos:[{idProducto:1,nombre:'QA',unidad:'UND',idProveedor:7}]});c.abrirPolitica({idProducto:1,nombre:'QA',unidad:'UND',idProveedor:7});c.politica!.plazoDias=10;c.politica!.entradaConfirmada=2;await fixture.whenStable();fixture.detectChanges();
  expect(c.erroresPolitica.some(x=>x.includes('fecha futura de llegada'))).toBe(true);expect(c.diasPlanificados).toBe(40);
  const advanced=fixture.nativeElement.querySelector('.plan-avanzado');expect(advanced.open).toBe(false);expect(advanced.textContent).toContain('Compra mínima');
  const save=[...fixture.nativeElement.querySelectorAll('button')].find((b:any)=>b.textContent.includes('Guardar plan de compra')) as HTMLButtonElement;
  expect(save.disabled).toBe(true);expect(fixture.nativeElement.querySelector('.plan-errores').textContent).toContain('2 unidades en camino');
 });
 it('guardar conserva condiciones avanzadas y completa sólo la nota administrativa',async()=>{
  await fixture.whenStable();const c=fixture.componentInstance;(service as any).proveedores=()=>of([]);const save=vi.fn((policy:unknown)=>of({mensaje:'Guardado'}));(service as any).abastecimiento=save;
  c.abrirPolitica({idProducto:1,nombre:'QA',unidad:'UND',idProveedor:7});c.politica!.plazoDias=10;c.politica!.seguridad=20;c.politica!.reservado=4;c.politica!.minimo=6;c.politica!.multiplo=6;
  const date=c.politica!.verificadaHasta;expect(date>new Date().toLocaleDateString('sv-SE',{timeZone:'America/Lima'})).toBe(true);
  expect(c.erroresPolitica).toEqual([]);c.guardarPolitica();expect(save).toHaveBeenCalledOnce();const body=save.mock.calls[0]![0] as any;
  expect(body.seguridad).toBe(20);expect(body.reservado).toBe(4);expect(body.minimo).toBe(6);expect(body.multiplo).toBe(6);expect(body.evidencia).toContain('registradas por el responsable');expect(body.verificadaHasta).toBe(date);
 });
 it('no confunde inventario cubierto por más de 90 días con falta de datos',()=>{
  const c=fixture.componentInstance;const p={idProducto:1,nombre:'QA',unidad:'UND',riesgo:{stock:40,coberturaDias:null,agotamiento:null,prioridad:'Verificar abastecimiento',cantidad:null,fechaPedido:null,idProveedor:1,explicacion:'Plazo pendiente'}};
  expect(c.coberturaTexto(p)).toBe('Más de 90 días');expect(c.agotamientoTexto(p)).toBe('No previsto en el período');
  expect(c.coberturaTexto({...p,riesgo:{...p.riesgo,prioridad:'Sin estimación'}})).toBe('Sin pronóstico vigente');expect(c.coberturaTexto({...p,riesgo:{...p.riesgo,coberturaDias:20}})).toBe('20 días');
 });
 it('todos los filtros de consulta están en grupos alineados y la ayuda del calendario está fuera de su etiqueta',()=>{
  const el=fixture.nativeElement;expect(el.querySelector('.filtros-contexto')).not.toBeNull();expect(el.querySelector('.filtros-periodo')).not.toBeNull();
  const input=el.querySelector('input[aria-label="Fecha objetivo del análisis"]');expect(input.parentElement.querySelector('small')).toBeNull();expect(el.querySelector('.filtros-pie small').textContent).toContain('Fecha disponible');
 });
 it('cantidad diaria, total, gráficas y porcentajes conservan unidades y significado',()=>{
  const c=fixture.componentInstance;expect(c.formatearCantidad(48.01,'UND')).toBe('48');expect(c.numero(14.31)).toBe('14.31');expect(c.dinero(2237.73)).toContain('2,237.73');
  const p={idProducto:1,nombre:'QA',unidad:'UND',estimado:[{date:'a',quantity:0.8},{date:'b',quantity:0.8}]};const serie=c.diasVisibles(p);expect(serie).toBe(c.diasVisibles(p));expect(serie.map(d=>d.quantity)).toEqual([1,1]);expect(p.estimado[0].quantity).toBe(0.8);
 });
});
