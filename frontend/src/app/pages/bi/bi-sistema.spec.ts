import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting,HttpTestingController } from '@angular/common/http/testing';
import { BiService } from './bi.service';
import { BiFiltro } from './bi.model';
import { IaService } from '../ia/ia.service';
describe('Datos operativos en BI e IA',()=>{
 afterEach(()=>TestBed.resetTestingModule());
 it('consulta, exporta, carga catálogos y revisa decisiones usando sólo la fuente del sistema',()=>{
  TestBed.configureTestingModule({providers:[provideHttpClient(),provideHttpClientTesting()]});
  const bi=TestBed.inject(BiService),ia=TestBed.inject(IaService),http=TestBed.inject(HttpTestingController);
  const f:BiFiltro={desde:'2026-07-16',hasta:'2026-10-16',procedencia:'referencia',agrupacion:'mes',pagina:1,tamanio:20};
  bi.consultar('ventas',f).subscribe();bi.exportar('compras',f,'pdf').subscribe();bi.exportar('clientes',f,'excel').subscribe();bi.opciones('referencia','Texto antiguo').subscribe();
  ia.consultar('pronostico','referencia',3,'2026-11-16').subscribe();ia.productos('referencia').subscribe();ia.avisos('referencia').subscribe();ia.revisar(7,'Revisada','Decisión','referencia').subscribe();
  const requests=http.match(()=>true);expect(requests).toHaveLength(8);
  for(const q of requests){expect(q.request.params.get('procedencia')).toBe('operativo');expect(q.request.params.has('buscar')).toBe(false);q.flush(q.request.responseType==='blob'?new Blob():{});}
  expect(requests.find(q=>q.request.url.endsWith('/ia/pronostico'))!.request.params.get('fechaObjetivo')).toBe('2026-11-16');
  expect(requests.find(q=>q.request.url.endsWith('/ia/pronostico'))!.request.params.get('producto')).toBe('3');
  http.verify();
 });
});
