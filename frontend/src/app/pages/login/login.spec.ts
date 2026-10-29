import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, Subject } from 'rxjs';
import { vi } from 'vitest';
import { LoginComponent } from './login';
import { AuthService } from '../../core/services/auth.service';
import { SessionService } from '../../core/services/session.service';
describe('Login administrativo',()=>{
 const api={login:vi.fn()};const session={guardarSesion:vi.fn(),obtenerRutaInicial:()=>'/admin'};const router={navigateByUrl:vi.fn().mockResolvedValue(true)};
 beforeEach(()=>{vi.clearAllMocks();TestBed.configureTestingModule({imports:[LoginComponent],providers:[{provide:AuthService,useValue:api},{provide:SessionService,useValue:session},{provide:Router,useValue:router}]});});
 it('PU-FE-AUTH-001 | formulario vacío muestra mensaje sin llamar API',async()=>{const f=TestBed.createComponent(LoginComponent);await f.whenStable();f.nativeElement.querySelector('form').dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}));await f.whenStable();expect(api.login).not.toHaveBeenCalled();expect(f.nativeElement.textContent).toContain('Ingresa tu correo y contraseña.');});
 it('PU-FE-AUTH-002 | normaliza correo y guarda sesión válida',()=>{const result={token:'sintetico'};api.login.mockReturnValue(of(result));const f=TestBed.createComponent(LoginComponent);f.componentInstance.correo=' ADMIN@EXAMPLE.TEST ';f.componentInstance.contrasena='SoloQA123!';f.componentInstance.ingresar();expect(api.login).toHaveBeenCalledWith({correo:'admin@example.test',contrasena:'SoloQA123!'});expect(session.guardarSesion).toHaveBeenCalledWith(result);expect(router.navigateByUrl).toHaveBeenCalledWith('/admin');});
 it('PU-FE-AUTH-003 | botón en espera y error del servidor',async()=>{const pending=new Subject();api.login.mockReturnValue(pending);const f=TestBed.createComponent(LoginComponent);f.componentInstance.correo='a@example.test';f.componentInstance.contrasena='incorrecta';f.componentInstance.ingresar();await f.whenStable();expect(f.nativeElement.querySelector('button[type=submit]').disabled).toBe(true);pending.error({error:{mensaje:'Credenciales inválidas.'}});f.changeDetectorRef.markForCheck();await f.whenStable();expect(f.nativeElement.textContent).toContain('Credenciales inválidas.');expect(f.nativeElement.querySelector('button[type=submit]').disabled).toBe(false);expect(session.guardarSesion).not.toHaveBeenCalled();});
});
