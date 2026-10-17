import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface IaImportes {horizonte:number|null;mes?:string|null;cantidad:number|null;ingresoNeto:number|null;costoReferencia:number|null;margenBruto:number|null;porcentajeMargen:number|null;estado:string;recomendacion:string;datosPendientes?:string[];}
export interface IaHistorial {dias:number;diasConVenta:number;diasFaltantes:number;diasConVentaFaltantes:number;desde:string|null;}
export interface IaPeriodo {desde:string;hasta:string;dias:number;disponibleDesde:string;disponibleHasta:string;personalizado:boolean;}
export interface IaProducto {idProducto:number;nombre:string;unidad:string;calidad?:Record<string,{wape:number|null;predicted:number;actual:number;horizon:number;method:string;origin:string;first:string;last:string}|null>;metodo?:string;cambioPatron?:boolean;stock?:number|null;suficiente?:boolean;motivo?:string;historial?:IaHistorial;periodoPersonalizado?:Partial<IaImportes>|null;
 cantidades?:Record<string,number|null>;mesSiguiente?:number|null;historico?:IaDia[];estimado?:IaDia[];
 intervalos?:Record<string,{lower:number;upper:number;windows:number}|null>;desde?:string;
 precioNeto?:number|null;variacion30?:number|null;observado30?:number;orientacion?:string;horizontes?:IaImportes[];mesSiguienteFinanciero?:IaImportes;historicoMensual?:{mes:string;ingresoNeto:number}[];estimadoMensual?:{mes:string;cantidad:number;ingresoNeto:number|null;desde:string;hasta:string}[];
 costoNeto?:number|null;fechaPrecio?:string;fechaCosto?:string;riesgo?:IaRiesgo;politica?:IaPolitica|null;proveedor?:string;idProveedor?:number|null;}
export interface IaDia {date:string;quantity:number;}
export interface IaRiesgo {stock:number|null;coberturaDias:number|null;agotamiento:string|null;prioridad:string;cantidad:number|null;fechaPedido:string|null;idProveedor:number|null;explicacion:string;fechaLimitePedido?:string|null;retrasoDias?:number;}
export interface IaPolitica {idProducto:number;idProveedor:number;plazoDias:number;coberturaDias:number;seguridad:number;minimo:number;multiplo:number;reservado:number;entradaConfirmada:number;fechaEntrada:string|null;evidencia:string;verificadaHasta:string;}
export interface IaRecomendacion {Id:number;IdProducto:number;Nombre:string;Unidad:string;Prioridad:string;Cantidad:number|null;FechaPedido:string|null;Mensaje:string;Estado:string;Creada:string;Motivo:string|null;IdProveedor:number|null;Proveedor:string|null;IdSolicitud:number|null;IdCompra:number|null;Stock?:number|null;Demanda30?:number|null;Demanda60?:number|null;Demanda90?:number|null;DemandaFecha?:number|null;Actual?:boolean;}
export interface IaRespuesta {periodo?:IaPeriodo;procedencia:string;mensaje:string;actualizado:string|null;advertencia?:string;productos?:IaProducto[]|number;conEstimacion?:number;historialOperativoVerificado?:boolean;recomendaciones?:IaRecomendacion[];confiabilidad?:{mensaje:string;resultados:Record<string,unknown>[]};}
export interface IaCompra {Id:number;IdProducto:number;IdProveedor:number;Cantidad:number;PrecioConIgv:number|null;Justificacion:string;IdCompra:number|null;Producto:string;Unidad:string;CodigoProducto:string;Proveedor:string;}
@Injectable({providedIn:'root'})
export class IaService {
 private readonly http=inject(HttpClient);private readonly base=environment.apiUrl+'/ia';
 consultar(vista:string,procedencia:string,producto:number|null,fechaObjetivo?:string){return this.http.get<IaRespuesta>(`${this.base}/${vista}`,{params:{procedencia,...(producto?{producto}: {}),...(fechaObjetivo?{fechaObjetivo}: {})}});}
 productos(procedencia:string){return this.http.get<IaProducto[]>(this.base+'/productos',{params:{procedencia}});}
 proveedores(){return this.http.get<{IdProveedor:number;Nombre:string}[]>(this.base+'/proveedores');}
 avisos(procedencia='operativo'){return this.http.get<{pendientes:number}>(this.base+'/avisos',{params:{procedencia}});}
 abastecimiento(p:IaPolitica){return this.http.put<{mensaje:string}>(this.base+'/abastecimiento',p);}
 revisar(id:number,estado:string,motivo:string,procedencia:string){return this.http.put(`${this.base}/recomendaciones/${id}`,{estado,motivo},{params:{procedencia}});}
 solicitud(id:number,cantidad:number,idProveedor:number,justificacion:string){return this.http.post<{id:number}>(`${this.base}/recomendaciones/${id}/solicitud`,{cantidad,idProveedor,justificacion});}
 previsualizar(p:IaPolitica){return this.http.post<IaVistaPrevia>(this.base+'/abastecimiento/vista-previa',p);}
 obtenerSolicitud(id:number){return this.http.get<IaCompra>(`${this.base}/solicitudes/${id}`);}
 vincular(id:number,idCompra:number){return this.http.put(`${this.base}/solicitudes/${id}/compra`,{idCompra});}
}

export interface IaVistaPrevia{riesgo:IaRiesgo;demanda:number;stockDisponible:number;recepcion:string;costoNeto:number|null;proveedor:string;guardado:false;}
