import {TestBed} from '@angular/core/testing';
import {of,Subject} from 'rxjs';
import {IaAvisosComponent} from './ia-avisos';
import {IaService} from './ia.service';
import {SessionService} from '../../core/services/session.service';

describe('Aviso flotante exclusivamente dentro de IA',()=>{
 let allowed=(p:string)=>true;
 const service={avisos:vi.fn((_s:string)=>of({pendientes:2}))};
 beforeEach(()=>{vi.useFakeTimers();allowed=()=>true;service.avisos.mockReset().mockImplementation(()=>of({pendientes:2}));TestBed.configureTestingModule({imports:[IaAvisosComponent],providers:[{provide:IaService,useValue:service},{provide:SessionService,useValue:{tienePermiso:(p:string)=>allowed(p)}}]});});
 afterEach(()=>{TestBed.resetTestingModule();vi.useRealTimers();});
 it('abre el análisis por evento, se puede cerrar y cancela el sondeo al salir',async()=>{
  const f=TestBed.createComponent(IaAvisosComponent);f.detectChanges();await vi.advanceTimersByTimeAsync(1);f.detectChanges();expect(service.avisos).toHaveBeenCalledWith('operativo');expect(f.nativeElement.textContent).toContain('2 por revisar');
  const event=vi.fn();f.componentInstance.revisar.subscribe(event);f.nativeElement.querySelector('.abrir').click();expect(event).toHaveBeenCalledOnce();expect(f.nativeElement.querySelector('a')).toBeNull();
  f.nativeElement.querySelector('[aria-label="Cerrar aviso de inventario"]').click();f.detectChanges();expect(f.nativeElement.querySelector('aside')).toBeNull();const count=service.avisos.mock.calls.length;f.destroy();await vi.advanceTimersByTimeAsync(61000);expect(service.avisos).toHaveBeenCalledTimes(count);
 });
 it('cambia la procedencia cancelando la consulta anterior y etiqueta la demostración',async()=>{
  const pending=new Subject<{pendientes:number}>();service.avisos.mockReturnValueOnce(pending);const f=TestBed.createComponent(IaAvisosComponent);f.detectChanges();await vi.advanceTimersByTimeAsync(1);expect(pending.observed).toBe(true);
  f.componentRef.setInput('procedencia','referencia');f.detectChanges();await vi.advanceTimersByTimeAsync(1);f.detectChanges();expect(pending.observed).toBe(false);expect(service.avisos).toHaveBeenLastCalledWith('referencia');expect(f.nativeElement.textContent).toContain('Demostración identificada');f.destroy();
 });
 it('no consulta ni muestra avisos sin permiso específico o de referencia',async()=>{
  allowed=p=>p==='IA_VER';const f=TestBed.createComponent(IaAvisosComponent);f.detectChanges();await vi.advanceTimersByTimeAsync(1);expect(service.avisos).not.toHaveBeenCalled();expect(f.nativeElement.querySelector('aside')).toBeNull();allowed=p=>p!=='BI_REFERENCIA';f.componentRef.setInput('procedencia','referencia');f.detectChanges();await vi.advanceTimersByTimeAsync(1);expect(service.avisos).not.toHaveBeenCalled();f.destroy();
 });
});
