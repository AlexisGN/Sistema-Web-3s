/** Conserva Inter cuando está disponible sin bloquear el arranque por una solicitud externa. */
export function cargarFuenteInter(doc: Document): void {
  const link = doc.getElementById('fuente-inter') as HTMLLinkElement | null;
  if (!link || link.media === 'all') return;
  const mostrar = () => { link.media = 'all'; };
  link.addEventListener('load', mostrar, { once: true });
  if (link.sheet) mostrar();
}
