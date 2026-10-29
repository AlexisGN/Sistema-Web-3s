import {AfterViewInit,ChangeDetectionStrategy,Component,ElementRef,Input,OnChanges,OnDestroy,ViewChild} from '@angular/core';
import {init,use,EChartsCoreOption,EChartsType} from 'echarts/core';
import {BarChart} from 'echarts/charts';
import {AriaComponent,GridComponent,TooltipComponent} from 'echarts/components';
import {SVGRenderer} from 'echarts/renderers';
use([BarChart,AriaComponent,GridComponent,TooltipComponent,SVGRenderer]);
@Component({selector:'s3s-ia-ranking',standalone:true,changeDetection:ChangeDetectionStrategy.OnPush,
 template:`<div #area role="img" [attr.aria-label]="titulo+'. Datos disponibles debajo del gráfico.'" [style.height.px]="alto"></div><details><summary>Ver valores</summary><table><thead><tr><th>Producto</th><th>{{dinero?'Importe sin IGV':'Cantidad ('+unidad+')'}}</th></tr></thead><tbody>@for(f of filas;track f['Nombre']){<tr><td>{{f['Nombre']}}</td><td>{{formato(f[valor])}}</td></tr>}</tbody></table></details>`,
 styles:[`:host{display:block;min-width:0}summary{font-size:12px;cursor:pointer;color:#405b79;margin-top:6px}table{width:100%;font-size:12px;border-collapse:collapse}th,td{text-align:left;padding:10px;border-bottom:1px solid #e5ebf3}td:last-child,th:last-child{text-align:right;white-space:nowrap}`]})
export class IaRankingComponent implements AfterViewInit,OnChanges,OnDestroy{
 @Input() filas:Record<string,unknown>[]=[];@Input() valor='Importe';@Input() unidad='';@Input() titulo='';@Input() enteros=false;
 @ViewChild('area')area!:ElementRef<HTMLDivElement>;private chart?:EChartsType;private observer?:ResizeObserver;
 get dinero(){return this.valor==='Importe';}get alto(){return Math.max(220,this.filas.length*48+55);}
 formato(v:unknown){return (this.dinero?'S/ ':'')+Number(v).toLocaleString('es-PE',{minimumFractionDigits:this.dinero?2:0,maximumFractionDigits:this.enteros?0:2})+(this.dinero?'':' '+this.unidad);}
 opciones():EChartsCoreOption{return {animation:false,aria:{enabled:true},tooltip:{trigger:'axis',renderMode:'richText',valueFormatter:(v:unknown)=>this.formato(v)},
  grid:{left:3,right:this.dinero?116:80,top:15,bottom:30,containLabel:true},
  xAxis:{type:'value',min:0,minInterval:this.enteros?1:undefined,axisLabel:{color:'#63758a',fontSize:10,hideOverlap:true,formatter:(v:number)=>new Intl.NumberFormat('es-PE',{notation:'compact',maximumFractionDigits:1}).format(v)},splitLine:{lineStyle:{color:'#edf1f6'}}},
  yAxis:{type:'category',inverse:true,data:this.filas.map(f=>String(f['Nombre'])),axisTick:{show:false},axisLine:{show:false},axisLabel:{width:135,overflow:'break',color:'#334b68',fontSize:11}},
  series:[{type:'bar',barMaxWidth:21,itemStyle:{color:this.dinero?'#3b638b':'#16836a',borderRadius:[0,5,5,0]},label:{show:true,position:'right',distance:8,color:'#223c58',fontSize:11,formatter:(p:{value:unknown})=>this.formato(p.value)},data:this.filas.map(f=>Number(f[this.valor]))}]};}
 ngAfterViewInit(){this.chart=init(this.area.nativeElement,undefined,{renderer:'svg'});this.observer=new ResizeObserver(()=>this.chart?.resize());this.observer.observe(this.area.nativeElement);this.pintar();}
 ngOnChanges(){this.pintar();}private pintar(){this.chart?.resize({height:this.alto});this.chart?.setOption(this.opciones(),{notMerge:true});}
 ngOnDestroy(){this.observer?.disconnect();this.chart?.dispose();}
}
