import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, Subject } from 'rxjs';
import { vi } from 'vitest';
import { UsuariosRolesComponent } from './usuarios-roles';
import { SessionService } from '../../core/services/session.service';
import { RolService } from '../../core/services/rol.service';
import { UsuarioService } from '../../core/services/usuario.service';
import { Permiso } from '../../core/models/permiso.model';
import { RolListado, RolOperacionResultado } from '../../core/models/rol.model';

describe('Usuarios y roles: continuidad del DOM al asignar permisos', () => {
  let fixture: ComponentFixture<UsuariosRolesComponent>;
  let permisosGuardados: Permiso[];
  let respuestaGuardado: Subject<RolOperacionResultado>;
  let api: { listar: ReturnType<typeof vi.fn>; obtenerPermisosPorRol: ReturnType<typeof vi.fn>; asignarPermisos: ReturnType<typeof vi.fn> };
  const roles: RolListado[] = [
    { idRol: 1, nombre: 'Administrador', estado: true, totalPermisos: 4, totalUsuarios: 1 },
    { idRol: 2, nombre: 'Compras / Almacén', estado: true, totalPermisos: 1, totalUsuarios: 1 }
  ];
  const boton = (nombre: string): HTMLButtonElement =>
    Array.from((fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('.permission-item'))
      .find((elemento: HTMLButtonElement) => elemento.querySelector('span')?.textContent === nombre)!;
  const actualizar = async () => { fixture.detectChanges(); await fixture.whenStable(); fixture.detectChanges(); };

  beforeEach(async () => {
    permisosGuardados = ['PRODUCTOS_VER', 'PRODUCTOS_EDITAR', 'SERVICIOS_EDITAR', 'USUARIOS_EDITAR']
      .map((nombre, indice) => ({ idPermiso: indice + 1, nombre, estado: true, asignado: indice === 0 }));
    respuestaGuardado = new Subject<RolOperacionResultado>();
    api = {
      listar: vi.fn(() => of(roles.map(rol => ({ ...rol })))),
      obtenerPermisosPorRol: vi.fn(() => of(permisosGuardados.map(permiso => ({ ...permiso })))),
      asignarPermisos: vi.fn(() => respuestaGuardado.asObservable())
    };
    await TestBed.configureTestingModule({
      imports: [UsuariosRolesComponent],
      providers: [provideRouter([]), SessionService,
        { provide: RolService, useValue: api },
        { provide: UsuarioService, useValue: { listar: () => of([]) } }]
    }).compileComponents();
    TestBed.inject(SessionService).guardarSesion({ idUsuario: 1, idRol: 1, correo: 'admin@example.test',
      rol: 'Administrador', token: 'qa', expira: '2099-01-01', permisos: [] });
    fixture = TestBed.createComponent(UsuariosRolesComponent);
    await actualizar();
    fixture.componentInstance.cambiarTab('permisos');
    fixture.componentInstance.cargarPermisosRol(2);
    await actualizar();
  });

  afterEach(() => { fixture.destroy(); localStorage.clear(); });

  it('FE-SCROLL-001 | conserva grupos y botones al activar/desactivar consecutivamente', async () => {
    const grupos = Array.from(fixture.nativeElement.querySelectorAll('.permission-group'));
    const botones = Array.from(fixture.nativeElement.querySelectorAll('.permission-item'));
    const consultasIniciales = api.obtenerPermisosPorRol.mock.calls.length;
    for (const nombre of ['PRODUCTOS_EDITAR', 'SERVICIOS_EDITAR', 'USUARIOS_EDITAR']) {
      const elemento = boton(nombre);
      for (const activo of [true, false, true, false]) {
        elemento.click(); await actualizar();
        expect(boton(nombre)).toBe(elemento);
        expect(elemento.classList.contains('active')).toBe(activo);
        grupos.forEach((grupo, indice) => expect(fixture.nativeElement.querySelectorAll('.permission-group')[indice]).toBe(grupo));
        botones.forEach((botonActual, indice) => expect(fixture.nativeElement.querySelectorAll('.permission-item')[indice]).toBe(botonActual));
      }
    }
    expect(api.asignarPermisos).not.toHaveBeenCalled();
    expect(api.obtenerPermisosPorRol).toHaveBeenCalledTimes(consultasIniciales);
  });

  it('FE-SCROLL-002 | mantiene los nodos cuando el guardado actual refresca datos del servidor', async () => {
    const elemento = boton('PRODUCTOS_EDITAR');
    const grupo = elemento.closest('.permission-group');
    const opcion = fixture.nativeElement.querySelector('select[name="idRolPermisosSeleccionado"] option:last-child');
    elemento.click(); await actualizar();
    fixture.nativeElement.querySelector('.permissions-actions button').click(); await actualizar();
    expect(api.asignarPermisos).toHaveBeenCalledExactlyOnceWith(2, { idsPermisos: [1, 2] });
    expect(fixture.componentInstance.procesando).toBe(true);
    permisosGuardados[1].asignado = true;
    respuestaGuardado.next({ idRol: 2, mensaje: 'Permisos actualizados correctamente.' });
    await actualizar();
    expect(boton('PRODUCTOS_EDITAR')).toBe(elemento);
    expect(elemento.closest('.permission-group')).toBe(grupo);
    expect(elemento.classList.contains('active')).toBe(true);
    expect(fixture.nativeElement.querySelector('select[name="idRolPermisosSeleccionado"] option:last-child')).toBe(opcion);
    expect(fixture.componentInstance.idRolPermisosSeleccionado).toBe(2);
    expect(fixture.componentInstance.procesando).toBe(false);
    expect(api.obtenerPermisosPorRol).toHaveBeenLastCalledWith(2);
  });

  it('FE-SCROLL-003 | conserva la selección y los nodos si el servidor rechaza el guardado', async () => {
    const elemento = boton('PRODUCTOS_EDITAR');
    elemento.click(); await actualizar();
    fixture.nativeElement.querySelector('.permissions-actions button').click(); await actualizar();
    respuestaGuardado.error({ error: { mensaje: 'No se pudo guardar.' } });
    await actualizar();
    expect(boton('PRODUCTOS_EDITAR')).toBe(elemento);
    expect(elemento.classList.contains('active')).toBe(true);
    expect(fixture.componentInstance.procesando).toBe(false);
    expect(fixture.componentInstance.error).toBeTruthy();
  });
});
