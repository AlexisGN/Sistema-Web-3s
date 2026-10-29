# Frontend de Sistema3S

Angular. Esta carpeta contiene la interfaz pública y el panel administrativo.

- Desarrollo: `npm ci` la primera vez y `npm start`. URL local: http://localhost:4200.
- Pruebas: `npm run test -- --watch=false`.
- Producción: usa `../Publicar.ps1` desde la raíz para compilar junto al backend y seleccionar la URL de API.
- `src/environments/environment.ts` conserva la URL de desarrollo. `environment.production.ts` usa `/api` por defecto y admite la URL definida por el publicador.
- El contenido compilado para servir está en `dist/Sistema3S.Client/browser` si ejecutas `npm run build` directamente. El publicador entrega `frontend/browser` en la salida seleccionada.

Consulta [la guía del proyecto](../README.md) para la estructura del backend, la IA y la configuración del servidor. No subas node_modules ni esta carpeta de fuentes al sitio estático.
