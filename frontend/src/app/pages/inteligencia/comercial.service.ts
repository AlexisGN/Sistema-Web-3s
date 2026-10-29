import {inject,Injectable} from '@angular/core';
import {HttpClient,HttpParams} from '@angular/common/http';
import {environment} from '../../../environments/environment';
import {BiFila} from '../bi/bi.model';
export interface ComercialFiltro {comparar?:boolean;desde:string;hasta:string;desdeB:string;hastaB:string;categoria?:number;elemento?:number;cliente?:number;proveedor?:number;ventanaConversion:number;escenario:string;diasInactividad:number;umbral:number;minimoOperaciones:number;importeMinimo:number;}
export interface ComercialResultado {vista:string;generadoUtc:string;filtros:ComercialFiltro;datos:Record<string,BiFila[]>;notas:string[];}
export interface ComercialDetalle {items:BiFila[];pagina:number;tamanioPagina:number;totalRegistros:number;}
export interface Prediccion {id:number;name:string;unit:string;stock:number|null;stockMinimo:number|null;sufficient:boolean;validated:boolean;method:string;first:string;last:string;totals:Record<string,number>|null;daily:{date:string;quantity:number}[];history:{date:string;quantity:number}[];quality:Record<string,{wape:number|null;mae:number|null;bias:number|null;samples:number;tests:{origin:string;first:string;last:string;actual:number;predicted:number}[]}>;reason?:string;}
export interface Predictivo {version:string;estado:string;actualizado:string|null;corte:string|null;productos:Prediccion[];memoria:BiFila[];mensaje:string;}
@Injectable({providedIn:'root'})
export class ComercialService {
 private http=inject(HttpClient);private base=environment.apiUrl;
 private params(f:ComercialFiltro){let p=new HttpParams();for(const[k,v]of Object.entries(f))if(v!=null&&v!=='')p=p.set(k,String(v));return p;}
 consultar(vista:string,f:ComercialFiltro){return this.http.get<ComercialResultado>(`${this.base}/inteligencia/${vista}`,{params:this.params(f)});}
 detalle(tipo:'stock'|'clientes',segmento:string,f:ComercialFiltro,pagina=1){return this.http.get<ComercialDetalle>(`${this.base}/inteligencia/detalle/${tipo}`,{params:this.params(f).set('segmento',segmento).set('pagina',pagina)});}
 predictivo(producto?:number){return this.http.get<Predictivo>(`${this.base}/ia/experimental`,{params:producto?{producto}: {}});}
 exportar(vista:string,f:ComercialFiltro,tipo:'excel'|'pdf'){return this.http.get(`${this.base}/inteligencia/${vista}/${tipo}`,{params:this.params(f),responseType:'blob'});}
}
