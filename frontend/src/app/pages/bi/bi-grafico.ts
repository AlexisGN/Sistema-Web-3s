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
  @Input() huecos=false;
  @Input() monetario=false;
  @Input() cantidadesEnteras=false;
  @Input() unidad='';
  @Input() estiloComercial=false;
  @Output() seleccionar=new EventEmitter<BiFila>();
  @ViewChild('grafico') contenedor!:ElementRef<HTMLDivElement>;
  private chart?:EChartsType; private observer?:ResizeObserver;
  get alto():number {return this.tipo==='barras'?Math.max(270,Math.min(440,new Set(this.filas.map(f=>String(f[this.etiqueta]))).size*30+68)):270;}
  ngAfterViewInit(){
    this.chart=init(this.contenedor.nativeElement,undefined,{renderer:'svg'});
    this.chart.on('click',p=>this.seleccionarPunto(p.name,p.seriesName));
    this.observer=new ResizeObserver(()=>this.chart?.resize());this.observer.observe(this.contenedor.nativeElement);this.pintar();
  }
  seleccionarPunto(nombre:string,serie?:string){
    const etiqueta=(f:BiFila)=>String(f[this.etiqueta]??'')+(this.valor==='Cantidad'&&this.etiqueta==='Nombre'?` (${f['Unidad']??'sin unidad'})`:'');
    const grupo=(f:BiFila)=>{const g=String(f[this.grupo]??this.titulo);return ({venta:'Ventas',compra:'Compras',cotizacion:'Cotizaciones'} as Record<string,string>)[g]??g;};
    const candidatas=this.filas.filter(f=>etiqueta(f)===nombre);
    const fila=(serie?candidatas.find(f=>grupo(f)===serie):undefined)??candidatas[0];
    if(fila)this.seleccionar.emit(fila);
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
    const esMoneda=this.monetario||this.valor==='Importe';
    const unidad=this.unidad||(esMoneda?'S/':this.valor==='Cantidad'?'Cantidad por unidad':this.valor==='Promedio'?'Saldo del SKU':'Registros');
    const reduced=typeof matchMedia==='function'&&matchMedia('(prefers-reduced-motion: reduce)').matches;
    const option:EChartsCoreOption={color:this.estiloComercial?['#277c9b','#a6bed0','#c28b3c','#388873','#6878a7']:['#CF1827','#334B68','#16836A','#946820','#6552A8'],animation:!reduced,
      aria:{enabled:true,label:{description:`${this.titulo}. Unidad: ${unidad}. Abre ${this.estiloComercial?'Ver detalle':'Ver datos'} para consultar la alternativa tabular.`},decal:{show:!this.estiloComercial}},tooltip:{trigger:this.tipo==='torta'?'item':'axis',renderMode:'richText',axisPointer:{label:{formatter:(p:{value:string|number})=>this.etiqueta==='Periodo'?fecha(String(p.value)):String(p.value)}},valueFormatter:(v:unknown)=>(esMoneda?'S/ ':'')+Number(v).toLocaleString('es-PE',{maximumFractionDigits:this.cantidadesEnteras?0:2})},
      legend:{show:!this.estiloComercial||groups.length>1,bottom:0,type:'scroll'},grid:{left:12,right:this.estiloComercial?36:18,top:20,bottom:48,containLabel:true},
      xAxis:{minInterval:this.cantidadesEnteras?1:undefined,type:this.tipo==='barras'?'value':'category',name:this.tipo==='barras'?unidad:'',nameLocation:this.estiloComercial?'middle':'end',nameGap:this.estiloComercial?27:15,data:labels,axisLabel:{hideOverlap:true,color:'#54647a',formatter:(value:string)=>this.etiqueta==='Fecha'?fecha(value).slice(0,5):this.etiqueta==='Periodo'?fecha(value):value}},
      yAxis:{minInterval:this.cantidadesEnteras?1:undefined,type:this.tipo==='barras'?'category':'value',name:this.tipo==='barras'?'':unidad,data:labels,inverse:this.tipo==='barras',axisLabel:{hideOverlap:false,color:'#54647a',fontSize:10,formatter:this.tipo==='barras'?largo:undefined,width:this.tipo==='barras'?165:undefined}},
      series:groups.map(g=>({name:({venta:'Ventas',compra:'Compras',cotizacion:'Cotizaciones'} as Record<string,string>)[g]??g,type:this.tipo==='linea'?'line':'bar',stack:this.tipo==='apiladas'?'total':undefined,
        showSymbol:this.estiloComercial||labels.length<40,symbolSize:this.estiloComercial?4:undefined,barMaxWidth:32,
        label:{show:this.mostrarValores,position:'top',fontSize:11,color:'#34465f',formatter:(p:{value:unknown})=>(esMoneda?'S/ ':'')+Number(p.value).toLocaleString('es-PE',{maximumFractionDigits:this.cantidadesEnteras?0:2})},
        data:labels.map(label=>totals.get(g)?.get(label)??(this.huecos?null:0))}))};
    if(this.tipo==='torta'){delete option['xAxis'];delete option['yAxis'];option['series']=[{type:'pie',radius:['42%','66%'],center:['50%','44%'],avoidLabelOverlap:true,label:{show:false},data:this.filas.map(f=>({name:String(f[this.etiqueta]),value:Number(f[this.valor]??0)}))}];}
    if(labels.length>40&&this.tipo!=='torta')option['dataZoom']=[{type:'inside'},{type:'slider',bottom:28,height:14}];
    return option;
  }
  // Actualizar modelo y SVG juntos evita eventos del puntero sobre series anteriores.
  private pintar(){this.chart?.setOption(this.opciones(),{notMerge:true,lazyUpdate:false});}
  ngOnDestroy(){this.observer?.disconnect();this.chart?.dispose();}
}
