import { TestBed } from '@angular/core/testing';
import { App } from './app';
import { provideRouter, Router } from '@angular/router';
import { LoginComponent } from './pages/login/login';
import { AuthService } from './core/services/auth.service';
import { SessionService } from './core/services/session.service';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([{path:'login',component:LoginComponent}]),{provide:AuthService,useValue:{}},{provide:SessionService,useValue:{}}],
    }).compileComponents();
  });

  it('PU-FE-APP-001 | monta la pantalla de acceso en /login', async () => {
    const fixture = TestBed.createComponent(App);
    await TestBed.inject(Router).navigateByUrl('/login');
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h2')?.textContent).toContain('Ingresar al panel');
    expect(compiled.querySelector<HTMLInputElement>('input[name="contrasena"]')?.type).toBe('password');
  });
});
