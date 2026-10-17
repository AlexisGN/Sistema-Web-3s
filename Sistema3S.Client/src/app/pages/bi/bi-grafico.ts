import { AfterViewInit,ChangeDetectionStrategy,Component,ElementRef,Input,OnChanges,OnDestroy,Output,EventEmitter,ViewChild } from '@angular/core';
import { EChartsCoreOption,EChartsType,init,use } from 'echarts/core';
import { BarChart,LineChart,PieChart } from 'echarts/charts';
import { GridComponent,LegendComponent,TooltipComponent,AriaComponent,DataZoomComponent } from 'echarts/components';
import { SVGRenderer } from 'echarts/renderers';
import { BiFila } from './bi.model';
use([BarChart,LineChart,PieChart,GridComponent,LegendComponent,TooltipComponent,AriaComponent,DataZoomComponent,SVGRenderer]);

@Component({selector:'s3s-bi-grafico',standalone:true,changeDetection:ChangeDetectionStrategy.OnPush,
 template:`<div #grafico class="grafico" role="img" [attr.aria-label]="titulo" [style.height.px]="alto"></div>`,
 styles:[`:host{display:block;min-width:0}.grafico{width:100%;height:270px}`]})
export class BiGraficoComponent implements AfterViewInit,OnChanges,OnDestroy {
  @Input() filas:BiFila[]=[];
  @Input() tipo:'linea'|'barras'|'columnas'|'apiladas'|'torta'='linea';
  @Input() titulo=''; @Input() etiqueta='Nombre'; @Input() valor='Importe'; @Input() grupo='Tipo';
  @Input() mostrarValores=false;
  @Input() cantidadesEnteras=false;
  @Input() unidad='';
  @Output() seleccionar=new EventEmitter<BiFila>();
  @ViewChild('grafico') contenedor!:ElementRef<HTMLDivElement>;
  private chart?:EChartsType; private observer?:ResizeObserver;
  get alto():number {return this.tipo==='barras'?Math.max(270,Math.min(440,new Set(this.filas.map(f=>String(f[this.etiqueta]))).size*30+68)):270;}
  ngAfterViewInit(){
    this.chart=init(this.contenedor.nativeElement,undefined,{renderer:'svg'});
    this.chart.on('click',p=>{if(p.name){const fila=this.filas.find(f=>String(f[this.etiqueta])===p.name);if(fila)this.seleccionar.emit(fila);}});
    this.observer=new ResizeObserver(()=>this.chart?.resize());this.observer.observe(this.contenedor.nativeElement);this.pintar();
  }
  ngOnChanges(){this.pintar();}
  opciones():EChartsCoreOption {
    const etiqueta=(f:BiFila)=>String(f[this.etiqueta]??'')+(this.valor==='Cantidad'&&this.etiqueta==='Nombre'?` (${f['Unidad']??'sin unidad'})`:'');
    const labels=[...new Set(this.filas.map(etiqueta))];
    const groups=[...new Set(this.filas.map(f=>String(f[this.grupo]??this.titulo)))];
    const totals=new Map<string,Map<string,number>>();
    for(const f of this.filas){const g=String(f[this.grupo]??this.titulo),label=etiqueta(f);if(!totals.has(g))totals.set(g,new Map());const values=totals.get(g)!;values.set(label,(values.get(label)??0)+Number(f[this.valor]??0));}
    const fecha=(v:string)=>v.slice(0,10).split('-').reverse().join('/');
    const largo=(v:string)=>v.length>27?v.replace(/(.{1,27})(?:\s|$)/g,'$1\n').trim():v;
    const unidad=this.unidad||(this.valor==='Importe'?'S/':this.valor==='Cantidad'?'Cantidad por unidad':this.valor==='Promedio'?'Saldo del SKU':'Registros');
    const reduced=typeof matchMedia==='function'&&matchMedia('(prefers-reduced-motion: reduce)').matches;
    const option:EChartsCoreOption={color:['#CF1827','#334B68','#16836A','#946820','#6552A8'],animation:!reduced,
      aria:{enabled:true,label:{description:`${this.titulo}. Unidad: ${unidad}. Abre Ver datos para consultar la alternativa tabular.`},decal:{show:true}},tooltip:{trigger:this.tipo==='torta'?'item':'axis',renderMode:'richText',axisPointer:{label:{formatter:(p:{value:string|number})=>this.etiqueta==='Periodo'?fecha(String(p.value)):String(p.value)}},valueFormatter:(v:unknown)=>(this.valor==='Importe'?'S/ ':'')+Number(v).toLocaleString('es-PE',{maximumFractionDigits:this.cantidadesEnteras?0:2})},
      legend:{bottom:0,type:'scroll'},grid:{left:12,right:18,top:20,bottom:48,containLabel:true},
      xAxis:{minInterval:this.cantidadesEnteras?1:undefined,type:this.tipo==='barras'?'value':'category',name:this.tipo==='barras'?unidad:'',data:labels,axisLabel:{hideOverlap:true,color:'#54647a',formatter:(value:string)=>this.etiqueta==='Periodo'?fecha(value):value}},
      yAxis:{minInterval:this.cantidadesEnteras?1:undefined,type:this.tipo==='barras'?'category':'value',name:this.tipo==='barras'?'':unidad,data:labels,inverse:this.tipo==='barras',axisLabel:{hideOverlap:false,color:'#54647a',fontSize:10,formatter:this.tipo==='barras'?largo:undefined,width:this.tipo==='barras'?165:undefined}},
      series:groups.map(g=>({name:({venta:'Ventas',compra:'Compras',cotizacion:'Cotizaciones'} as Record<string,string>)[g]??g,type:this.tipo==='linea'?'line':'bar',stack:this.tipo==='apiladas'?'total':undefined,
        showSymbol:labels.length<40,barMaxWidth:32,
        label:{show:this.mostrarValores,position:'top',fontSize:11,color:'#34465f',formatter:(p:{value:unknown})=>(this.valor==='Importe'?'S/ ':'')+Number(p.value).toLocaleString('es-PE',{maximumFractionDigits:this.cantidadesEnteras?0:2})},
        data:labels.map(label=>totals.get(g)?.get(label)??0)}))};
    if(this.tipo==='torta'){delete option['xAxis'];delete option['yAxis'];option['series']=[{type:'pie',radius:['42%','66%'],center:['50%','44%'],avoidLabelOverlap:true,label:{show:false},data:this.filas.map(f=>({name:String(f[this.etiqueta]),value:Number(f[this.valor]??0)}))}];}
    if(labels.length>40&&this.tipo!=='torta')option['dataZoom']=[{type:'inside'},{type:'slider',bottom:28,height:14}];
    return option;
  }
  private pintar(){this.chart?.setOption(this.opciones(),{notMerge:true,lazyUpdate:true});}
  ngOnDestroy(){this.observer?.disconnect();this.chart?.dispose();}
}
