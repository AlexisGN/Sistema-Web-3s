import {AfterViewInit,ChangeDetectionStrategy,Component,ElementRef,EventEmitter,Input,OnChanges,OnDestroy,Output,ViewChild} from '@angular/core';
import {EChartsCoreOption,EChartsType,init,use} from 'echarts/core';
import {BarChart,LineChart,PieChart} from 'echarts/charts';
import {AriaComponent,DataZoomComponent,GridComponent,LegendComponent,MarkAreaComponent,MarkLineComponent,TooltipComponent} from 'echarts/components';
import {SVGRenderer} from 'echarts/renderers';
import {BiFila} from '../bi/bi.model';

use([BarChart,LineChart,PieChart,AriaComponent,DataZoomComponent,GridComponent,LegendComponent,MarkAreaComponent,MarkLineComponent,TooltipComponent,SVGRenderer]);
export type SemanticaComercial='normal'|'evolucion'|'comparacion'|'estado-stock'|'stock'|'conversion'|'recurrencia'|'prevision'|'cobertura'|'saldo'|'participacion'|'variacion';
export const COLORES_COMERCIALES={azul:'#2563eb',verde:'#15803d',rojo:'#dc3545',ambar:'#d97706',violeta:'#7c3aed',turquesa:'#0891b2',base:'#94a3b8',gris:'#64748b'};
const paleta=['#2563eb','#0891b2','#7c3aed','#db2777','#d97706','#0d9488','#4f46e5','#a16207'];

@Component({selector:'s3s-comercial-grafico',standalone:true,changeDetection:ChangeDetectionStrategy.OnPush,
 template:`<div #lienzo class="lienzo" role="img" [attr.aria-label]="titulo+' · '+unidad+'. Usa Ver detalle para consultar los datos.'" [style.height.px]="alto"></div>`,
 styles:[`:host{display:block;min-width:0}.lienzo{width:100%;min-width:0}`]})
export class ComercialGraficoComponent implements AfterViewInit,OnChanges,OnDestroy{
 @Input() filas:BiFila[]=[];@Input() tipo:'linea'|'barras'|'columnas'|'apiladas'|'torta'='linea';
 @Input() titulo='';@Input() etiqueta='Nombre';@Input() valor='Importe';@Input() grupo='Serie';@Input() unidad='';
 @Input() monetario=false;@Input() enteros=false;@Input() huecos=false;@Input() semantica:SemanticaComercial='normal';@Input() referencia:number|null=null;
 @Output() seleccionar=new EventEmitter<BiFila>();
 @ViewChild('lienzo') lienzo!:ElementRef<HTMLDivElement>;private chart?:EChartsType;private observer?:ResizeObserver;
 get alto(){return this.semantica==='cobertura'?220:this.tipo==='barras'?Math.max(300,Math.min(460,new Set(this.filas.map(f=>f[this.etiqueta])).size*43+70)):this.tipo==='torta'?300:330;}
 formato(v:unknown,compacto=false){const n=Number(Array.isArray(v)?v[v.length-1]:v);if(!Number.isFinite(n))return 'Sin dato';return (this.monetario&&!compacto?'S/ ':'')+n.toLocaleString('es-PE',{maximumFractionDigits:this.enteros?0:compacto?1:2,...(compacto?{notation:'compact' as const}:{})})+(this.unidad==='%'?' %':'');}
 color(f:BiFila,g='',i=0):string{
  const c=COLORES_COMERCIALES,n=String(f[this.etiqueta]??''),tono=String(f['Tono']??'');
  if(this.semantica==='evolucion'||this.semantica==='comparacion')return ({sube:c.verde,baja:c.rojo,parcial:c.ambar,base:c.base,igual:c.azul} as Record<string,string>)[tono]??(g.includes('B')?c.base:c.azul);
  if(this.semantica==='estado-stock')return n==='Sin stock'?c.rojo:n==='Bajo mínimo con stock'?c.ambar:n==='Sobre el mínimo'?c.verde:c.gris;
  if(this.semantica==='stock')return g==='Mínimo registrado'?c.base:({critico:c.ambar,agotado:c.rojo,disponible:c.verde,desconocido:c.gris} as Record<string,string>)[tono]??c.azul;
  if(this.semantica==='conversion')return n==='Terminaron en venta'?c.verde:c.ambar;
  if(this.semantica==='recurrencia')return n==='Con compra anterior'?c.violeta:c.turquesa;
  if(this.semantica==='prevision')return g.includes('previst')||g.includes('estimad')?c.violeta:c.azul;
  if(this.semantica==='cobertura')return n==='Stock actual'?c.turquesa:c.violeta;
  if(this.semantica==='saldo'){const v=Number(f[this.valor]);return v<=0?c.rojo:v<=(this.referencia??0)?c.ambar:c.verde;}
  if(this.semantica==='variacion')return Number(f[this.valor])>0?c.verde:Number(f[this.valor])<0?c.rojo:c.base;
  if(this.semantica==='participacion'&&(g==='B'||g==='Período B'))return c.base;
  if(this.semantica==='participacion'&&(g==='A'||g==='Período A'))return c.azul;
  if(this.tipo==='torta'||this.semantica==='participacion'||(!g.includes('Período')&&this.tipo==='barras')){const hash=[...n].reduce((a,x)=>a+x.charCodeAt(0),0);return paleta[hash%paleta.length];}
  return paleta[i%paleta.length];
 }
 seleccionarDato(data:unknown){if(data&&typeof data==='object'&&'original' in data&&data.original)this.seleccionar.emit(data.original as BiFila);}
 opciones():EChartsCoreOption{
  const label=(f:BiFila)=>String(f[this.etiqueta]??'');
  const labels=[...new Set(this.filas.map(label))];
  const groups=[...new Set(this.filas.map(f=>String(f[this.grupo]??this.titulo)))];
  const small=(this.lienzo?.nativeElement.clientWidth??800)<480;
  const shorten=(s:string)=>/^\d{4}-\d{2}-\d{2}$/.test(s)?s.slice(8,10)+'/'+s.slice(5,7):/ de \d{4}$/.test(s)?s.slice(0,3)+' '+s.slice(-4):s;
  const option:EChartsCoreOption={animation:typeof matchMedia==='function'?!matchMedia('(prefers-reduced-motion: reduce)').matches:true,
   color:paleta,aria:{enabled:true,decal:{show:false},label:{description:this.titulo+'. '+this.filas.slice(0,40).map(f=>String(f[this.etiqueta]??'')+' · '+String(f[this.grupo]??'')+': '+(f[this.valor]==null?'Sin dato':this.formato(f[this.valor]))).join('; ')+(this.filas.length>40?'. Consulta todos los valores en Ver detalle.':'')}},
   tooltip:{trigger:this.tipo==='torta'?'item':'axis',renderMode:'richText',confine:true,
    valueFormatter:(v:unknown)=>this.formato(v),axisPointer:{type:this.tipo==='linea'?'line':'shadow'}},
   legend:{show:groups.length>1&&this.tipo!=='torta',bottom:0,type:'scroll',textStyle:{fontSize:11,color:'#475569'}},
   grid:{left:6,right:20,top:30,bottom:groups.length>1?56:38,containLabel:true},
   xAxis:{type:this.tipo==='barras'?'value':'category',data:labels,name:this.tipo==='barras'?this.unidad:'',nameGap:24,nameLocation:'middle',
    minInterval:this.enteros&&this.tipo==='barras'?1:undefined,axisTick:{show:false},axisLine:{lineStyle:{color:'#cbd5e1'}},
    axisLabel:{color:'#52657a',fontSize:11,hideOverlap:true,formatter:this.tipo==='barras'?(v:unknown)=>this.formato(v,true):shorten}},
   yAxis:{type:this.tipo==='barras'?'category':'value',data:labels,inverse:this.tipo==='barras',name:this.tipo==='barras'?'':this.unidad,
    minInterval:this.enteros&&this.tipo!=='barras'?1:undefined,axisLine:{show:false},axisTick:{show:false},splitLine:{lineStyle:{color:'#e5ebf2',type:'dashed'}},
    axisLabel:{color:'#334155',fontSize:11,width:this.tipo==='barras'?(small?108:180):undefined,overflow:'break',lineHeight:15,
     formatter:this.tipo==='barras'?undefined:(v:unknown)=>this.formato(v,true)}},
   series:groups.map((g,i)=>{
    const rows=this.filas.filter(f=>String(f[this.grupo]??this.titulo)===g);
    const future=this.semantica==='prevision'&&(g.includes('previst')||g.includes('estimad'));
    const color=this.color(rows[0]??{},g,i);
    return {name:g,type:this.tipo==='linea'?'line':'bar',stack:this.tipo==='apiladas'?'total':undefined,barMaxWidth:34,
     label:this.semantica==='stock'&&g==='Stock actual'?{show:true,position:'right',fontSize:11,color:'#334155'}:undefined,
     smooth:false,showSymbol:labels.length<36,symbolSize:7,connectNulls:false,
     lineStyle:{width:3,type:future?'dashed':'solid',color},itemStyle:{color,borderRadius:this.tipo==='barras'?[0,5,5,0]:[5,5,0,0]},
     areaStyle:future?{color:'#7c3aed',opacity:.07}:undefined,
     data:labels.map(name=>{const matches=rows.filter(f=>label(f)===name);const f=matches[0];const n=matches.filter(f=>f[this.valor]!=null&&Number.isFinite(Number(f[this.valor]))).reduce((a,f)=>a+Number(f[this.valor]),0);
      return {value:f&&f[this.valor]!=null?n:this.huecos?null:0,itemStyle:{color:f?this.color(f,g,i):color},original:f};})};
   })};
  if(this.semantica==='evolucion'&&groups.length===1){
   const s=(option['series'] as any[])[0];s.type='bar';s.barMaxWidth=48;
   (option['series'] as any[]).push({...s,name:'Evolución',type:'line',lineStyle:{color:'#2563eb',width:2},itemStyle:{color:'#2563eb'},symbolSize:5,z:4,tooltip:{show:false},data:s.data.map((d:any)=>({...d,itemStyle:{color:'#2563eb'}}))});
  }
  if(this.tipo==='torta'){
   delete option['xAxis'];delete option['yAxis'];
   option['legend']={show:true,bottom:0,left:'center',orient:'vertical',type:'plain',itemGap:7,textStyle:{fontSize:11,color:'#475569',width:small?220:240,overflow:'break',lineHeight:14}};
   option['series']=[{name:this.titulo,type:'pie',radius:['47%','70%'],center:['50%','42%'],avoidLabelOverlap:true,padAngle:2,
    label:{show:true,position:'inside',color:'#fff',fontWeight:700,formatter:(p:{percent:number})=>p.percent>=8?Math.round(p.percent)+' %':''},
    data:this.filas.map((f,i)=>({name:label(f),value:Number(f[this.valor]??0),itemStyle:{color:this.color(f,'',i)},original:f}))}];
  }
  if(this.semantica==='saldo'){
   const s=(option['series'] as any[])[0];s.type='line';s.showSymbol=true;s.symbolSize=6;s.lineStyle={color:'#334155',width:2};
   s.markLine={symbol:'none',silent:true,label:{formatter:'Stock cero',position:'insideEndTop'},lineStyle:{color:COLORES_COMERCIALES.rojo,type:'dashed'},data:[{yAxis:0}]};
   const min=Math.max(0,this.referencia??0);const values=this.filas.map(f=>Number(f[this.valor]));const low=Math.min(0,...values),high=Math.max(min,...values);
   const zones:any[]=[];if(low<0)zones.push([{yAxis:low,itemStyle:{color:'rgba(220,53,69,.07)'}},{yAxis:0}]);if(min>0)zones.push([{yAxis:0,itemStyle:{color:'rgba(217,119,6,.07)'}},{yAxis:min}]);if(high>min)zones.push([{yAxis:min,itemStyle:{color:'rgba(21,128,61,.05)'}},{yAxis:high}]);
   s.markArea={silent:true,data:zones};
  }
  if(labels.length>45&&this.tipo!=='torta'){
   const firstFuture=this.filas.find(f=>String(f[this.grupo]).includes('previst'))?.[this.etiqueta];
   const start=firstFuture?Math.max(0,labels.indexOf(String(firstFuture))-30):0;
   option['dataZoom']=[{type:'inside',startValue:start,endValue:labels.length-1},{type:'slider',bottom:25,height:14,startValue:start,endValue:labels.length-1}];
   (option['grid'] as Record<string,unknown>)['bottom']=76;
  }
  return option;
 }
 ngAfterViewInit(){this.chart=init(this.lienzo.nativeElement,undefined,{renderer:'svg'});this.chart.on('click',p=>this.seleccionarDato(p.data));this.observer=new ResizeObserver(()=>{this.chart?.resize();this.pintar();});this.observer.observe(this.lienzo.nativeElement);this.pintar();}
 ngOnChanges(){this.pintar();}private pintar(){this.chart?.setOption(this.opciones(),{notMerge:true,lazyUpdate:false});}
 ngOnDestroy(){this.observer?.disconnect();this.chart?.dispose();}
}
