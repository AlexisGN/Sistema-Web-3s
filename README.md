# Sistema3S

Aplicación .NET 10, Angular, SQL Server y motor predictivo Python 3.11. El backend principal contiene únicamente la API y los archivos de IA que utiliza.

```text
Sistema3S/
  backend/
    Sistema3S.Web/   API, servicios, configuración local, migraciones y archivos públicos
    IA/              motor, compatibilidad, modelos y entorno Python del equipo
  frontend/          interfaz Angular
  Sistema3S.slnx     solución de Visual Studio para ejecutar la API
```

La API ejecuta la IA automáticamente según su programación. No se inicia otro servidor de IA. `.git`, `.gitignore` y `.gitattributes` conservan el repositorio y no forman parte del despliegue.

## Ejecutar en desarrollo

Abre `Sistema3S.slnx` en Visual Studio y ejecuta el perfil habitual de Sistema3S.Web. Desde terminal:

```powershell
dotnet run --project backend/Sistema3S.Web/Sistema3S.Web.csproj --launch-profile https
cd frontend
npm start
```

La conexión privada, los modelos, el entorno Python y los archivos subidos actuales se conservan. Las pruebas y utilidades anteriores se guardaron fuera del proyecto en `../Sistema3S_Desarrollo_20261028`. Su solución `Sistema3S.Desarrollo.slnx` permite continuar el mantenimiento sin incluir las pruebas en el backend que se despliega.

## Publicar el backend sin scripts propios

En Visual Studio: clic derecho en Sistema3S.Web → Publicar → Carpeta. También puedes usar la herramienta estándar de .NET desde esta raíz, con una carpeta de salida vacía fuera del proyecto:

```powershell
dotnet publish backend/Sistema3S.Web/Sistema3S.Web.csproj -c Release -o D:/Publicacion/Sistema3S-backend
```

La salida incluye `Sistema3S.Web.dll`, sus dependencias, `wwwroot`, `Database` y **una carpeta IA dentro de la misma publicación**. Esa carpeta IA lleva el motor actual, el motor compatible y los modelos existentes. Se copia la publicación completa al servidor. No se necesita Publicar.ps1 ni copiar tools por separado.

No copies simplemente las fuentes del backend: .NET requiere compilarlas. Las carpetas bin/obj se regeneran al compilar y no se suben por separado. La publicación no copia pruebas, herramientas de desarrollo, entornos Python locales ni configuraciones privadas. `appsettings.Publicacion.json` aporta una plantilla sin credenciales y se publica como `appsettings.json`; los appsettings locales e ia.local.json no salen del equipo. No sobrescribas archivos subidos o modelos más recientes de un servidor activo sin respaldarlos y comprobar qué versión vas a actualizar. Al migrar una base existente, sus modelos y archivos deben corresponder a esa misma base.

En el servidor instala el runtime ASP.NET Core .NET 10 y Python 3.11. Crea su entorno Python dentro de IA y configura sus dependencias; no se instala automáticamente ni se reutiliza el entorno de Windows en Linux. Por ejemplo, desde la carpeta publicada, en Windows:

```powershell
py -3.11 -m venv IA/.venv
IA/.venv/Scripts/python.exe -m pip install -r IA/requirements-windows.txt
```

En Linux: `python3.11 -m venv IA/.venv` y `IA/.venv/bin/python -m pip install -r IA/requirements.txt`. El motor requiere TensorFlow2.15.1, Keras2.15.0 y NumPy1.26.4; conserva esos contratos al instalar.

Configura `ConnectionStrings__BD_3S`, `Jwt__Key` (al menos32caracteres), `Jwt__Issuer`, `Jwt__Audience`, `ClienteWeb__UrlPublica`, los orígenes `Cors__AllowedOrigins__0` y los servicios externos que utilices. `Ia__Habilitada` e `Ia__HistorialOperativoVerificado` deben reflejar el historial autorizado del servidor. Por defecto la publicación resuelve Python en IA/.venv y los modelos en IA/modelos; puedes declarar `Ia__Python` o `Ia__DatosPrivados` para otras rutas privadas. Los modelos y datos de IA quedan fuera de wwwroot. El usuario del servicio necesita permisos sobre modelos y archivos de trabajo de IA.

El directorio de trabajo del servicio debe ser la carpeta publicada; ejecutar allí `dotnet Sistema3S.Web.dll`. Configura HTTPS y el proceso residente en el alojamiento. La base de datos se instala o migra por separado, conservando sus registros; publicar archivos no recrea ni cambia bases.

## Publicar el frontend

Desde frontend, `npm ci` si aún no están las dependencias y `npm run build -- --configuration production`. Servir el contenido de `dist/Sistema3S.Client/browser`, con fallback de rutas a index.html. La configuración de producción usa `/api` en el mismo dominio; dirige API y archivos del backend al servidor correspondiente. Para una API en otro dominio, cambia el valor de respaldo `/api` de `apiUrl` en `src/environments/environment.production.ts` por su URL HTTPS completa terminada en `/api` antes de compilar, y configura el origen permitido en el backend. `src/environments/environment.ts` conserva la dirección de desarrollo; no se publican node_modules ni fuentes.

## Rendimiento del catálogo público

Las respuestas anónimas de api/publico tienen caché15s y coalescencia de lecturas; una edición normal de productos/servicios invalida la generación. `Publico__CacheSegundos` permite0–60;0desactiva. Cambios SQL externos u otras instancias pueden tardar hasta ese tiempo, sin invalidación distribuida prometida. Se conservan permisos, filtros y validación de operaciones. La compresión sólo aplica al JSON público. Para medir capacidad utiliza Release y frontend de producción: ngserve es para desarrollo, y JMeterHTTP no ejecuta Angular ni mide pintura visual.
