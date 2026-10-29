import { cargarFuenteInter } from './cargar-fuente';

describe('Fuente inicial no bloqueante', () => {
  afterEach(() => document.getElementById('fuente-inter')?.remove());
  it('permite arrancar si no existe una fuente remota', () => {
    expect(() => cargarFuenteInter(document)).not.toThrow();
  });
  it('no bloquea al estar pendiente y habilita la fuente cuando llega', () => {
    const link = document.createElement('link'); link.id = 'fuente-inter'; link.media = 'print';
    document.head.append(link); cargarFuenteInter(document);
    expect(link.media).toBe('print'); link.dispatchEvent(new Event('load'));
    expect(link.media).toBe('all');
  });
  it('habilita una hoja que terminó de cargar antes de Angular', () => {
    const link = document.createElement('link'); link.id = 'fuente-inter'; link.media = 'print';
    Object.defineProperty(link, 'sheet', { value: {} }); document.head.append(link);
    cargarFuenteInter(document); expect(link.media).toBe('all');
  });
});
