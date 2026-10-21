import { init,setPlatformAPI } from 'echarts/core';
import { BiGraficoComponent } from './bi-grafico';

describe('BI gráficos de comparación en SVG',()=>{
  beforeAll(()=>setPlatformAPI({measureText:text=>({width:String(text).length*7})}));
  it('renderiza ambos periodos y una etiqueta visible para la base cero',()=>{
    const component=new BiGraficoComponent();component.tipo='columnas';component.grupo='Serie';component.mostrarValores=true;
    component.filas=[{Nombre:'Ventas',Serie:'Periodo actual',Importe:3740},{Nombre:'Ventas',Serie:'Periodo comparado',Importe:0}];
    const chart=init(null,undefined,{renderer:'svg',ssr:true,width:720,height:270});
    try{
      const options=component.opciones();chart.setOption({...options,animation:false},{notMerge:true});
      const svg=chart.renderToSVGString();
      expect(svg).toContain('Periodo actual');expect(svg).toContain('Periodo comparado');expect(svg).toContain('S/ 0');expect(svg).toContain('3,740');
      expect((options['series'] as any[]).map(s=>s.data)).toEqual([[3740],[0]]);
    }finally{chart.dispose();}
  });
  it('al cambiar los datos no conserva series comparativas anteriores',()=>{
    const component=new BiGraficoComponent();component.tipo='columnas';component.grupo='Serie';
    component.filas=[{Nombre:'Ventas',Serie:'Periodo actual',Importe:100},{Nombre:'Ventas',Serie:'Periodo comparado',Importe:50}];
    const chart=init(null,undefined,{renderer:'svg',ssr:true,width:720,height:270});
    try{
      chart.setOption({...component.opciones(),animation:false},{notMerge:true});
      component.filas=[{Nombre:'Ventas',Serie:'Periodo actual',Importe:80}];
      chart.setOption({...component.opciones(),animation:false},{notMerge:true});
      const svg=chart.renderToSVGString();expect(svg).toContain('Periodo actual');expect(svg).not.toContain('Periodo comparado');
    }finally{chart.dispose();}
  });
  it('renderiza los dos elementos con colores distintos y conserva períodos sin ventas',()=>{
    const component=new BiGraficoComponent();component.grupo='Serie';component.etiqueta='Periodo';component.mostrarValores=true;
    component.filas=[{Periodo:'2026-09-01',Serie:'A · Producto A',Importe:100},{Periodo:'2026-09-01',Serie:'B · Producto B',Importe:200},{Periodo:'2026-10-01',Serie:'A · Producto A',Importe:0},{Periodo:'2026-10-01',Serie:'B · Producto B',Importe:0}];
    const options=component.opciones();expect((options['series'] as any[]).map(s=>s.data)).toEqual([[100,0],[200,0]]);
    const chart=init(null,undefined,{renderer:'svg',ssr:true,width:720,height:270});
    try{chart.setOption({...options,animation:false},{notMerge:true});const svg=chart.renderToSVGString();expect(svg).toContain('A · Producto A');expect(svg).toContain('B · Producto B');expect(svg).toContain('#CF1827');expect(svg).toContain('#334B68');expect(svg).toContain('S/ 0');}finally{chart.dispose();}
  });
  it('separa fechas observadas y futuras con huecos y mantiene el comportamiento anterior por defecto',()=>{
    const component=new BiGraficoComponent();component.etiqueta='Fecha';component.valor='Cantidad';component.grupo='Serie';component.huecos=true;
    component.filas=[{Fecha:'2026-10-18',Serie:'Observado',Cantidad:3},{Fecha:'2026-10-20',Serie:'Experimental',Cantidad:2}];
    expect((component.opciones()['series'] as any[]).map(s=>s.data)).toEqual([[3,null],[null,2]]);
    expect((component.opciones()['xAxis'] as any).axisLabel.formatter('2026-10-20')).toBe('20/10');
    component.huecos=false;expect((component.opciones()['series'] as any[]).map(s=>s.data)).toEqual([[3,0],[0,2]]);
  });
  it('el clic identifica la serie B aunque comparta mes con A',()=>{const c=new BiGraficoComponent();c.etiqueta='Mes';c.grupo='Serie';c.filas=[{Mes:'Octubre de 2026',Serie:'A',Importe:10},{Mes:'Octubre de 2026',Serie:'B',Importe:20}];const emit=vi.spyOn(c.seleccionar,'emit');c.seleccionarPunto('Octubre de 2026','B');expect(emit).toHaveBeenCalledWith(c.filas[1]);});
  it('el clic resuelve la etiqueta de cantidad que incluye la unidad',()=>{const c=new BiGraficoComponent();c.valor='Cantidad';c.grupo='Serie';c.filas=[{Nombre:'Equipo',Unidad:'UND',Serie:'Stock',Cantidad:10}];const emit=vi.spyOn(c.seleccionar,'emit');c.seleccionarPunto('Equipo (UND)','Stock');expect(emit).toHaveBeenCalledWith(c.filas[0]);});

});
