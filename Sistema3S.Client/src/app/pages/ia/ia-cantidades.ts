import {IaDia} from './ia.service';

export function unidadEntera(unidad:string):boolean {
 return ['UND','UN','U','UNIDAD','UNIDADES','PZA','PZAS','PIEZA','PIEZAS'].includes(unidad.trim().toUpperCase().replace(/\./g,''));
}
export function cantidadVisible(valor:number|null|undefined,unidad:string):number|null {
 return valor==null||!Number.isFinite(valor)?null:unidadEntera(unidad)?Math.round(valor):valor;
}
export function serieUnidades(dias:readonly IaDia[],unidad:string):IaDia[] {
 if(!unidadEntera(unidad))return dias.map(d=>({...d}));
 let acumulado=0,anterior=0;
 return dias.map(d=>{acumulado+=Math.max(0,d.quantity);const total=Math.round(acumulado+Number.EPSILON*100);const quantity=total-anterior;anterior=total;return {...d,quantity};});
}
