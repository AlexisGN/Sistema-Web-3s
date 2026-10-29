import {Component,Input,ViewChild,ElementRef,OnChanges,AfterViewInit,OnDestroy,ChangeDetectionStrategy} from '@angular/core';
import {init,use,EChartsType,EChartsCoreOption} from 'echarts/core';
import {LineChart} from 'echarts/charts';
import {GridComponent,LegendComponent,TooltipComponent,AriaComponent,DataZoomComponent} from 'echarts/components';
import {SVGRenderer} from 'echarts/renderers';
import {unidadEntera} from './ia-cantidades';
import {IaDia} from './ia.service';
use([LineChart,GridComponent,LegendComponent,TooltipComponent,AriaComponent,DataZoomComponent,SVGRenderer]);
@Component({selector:'s3s-ia-grafico',standalone:true,changeDetection:ChangeDetectionStrategy.OnPush,
 template:`<div #area role="img" [attr.aria-label]="titulo+'; datos disponibles en la tabla'" style="height:300px;width:100%"></div>`,styles:[`:host{display:block;min-width:0}`]})
export class IaGraficoComponent implements AfterViewInit,OnChanges,OnDestroy {
 @Input() historico:IaDia[]=[];@Input() estimado:IaDia[]=[];@Input() unidad='';@Input() titulo='Ventas observadas y estimadas';
 @Input() horizonte=30;
 @ViewChild('area')area!:ElementRef<HTMLDivElement>;private chart?:EChartsType;private observer?:ResizeObserver;
 opciones():EChartsCoreOption {
  const estimated=this.estimado.slice(0,this.horizonte);
  const dates=[...new Set([...this.historico,...estimated].map(x=>x.date))].sort();
  const history=new Map(this.historico.map(x=>[x.date,x.quantity]));const future=new Map(estimated.map(x=>[x.date,x.quantity]));
  return {color:['#334B68','#CF1827'],aria:{enabled:true},animation:false,tooltip:{trigger:'axis',renderMode:'richText',valueFormatter:(v:unknown)=>Number(v).toLocaleString('es-PE',{maximumFractionDigits:unidadEntera(this.unidad)?0:2})},legend:{bottom:0},
   grid:{left:15,right:20,top:30,bottom:65,containLabel:true},xAxis:{type:'category',data:dates,axisLabel:{formatter:(x:string)=>x.slice(0,10).split('-').reverse().join('/')}},
   yAxis:{type:'value',name:this.unidad,minInterval:unidadEntera(this.unidad)?1:undefined},dataZoom:[{type:'inside'},{type:'slider',bottom:25,height:14}],
   series:[{name:'Ventas observadas',type:'line',showSymbol:false,data:dates.map(d=>history.get(d)??null)},
    {name:'Ventas estimadas',type:'line',showSymbol:false,lineStyle:{type:'dashed'},data:dates.map(d=>future.get(d)??null)}]};
 }
 ngAfterViewInit(){this.chart=init(this.area.nativeElement,undefined,{renderer:'svg'});this.observer=new ResizeObserver(()=>this.chart?.resize());this.observer.observe(this.area.nativeElement);this.pintar();}
 ngOnChanges(){this.pintar();}private pintar(){this.chart?.setOption(this.opciones(),{notMerge:true});}
 ngOnDestroy(){this.observer?.disconnect();this.chart?.dispose();}
}
