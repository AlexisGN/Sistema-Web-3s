import { baseRequerida, esVisualizacionFuncional, MINIMO_MODULO_VISIBLE, validarSeleccionPermisos } from './reglas-seleccion-permisos';
import { Permiso } from './permiso.model';

describe('Dependencias y mínimo visible del rol',()=>{
  const seleccion=(...nombres:string[]):Permiso[]=>nombres.map((nombre,i)=>({idPermiso:i+1,nombre,estado:true,asignado:true}));
  it.each(['PRODUCTOS','SERVICIOS','CLIENTES','PROVEEDORES','COMPRAS','CAJA','INVENTARIO','COTIZACIONES','VENTAS','USUARIOS','ROLES','NUEVO'])('FE-SEL-005 | %s exige su propio VER, admite VER solo y respeta la convención futura',modulo=>{
    expect(baseRequerida(`${modulo}_EDITAR`)).toBe(`${modulo}_VER`);
    expect(validarSeleccionPermisos(seleccion('INICIO_VER',`${modulo}_EDITAR`))).toContain(`${modulo}_VER`);
    expect(validarSeleccionPermisos(seleccion(`${modulo}_VER`))).toBeNull();
    expect(validarSeleccionPermisos(seleccion(`${modulo}_VER`,`${modulo}_EDITAR`))).toBeNull();
  });
  it('FE-SEL-006 | INICIO y Ver reportes no cuentan como módulos operativos',()=>{
    expect(esVisualizacionFuncional('INICIO_VER')).toBe(false);
    for(const nombres of [[],['INICIO_VER'],['Ver reportes']])expect(validarSeleccionPermisos(seleccion(...nombres))).toBe(MINIMO_MODULO_VISIBLE);
    expect(validarSeleccionPermisos(seleccion('INICIO_VER','PRODUCTOS_VER'))).toBeNull();
  });
  it('FE-SEL-007 | Gestionar existente depende de VER; se normaliza el código y se rechaza inactivo',()=>{
    expect(baseRequerida('Gestionar productos')).toBe('PRODUCTOS_VER');
    expect(validarSeleccionPermisos(seleccion('Gestionar productos','COMPRAS_VER'))).toContain('PRODUCTOS_VER');
    expect(validarSeleccionPermisos(seleccion('Gestionar productos',' productos_ver '))).toBeNull();
    const permisos=seleccion('PRODUCTOS_VER');permisos[0].estado=false;expect(validarSeleccionPermisos(permisos)).toContain('inactivos');
  });
});
