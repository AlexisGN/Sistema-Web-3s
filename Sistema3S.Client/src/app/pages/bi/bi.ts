import { ChangeDetectionStrategy,Component,DestroyRef,OnInit,inject,signal,computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject,Subscription,catchError,map,of,switchMap,tap,timer } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { BiService } from './bi.service';
import { BiFiltro,BiFila,BiResultado,BiVista,BiComparacion,BiIndicador,rangoRapido,diasPeriodo,periodoComparable } from './bi.model';
import { BiGraficoComponent } from './bi-grafico';
import { AvisoComponent } from '../../shared/mensajes/aviso';
import { UiIconComponent } from '../../shared/ui-icon/ui-icon';
import { SessionService } from '../../core/services/session.service';

type BiCriterio='elementos'|'clientes'|'proveedores';

@Component({selector:'app-bi',standalone:true,imports:[CommonModule,FormsModule,BiGraficoComponent,AvisoComponent,UiIconComponent,RouterLink],
 templateUrl:'./bi.html',styleUrl:'./bi.scss',changeDetection:ChangeDetectionStrategy.OnPush})
export class BiComponent implements OnInit {
  readonly api=inject(BiService);readonly session=inject(SessionService);private readonly destroy=inject(DestroyRef);
  readonly vistas:{id:BiVista;titulo:string}[]=[{id:'resumen',titulo:'Resumen gerencial'},{id:'ventas',titulo:'Análisis de ventas'},{id:'productos',titulo:'Productos y categorías'},{id:'clientes',titulo:'Clientes y cotizaciones'},{id:'compras',titulo:'Compras y proveedores'},{id:'inventario',titulo:'Inventario y rotación'}];
  vista:BiVista='resumen';filtro:BiFiltro={...rangoRapido('actual'),agrupacion:'mes',procedencia:'operativo',pagina:1,tamanio:20};
  comparar=false;modoComparacion:BiComparacion='anterior';busqueda='';verTodosHallazgos=false;readonly resultado=signal<BiResultado|null>(null);readonly opciones=signal<Record<string,BiFila[]>>({});readonly cargando=signal(false);readonly error=signal('');readonly exportando=signal(false);
  readonly solicitudes=new Subject<{vista:BiVista;filtro:BiFiltro}>();private readonly buscar=new Subject<{procedencia:string;texto:string}>();
  readonly coincidencias=signal<Record<string,BiFila[]>>({});readonly buscandoCriterios=signal(false);readonly errorBusqueda=signal('');
  readonly gruposBusqueda=computed(()=>[
    {tipo:'elementos' as BiCriterio,titulo:'Productos y servicios',filas:this.coincidencias()['elementos']??[]},
    {tipo:'clientes' as BiCriterio,titulo:'Clientes',filas:this.coincidencias()['clientes']??[]},
    {tipo:'proveedores' as BiCriterio,titulo:'Proveedores',filas:this.coincidencias()['proveedores']??[]}
  ].filter(g=>g.filas.length>0));
  readonly totalCoincidencias=computed(()=>this.gruposBusqueda().reduce((n,g)=>n+g.filas.length,0));
  readonly titulo=computed(()=>this.vistas.find(v=>v.id===this.resultado()?.vista)?.titulo??'Resumen gerencial');
  readonly etiquetas:Record<string,string>={venta:'Ventas confirmadas',compra:'Compras vigentes',cotizacion:'Cotizaciones'};
  readonly detalleFilas=signal<BiFila[]>([]);readonly detalleAbierto=signal(false);
  readonly detalleContexto=signal<BiFila|null>(null);readonly detalleCargando=signal(false);readonly detalleError=signal('');private consultaDetalle?:Subscription;
  readonly comparacionElementos=computed(()=>this.filas('comparacionElementos'));
  readonly unidadesVenta=computed(()=>this.agruparUnidades('venta'));
  readonly unidadesCompra=computed(()=>this.agruparUnidades('compra'));
  readonly seriesElementos=computed(()=>this.comparacionElementos().map((r,i)=>({...r,Nombre:'Resultado',Serie:`${i===0?'A':'B'} · ${r['Nombre']}`})));
  readonly columnas:Record<string,string>={Nombre:'Nombre',Importe:'Importe con IGV',Cantidad:'Cantidad',Operaciones:'Operaciones',Estado:'Estado',Tipo:'Operación',Serie:'Periodo de consulta',Periodo:'Periodo',Unidad:'Unidad',Participacion:'Participación %',StockActual:'Stock actual',StockMinimo:'Mínimo',Entradas:'Entradas',Salidas:'Salidas',Movimientos:'Movimientos',Ajustes:'Ajustes',Categoria:'Categoría',TipoElemento:'Tipo',Tercero:'Cliente o proveedor',Fecha:'Fecha',Segmento:'Tipo de comprador',Clientes:'Clientes',Promedio:'Saldo promedio',Minimo:'Saldo mínimo',Maximo:'Saldo máximo',Convertidas:'Convertidas',Origen:'Origen',AltasEnPeriodo:'Altas en el periodo'};
  ngOnInit(){
    Object.assign(this.columnas,{CostoUnitario:'Costo unitario ponderado con IGV',Proveedores:'Proveedores',Cotizaciones:'Cotizaciones',Oportunidades:'Oportunidades abiertas',ImportePendiente:'Importe de oportunidades',Convertidas:'Convertidas en venta'});
    this.solicitudes.pipe(tap(()=>{this.cargando.set(true);this.error.set('');}),switchMap(q=>this.api.consultar(q.vista,q.filtro).pipe(map(result=>({result,error:''})),catchError(e=>of({result:null,error:e.error?.mensaje||'No pudimos cargar el análisis. Intenta nuevamente.'})))),takeUntilDestroyed(this.destroy))
      .subscribe(r=>{this.cargando.set(false);if(r.result)this.resultado.set(r.result);this.error.set(r.error);});
    this.buscar.pipe(switchMap(q=>(q.texto?timer(250):of(0)).pipe(switchMap(()=>this.api.opciones(q.procedencia,q.texto)),
      map(opciones=>({q,opciones,error:''})),catchError(()=>of({q,opciones:{},error:'No pudimos buscar coincidencias. Intenta nuevamente.'})))),takeUntilDestroyed(this.destroy)).subscribe(r=>{
        // Una respuesta anterior no puede reemplazar lo que se acaba de escribir o la nueva procedencia.
        if(r.q.procedencia!==this.filtro.procedencia||r.q.texto!==this.textoBusqueda())return;
        this.buscandoCriterios.set(false);this.errorBusqueda.set(r.error);
        if(r.error){if(!r.q.texto)this.error.set('No pudimos cargar los criterios de búsqueda.');return;}
        if(r.q.texto)this.coincidencias.set(r.opciones);else this.actualizarCatalogo(r.opciones);
      });
    this.cargarOpciones();this.aplicar();
  }
  filas(key:string):BiFila[]{return this.resultado()?.datos[key]??[];}
  porTipo(key:string,tipo:string):BiFila[]{return this.filas(key).filter(r=>r['Tipo']===tipo);}
  kpi(tipo:string):BiFila|undefined{return this.filas('kpis').find(r=>r['Tipo']===tipo);}
  readonly datosComparacion=computed<BiFila[]>(()=>{
    const r=this.resultado();if(!r?.compararDesde||r.vista==='inventario')return [];
    const tipos=r.vista==='resumen'?['venta','compra','cotizacion']:[r.vista==='compras'?'compra':r.vista==='clientes'?'cotizacion':'venta'];
    return tipos.flatMap(tipo=>{
      const actual=this.kpi(tipo),anterior=this.porTipo('comparacion',tipo)[0];
      return [
        {Nombre:this.etiquetas[tipo],Serie:'Periodo actual',Importe:this.numero(actual?.['Importe']),Operaciones:this.numero(actual?.['Operaciones'])},
        {Nombre:this.etiquetas[tipo],Serie:'Periodo comparado',Importe:this.numero(anterior?.['Importe']),Operaciones:this.numero(anterior?.['Operaciones'])}
      ];
    });
  });
  comparacionSinOperaciones():boolean{return !this.datosComparacion().some(f=>f['Serie']==='Periodo comparado'&&this.numero(f['Operaciones'])>0);}
  elementoAnalizado():string{return String(this.filas('filtros')[0]?.['Elemento']??this.opciones()['elementos']?.find(e=>e['Id']===this.resultado()?.filtros.elemento)?.['Nombre']??'Producto o servicio seleccionado');}
  numero(v:unknown):number {return Number(v??0);}
  moneda(v:unknown):string{return new Intl.NumberFormat('es-PE',{style:'currency',currency:'PEN'}).format(this.numero(v));}
  valor(v:unknown,key:string):string {
    if(v==null)return 'No calculable';
    if(['Importe','CostoUnitario','ImportePendiente','Base','Igv'].includes(key))return this.moneda(v);
    if(['Fecha','Periodo'].includes(key))return String(v).slice(0,10).split('-').reverse().join('/');
    if(key==='Tipo')return ({venta:'Venta',compra:'Compra',cotizacion:'Cotización'} as Record<string,string>)[String(v)]??String(v);
    return typeof v==='number'?v.toLocaleString('es-PE',{maximumFractionDigits:2}):String(v);
  }
  campos(rows:BiFila[]):string[]{return rows.length?Object.keys(rows[0]).filter(k=>k in this.columnas):[];}
  ir(v:BiVista){this.vista=v;this.filtro.pagina=1;this.filtro.estado=undefined;this.filtro.cliente=undefined;this.filtro.proveedor=undefined;this.filtro.compararElemento=undefined;if(v==='clientes')this.filtro.categoria=undefined;if(v==='inventario'){this.comparar=false;this.cambiarComparacion();}if(v==='inventario'||v==='compras'){if(this.filtro.categoria===0)this.filtro.categoria=undefined;const e=this.opciones()['elementos']?.find(x=>x['Id']===this.filtro.elemento);if(e?.['TipoElemento']==='Servicio')this.filtro.elemento=undefined;}if(this.busqueda)this.limpiarBusqueda();this.aplicar();}
  aplicar(){
    this.error.set('');this.periodoCambio();this.cerrarDetalle();
    if(this.filtro.compararElemento!==undefined && (!this.filtro.elemento||this.filtro.elemento===this.filtro.compararElemento)){this.error.set('Selecciona dos productos o servicios diferentes.');return;}
    const f={...this.filtro,modoComparacion:this.comparar?this.modoComparacion:'ninguna' as BiComparacion};
    if(!diasPeriodo(f.desde,f.hasta)){this.error.set('Selecciona un periodo válido.');return;}
    if(!this.comparar){delete f.compararDesde;delete f.compararHasta;}
    else if(diasPeriodo(f.compararDesde??'',f.compararHasta??'')!==diasPeriodo(f.desde,f.hasta)){this.error.set('Selecciona una comparación con el mismo número de días naturales, incluyendo inicio y fin.');return;}
    this.solicitudes.next({vista:this.vista,filtro:f});
  }
  periodoCambio(){this.error.set('');if(this.comparar&&this.modoComparacion!=='personalizado'){const fechas=periodoComparable(this.filtro.desde,this.filtro.hasta,this.modoComparacion);if(fechas)Object.assign(this.filtro,fechas);}}
  cambiarComparacion(){this.error.set('');if(this.comparar)this.periodoCambio();else{delete this.filtro.compararDesde;delete this.filtro.compararHasta;}}
  indicador(k:BiIndicador):string {if(k.valor==null)return k.etiqueta==='Cantidad vendida'?'Por unidad':'No calculable';return k.formato==='moneda'?this.moneda(k.valor):k.formato==='porcentaje'?this.valor(k.valor,'')+' %':this.valor(k.valor,'');}
  actividad(tipo:string):BiFila[]{return this.porTipo('operaciones',tipo);}
  private agruparUnidades(tipo:string){
    const grupos=new Map<string,BiFila[]>();
    for(const fila of this.porTipo('cantidadesElementos',tipo)){const unidad=String(fila['Unidad']??'Sin unidad');if(!grupos.has(unidad))grupos.set(unidad,[]);grupos.get(unidad)!.push(fila);}
    return Array.from(grupos,([unidad,filas])=>({unidad,filas}));
  }
  productoCambio(){if(!this.filtro.elemento||this.filtro.elemento===this.filtro.compararElemento)this.filtro.compararElemento=undefined;}
  cantidadesComparables(){return this.filas('comparacionMetricas').find(f=>f['Indicador']==='Cantidad comercializada')?.['DiferenciaAbsoluta']!=null;}
  unidadComparada(){return String(this.comparacionElementos()[0]?.['Unidad']??'Cantidad');}
  metricaComparacion(fila:BiFila,campo:string):string {const v=fila[campo];if(v==null)return 'No calculable';if(fila['Unidad']==='S/'&&campo!=='DiferenciaPorcentual')return this.moneda(v);return this.valor(v,'')+(campo==='DiferenciaPorcentual'?' %':'');}
  operacionesTitulo(){return ({ventas:'Ventas que explican el resultado',clientes:'Cotizaciones para seguimiento comercial',compras:'Compras que explican el abastecimiento'} as Record<string,string>)[this.resultado()?.vista??'']??'';}
  aporte(row:BiFila){return row['Tipo']==='cotizacion'?'Incluida en cotizaciones emitidas':row['Estado']==='Confirmada'||(row['Tipo']==='compra'&&row['Estado']==='Registrada')?'Incluida en el indicador vigente':'Excluida del indicador vigente';}
  detalleEtiqueta(row:BiFila){return 'Desglose de '+({venta:'venta',compra:'compra',cotizacion:'cotización'} as Record<string,string>)[String(row['Tipo'])];}
  rapido(tipo:string){Object.assign(this.filtro,rangoRapido(tipo));this.filtro.pagina=1;this.aplicar();}
  textoBusqueda(){return this.busqueda.trim().replace(/\s+/g,' ');}
  cargarOpciones(){const texto=this.textoBusqueda();this.coincidencias.set({});this.errorBusqueda.set('');this.buscandoCriterios.set(!!texto);this.buscar.next({procedencia:this.filtro.procedencia,texto});}
  limpiarBusqueda(){this.busqueda='';this.cargarOpciones();}
  private actualizarCatalogo(catalogo:Record<string,BiFila[]>){
    const ids:Record<string,(number|undefined)[]>={categorias:[this.filtro.categoria],elementos:[this.filtro.elemento,this.filtro.compararElemento],clientes:[this.filtro.cliente],proveedores:[this.filtro.proveedor]};
    const completo={...catalogo};
    for(const [key,seleccion] of Object.entries(ids)){
      const filas=[...(catalogo[key]??[])];
      for(const fila of this.opciones()[key]??[])if(seleccion.includes(fila['Id'] as number)&&!filas.some(f=>f['Id']===fila['Id']))filas.push(fila);
      completo[key]=filas;
    }
    this.opciones.set(completo);
  }
  analizarCriterio(tipo:BiCriterio,fila:BiFila){
    const id=fila['Id'];if(typeof id!=='number'||!Number.isFinite(id))return;
    Object.assign(this.filtro,{categoria:undefined,elemento:undefined,compararElemento:undefined,cliente:undefined,proveedor:undefined,estado:undefined,pagina:1});
    if(tipo==='elementos'){this.vista='productos';this.filtro.elemento=id;}
    else if(tipo==='clientes'){this.vista='clientes';this.filtro.cliente=id;}
    else{this.vista='compras';this.filtro.proveedor=id;}
    this.opciones.update(o=>({...o,[tipo]:[...(o[tipo]??[]).filter(f=>f['Id']!==id),fila]}));
    this.detalleAbierto.set(false);this.limpiarBusqueda();this.aplicar();
  }
  quitarTercero(tipo:'cliente'|'proveedor'){this.filtro[tipo]=undefined;this.filtro.pagina=1;this.aplicar();}
  terceroAnalizado(tipo:'cliente'|'proveedor'):string {
    const key=tipo==='cliente'?'Cliente':'Proveedor',grupo=tipo==='cliente'?'clientes':'proveedores';
    return String(this.filas('filtros')[0]?.[key]??this.opciones()[grupo]?.find(f=>f['Id']===this.resultado()?.filtros[tipo])?.['Nombre']??key);
  }
  procedencia(){this.filtro.cliente=undefined;this.filtro.proveedor=undefined;this.filtro.pagina=1;this.opciones.set({});this.cargarOpciones();this.aplicar();}
  seleccionar(fila:BiFila){if(typeof fila['Id']!=='number')return;this.filtro.elemento=fila['Id'];this.filtro.compararElemento=undefined;this.filtro.pagina=1;this.aplicar();}
  volverAlTotal(){this.filtro.elemento=undefined;this.filtro.compararElemento=undefined;this.filtro.pagina=1;this.detalleAbierto.set(false);this.aplicar();}
  pagina(delta:number){const f=this.resultado()?.filtros;if(!f)return;this.filtro={...f,pagina:f.pagina+delta};this.aplicar();}
  totalTabla(key:string):number{return this.numero(this.filas(key)[0]?.['TotalFilas']);}
  exportar(tipo:'pdf'|'excel'){
    const r=this.resultado();if(!r||this.exportando())return;this.exportando.set(true);
    this.api.exportar(r.vista,r.filtros,tipo).pipe(takeUntilDestroyed(this.destroy)).subscribe({next:blob=>{
      const url=URL.createObjectURL(blob),a=document.createElement('a');a.href=url;a.download=`3S-BI-${r.vista}.${tipo==='excel'?'xlsx':'pdf'}`;a.click();URL.revokeObjectURL(url);this.exportando.set(false);
    },error:()=>{this.exportando.set(false);this.error.set('No se pudo generar el archivo. Intenta nuevamente.');}});
  }
  detalle(fila:BiFila){
    if(!this.puedeDetalle(fila))return;
    const tipo=String(fila['Tipo']);
    const filtros=this.resultado()!.filtros;this.cerrarDetalle();this.detalleContexto.set({...fila});this.detalleAbierto.set(true);this.detalleCargando.set(true);
    this.consultaDetalle=this.api.detalle(tipo,this.numero(fila['Id'])).pipe(takeUntilDestroyed(this.destroy)).subscribe({next:d=>{
      const filas=filtros.elemento?d.filter(r=>r['Id']===filtros.elemento||r['Id']===filtros.compararElemento):d;
      this.detalleFilas.set(filas.map(({Fecha,Estado,...campos})=>campos));this.detalleCargando.set(false);
    },error:()=>{this.detalleCargando.set(false);this.detalleError.set('No se pudo cargar el desglose comercial. Intenta nuevamente.');}});
  }
  cerrarDetalle(){this.consultaDetalle?.unsubscribe();this.detalleAbierto.set(false);this.detalleFilas.set([]);this.detalleContexto.set(null);this.detalleCargando.set(false);this.detalleError.set('');}
  puedeDetalle(fila:BiFila){const permisos:Record<string,string>={venta:'VENTAS_VER',compra:'COMPRAS_VER',cotizacion:'COTIZACIONES_VER'};return this.resultado()?.filtros.procedencia==='operativo'&&this.session.tienePermiso(permisos[String(fila['Tipo'])]??'BI_VER');}
}
