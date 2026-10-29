import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { ProductosComponent } from './pages/productos/productos';
import { ClientesComponent } from './pages/clientes/clientes';
import { VentasComponent } from './pages/ventas/ventas';
import { ComprasComponent } from './pages/compras/compras';
import { SessionService } from './core/services/session.service';
import { numeroNacionalPeru,telefonoCompletoPeru } from './shared/telefono-pe/telefono-pe';
describe('Formularios de gestión',()=>{
 const cdr:any={detectChanges:vi.fn()};
 beforeEach(()=>TestBed.configureTestingModule({providers:[{provide:SessionService,useValue:{esAdministrador:()=>false,tienePermiso:()=>true,obtenerUsuario:()=>({idUsuario:2})}}]}));
 it('PU-FE-PRO-001 | producto incompleto no llega a API',()=>{const api={crear:vi.fn()};const c=TestBed.runInInjectionContext(()=>new ProductosComponent(api as any,{} as any,cdr));c.guardarProducto();expect(api.crear).not.toHaveBeenCalled();expect(Object.keys(c.erroresCampo).length).toBeGreaterThan(0);});
 it('PU-FE-CLI-001 | cliente incompleto indica campo obligatorio',()=>{const api={crear:vi.fn()};const c=TestBed.runInInjectionContext(()=>new ClientesComponent(api as any,cdr));c.guardarCliente();expect(api.crear).not.toHaveBeenCalled();expect(Object.keys(c.erroresCampo).length).toBeGreaterThan(0);});
 it('PU-FE-VEN-001 | venta sin cliente no se envía',()=>{const api={registrar:vi.fn()};const c=TestBed.runInInjectionContext(()=>new VentasComponent(api as any,{} as any,cdr));c.guardarVenta();expect(api.registrar).not.toHaveBeenCalled();expect(Object.keys(c.erroresCampo).length).toBeGreaterThan(0);expect(c.guardando).toBe(false);});
 it('PU-FE-COM-001 | compra sin proveedor no se envía',()=>{const api={registrar:vi.fn()};const c=TestBed.runInInjectionContext(()=>new ComprasComponent(api as any,cdr));c.guardarCompra();expect(api.registrar).not.toHaveBeenCalled();expect(Object.keys(c.erroresCampo).length).toBeGreaterThan(0);expect(c.guardando).toBe(false);});
 it('PU-FE-TEL-001 | teléfono conserva un solo prefijo +51',()=>{expect(numeroNacionalPeru('+51 999 999 999')).toBe('999999999');expect(telefonoCompletoPeru('999999999')).toBe('+51999999999');expect(telefonoCompletoPeru('+51999999999')).toBe('+51999999999');expect(telefonoCompletoPeru('')).toBe('');});
});
