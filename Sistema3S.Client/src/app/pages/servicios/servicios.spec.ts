import { ServiciosComponent } from './servicios';
import { vi } from 'vitest';
describe('Servicios',()=>{
 it('PU-FE-SER-001 | vacío muestra validación y no guarda',()=>{const api={crear:vi.fn()};const c=new ServiciosComponent(api as any,{detectChanges:vi.fn()} as any);c.guardarServicio();expect(api.crear).not.toHaveBeenCalled();expect(Object.keys(c.erroresCampo).length).toBeGreaterThan(0);});
 it('PU-FE-SER-002 | rechaza archivo que no es imagen',()=>{const c=new ServiciosComponent({} as any,{detectChanges:vi.fn()} as any);const input={files:[new File(['test'],'qa.exe',{type:'application/octet-stream'})],value:'qa.exe'};c.seleccionarImagen({target:input} as any);expect(c.imagenArchivo).toBeNull();expect(c.erroresCampo['imagenUrl']).toContain('JPG');expect(input.value).toBe('');});
});
