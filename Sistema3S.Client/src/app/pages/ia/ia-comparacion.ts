import {AfterViewInit,ChangeDetectionStrategy,Component,ElementRef,EventEmitter,Input,OnChanges,OnDestroy,Output,ViewChild} from '@angular/core';
import {init,use,EChartsCoreOption,EChartsType} from 'echarts/core';
import {BarChart} from 'echarts/charts';
import {AriaComponent,GridComponent,LegendComponent,TooltipComponent} from 'echarts/components';
import {SVGRenderer} from 'echarts/renderers';
import {unidadEntera} from './ia-cantidades';
use([BarChart,AriaComponent,GridComponent,LegendComponent,TooltipComponent,SVGRenderer]);
export interface IaComparacionFila{id:number;nombre:string;actual:number;previsto:number;}
@Component({selector:'s3s-ia-comparacion',standalone:true,changeDetection:ChangeDetectionStrategy.OnPush,
 template:`<div class="desplazamiento"><div #area role="img" [attr.aria-label]="'Stock actual y ventas previstas en '+unidad+'. Valores disponibles debajo del gráfico.'" [style.height.px]="alto"></div></div>`,
 styles:[`:host{display:block;min-width:0}.desplazamiento{overflow:auto;max-height:560px}.desplazamiento>div{min-width:420px;width:100%}`]})
export class IaComparacionComponent implements AfterViewInit,OnChanges,OnDestroy{
 @Input() filas:IaComparacionFila[]=[];@Input() unidad='UND';@Output() seleccionar=new EventEmitter<number>();
 @ViewChild('area')area!:ElementRef<HTMLDivElement>;private chart?:EChartsType;private observer?:ResizeObserver;
 get alto(){return Math.max(230,this.filas.length*54+70);}
 opciones():EChartsCoreOption{return {animation:false,color:['#334b68','#cf1827'],aria:{enabled:true,decal:{show:true}},
  tooltip:{trigger:'axis',renderMode:'richText',axisPointer:{type:'shadow'},valueFormatter:(v:unknown)=>Number(v).toLocaleString('es-PE',{maximumFractionDigits:unidadEntera(this.unidad)?0:2})+' '+this.unidad},
  legend:{top:0,left:0},grid:{left:8,right:45,top:48,bottom:25,containLabel:true},
  xAxis:{type:'value',min:0,minInterval:unidadEntera(this.unidad)?1:undefined,splitLine:{lineStyle:{color:'#e9eef5'}}},
  yAxis:{type:'category',inverse:true,data:this.filas.map(f=>f.nombre),axisTick:{show:false},axisLine:{show:false},axisLabel:{width:185,overflow:'truncate',fontSize:12,color:'#334b68'}},
  series:[{name:'Stock actual',type:'bar',barMaxWidth:13,itemStyle:{borderRadius:[0,4,4,0]},data:this.filas.map(f=>f.actual)},
   {name:'Ventas previstas',type:'bar',barMaxWidth:13,itemStyle:{borderRadius:[0,4,4,0]},data:this.filas.map(f=>f.previsto)}]};}
 ngAfterViewInit(){this.chart=init(this.area.nativeElement,undefined,{renderer:'svg'});this.chart.on('click',p=>{if(p.dataIndex!=null&&this.filas[p.dataIndex])this.seleccionar.emit(this.filas[p.dataIndex].id);});this.observer=new ResizeObserver(()=>this.chart?.resize());this.observer.observe(this.area.nativeElement);this.pintar();}
 ngOnChanges(){this.pintar();}private pintar(){this.chart?.resize({height:this.alto});this.chart?.setOption(this.opciones(),{notMerge:true});}
 ngOnDestroy(){this.observer?.disconnect();this.chart?.dispose();}
}
