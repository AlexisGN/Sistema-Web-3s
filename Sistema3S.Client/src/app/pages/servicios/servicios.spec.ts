import { ServiciosComponent } from './servicios';
import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { SessionService } from '../../core/services/session.service';
describe('Servicios',()=>{
 beforeEach(()=>{TestBed.configureTestingModule({providers:[SessionService]});TestBed.inject(SessionService).guardarSesion({token:'qa',rol:'Personal QA',permisos:['SERVICIOS_CREAR']});});
 afterEach(()=>localStorage.clear());
 it('PU-FE-SER-001 | vacío muestra validación y no guarda',()=>{const api={crear:vi.fn()};const c=TestBed.runInInjectionContext(()=>new ServiciosComponent(api as any,{detectChanges:vi.fn()} as any));c.guardarServicio();expect(api.crear).not.toHaveBeenCalled();expect(Object.keys(c.erroresCampo).length).toBeGreaterThan(0);});
 it('PU-FE-SER-002 | rechaza archivo que no es imagen',()=>{const c=TestBed.runInInjectionContext(()=>new ServiciosComponent({} as any,{detectChanges:vi.fn()} as any));const input={files:[new File(['test'],'qa.exe',{type:'application/octet-stream'})],value:'qa.exe'};c.seleccionarImagen({target:input} as any);expect(c.imagenArchivo).toBeNull();expect(c.erroresCampo['imagenUrl']).toContain('JPG');expect(input.value).toBe('');});
});
