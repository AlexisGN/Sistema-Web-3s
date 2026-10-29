// La URL se fija al publicar; /api corresponde al backend bajo el mismo dominio.
declare const SISTEMA3S_API_URL: string;

export const environment = {
  production: true,
  apiUrl: typeof SISTEMA3S_API_URL !== 'undefined' ? SISTEMA3S_API_URL : '/api'
};
