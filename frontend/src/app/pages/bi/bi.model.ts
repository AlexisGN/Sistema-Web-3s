export type BiVista='resumen'|'ventas'|'productos'|'clientes'|'compras'|'inventario';
export type BiFila=Record<string,string|number|null>;
export type BiComparacion='ninguna'|'anterior'|'anioAnterior'|'personalizado';
export interface BiFiltro {
  desde:string; hasta:string; compararDesde?:string; compararHasta?:string;
  modoComparacion?:BiComparacion;
  agrupacion:'dia'|'semana'|'mes'|'anio'; procedencia:'operativo'|'referencia';
  categoria?:number; elemento?:number; compararElemento?:number; cliente?:number; proveedor?:number; estado?:string;
  pagina:number; tamanio:number;
}
export interface BiResultado {
  vista:BiVista; filtros:BiFiltro; compararDesde:string|null; compararHasta:string|null;
  generadoUtc:string; moneda:string; zonaHoraria:string;
  datos:Record<string,BiFila[]>; notas:string[];
  indicadores:BiIndicador[]; hallazgos:BiHallazgo[];
}
export interface BiIndicador {etiqueta:string;valor:number|null;formato:string;descripcion:string;}
export interface BiHallazgo {titulo:string;evidencia:string;interpretacion:string;accionSugerida:string;impactoEsperado:string;}
function dia(valor:string):number|null {
  if(!/^\d{4}-\d{2}-\d{2}$/.test(valor))return null;
  const time=Date.parse(valor+'T00:00:00Z');
  return Number.isFinite(time)&&new Date(time).toISOString().slice(0,10)===valor?time/86400000:null;
}
export function diasPeriodo(desde:string,hasta:string):number|null {const a=dia(desde),b=dia(hasta);return a!=null&&b!=null&&b>=a?b-a+1:null;}
export function periodoComparable(desde:string,hasta:string,modo:BiComparacion):{compararDesde:string;compararHasta:string}|null {
  const duracion=diasPeriodo(desde,hasta),inicio=dia(desde);if(!duracion||inicio==null||modo==='ninguna'||modo==='personalizado')return null;
  let primerDia=inicio-duracion;
  if(modo==='anioAnterior'){
    const [y,m,d]=desde.split('-').map(Number);const ultimoDia=new Date(Date.UTC(y-1,m,0)).getUTCDate();
    primerDia=Date.UTC(y-1,m-1,Math.min(d,ultimoDia))/86400000;
  }
  const texto=(v:number)=>new Date(v*86400000).toISOString().slice(0,10);
  return {compararDesde:texto(primerDia),compararHasta:texto(primerDia+duracion-1)};
}
export function fechaPeru(hoy=new Date()):string { return new Intl.DateTimeFormat('en-CA',{timeZone:'America/Lima',year:'numeric',month:'2-digit',day:'2-digit'}).format(hoy); }
export function rangoRapido(tipo:string,hoy=fechaPeru()):{desde:string;hasta:string} {
  const [y,m,d]=hoy.split('-').map(Number);
  const fecha=(a:number,b:number,c:number)=>new Date(Date.UTC(a,b,c)).toISOString().slice(0,10);
  if(tipo==='anterior')return {desde:fecha(y,m-2,1),hasta:fecha(y,m-1,0)};
  if(tipo==='tres')return {desde:fecha(y,m-3,1),hasta:hoy};
  if(tipo==='seis')return {desde:fecha(y,m-6,1),hasta:hoy};
  if(tipo==='anio')return {desde:fecha(y,0,1),hasta:hoy};
  return {desde:fecha(y,m-1,1),hasta:fecha(y,m-1,d)};
}
