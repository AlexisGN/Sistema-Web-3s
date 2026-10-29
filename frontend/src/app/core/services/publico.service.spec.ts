import { PublicoService } from './publico.service';
import { environment } from '../../../environments/environment';
describe('Imágenes públicas',()=>{
 const s=new PublicoService({} as any);
 it('PU-FE-IMG-001 | uploads apunta al backend también en historial y carrito',()=>{expect(s.normalizarUrlPublica('uploads/catalogo/qa.png')).toBe(environment.apiUrl.replace(/\/api\/?$/,'')+'/uploads/catalogo/qa.png');expect(s.normalizarUrlPublica('/uploads/catalogo/qa.png')).toBe(environment.apiUrl.replace(/\/api\/?$/,'')+'/uploads/catalogo/qa.png');});
 it('PU-FE-IMG-002 | assets y URL absoluta conservan ubicación',()=>{expect(s.normalizarUrlPublica('assets/images/logo-3s.png')).toBe('/assets/images/logo-3s.png');expect(s.normalizarUrlPublica('https://example.test/qa.png')).toBe('https://example.test/qa.png');expect(s.normalizarUrlPublica(null)).toBe('');});
});
