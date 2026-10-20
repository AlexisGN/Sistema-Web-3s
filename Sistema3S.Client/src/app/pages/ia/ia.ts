import {IaRestockComponent} from './ia-restock';
import {IaDialogoComponent} from './ia-dialogo';
import {IaRankingComponent} from './ia-ranking';
import {IaComparacionComponent} from './ia-comparacion';
import {cantidadVisible,serieUnidades,unidadEntera} from './ia-cantidades';
import {Component,OnInit,OnDestroy,Input,inject,signal} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {Router,RouterLink} from '@angular/router';
import {Subscription,forkJoin,of,catchError} from 'rxjs';
import {IaAvisosComponent} from './ia-avisos';
import {SessionService} from '../../core/services/session.service';
import {AvisoComponent} from '../../shared/mensajes/aviso';
import {UiIconComponent} from '../../shared/ui-icon/ui-icon';
import {IaGraficoComponent} from './ia-grafico';
import {BiGraficoComponent} from '../bi/bi-grafico';
import {IaService,IaProducto,IaRespuesta,IaRecomendacion,IaPolitica,IaDia,IaVistaPrevia} from './ia.service';
@Component({selector:'app-ia',standalone:true,imports:[IaRestockComponent,IaRankingComponent,IaDialogoComponent,IaComparacionComponent,IaAvisosComponent,CommonModule,FormsModule,RouterLink,AvisoComponent,UiIconComponent,IaGraficoComponent,BiGraficoComponent],templateUrl:'./ia.html',styleUrl:'./ia.scss'})
export class IaComponent implements OnInit,OnDestroy {
 @Input() clienteFinal=false;
 readonly session=inject(SessionService);private readonly service=inject(IaService);private readonly router=inject(Router);
 readonly tabs=[{id:'resumen',nombre:'Resumen inteligente',permiso:'IA_VER'},
  {id:'pronostico',nombre:'Pronóstico de ventas',permiso:'IA_PRONOSTICOS_VER'},
  {id:'proyeccion',nombre:'Proyección y rentabilidad',permiso:'IA_PROYECCIONES_VER'},
  {id:'inventario',nombre:'Riesgo de inventario',permiso:'IA_ALERTAS_VER'},
  {id:'compras',nombre:'Compras recomendadas',permiso:'IA_COMPRAS_GESTIONAR'},
  {id:'seguimiento',nombre:'Seguimiento de recomendaciones',permiso:'IA_COMPRAS_GESTIONAR'}];
 readonly secciones=[{id:'resumen',nombre:'Resumen gerencial',detalle:'Resultados y decisiones',icono:'activity',vistas:['resumen','proyeccion']},{id:'pronostico',nombre:'Ventas previstas',detalle:'Qué se podría vender',icono:'chart',vistas:['pronostico']},{id:'inventario',nombre:'Abastecimiento',detalle:'Stock y próximas compras',icono:'box',vistas:['inventario','compras','seguimiento']}];
 get seccionesVisibles(){return this.secciones.filter(s=>s.vistas.some(v=>this.puede(this.tabs.find(t=>t.id===v)!.permiso)));}
 seccionActiva(id:string){return this.secciones.find(s=>s.id===id)?.vistas.includes(this.vista)??false;}
 abrirSeccion(id:string){const s=this.secciones.find(s=>s.id===id);const v=s?.vistas.find(v=>this.puede(this.tabs.find(t=>t.id===v)!.permiso));if(v)this.cambiarVista(v);}
 get tituloVista(){return ({resumen:'La próxima decisión empieza aquí',pronostico:'¿Qué productos se podrían vender?',proyeccion:'¿Cuánto ingreso y margen se espera?',inventario:'¿Alcanza el stock para las ventas previstas?',compras:'Revisa y prepara la próxima compra',seguimiento:'Decisiones y resultados'} as Record<string,string>)[this.vista];}
 unidadComparacion='UND';
 get unidadesComparacion(){return [...new Set(this.productos.map(p=>p.unidad))];}
 get unidadComparacionActiva(){return this.unidadesComparacion.includes(this.unidadComparacion)?this.unidadComparacion:this.unidadesComparacion[0]??'UND';}
 private comparacionVentas:unknown=null;private comparacionOrigen:unknown=null;private comparacionClave='';private comparacionCache:{id:number;nombre:string;actual:number;previsto:number}[]=[];
 get comparacionStock(){const key=this.vista+this.tituloHorizonte+this.unidadComparacionActiva;if(this.comparacionOrigen!==this.respuesta()||this.comparacionVentas!==this.inventarioVentas()||key!==this.comparacionClave){this.comparacionOrigen=this.respuesta();this.comparacionVentas=this.inventarioVentas();this.comparacionClave=key;this.comparacionCache=this.productos.filter(p=>p.unidad===this.unidadComparacionActiva&&(p.riesgo?.stock??p.stock)!=null).flatMap(p=>{const venta=this.vista==='inventario'?this.inventarioVentas().find(x=>x.idProducto===p.idProducto):p;const qty=venta?this.cantidadSeleccionada(venta):null;return qty==null?[]:[{id:p.idProducto,nombre:p.nombre,actual:cantidadVisible(p.riesgo?.stock??p.stock,p.unidad)!,previsto:cantidadVisible(qty,p.unidad)!}];}).sort((a,b)=>(b.previsto-b.actual)-(a.previsto-a.actual));}return this.comparacionCache;}
 readonly inventarioVentas=signal<IaProducto[]>([]);
 get stockEnRiesgo(){return this.comparacionStock.filter(p=>p.previsto>p.actual).length;}
 verProducto(id:number){if(!this.puede('IA_PRONOSTICOS_VER'))return;this.producto=id;this.cambiarVista('pronostico');}
 get margenResumen(){return this.sumarConocidos(this.resumenFinanzas().map(p=>this.financieroResumen(p)?.margenBruto));}
 get rangoConsulta(){if(this.vista==='proyeccion'&&this.periodoFinanciero==='mes'&&!this.esFechaPersonalizada){const d=new Date(this.hoyPeru()+'T00:00:00Z');d.setUTCMonth(d.getUTCMonth()+1,1);const first=d.toISOString().slice(0,10);d.setUTCMonth(d.getUTCMonth()+1,0);return this.fecha(first)+' — '+this.fecha(d.toISOString().slice(0,10));}return this.fecha(this.respuesta()?.periodo?.desde||this.fechaMinima)+' — '+this.fecha(this.esFechaPersonalizada?this.fechaObjetivo:this.sumarDias(this.respuesta()?.periodo?.desde||this.fechaMinima,this.horizonte-1));}
 vista='resumen';procedencia='operativo';producto:number|null=null;horizonte=30;periodoFinanciero='dias';
 mostrarAnteriores=false;fechaObjetivo='';private calendarioDesde='';private calendarioHasta='';
 readonly resumenVentas=signal<IaProducto[]>([]);readonly resumenFinanzas=signal<IaProducto[]>([]);readonly resumenRiesgos=signal<IaProducto[]>([]);readonly consultasParciales=signal(false);readonly riesgosDisponibles=signal(false);
 readonly catalogo=signal<IaProducto[]>([]);readonly respuesta=signal<IaRespuesta|null>(null);readonly cargando=signal(false);readonly error=signal('');readonly mensaje=signal('');
 readonly proveedores=signal<{IdProveedor:number;Nombre:string}[]>([]);readonly ocupada=signal(false);
 revision:IaRecomendacion|null=null;estadoRevision='Revisada';motivo='';cantidad=1;proveedor=0;
 politica:IaPolitica|null=null;politicaProducto:IaProducto|null=null;
 readonly calculandoPlan=signal(false);readonly errorPlan=signal('');readonly vistaPrevia=signal<IaVistaPrevia|null>(null);private clavePlan='';private peticionPlan?:Subscription;
 paginaDiaria=1;
 cerrarPolitica(){if(this.ocupada())return;this.peticionPlan?.unsubscribe();this.politica=null;this.vistaPrevia.set(null);this.calculandoPlan.set(false);}
 get vistaPreviaActual(){return this.politica&&JSON.stringify(this.politica)===this.clavePlan?this.vistaPrevia():null;}
 calcularPlan(){if(!this.politica||this.erroresPolitica.length||this.calculandoPlan())return;this.peticionPlan?.unsubscribe();this.clavePlan=JSON.stringify(this.politica);const policy={...this.politica,evidencia:this.politica.evidencia.trim()||'Condiciones de reposición registradas por el responsable en Sistema3S.'};this.calculandoPlan.set(true);this.errorPlan.set('');this.vistaPrevia.set(null);this.peticionPlan=this.service.previsualizar(policy).subscribe({next:r=>{this.vistaPrevia.set(r);this.calculandoPlan.set(false);},error:e=>{this.errorPlan.set(e.error?.mensaje??'No se pudo calcular el plan. Actualiza la consulta.');this.calculandoPlan.set(false);}});}
 get unidadPlan(){return this.politicaProducto?.unidad??'UND';}
 get tarjetasResumen(){return this.tarjetasStock(this.resumenRiesgos(),this.resumenVentas()).slice(0,5);}
 get tarjetasInventario(){return this.tarjetasStock(this.productos,this.inventarioVentas());}
 private tarjetasStock(productos:IaProducto[],ventas:IaProducto[]){return productos.map(p=>{const venta=ventas.find(v=>v.idProducto===p.idProducto),q=venta?this.cantidadSeleccionada(venta):null,stock=p.riesgo?.stock??p.stock??null,cobertura=p.riesgo?.coberturaDias;const porcentaje=q!=null&&q>0&&stock!=null?Math.min(100,Math.max(0,stock/q*100)):null;const nivel=p.riesgo?.prioridad==='Urgente'||(cobertura!=null&&cobertura<=7)?'critico':porcentaje!=null&&porcentaje<100?'bajo':'normal';return {p,stock,cantidad:q,porcentaje,nivel,estado:nivel==='critico'?'Reponer pronto':nivel==='bajo'?'Planificar':'Revisar',cobertura};}).sort((a,b)=>(a.cobertura??999)-(b.cobertura??999));}
 get calidadResumen(){const groups=new Map<string,{unidad:string;error:number;observado:number;productos:number;cumplen:number}>();for(const p of (this.vista==='pronostico'?this.productos:this.resumenVentas())){const q=p.calidad?.['30'];if(!q)continue;const g=groups.get(p.unidad)??{unidad:p.unidad,error:0,observado:0,productos:0,cumplen:0};g.error+=Math.abs(q.predicted-q.actual);g.observado+=q.actual;g.productos++;if(q.wape!=null&&q.wape<=.15)g.cumplen++;groups.set(p.unidad,g);}return [...groups.values()].map(g=>({...g,wape:g.observado>0?g.error/g.observado*100:null}));}
 get calidadFueraObjetivo(){return this.calidadResumen.some(g=>g.cumplen<g.productos);}
 diasPagina(p:IaProducto){return this.diasVisibles(p).slice(0,this.diasSeleccionados).slice((this.paginaDiaria-1)*10,this.paginaDiaria*10);}
 get paginasDiarias(){return Math.ceil(this.diasSeleccionados/10);}
 pedidoTexto(p:IaProducto){return (p.riesgo?.retrasoDias??0)>0?'Pedir hoy':p.riesgo?.fechaPedido?this.fecha(p.riesgo.fechaPedido):'Sin pedido previsto';}

 private consulta?:Subscription;private opciones?:Subscription;private acciones=new Subscription();
 private financieroFuente:IaRespuesta|null=null;private financieroHorizonte=0;private financieroCache:{Nombre:string;Serie:string;Importe:number}[]=[];
 private mensualFuente:IaRespuesta|null=null;private mensualCache={historico:[] as {date:string;quantity:number}[],estimado:[] as {date:string;quantity:number}[]};
 ngOnInit(){if(this.clienteFinal&&this.puede('IA_ALERTAS_VER'))this.vista='inventario';this.cargarCatalogo();this.cargar();}
 ngOnDestroy(){this.peticionPlan?.unsubscribe();this.consulta?.unsubscribe();this.opciones?.unsubscribe();this.acciones.unsubscribe();}
 puede(p:string){return this.session.tienePermiso(p);}
 get productos():IaProducto[]{const value=this.respuesta()?.productos;return Array.isArray(value)?value:[];}
 get recomendaciones(){const rows=this.respuesta()?.recomendaciones??[];return this.vista==='seguimiento'&&this.mostrarAnteriores?rows:rows.filter(r=>r.Actual!==false);}
 get avisosAnteriores(){return (this.respuesta()?.recomendaciones??[]).filter(r=>r.Actual===false).length;}
 get hayCantidadRecomendada(){return this.recomendaciones.some(r=>r.Cantidad!=null);}
 get hayFechaPedido(){return this.recomendaciones.some(r=>r.FechaPedido!=null);}
 get hayProveedor(){return this.recomendaciones.some(r=>!!r.Proveedor);}
 get columnasRecomendaciones(){return 5+Number(this.hayCantidadRecomendada)+Number(this.hayFechaPedido)+Number(this.hayProveedor);}
 private seriesVisibles=new WeakMap<object,Map<string,IaDia[]>>();
 readonly unidadEntera=unidadEntera;
 formatearCantidad(v:number|null|undefined,unidad:string){const value=cantidadVisible(v,unidad);return value==null?'—':value.toLocaleString('es-PE',{maximumFractionDigits:unidadEntera(unidad)?0:2});}
 diasVisibles(p:IaProducto){const source=p.estimado??[];let cache=this.seriesVisibles.get(source);if(!cache){cache=new Map();this.seriesVisibles.set(source,cache);}if(!cache.has(p.unidad))cache.set(p.unidad,serieUnidades(source,p.unidad));return cache.get(p.unidad)!;}
 get guiaVista(){return ({resumen:'Revisa cuánto podrías vender, qué productos necesitan atención y qué datos faltan para decidir.',pronostico:'Consulta las ventas previstas por producto y compáralas con las ventas recientes.',proyeccion:'Revisa ingresos y margen bruto previstos. El margen necesita un costo de compra válido.',inventario:'Identifica qué producto podría agotarse y revisa sus condiciones de reposición. Se aprovecha el proveedor registrado en compras.',compras:'Revisa las cantidades sugeridas antes de preparar una compra. Los avisos sin cantidad indican datos por completar.',seguimiento:'Consulta las decisiones tomadas y su resultado. Los avisos anteriores se pueden desplegar.'} as Record<string,string>)[this.vista];}
 get pendientesFinancieros(){return this.productos.filter(p=>this.financiero(p)?.cantidad==null||p.precioNeto==null||p.costoNeto==null);}
 pendienteFinanciero(p:IaProducto){
  if(this.financiero(p)?.cantidad==null)return p.motivo||'Revisa el historial en Pronóstico de ventas: se necesitan 90 días de historia y ventas confirmadas en al menos cuatro fechas.';
  if(p.precioNeto==null)return 'Revisa una venta confirmada de este producto con precio válido dentro de los últimos 90 días, incluido hoy.';
  if(p.costoNeto==null)return 'Revisa una compra vigente de este producto con cantidad y costo válidos en el historial, incluido hoy. Primero se usan compras recientes y, si no hay, la última compra válida. Sin ese costo no se calcula el margen.';
  return this.financiero(p)?.recomendacion??'Datos disponibles para revisar ingresos y margen bruto.';
 }
 pasoPendiente(p:IaProducto){return this.financiero(p)?.cantidad==null?'Revisar ventas':p.precioNeto==null?'Revisar ventas':'Revisar compras';}
 irPendiente(p:IaProducto){if(this.financiero(p)?.cantidad==null||p.precioNeto==null){this.producto=p.idProducto;this.cambiarVista('pronostico');}else if(this.puede('COMPRAS_VER'))this.router.navigate(['/admin/compras']);}
 puedeResolver(p:IaProducto){return this.financiero(p)?.cantidad==null||p.precioNeto==null?this.puede('IA_PRONOSTICOS_VER'):this.puede('COMPRAS_VER');}
 revisarAbastecimiento(rec:IaRecomendacion){this.verInventario(rec.IdProducto);}
 verInventario(id:number){this.producto=id;this.cambiarVista('inventario');}
 prioridadTexto(value:string|null|undefined){return value==='Verificar abastecimiento'?'Configurar reposición':value??'';}
 mensajeRecomendacion(rec:IaRecomendacion){return rec.Actual!==false&&rec.Prioridad==='Verificar abastecimiento'&&rec.Cantidad==null?(rec.IdProveedor?'Proveedor identificado. Faltan las condiciones de reposición para calcular cuánto comprar.':'Faltan la asociación del proveedor y las condiciones de reposición del producto; no es una nueva verificación del proveedor.'):rec.Mensaje;}
 costoHistorico(p:IaProducto){return !!p.fechaCosto&&p.fechaCosto.slice(0,10)<this.sumarDias(this.hoyPeru(),-89);}
 coberturaTexto(p:IaProducto){const risk=p.riesgo;if(risk?.coberturaDias!=null)return this.numero(risk.coberturaDias)+' días';return !risk||['Sin información','Sin estimación','Revisar stock'].includes(risk.prioridad)?'Sin pronóstico vigente':this.esFechaPersonalizada?'Cubierto hasta '+this.fecha(this.fechaObjetivo):'Más de 90 días';}
 agotamientoTexto(p:IaProducto){return p.riesgo?.agotamiento?this.fecha(p.riesgo.agotamiento):this.coberturaTexto(p).startsWith('Sin')?'Sin pronóstico vigente':'No previsto en el período';}
 get coberturaIngresos(){return this.resumenFinanzas().filter(p=>this.financieroResumen(p)?.ingresoNeto!=null).length;}
 get coberturaCostos(){return this.productos.filter(p=>this.financiero(p)?.costoReferencia!=null).length;}
 get coberturaMargen(){return this.productos.filter(p=>this.financiero(p)?.margenBruto!=null).length;}
 get textoCosto(){return this.coberturaCostos?this.dinero(this.finanzasTotales.costo):this.productos.some(p=>this.financiero(p)?.cantidad!=null)?'Falta costo de compra':'Falta pronóstico de ventas';}
 get textoMargen(){return this.coberturaMargen?this.dinero(this.finanzasTotales.margen):!this.productos.some(p=>this.financiero(p)?.cantidad!=null)?'Falta pronóstico de ventas':this.productos.some(p=>this.financiero(p)?.ingresoNeto!=null)?'Pendiente de costo':'Falta precio de venta';}

 cambiarVista(v:string){const tab=this.tabs.find(t=>t.id===v);if(!tab||!this.puede(tab.permiso))return;this.vista=v;this.revision=null;this.cerrarPolitica();this.cargar();}
 cargarCatalogo(){this.opciones?.unsubscribe();this.opciones=this.service.productos(this.procedencia).subscribe({next:p=>this.catalogo.set(p),error:()=>this.catalogo.set([])});}
 cargar(){
  this.procedencia='operativo';
  const tab=this.tabs.find(t=>t.id===this.vista)!;if(!this.puede(tab.permiso))return;
  this.consulta?.unsubscribe();this.cargando.set(true);this.error.set('');
  this.paginaDiaria=1;this.seriesVisibles=new WeakMap();this.respuesta.set(null);this.resumenVentas.set([]);this.resumenFinanzas.set([]);this.resumenRiesgos.set([]);this.inventarioVentas.set([]);this.consultasParciales.set(false);this.riesgosDisponibles.set(false);
  if(this.fechaObjetivo && (!/^\d{4}-\d{2}-\d{2}$/.test(this.fechaObjetivo)||this.fechaObjetivo<this.fechaMinima||this.fechaObjetivo>this.fechaMaxima)){
   this.error.set('Selecciona una fecha futura dentro del pronóstico disponible: '+this.fecha(this.fechaMinima)+' al '+this.fecha(this.fechaMaxima)+'.');this.cargando.set(false);return;
  }
  const consultar=(view:string)=>this.esFechaPersonalizada?this.service.consultar(view,this.procedencia,this.producto,this.fechaObjetivo):this.service.consultar(view,this.procedencia,this.producto);
  const rows=(r:IaRespuesta|null):IaProducto[]=>Array.isArray(r?.productos)?r.productos:[];
  const optional=(view:string,permission:string)=>this.puede(permission)?consultar(view).pipe(catchError(()=>{this.consultasParciales.set(true);return of(null);})):of(null);
  this.consulta=forkJoin({principal:consultar(this.vista),
   ventas:this.vista==='resumen'||this.vista==='inventario'?optional('pronostico','IA_PRONOSTICOS_VER'):of(null),
   finanzas:this.vista==='resumen'||this.vista==='pronostico'?optional('proyeccion','IA_PROYECCIONES_VER'):of(null),
   riesgos:this.vista==='resumen'?optional('inventario','IA_ALERTAS_VER'):of(null)}).subscribe({next:r=>{if(r.principal.periodo){this.calendarioDesde=r.principal.periodo.disponibleDesde;this.calendarioHasta=r.principal.periodo.disponibleHasta;}this.respuesta.set(r.principal);this.resumenVentas.set(rows(r.ventas));this.inventarioVentas.set(rows(r.ventas));this.resumenFinanzas.set(rows(r.finanzas));this.resumenRiesgos.set(rows(r.riesgos));this.riesgosDisponibles.set(r.riesgos!=null);this.prepararResumen();this.cargando.set(false);},error:e=>{this.respuesta.set(null);this.error.set(e.error?.mensaje??'No se pudo consultar el análisis.');this.cargando.set(false);}});
 }
 get esFechaPersonalizada(){return !!this.fechaObjetivo;}
 private hoyPeru(){return new Date().toLocaleDateString('sv-SE',{timeZone:'America/Lima'});}
 private sumarDias(fecha:string,dias:number){const date=new Date(fecha+'T00:00:00Z');date.setUTCDate(date.getUTCDate()+dias);return date.toISOString().slice(0,10);}
 get fechaMinima(){return this.calendarioDesde||this.sumarDias(this.hoyPeru(),1);}
 get fechaMaxima(){return this.calendarioHasta||this.sumarDias(this.hoyPeru(),90);}
 get diasSeleccionados(){return this.esFechaPersonalizada?(this.respuesta()?.periodo?.dias??Math.max(0,Math.round((Date.parse(this.fechaObjetivo)-Date.parse(this.fechaMinima))/86400000)+1)):this.horizonte;}
 get tituloHorizonte(){return this.esFechaPersonalizada?'hasta '+this.fecha(this.fechaObjetivo):this.horizonte+' días';}
 get tituloIngresosResumen(){return 'Ingresos previstos · '+this.tituloHorizonte;}
 usarHorizonte(){this.fechaObjetivo='';this.cargar();}
 cantidadSeleccionada(p:IaProducto){return this.esFechaPersonalizada?p.periodoPersonalizado?.cantidad:p.cantidades?.[String(this.horizonte)];}
 financieroResumen(p:IaProducto){return this.esFechaPersonalizada?p.periodoPersonalizado:p.horizontes?.find(h=>h.horizonte===this.horizonte);}
 volver(){this.producto=null;this.cargar();}
 numero(v:number|null|undefined){return v==null?'Información insuficiente':v.toLocaleString('es-PE',{maximumFractionDigits:2});}
 dinero(v:number|null|undefined){return v==null?'Información insuficiente':'S/ '+v.toLocaleString('es-PE',{minimumFractionDigits:2,maximumFractionDigits:2});}
 fecha(v:string|null|undefined){return v?new Date(v.length===10?v+'T12:00:00':v.endsWith('Z')?v:v+'Z').toLocaleDateString('es-PE'):'Sin fecha estimada';}
 financiero(p:IaProducto){return this.esFechaPersonalizada?p.periodoPersonalizado:this.periodoFinanciero==='mes'?p.mesSiguienteFinanciero:p.horizontes?.find(h=>h.horizonte===this.horizonte);}
 get tituloFinanciero(){return this.esFechaPersonalizada?this.tituloHorizonte:this.periodoFinanciero==='mes'?'Próximo mes calendario':this.horizonte+' días';}
 get graficoFinanciero(){const key=this.esFechaPersonalizada?-1:this.periodoFinanciero==='mes'?0:this.horizonte;if(this.financieroFuente!==this.respuesta()||this.financieroHorizonte!==key){this.financieroFuente=this.respuesta();this.financieroHorizonte=key;this.financieroCache=this.productos.flatMap(p=>{const h=this.financiero(p);return [...(h?.ingresoNeto!=null?[{Nombre:p.nombre,Serie:'Ingresos estimados',Importe:h.ingresoNeto}]:[]),...(h?.margenBruto!=null?[{Nombre:p.nombre,Serie:'Margen bruto estimado',Importe:h.margenBruto}]:[])];});}return this.financieroCache;}
 get graficoMensual(){if(this.mensualFuente!==this.respuesta()){this.mensualFuente=this.respuesta();const historic=new Map<string,number>(),estimated=new Map<string,number>();for(const p of this.productos){if(!p.estimadoMensual?.some(m=>m.ingresoNeto!=null))continue;for(const m of p.historicoMensual??[])historic.set(m.mes,(historic.get(m.mes)??0)+m.ingresoNeto);for(const m of p.estimadoMensual??[])if(m.ingresoNeto!=null)estimated.set(m.mes,(estimated.get(m.mes)??0)+m.ingresoNeto);}const rows=(m:Map<string,number>)=>[...m].sort(([a],[b])=>a.localeCompare(b)).map(([month,quantity])=>({date:month+'-01',quantity}));this.mensualCache={historico:rows(historic),estimado:rows(estimated)};}return this.mensualCache;}
 private demandaGraficos:Record<number,{Nombre:string;Cantidad:number;Unidad:string}[]>={};
 private ingresoGrafico:{Nombre:string;Importe:number}[]=[];
 private prepararResumen(){
  if(!this.unidadesDemanda.includes(this.unidadDemanda))this.unidadDemanda=this.unidadesDemanda[0]??'UND';
  this.demandaGraficos={};
  for(const h of [30,60,90])this.demandaGraficos[h]=this.resumenVentas().filter(p=>p.suficiente&&p.cantidades?.[String(h)]!=null).map(p=>({Nombre:p.nombre,Cantidad:cantidadVisible(p.cantidades![String(h)]!,p.unidad)!,Unidad:p.unidad})).sort((a,b)=>b.Cantidad-a.Cantidad);
  this.demandaGraficos[0]=this.resumenVentas().filter(p=>p.suficiente&&p.periodoPersonalizado?.cantidad!=null).map(p=>({Nombre:p.nombre,Cantidad:cantidadVisible(p.periodoPersonalizado!.cantidad!,p.unidad)!,Unidad:p.unidad})).sort((a,b)=>b.Cantidad-a.Cantidad);
  this.ingresoGrafico=this.resumenFinanzas().filter(p=>this.financieroResumen(p)?.ingresoNeto!=null).map(p=>({Nombre:p.nombre,Importe:this.financieroResumen(p)!.ingresoNeto!})).sort((a,b)=>b.Importe-a.Importe).slice(0,5);
 }

 get unidadesDemanda(){return [...new Set(this.resumenVentas().map(p=>p.unidad))];}
 unidadDemanda='UND';
 private demandaCache:Record<string,{Nombre:string;Cantidad:number}[]>={};private demandaOrigen:unknown=null;
 get graficoDemanda(){if(this.demandaOrigen!==this.demandaGraficos){this.demandaOrigen=this.demandaGraficos;this.demandaCache={};}const indice=this.esFechaPersonalizada?0:this.horizonte;const key=indice+':'+this.unidadDemanda;return this.demandaCache[key]??(this.demandaCache[key]=(this.demandaGraficos[indice]??[]).filter(p=>p.Unidad===this.unidadDemanda).slice(0,5));}
 get graficoIngresoResumen(){return this.ingresoGrafico;}
 get ingresosMes(){return this.sumarConocidos(this.resumenFinanzas().map(p=>this.financieroResumen(p)?.ingresoNeto));}
 sumarConocidos(values:(number|null|undefined)[]){const known=values.filter((v):v is number=>v!=null);return known.length?known.reduce((a,b)=>a+b,0):null;}
 get demandaAgrupada(){return this.unidadesDemanda.map(unidad=>({unidad,cantidad:this.sumarConocidos(this.resumenVentas().filter(p=>p.unidad===unidad).map(p=>cantidadVisible(this.cantidadSeleccionada(p),p.unidad)))}));}
 get riesgosPrioritarios(){return this.resumenRiesgos().filter(p=>['Urgente','Alta','Revisar stock'].includes(p.riesgo?.prioridad??'')||(p.riesgo?.coberturaDias!=null&&p.riesgo.coberturaDias<=this.diasSeleccionados));}
 get reposiciones(){return this.resumenRiesgos().filter(p=>(p.riesgo?.cantidad??0)>0);}
 get margenRiesgos(){return this.resumenFinanzas().filter(p=>(this.financieroResumen(p)?.porcentajeMargen??100)<15);}
 financieroPronostico(p:IaProducto){return this.resumenFinanzas().find(x=>x.idProducto===p.idProducto);}
 tendencia(p:IaProducto){return p.variacion30==null?'Sin base comparable':(p.variacion30>0?'+':'')+this.numero(p.variacion30)+' % frente a los últimos 30 días';}
 get finanzasTotales(){return {ingreso:this.sumarConocidos(this.productos.map(p=>this.financiero(p)?.ingresoNeto)),costo:this.sumarConocidos(this.productos.map(p=>this.financiero(p)?.costoReferencia)),margen:this.sumarConocidos(this.productos.map(p=>this.financiero(p)?.margenBruto))};}
 analizar(p:IaProducto){this.producto=p.idProducto;this.cargar();}
 abrirRevision(r:IaRecomendacion){this.revision=r;this.motivo='';this.estadoRevision='Revisada';this.cantidad=Math.max(1,Math.ceil(r.Cantidad??1));this.proveedor=r.IdProveedor??0;this.obtenerProveedores();}
 obtenerProveedores(){if(!this.puede('IA_CONFIGURAR_ABASTECIMIENTO')&&!this.puede('IA_BORRADORES_CREAR'))return;this.acciones.add(this.service.proveedores().subscribe({next:p=>this.proveedores.set(p),error:e=>this.error.set(e.error?.mensaje??'No se pudieron consultar los proveedores.')}));}
 guardarRevision(){if(!this.revision||!this.motivo.trim()||this.ocupada())return;this.ocupada.set(true);this.acciones.add(this.service.revisar(this.revision.Id,this.estadoRevision,this.motivo,this.procedencia).subscribe({next:()=>{this.ocupada.set(false);this.revision=null;this.mensaje.set('Revisión registrada.');this.cargar();},error:e=>this.fallo(e)}));}
 prepararCompra(){if(!this.revision||!this.motivo.trim()||this.procedencia!=='operativo'||!this.puede('COMPRAS_CREAR')||this.ocupada())return;this.ocupada.set(true);this.acciones.add(this.service.solicitud(this.revision.Id,this.cantidad,this.proveedor,this.motivo).subscribe({next:r=>{this.ocupada.set(false);this.router.navigate(['/admin/compras'],{queryParams:{solicitudIA:r.id}});},error:e=>this.fallo(e)}));}
 abrirPolitica(p:IaProducto){this.peticionPlan?.unsubscribe();this.calculandoPlan.set(false);this.politicaProducto=p;this.vistaPrevia.set(null);this.errorPlan.set('');this.error.set('');this.obtenerProveedores();const today=this.sumarDias(this.hoyPeru(),90);this.politica=p.politica?{...p.politica}:{idProducto:p.idProducto,idProveedor:p.idProveedor??0,plazoDias:0,coberturaDias:30,seguridad:0,minimo:1,multiplo:1,reservado:0,entradaConfirmada:0,fechaEntrada:null,evidencia:'',verificadaHasta:today};}
 get erroresPolitica(){
  const p=this.politica;if(!p)return [];
  const errores:string[]=[];const entero=(n:number,min:number,max:number)=>Number.isInteger(n)&&n>=min&&n<=max;
  if(!entero(p.idProveedor,1,2147483647))errores.push('Selecciona el proveedor que abastece este producto.');
  if(!entero(p.plazoDias,1,90))errores.push('Indica cuántos días demora el proveedor en entregar: de 1 a 90.');
  if(!entero(p.coberturaDias,1,89)||p.plazoDias+p.coberturaDias>90)errores.push('El plazo de entrega más los días de stock debe ser como máximo 90 días.');
  if(!entero(p.minimo,1,1000000)||!entero(p.multiplo,1,1000000))errores.push('La compra mínima y el múltiplo deben ser enteros positivos.');
  if([p.seguridad,p.reservado,p.entradaConfirmada].some(v=>!Number.isFinite(v)||v<0||v>1000000))errores.push('Las reservas y entradas deben estar entre 0 y 1 000 000.');
  const hoy=this.hoyPeru(),max=this.sumarDias(hoy,90);
  if(p.entradaConfirmada>0&&(!p.fechaEntrada||p.fechaEntrada<=hoy||p.fechaEntrada>max))errores.push('Indica la fecha futura de llegada de las '+this.formatearCantidad(p.entradaConfirmada,'')+' unidades en camino, dentro de los próximos 90 días.');
  if(!p.verificadaHasta||p.verificadaHasta<hoy||p.verificadaHasta>max)errores.push('Actualiza la fecha de revisión de estas condiciones: entre hoy y los próximos 90 días.');
  if(p.evidencia.length>500)errores.push('La nota admite hasta 500 caracteres.');
  return errores;
 }
 get diasPlanificados(){return this.politica?this.politica.plazoDias+this.politica.coberturaDias:0;}
 guardarPolitica(){if(!this.politica||this.ocupada()||this.erroresPolitica.length)return;const policy={...this.politica,evidencia:this.politica.evidencia.trim()||'Condiciones de reposición registradas por el responsable en Sistema3S.'};this.ocupada.set(true);this.acciones.add(this.service.abastecimiento(policy).subscribe({next:()=>{this.politica=null;this.ocupada.set(false);this.mensaje.set('Condiciones de reposición guardadas.');this.cargar();},error:e=>this.fallo(e)}));}
 private fallo(e:any){this.error.set(e.error?.mensaje??'No se pudo guardar. Revisa los datos e intenta nuevamente.');this.ocupada.set(false);}
}
