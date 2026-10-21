import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { vi } from 'vitest';
import { MensajesService } from './mensajes.service';
import { SessionService } from '../../core/services/session.service';
import { ProductosComponent } from '../../pages/productos/productos';
import { ServiciosComponent } from '../../pages/servicios/servicios';
import { ProveedoresComponent } from '../../pages/proveedores/proveedores';
import { ClientesComponent } from '../../pages/clientes/clientes';
import { UsuariosRolesComponent } from '../../pages/usuarios-roles/usuarios-roles';
import { CotizacionesComponent } from '../../pages/cotizaciones/cotizaciones';

const pantallas=[ProductosComponent,ServiciosComponent,ProveedoresComponent,ClientesComponent,UsuariosRolesComponent,CotizacionesComponent];
const casos: Array<{nombre:string; pantalla:any; servicio:string; api:string; accion:string; entrada:any; argumentos:any[]; preparar?:(c:any)=>void}> = [
  {nombre:'eliminar producto',pantalla:ProductosComponent,servicio:'productoService',api:'eliminar',accion:'eliminarProducto',entrada:42,argumentos:[42]},
  {nombre:'eliminar servicio',pantalla:ServiciosComponent,servicio:'servicioService',api:'eliminar',accion:'eliminarServicio',entrada:42,argumentos:[42]},
  {nombre:'eliminar proveedor',pantalla:ProveedoresComponent,servicio:'proveedorService',api:'eliminar',accion:'eliminarProveedor',entrada:42,argumentos:[42]},
  {nombre:'desactivar cliente',pantalla:ClientesComponent,servicio:'clienteService',api:'eliminar',accion:'eliminarCliente',entrada:{idCliente:42,nombres:'Cliente QA'},argumentos:[42]},
  {nombre:'desactivar usuario',pantalla:UsuariosRolesComponent,servicio:'usuarioService',api:'desactivar',accion:'desactivarUsuario',entrada:{idUsuario:42,correo:'usuario@example.test'},argumentos:[42]},
  {nombre:'desactivar rol',pantalla:UsuariosRolesComponent,servicio:'rolService',api:'desactivar',accion:'desactivarRol',entrada:{idRol:42,nombre:'Rol QA'},argumentos:[42]},
  {nombre:'enviar cotización por correo',pantalla:CotizacionesComponent,servicio:'cotizacionService',api:'enviarCorreo',accion:'enviarCorreo',entrada:{idCotizacion:42,correoCliente:'cliente@example.test'},argumentos:[42,1],preparar:c=>vi.spyOn(c,'puedeEnviarCorreo').mockReturnValue(true)},
  {nombre:'confirmar envío por WhatsApp',pantalla:CotizacionesComponent,servicio:'cotizacionService',api:'marcarRespondidaWhatsApp',accion:'confirmarWhatsAppEnviado',entrada:undefined,argumentos:[42,1],preparar:c=>c.cotizacionWhatsAppPendiente={idCotizacion:42}},
  {nombre:'aprobar cotización',pantalla:CotizacionesComponent,servicio:'cotizacionService',api:'aprobar',accion:'aprobarCotizacion',entrada:{idCotizacion:42},argumentos:[42,1],preparar:c=>vi.spyOn(c,'puedeAprobar').mockReturnValue(true)},
  {nombre:'cancelar cotización',pantalla:CotizacionesComponent,servicio:'cotizacionService',api:'cancelar',accion:'cancelarCotizacion',entrada:{idCotizacion:42},argumentos:[42,1],preparar:c=>vi.spyOn(c,'puedeCancelar').mockReturnValue(true)}
];

describe('Confirmaciones visuales: conservar acciones existentes',()=>{
  beforeEach(async()=>{
    await TestBed.configureTestingModule({imports:pantallas,providers:[provideRouter([]),provideHttpClient(),provideHttpClientTesting()]}).compileComponents();
    TestBed.inject(SessionService).guardarSesion({idUsuario:1,idRol:1,correo:'admin@example.test',rol:'Administrador',token:'qa',expira:'2099-01-01',permisos:[]});
  });
  afterEach(()=>{localStorage.clear();vi.restoreAllMocks();});

  for(const caso of casos) it(`MSG-FLUJO | ${caso.nombre}: cancelar no llama API y aceptar llama una vez con los mismos argumentos`,async()=>{
    const fixture=TestBed.createComponent(caso.pantalla);const c:any=fixture.componentInstance;
    caso.preparar?.(c);
    const api=vi.spyOn(c[caso.servicio],caso.api).mockReturnValue(new Subject().asObservable());
    const mensajes=TestBed.inject(MensajesService);
    const cancelada=c[caso.accion](caso.entrada);expect(mensajes.dialogo()).not.toBeNull();expect(api).not.toHaveBeenCalled();
    mensajes.resolver(false);await cancelada;expect(api).not.toHaveBeenCalled();
    const aceptada=c[caso.accion](caso.entrada);expect(api).not.toHaveBeenCalled();
    mensajes.resolver(true);await aceptada;expect(api).toHaveBeenCalledExactlyOnceWith(...caso.argumentos);
    fixture.destroy();
  });

  it('MSG-FLUJO | duplicar línea conserva el detalle al cancelar y agrega la línea al aceptar',async()=>{
    const fixture=TestBed.createComponent(CotizacionesComponent);const c=fixture.componentInstance;
    vi.spyOn(c,'obtenerElementoSeleccionadoDetalle').mockReturnValue({precioReferencial:10} as any);
    c.cotizacion.detalles=[{idElementoCatalogo:42,cantidad:1,precioUnitario:10,observacion:null}];
    c.detalleTemporal={idElementoCatalogo:42,cantidad:2,precioUnitario:10,observacion:null};
    const mensajes=TestBed.inject(MensajesService);
    const cancelar=c.agregarDetalle();expect(c.cotizacion.detalles).toHaveLength(1);mensajes.resolver(false);await cancelar;
    expect(c.cotizacion.detalles).toHaveLength(1);expect(c.detalleTemporal.cantidad).toBe(2);
    const aceptar=c.agregarDetalle();mensajes.resolver(true);await aceptar;
    expect(c.cotizacion.detalles).toHaveLength(2);expect(c.cotizacion.detalles[1]).toMatchObject({idElementoCatalogo:42,cantidad:2,precioUnitario:10});fixture.destroy();
  });

  it('MSG-FLUJO | la denegación de permisos y la protección del usuario actual preceden al diálogo',async()=>{
    const fixture=TestBed.createComponent(UsuariosRolesComponent);const c=fixture.componentInstance;
    const mensajes=TestBed.inject(MensajesService);const confirmar=vi.spyOn(mensajes,'confirmar');
    await c.desactivarUsuario({idUsuario:1,correo:'admin@example.test'} as any);
    expect(confirmar).not.toHaveBeenCalled();expect(c.error).toContain('propio usuario');
    vi.spyOn(TestBed.inject(SessionService),'tienePermiso').mockReturnValue(false);
    await c.desactivarUsuario({idUsuario:42,correo:'usuario@example.test'} as any);
    expect(confirmar).not.toHaveBeenCalled();fixture.destroy();
  });
});
