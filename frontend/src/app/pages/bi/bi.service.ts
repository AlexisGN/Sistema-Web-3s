import { Injectable,inject } from '@angular/core';
import { HttpClient,HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { BiFiltro,BiResultado,BiFila,BiVista } from './bi.model';

@Injectable({providedIn:'root'})
export class BiService {
  private readonly http=inject(HttpClient);
  private readonly url=environment.apiUrl+'/bi';
  parametros(f:BiFiltro):HttpParams {
    let p=new HttpParams();
    for(const [key,value] of Object.entries({...f,procedencia:'operativo'}))if(value!==undefined && value!==null && value!=='')p=p.set(key,String(value));
    return p;
  }
  consultar(vista:BiVista,f:BiFiltro){return this.http.get<BiResultado>(`${this.url}/${vista}`,{params:this.parametros(f)});}
  opciones(_procedencia:string,_buscar:string){return this.http.get<Record<string,BiFila[]>>(`${this.url}/opciones`,{params:{procedencia:'operativo'}});}
  detalle(tipo:string,id:number){return this.http.get<BiFila[]>(`${this.url}/operaciones/${tipo}/${id}`);}
  exportar(vista:BiVista,f:BiFiltro,formato:'pdf'|'excel'){return this.http.get(`${this.url}/${vista}/${formato}`,{params:this.parametros(f),responseType:'blob'});}
}
