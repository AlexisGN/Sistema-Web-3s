import { Component, inject } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NavigationStart, Router, provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { vi } from 'vitest';
import { CentroMensajesComponent } from './centro-mensajes';
import { MensajesService } from './mensajes.service';

@Component({ standalone: true, imports: [CentroMensajesComponent], template: `<button id="origen" type="button" (click)="pedir()">Eliminar producto de prueba</button><s3s-centro-mensajes />` })
class PruebaMensajes {
  readonly mensajes = inject(MensajesService);
  resultado?: boolean;
  async pedir() { this.resultado = await this.mensajes.confirmar('¿Deseas eliminar este producto?', {titulo:'Eliminar producto', aceptar:'Eliminar', tipo:'error', icono:'trash'}); }
}

describe('Mensajes compartidos: decisiones, accesibilidad y continuidad', () => {
  afterEach(() => vi.restoreAllMocks());

  it('MSG-001 | sólo resuelve al decidir; cancelar no se interpreta como aceptar', async () => {
    TestBed.configureTestingModule({providers:[provideRouter([])]});
    const servicio=TestBed.inject(MensajesService);const resultado=vi.fn();
    const primera=servicio.confirmar('Primera operación').then(resultado);
    expect(resultado).not.toHaveBeenCalled();expect(servicio.dialogo()?.mensaje).toBe('Primera operación');
    expect(await servicio.confirmar('Doble clic')).toBe(false);
    servicio.resolver(false);await primera;expect(resultado).toHaveBeenCalledWith(false);
    const segunda=servicio.confirmar('Segunda operación');servicio.resolver(true);expect(await segunda).toBe(true);
    expect(servicio.dialogo()).toBeNull();servicio.resolver(true);
  });

  it('MSG-002 | navegar cancela la decisión pendiente y evita ejecutar una acción de la pantalla anterior', async () => {
    const eventos=new Subject<NavigationStart>();
    TestBed.configureTestingModule({providers:[{provide:Router,useValue:{events:eventos}}]});
    const servicio=TestBed.inject(MensajesService);const resultado=servicio.confirmar('Operación pendiente');
    servicio.advertir('Aviso');eventos.next(new NavigationStart(1,'/otra'));
    expect(await resultado).toBe(false);expect(servicio.avisos()).toEqual([]);
  });

  it('MSG-003 | conserva avisos legibles hasta cerrar y no duplica el mismo aviso', () => {
    TestBed.configureTestingModule({});const servicio=TestBed.inject(MensajesService);
    servicio.advertir('Debe activar PRODUCTOS_VER.');servicio.advertir('Debe activar PRODUCTOS_VER.');
    expect(servicio.avisos()).toHaveLength(1);expect(servicio.avisos()[0].tipo).toBe('warning');
    servicio.cerrarAviso(servicio.avisos()[0].id);expect(servicio.avisos()).toEqual([]);
  });

  it('MSG-004 | muestra el diálogo visual, enfoca Cancelar, restaura foco sin scroll y admite Escape', async () => {
    // JSDOM no implementa la API de diálogo; el navegador real se verifica por separado.
    Object.defineProperty(HTMLDialogElement.prototype,'showModal',{configurable:true,value:function(this:HTMLDialogElement){this.setAttribute('open','');}});
    Object.defineProperty(HTMLDialogElement.prototype,'close',{configurable:true,value:function(this:HTMLDialogElement){this.removeAttribute('open');}});
    await TestBed.configureTestingModule({imports:[PruebaMensajes],providers:[provideRouter([])]}).compileComponents();
    const fixture=TestBed.createComponent(PruebaMensajes);fixture.detectChanges();
    document.body.appendChild(fixture.nativeElement);
    const origen=fixture.nativeElement.querySelector('#origen') as HTMLButtonElement;origen.focus();
    const enfocar=vi.spyOn(origen,'focus');origen.click();fixture.detectChanges();
    const dialogo=fixture.nativeElement.querySelector('dialog') as HTMLDialogElement;
    expect(dialogo.open).toBe(true);expect(dialogo.getAttribute('aria-labelledby')).toBe('s3s-dialogo-titulo');
    expect(dialogo.querySelector('h2')?.textContent).toBe('Eliminar producto');
    expect(document.activeElement?.textContent).toBe('Cancelar');
    dialogo.dispatchEvent(new Event('cancel',{cancelable:true}));fixture.detectChanges();await fixture.whenStable();
    expect(fixture.componentInstance.resultado).toBe(false);expect(dialogo.open).toBe(false);
    expect(enfocar).toHaveBeenCalledWith({preventScroll:true});expect(document.activeElement).toBe(origen);
    origen.click();fixture.detectChanges();
    (dialogo.querySelector('.dialogo-principal') as HTMLButtonElement).click();fixture.detectChanges();await fixture.whenStable();
    expect(fixture.componentInstance.resultado).toBe(true);expect(dialogo.open).toBe(false);
    fixture.destroy();fixture.nativeElement.remove();
  });
});
