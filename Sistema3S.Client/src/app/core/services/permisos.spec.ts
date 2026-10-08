import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { firstValueFrom, of } from 'rxjs';
import { vi } from 'vitest';
import { SessionService } from './session.service';
import { AuthService } from './auth.service';
import { permissionGuard } from '../guards/permission.guard';
import { ProductosComponent } from '../../pages/productos/productos';
import { ServiciosComponent } from '../../pages/servicios/servicios';
import { ProveedoresComponent } from '../../pages/proveedores/proveedores';
import { InventarioComponent } from '../../pages/inventario/inventario';
import { ProductoService } from './producto';
import { ServicioService } from './servicio';
import { ProveedorService } from './proveedor';
import { InventarioService } from './inventario';
import { CatalogoService } from './catalogo';
import { AdminLayoutComponent } from '../../shared/admin-layout/admin-layout';
import { CajaComponent } from '../../pages/caja/caja';
import { CajaService } from './caja.service';
import { environment } from '../../../environments/environment';

const sessionData=(permisos:string[],rol='Compras / Almacén')=>({token:'qa',rol,permisos,idUsuario:2,idRol:2,correo:'qa@example.test',expira:new Date(Date.now()+3600000).toISOString()});
const product={idProducto:1,idElementoCatalogo:1,nombre:'Producto permisos QA',codigoProducto:'QA',categoria:'Pruebas',stockActual:5,stockMinimo:2,estado:true,precioReferencial:100};
const page=(items:any[])=>of({items,pagina:1,tamanioPagina:5,totalRegistros:items.length,totalPaginas:1});
afterEach(()=>{localStorage.clear();vi.restoreAllMocks();});
describe('Sesión de permisos',()=>{
  it.each(['Compras / Almacén','Encargado de Almacén','Encargado de Ventas','Vendedor','Personal QA'])('FE-PERM-001 | %s no recibe acciones por nombre de rol',rol=>{
    const s=new SessionService();s.guardarSesion(sessionData(['PRODUCTOS_VER'],rol));
    expect(s.tienePermiso('PRODUCTOS_VER')).toBe(true);expect(s.tienePermiso('PRODUCTOS_EDITAR')).toBe(false);expect(s.tienePermiso('COMPRAS_CREAR')).toBe(false);
  });
  it('FE-PERM-002 | ignora detalle no asignado y no fusiona claims antiguos con la lista vigente',()=>{
    const s=new SessionService();const token='x.'+btoa(JSON.stringify({permiso:['PRODUCTOS_EDITAR']}))+'.x';
    s.guardarSesion({...sessionData([]),token,permisosDetalle:[{nombre:'PRODUCTOS_EDITAR',asignado:false}]});expect(s.obtenerPermisos()).toEqual([]);
    s.guardarSesion({token:'qa',rol:'QA',permisosDetalle:[{nombre:'PRODUCTOS_VER',asignado:true},{nombre:'PRODUCTOS_ELIMINAR',asignado:false}]});expect(s.obtenerPermisos()).toEqual(['PRODUCTOS_VER']);
  });
  it('FE-PERM-003 | una revocación reemplaza permisos y mantiene el token',()=>{
    const s=new SessionService();s.guardarSesion(sessionData(['PRODUCTOS_VER','PRODUCTOS_EDITAR']));s.actualizarPermisos({permisos:['PRODUCTOS_VER'],rol:'Compras / Almacén'});expect(s.obtenerToken()).toBe('qa');expect(s.tienePermiso('PRODUCTOS_EDITAR')).toBe(false);
  });
  it('FE-PERM-004 | conserva excepción de administrador y anulación exclusiva',()=>{
    const s=new SessionService();s.guardarSesion(sessionData(['COMPRAS_ANULAR']));expect(s.tienePermiso('COMPRAS_ANULAR')).toBe(false);s.guardarSesion(sessionData([],'Administrador'));expect(s.tienePermiso('PRODUCTOS_EDITAR')).toBe(true);
  });
  it('FE-PERM-013 | menú y refresco cada 15 segundos respetan revocaciones y no agregan módulos por rol',()=>{
    vi.useFakeTimers();
    const s=new SessionService();s.guardarSesion(sessionData(['PRODUCTOS_VER','COMPRAS_VER']));
    const router={url:'/admin/productos',navigate:vi.fn(),navigateByUrl:vi.fn()};
    const auth={perfil:vi.fn(()=>{s.actualizarPermisos({permisos:['PRODUCTOS_VER'],rol:'Compras / Almacén'});return of({});})};
    const c=new AdminLayoutComponent({get:()=>of({})} as any,router as any,s,auth as any,{markForCheck:vi.fn()} as any);
    try {
      c.ngOnInit();expect(c.menuVisible.flatMap(g=>g.items).map(i=>i.route)).toEqual(['/admin/productos','/admin/compras']);
      vi.advanceTimersByTime(15000);expect(auth.perfil).toHaveBeenCalledTimes(1);expect(c.menuVisible.flatMap(g=>g.items).map(i=>i.route)).toEqual(['/admin/productos']);
    } finally {c.ngOnDestroy();vi.useRealTimers();}
  });
});
describe('Rutas y refresco',()=>{
  beforeEach(()=>TestBed.configureTestingModule({providers:[provideHttpClient(),provideHttpClientTesting(),provideRouter([]),SessionService,AuthService]}));
  it('FE-PERM-005 | guard consulta BD y no permite la excepción antigua de ruta inicial',async()=>{
    const s=TestBed.inject(SessionService);s.guardarSesion(sessionData(['COMPRAS_VER']));
    const result=TestBed.runInInjectionContext(()=>permissionGuard({data:{permisos:['PRODUCTOS_VER']}} as any,{url:'/admin/productos'} as any));
    const promise=firstValueFrom(result as any);TestBed.inject(HttpTestingController).expectOne(environment.apiUrl+'/auth/perfil').flush({permisos:['PRODUCTOS_VER'],rol:'Compras / Almacén',idRol:2});expect(await promise).toBe(true);
    const denied=TestBed.runInInjectionContext(()=>permissionGuard({data:{permisos:['COMPRAS_VER']}} as any,{url:'/admin/compras'} as any));
    const p=firstValueFrom(denied as any);TestBed.inject(HttpTestingController).expectOne(environment.apiUrl+'/auth/perfil').flush({permisos:['PRODUCTOS_VER'],rol:'Compras / Almacén'});expect(TestBed.inject(Router).serializeUrl(await p as any)).toBe('/admin/productos');
  });
  it('FE-PERM-006 | sesión inactiva descarta permisos al fallar perfil con 401',async()=>{
    const s=TestBed.inject(SessionService);s.guardarSesion(sessionData(['PRODUCTOS_VER']));const p=firstValueFrom(TestBed.inject(AuthService).perfil()).catch(e=>e.status);
    TestBed.inject(HttpTestingController).expectOne(environment.apiUrl+'/auth/perfil').flush({}, {status:401,statusText:'Unauthorized'});expect(await p).toBe(401);expect(s.estaAutenticado()).toBe(false);
  });
});
describe('DOM de acciones administrativas',()=>{
  const init=async(component:any,permissions:string[],providers:any[])=>{
    TestBed.configureTestingModule({imports:[component],providers:[SessionService,...providers]});await TestBed.compileComponents();
    TestBed.inject(SessionService).guardarSesion(sessionData(permissions));vi.spyOn(window,'scrollTo').mockImplementation(()=>{});
    const f=TestBed.createComponent(component);f.detectChanges();await f.whenStable();f.detectChanges();return f;
  };
  const buttons=(f:any)=>Array.from(f.nativeElement.querySelectorAll('button')).map((b:any)=>b.textContent.trim());
  it.each([[false,false,false],[true,false,false],[false,true,false],[true,true,true]])('FE-PERM-007 | Productos crear=%s editar=%s eliminar=%s',async(create,edit,del)=>{
    const perms=['PRODUCTOS_VER',...(create?['PRODUCTOS_CREAR']:[]),...(edit?['PRODUCTOS_EDITAR']:[]),...(del?['PRODUCTOS_ELIMINAR']:[])];
    const api={listar:vi.fn(()=>page([product])),crear:vi.fn(),actualizar:vi.fn(),eliminar:vi.fn()};
    const f=await init(ProductosComponent,perms,[{provide:ProductoService,useValue:api},{provide:CatalogoService,useValue:{listarCategorias:()=>of([]),listarMarcas:()=>of([]),listarUnidadesMedida:()=>of([])}}]);
    expect(buttons(f).includes('Editar')).toBe(edit);expect(buttons(f).includes('Eliminar')).toBe(del);expect(!!f.nativeElement.querySelector('form.producto-form')).toBe(create);
    if(!edit)(f.componentInstance as ProductosComponent).editarProducto(product as any);if(!del)(f.componentInstance as ProductosComponent).eliminarProducto(1);
    expect(api.actualizar).not.toHaveBeenCalled();expect(api.eliminar).not.toHaveBeenCalled();
    if(edit){const b=Array.from(f.nativeElement.querySelectorAll('button')).find((b:any)=>b.textContent.trim()==='Editar') as HTMLButtonElement;b.click();f.detectChanges();expect(f.nativeElement.querySelector('form.producto-form')).toBeTruthy();}
    f.destroy();
  });
  it('FE-PERM-008 | revocar editar retira el botón y formulario del DOM',async()=>{
    const f=await init(ProductosComponent,['PRODUCTOS_VER','PRODUCTOS_EDITAR'],[{provide:ProductoService,useValue:{listar:()=>page([product])}},{provide:CatalogoService,useValue:{listarCategorias:()=>of([]),listarMarcas:()=>of([]),listarUnidadesMedida:()=>of([])}}]);
    (f.componentInstance as ProductosComponent).editarProducto(product as any);f.detectChanges();expect(f.nativeElement.querySelector('form')).toBeTruthy();TestBed.inject(SessionService).actualizarPermisos({permisos:['PRODUCTOS_VER']});f.detectChanges();expect(buttons(f)).not.toContain('Editar');expect(f.nativeElement.querySelector('form.producto-form')).toBeNull();f.destroy();
  });
  it('FE-PERM-009 | Servicios sólo VER retira crear, editar y eliminar',async()=>{
    const f=await init(ServiciosComponent,['SERVICIOS_VER'],[{provide:ServicioService,useValue:{listar:()=>page([{...product,idServicio:1}])}}]);expect(buttons(f)).not.toContain('Editar');expect(buttons(f)).not.toContain('Eliminar');expect(f.nativeElement.querySelector('form.servicio-form')).toBeNull();f.destroy();
  });
  it('FE-PERM-010 | Proveedores sólo VER retira crear, editar y eliminar',async()=>{
    const f=await init(ProveedoresComponent,['PROVEEDORES_VER'],[{provide:ProveedorService,useValue:{listar:()=>page([{idProveedor:1,ruc:'20000000001',razonSocial:'Proveedor QA',estado:true}])}}]);expect(buttons(f)).not.toContain('Editar');expect(buttons(f)).not.toContain('Eliminar');expect(f.nativeElement.querySelector('form.proveedor-form')).toBeNull();f.destroy();
  });
  it('FE-PERM-011 | Inventario sólo VER no solicita historial ni muestra ajustes',async()=>{
    const api={resumen:()=>of({}),listar:()=>page([]),listarMovimientosRecientes:vi.fn(()=>of([]))};
    const f=await init(InventarioComponent,['INVENTARIO_VER'],[{provide:InventarioService,useValue:api}]);expect(api.listarMovimientosRecientes).not.toHaveBeenCalled();expect(f.nativeElement.querySelector('.panel-actions')).toBeNull();expect(f.nativeElement.querySelector('.recent-panel')).toBeNull();f.destroy();
  });
  it('FE-PERM-012 | Caja sólo VER funciona para Compras y oculta abrir, mover, cerrar y reporte',async()=>{
    const api={obtenerCajaActiva:()=>of(null),obtenerReporte:vi.fn(()=>of([]))};
    const f=await init(CajaComponent,['CAJA_VER'],[{provide:CajaService,useValue:api}]);expect(api.obtenerReporte).not.toHaveBeenCalled();expect(f.nativeElement.querySelector('.access-box')).toBeNull();expect(f.nativeElement.querySelector('.quick-actions button')).toBeNull();expect(buttons(f)).not.toContain('Abrir caja');f.destroy();
  });
});
