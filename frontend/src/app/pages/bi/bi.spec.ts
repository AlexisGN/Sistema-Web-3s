import { NO_ERRORS_SCHEMA } from '@angular/core';
import { provideRouter } from '@angular/router';
import { ComponentFixture } from '@angular/core/testing';
import { of,Subject,throwError } from 'rxjs';
import { BiComponent } from './bi';
import { BiGraficoComponent } from './bi-grafico';
import { SessionService } from '../../core/services/session.service';
import { HttpTestingController,provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { rangoRapido,fechaPeru,BiFiltro,BiFila,BiVista,periodoComparable,diasPeriodo } from './bi.model';
import { BiService } from './bi.service';
import { baseRequerida } from '../../core/models/reglas-seleccion-permisos';

describe('BI fechas y comunicación',()=>{
  it('acepta doce días inclusivos y construye septiembre 19 al 30',()=>{expect(diasPeriodo('2026-10-01','2026-10-12')).toBe(12);expect(periodoComparable('2026-10-01','2026-10-12','anterior')).toEqual({compararDesde:'2026-09-19',compararHasta:'2026-09-30'});});
  it('no genera fechas de comparación desactivada o personalizada',()=>{expect(periodoComparable('2026-10-01','2026-10-12','ninguna')).toBeNull();expect(periodoComparable('2026-10-01','2026-10-12','personalizado')).toBeNull();});
  it('año anterior conserva duración inclusive aunque febrero sea bisiesto',()=>expect(periodoComparable('2024-02-01','2024-02-29','anioAnterior')).toEqual({compararDesde:'2023-02-01',compararHasta:'2023-03-01'}));
  it('rechaza fechas inexistentes y rangos invertidos',()=>{expect(diasPeriodo('2026-02-30','2026-03-01')).toBeNull();expect(diasPeriodo('2026-10-12','2026-10-01')).toBeNull();});
  it('usa el día de Perú aunque UTC ya esté en el siguiente día',()=>expect(fechaPeru(new Date('2026-10-13T02:00:00Z'))).toBe('2026-10-12'));
  it('mes anterior contempla cambios de año y febrero bisiesto',()=>{expect(rangoRapido('anterior','2026-01-12')).toEqual({desde:'2025-12-01',hasta:'2025-12-31'});expect(rangoRapido('anterior','2024-03-12').hasta).toBe('2024-02-29');});
  it('periodos rápidos no dependen de octubre de 2026',()=>expect(rangoRapido('seis','2029-08-15')).toEqual({desde:'2029-03-01',hasta:'2029-08-15'}));
  it('exportar y consultar referencia requieren BI_VER con la regla existente',()=>{expect(baseRequerida('BI_EXPORTAR')).toBe('BI_VER');expect(baseRequerida('BI_REFERENCIA')).toBe('BI_VER');});
  it('envía filtros al servidor y la desuscripción cancela una consulta obsoleta',()=>{
    TestBed.configureTestingModule({providers:[provideHttpClient(),provideHttpClientTesting()]});
    const s=TestBed.inject(BiService),http=TestBed.inject(HttpTestingController);
    const f:BiFiltro={desde:'2026-04-01',hasta:'2026-10-12',agrupacion:'mes',procedencia:'operativo',pagina:2,tamanio:20,elemento:1};
    const sub=s.consultar('ventas',f).subscribe();const q=http.expectOne(r=>r.url.endsWith('/bi/ventas'));expect(q.request.params.get('pagina')).toBe('2');expect(q.request.params.get('elemento')).toBe('1');sub.unsubscribe();expect(q.cancelled).toBe(true);http.verify();
  });
});

describe('BI interacción del formulario',()=>{
  let fixture:ComponentFixture<BiComponent>;
  const respuesta=(vista:string,filtro:BiFiltro,datos:Record<string,BiFila[]>={})=>({vista,filtros:{...filtro},compararDesde:filtro.modoComparacion==='ninguna'?null:filtro.compararDesde,compararHasta:filtro.modoComparacion==='ninguna'?null:filtro.compararHasta,generadoUtc:'2026-10-13T14:00:00Z',moneda:'PEN',zonaHoraria:'America/Lima',datos,notas:[],indicadores:[],hallazgos:[]});
  const api={consultar:vi.fn((vista:string,filtro:BiFiltro)=>of(respuesta(vista,filtro))),opciones:vi.fn((_procedencia:string,_buscar:string)=>of({} as Record<string,BiFila[]>)),detalle:vi.fn((_tipo:string,_id:number)=>of([] as BiFila[]))};
  beforeEach(async()=>{api.consultar.mockImplementation((vista,filtro)=>of(respuesta(vista,filtro)));api.opciones.mockReset().mockImplementation(()=>of({}));api.detalle.mockReset().mockImplementation(()=>of([]));TestBed.configureTestingModule({imports:[BiComponent],providers:[provideRouter([]),{provide:BiService,useValue:api},{provide:SessionService,useValue:{tienePermiso:()=>true}}]});TestBed.overrideComponent(BiComponent,{remove:{imports:[BiGraficoComponent]},add:{schemas:[NO_ERRORS_SCHEMA]}});fixture=TestBed.createComponent(BiComponent);fixture.detectChanges();await fixture.whenStable();fixture.componentInstance.filtro.desde='2026-10-01';fixture.componentInstance.filtro.hasta='2026-10-12';api.consultar.mockClear();});
  afterEach(()=>{fixture.destroy();TestBed.resetTestingModule();vi.useRealTimers();vi.restoreAllMocks();});
  it('el checkbox aplica doce días y al apagarlo retira inmediatamente la comparación',()=>{const check:HTMLInputElement=fixture.nativeElement.querySelector('input[name="comparar"]');check.checked=true;check.dispatchEvent(new Event('change'));fixture.detectChanges();expect(api.consultar.mock.calls.at(-1)?.[1]).toMatchObject({modoComparacion:'anterior',compararDesde:'2026-09-19',compararHasta:'2026-09-30'});expect(fixture.nativeElement.textContent).toContain('19/09/2026');check.checked=false;check.dispatchEvent(new Event('change'));fixture.detectChanges();expect(api.consultar.mock.calls.at(-1)?.[1].modoComparacion).toBe('ninguna');expect(api.consultar.mock.calls.at(-1)?.[1].compararDesde).toBeUndefined();expect(fixture.nativeElement.textContent).not.toContain('19/09/2026');});
  it('un error de duración no persiste después de corregir la comparación',()=>{const c=fixture.componentInstance;c.comparar=true;c.modoComparacion='personalizado';c.filtro.compararDesde='2026-09-01';c.filtro.compararHasta='2026-09-30';c.aplicar();fixture.detectChanges();expect(c.error()).toContain('mismo número de días');expect(api.consultar).not.toHaveBeenCalled();c.filtro.compararDesde='2026-09-19';c.aplicar();fixture.detectChanges();expect(c.error()).toBe('');expect(api.consultar).toHaveBeenCalledOnce();});
  it('dibuja ambos periodos en las cinco vistas comerciales con la métrica correspondiente y los retira al desactivar',()=>{
    api.consultar.mockImplementation((vista,filtro)=>of(respuesta(vista,filtro,{kpis:[{Tipo:'venta',Importe:100,Operaciones:2},{Tipo:'compra',Importe:80,Operaciones:3},{Tipo:'cotizacion',Importe:70,Operaciones:4}],comparacion:[{Tipo:'venta',Importe:50,Operaciones:1},{Tipo:'compra',Importe:40,Operaciones:2},{Tipo:'cotizacion',Importe:35,Operaciones:3}]})));
    const c=fixture.componentInstance;c.comparar=true;
    for(const vista of ['resumen','ventas','productos','clientes','compras'] as BiVista[]){
      c.ir(vista);fixture.detectChanges();
      const chart=fixture.nativeElement.querySelector('.comparacion-grafico s3s-bi-grafico') as any;
      expect(chart).not.toBeNull();expect(chart.getAttribute('grupo')).toBe('Serie');expect(chart.mostrarValores).toBe(true);
      expect(chart.valor).toBe(vista==='clientes'?'Operaciones':'Importe');
      expect(chart.filas.filter((r:BiFila)=>r['Serie']==='Periodo actual')).toHaveLength(vista==='resumen'?3:1);
      expect(chart.filas.filter((r:BiFila)=>r['Serie']==='Periodo comparado')).toHaveLength(vista==='resumen'?3:1);
      expect(fixture.nativeElement.querySelector('.comparacion-sin-datos')).toBeNull();
    }
    c.comparar=false;c.aplicar();fixture.detectChanges();expect(fixture.nativeElement.querySelector('.comparacion-grafico')).toBeNull();
  });
  it('mantiene el gráfico con cero y explica un periodo comparable sin operaciones',()=>{
    api.consultar.mockImplementation((vista,filtro)=>of(respuesta(vista,filtro,{kpis:[{Tipo:'venta',Importe:3740,Operaciones:6}],comparacion:[]})));
    const c=fixture.componentInstance;c.comparar=true;c.ir('ventas');fixture.detectChanges();
    const chart=fixture.nativeElement.querySelector('.comparacion-grafico s3s-bi-grafico') as any;
    expect(chart.filas).toEqual([{Nombre:'Ventas confirmadas',Serie:'Periodo actual',Importe:3740,Operaciones:6},{Nombre:'Ventas confirmadas',Serie:'Periodo comparado',Importe:0,Operaciones:0}]);
    expect(fixture.nativeElement.querySelector('.comparacion-sin-datos').textContent).toContain('No hay operaciones registradas');
  });
  it('Analizar y Volver al total recuperan el catálogo sin recargar ni perder el periodo, la comparación o la información elegida',()=>{
    const rows:BiFila[]=[{Id:5,Nombre:'Producto A',TipoElemento:'Producto',Importe:100},{Id:6,Nombre:'Producto B',TipoElemento:'Producto',Importe:50}];
    api.consultar.mockImplementation((vista,filtro)=>of(respuesta(vista,filtro,{productosIngresos:filtro.elemento?rows.filter(r=>r['Id']===filtro.elemento):rows,filtros:[{Elemento:filtro.elemento?'Producto A':'Todos los productos y servicios'}]})));
    const c=fixture.componentInstance;c.comparar=true;c.filtro.procedencia='referencia';c.filtro.categoria=1;c.aplicar();fixture.detectChanges();
    const url=window.location.href;
    const panel=()=>Array.from(fixture.nativeElement.querySelectorAll('.panel')).find((p:any)=>p.querySelector('h3')?.textContent==='Productos con mayores ingresos') as HTMLElement;
    expect(panel().querySelectorAll('tbody tr')).toHaveLength(2);
    panel().querySelector('details')!.open=true;panel().querySelector<HTMLButtonElement>('tbody button')!.click();fixture.detectChanges();
    expect(api.consultar.mock.calls.at(-1)?.[1].elemento).toBe(5);expect(panel().querySelectorAll('tbody tr')).toHaveLength(1);
    const back=panel().querySelector<HTMLButtonElement>('.volver-total')!;expect(back.type).toBe('button');back.click();fixture.detectChanges();
    expect(api.consultar.mock.calls.at(-1)?.[1]).toMatchObject({desde:'2026-10-01',hasta:'2026-10-12',modoComparacion:'anterior',compararDesde:'2026-09-19',compararHasta:'2026-09-30',procedencia:'operativo',categoria:1,pagina:1});
    expect(c.filtro.elemento).toBeUndefined();expect(c.filtro.compararElemento).toBeUndefined();expect(panel().querySelectorAll('tbody tr')).toHaveLength(2);expect(panel().querySelector('.volver-total')).toBeNull();expect(window.location.href).toBe(url);
  });
  it('las seis vistas retiran buscador y elección de fuente, conservando sus filtros comerciales',()=>{
    const c=fixture.componentInstance;
    for(const vista of c.vistas){c.ir(vista.id);fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('#bi-busqueda')).toBeNull();
      expect(fixture.nativeElement.querySelector('#bi-origen')).toBeNull();
      expect(fixture.nativeElement.querySelector('.resultados-busqueda')).toBeNull();
      expect(fixture.nativeElement.textContent).not.toContain('Referencia sintética');
      expect(fixture.nativeElement.querySelector('#bi-producto')).not.toBeNull();
      expect(api.consultar.mock.calls.at(-1)?.[1].procedencia).toBe('operativo');
    }
    expect(fixture.nativeElement.querySelector('#bi-desde')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('#bi-hasta')).not.toBeNull();
  });
  it('un estado anterior de referencia vuelve a operativo al consultar sin perder fechas ni comparaciones',()=>{
    const c=fixture.componentInstance;c.filtro.procedencia='referencia';c.comparar=true;c.aplicar();
    expect(c.filtro.procedencia).toBe('operativo');expect(api.consultar.mock.calls.at(-1)?.[1]).toMatchObject({procedencia:'operativo',desde:'2026-10-01',hasta:'2026-10-12',compararDesde:'2026-09-19',compararHasta:'2026-09-30'});
    c.cargarOpciones();expect(api.opciones).toHaveBeenLastCalledWith('operativo','');
  });
  it('Analizar cliente sigue disponible desde la tabla y conserva fechas y comparación sin buscador',()=>{
    api.consultar.mockImplementation((vista,filtro)=>of(respuesta(vista,filtro,{clientesIngresos:[{Id:4,Nombre:'Cliente activo',Importe:250}]})));
    const c=fixture.componentInstance;c.comparar=true;c.ir('ventas');fixture.detectChanges();
    const boton=Array.from(fixture.nativeElement.querySelectorAll('button')).find((b:any)=>b.textContent.trim()==='Analizar cliente') as HTMLButtonElement;
    expect(boton).toBeDefined();boton.click();fixture.detectChanges();
    expect(api.consultar.mock.calls.at(-1)?.[0]).toBe('clientes');
    expect(api.consultar.mock.calls.at(-1)?.[1]).toMatchObject({cliente:4,desde:'2026-10-01',hasta:'2026-10-12',procedencia:'operativo',compararDesde:'2026-09-19',compararHasta:'2026-09-30'});
    expect(fixture.nativeElement.querySelector('#bi-cliente').textContent).toContain('Cliente activo');
    fixture.nativeElement.querySelector('.quitar-tercero').click();fixture.detectChanges();expect(c.filtro.cliente).toBeUndefined();
  });
  const elementos:BiFila[]=[{Id:5,Nombre:'Producto A',TipoElemento:'Producto',Unidad:'UND',Importe:100,Operaciones:1,Cantidad:2},{Id:6,Nombre:'Producto B',TipoElemento:'Producto',Unidad:'UND',Importe:200,Operaciones:2,Cantidad:4}];
  const metricas:BiFila[]=[{Indicador:'Ingresos con IGV',Primero:100,Segundo:200,Unidad:'S/',DiferenciaAbsoluta:100,DiferenciaPorcentual:100},{Indicador:'Ventas confirmadas',Primero:1,Segundo:2,Unidad:'Ventas',DiferenciaAbsoluta:1,DiferenciaPorcentual:100},{Indicador:'Cantidad comercializada',Primero:2,Segundo:4,Unidad:'UND',DiferenciaAbsoluta:2,DiferenciaPorcentual:100}];
  it('seleccionar dos elementos y enviar el formulario dibuja totales, diferencias y evolución separada conservando los filtros',async()=>{
    api.consultar.mockImplementation((vista,filtro)=>of(respuesta(vista,filtro,filtro.compararElemento?{comparacionElementos:elementos,comparacionMetricas:metricas,productosEvolucion:[{Id:5,Nombre:'Producto A',Serie:'A · Producto A',Periodo:'2026-10-01',Importe:100,Cantidad:2},{Id:6,Nombre:'Producto B',Serie:'B · Producto B',Periodo:'2026-10-01',Importe:200,Cantidad:4}]}:{})));
    const c=fixture.componentInstance;c.opciones.set({elementos});c.ir('productos');fixture.detectChanges();await fixture.whenStable();fixture.detectChanges();
    const a=fixture.nativeElement.querySelector('#bi-producto') as HTMLSelectElement,b=fixture.nativeElement.querySelector('#bi-producto-comparar') as HTMLSelectElement;
    expect(b.disabled).toBe(true);a.selectedIndex=1;a.dispatchEvent(new Event('change'));fixture.detectChanges();await fixture.whenStable();fixture.detectChanges();expect(b.disabled).toBe(false);
    expect(Array.from(b.options).some(o=>o.textContent==='Producto A')).toBe(false);b.selectedIndex=1;b.dispatchEvent(new Event('change'));fixture.detectChanges();
    c.filtro.categoria=1;c.filtro.procedencia='referencia';fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}));fixture.detectChanges();
    expect(api.consultar.mock.calls.at(-1)?.[1]).toMatchObject({elemento:5,compararElemento:6,categoria:1,procedencia:'operativo',desde:'2026-10-01',hasta:'2026-10-12'});
    const panel=fixture.nativeElement.querySelector('.comparacion-productos');expect(panel.textContent).toContain('Diferencias de B respecto de A');expect(panel.querySelector('tbody').textContent).toContain('100 %');
    expect(panel.querySelector('s3s-bi-grafico[titulo="Ingresos de A y B"]').filas.map((f:BiFila)=>f['Importe'])).toEqual([100,200]);
    expect(panel.querySelector('s3s-bi-grafico[titulo="Ventas de A y B"]').getAttribute('valor')).toBe('Operaciones');expect(panel.querySelector('s3s-bi-grafico[titulo="Cantidades de A y B"]').unidad).toBe('UND');
    expect(panel.querySelector('s3s-bi-grafico[titulo="Evolución de ingresos de A y B"]').getAttribute('grupo')).toBe('Serie');
    a.selectedIndex=0;a.dispatchEvent(new Event('change'));fixture.detectChanges();await fixture.whenStable();fixture.detectChanges();expect(c.filtro.compararElemento).toBeUndefined();expect(b.disabled).toBe(true);
  });
  it('unidades incompatibles conservan cifras propias y ocultan gráficos de cantidades y diferencias físicas',()=>{
    api.consultar.mockImplementation((vista,filtro)=>of(respuesta(vista,filtro,{comparacionElementos:[elementos[0],{...elementos[1],Unidad:'L'}],comparacionMetricas:[metricas[0],metricas[1],{...metricas[2],Unidad:'Unidades diferentes',DiferenciaAbsoluta:null,DiferenciaPorcentual:null}]})));
    const c=fixture.componentInstance;c.filtro.elemento=5;c.filtro.compararElemento=6;c.vista='productos';c.aplicar();fixture.detectChanges();const panel=fixture.nativeElement.querySelector('.comparacion-productos');
    expect(panel.textContent).toContain('Las unidades son diferentes');expect(panel.querySelector('s3s-bi-grafico[titulo="Cantidades de A y B"]')).toBeNull();expect(panel.querySelectorAll('tbody tr')[2].textContent).toContain('No calculable');
    expect(panel.querySelector('s3s-bi-grafico[titulo="Ingresos de A y B"]')).not.toBeNull();
    c.filtro.compararElemento=5;api.consultar.mockClear();c.aplicar();expect(api.consultar).not.toHaveBeenCalled();expect(c.error()).toContain('diferentes');
  });
  it('las secciones se especializan por vista y el BI no repite Operaciones consultadas ni un historial técnico',()=>{
    const c=fixture.componentInstance;
    for(const [vista,titulo] of [['ventas','Volumen vendido por unidad'],['productos','Demanda por unidad de medida'],['clientes','Oportunidades comerciales por cliente'],['compras','Costos y cantidades de abastecimiento'],['inventario','Entradas, salidas y ajustes']] as [BiVista,string][]){
      c.ir(vista);fixture.detectChanges();expect(fixture.nativeElement.textContent).toContain(titulo);expect(fixture.nativeElement.textContent).not.toContain('Operaciones consultadas');expect(fixture.nativeElement.textContent).not.toContain('Detalle de la operación');
      expect(fixture.nativeElement.querySelector('.desglose-comercial')===null).toBe(vista==='productos'||vista==='inventario');
    }
  });
  it('el desglose de una compra explica el aporte al KPI, utiliza sus conceptos y enlaza Trazabilidad sólo con permiso',()=>{
    const compra:BiFila={Id:20,Tipo:'compra',Tercero:'Proveedor seleccionado',Fecha:'2026-10-03',Estado:'Anulada',Importe:100};
    api.consultar.mockImplementation((vista,filtro)=>of(respuesta(vista,filtro,{operaciones:[compra]})));api.detalle.mockImplementation(()=>of([{Id:5,Nombre:'Producto A',Fecha:'2026-10-03',Estado:'Anulada',Cantidad:1,Unidad:'UND',Importe:100},{Id:6,Nombre:'Otro producto',Fecha:'2026-10-03',Estado:'Anulada',Cantidad:8,Unidad:'UND',Importe:800}]));
    const c=fixture.componentInstance;c.filtro.elemento=5;c.ir('compras');fixture.detectChanges();const ops=fixture.nativeElement.querySelector('.desglose-comercial');
    expect(ops.querySelector('tbody').textContent).toContain('Compra');expect(ops.querySelector('tbody').textContent).not.toContain('Compras vigentes');expect(ops.textContent).toContain('Excluida del indicador vigente');
    ops.querySelector('tbody button').click();fixture.detectChanges();expect(api.detalle).toHaveBeenCalledWith('compra',20);
    const detalle=fixture.nativeElement.querySelector('.desglose-seleccionado');expect(detalle.textContent).toContain('Proveedor seleccionado');expect(detalle.textContent).toContain('Producto A');expect(detalle.textContent).not.toContain('Otro producto');expect(detalle.querySelector('a').getAttribute('href')).toBe('/admin/auditoria');
    vi.spyOn(TestBed.inject(SessionService),'tienePermiso').mockImplementation(p=>p!=='AUDITORIA_VER');c.detalle(compra);fixture.detectChanges();expect(fixture.nativeElement.querySelector('.desglose-seleccionado a')).toBeNull();
  });
  it('cambiar filtros o pestaña cancela el desglose anterior y ninguna respuesta tardía lo vuelve a abrir',()=>{
    const pendiente=new Subject<BiFila[]>();api.detalle.mockImplementation(()=>pendiente);const c=fixture.componentInstance;
    c.detalle({Id:1,Tipo:'venta',Tercero:'Cliente',Estado:'Confirmada',Importe:100});fixture.detectChanges();expect(pendiente.observers).toHaveLength(1);expect(c.detalleCargando()).toBe(true);
    c.ir('compras');fixture.detectChanges();expect(pendiente.observers).toHaveLength(0);pendiente.next([{Nombre:'Respuesta anterior'}]);expect(c.detalleAbierto()).toBe(false);
  });
});
