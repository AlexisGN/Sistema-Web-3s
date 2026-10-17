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
});
