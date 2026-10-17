# Instalación limpia de Sistema3S

Estos archivos preparan una base nueva. No se deben ejecutar sobre BD_3S de desarrollo ni sobre una instalación que contenga información del cliente. La estructura exige una base vacía. Los catálogos contienen estados, tipos, roles, permisos, categorías, marcas y unidades; no incluyen cuentas, clientes, proveedores, productos, existencias, ventas, compras, pagos ni referencia sintética.

1. Instalar SQL Server y el SDK/runtime .NET 10. Crear una nueva base `BD_3S` en la instancia del cliente con collation y configuración acordadas. Usar una cuenta de migración con capacidad DDL y otra cuenta de aplicación con los permisos necesarios para los procedimientos existentes y lectura de las vistas BI.
2. Ejecutar `01-estructura.sql` con una herramienta que entienda separadores GO, deteniéndose ante errores. Ejecutar `02-catalogos.sql` en la misma base nueva. Configurar y revisar la identidad pública, documentos legales, SMTP, URL pública, CORS y series comerciales del cliente. Los contadores de las series empiezan en cero.
3. Ejecutar `Sistema3S.Web/Database/20261012_BI.sql`. Ejecutar también `Sistema3S.Web/Database/20261013_BI_RelacionesReferencia.sql`; añade vínculos opcionales de referencia, sin datos. Estas migraciones no cargan historial. No ejecutar `tools/bi/carga-referencia-2026.sql` ni `tools/bi/carga-ampliacion-2026.sql` en la instalación del cliente.
4. Crear el primer administrador con `tools/Instalacion3S`: establecer exclusivamente para ese proceso `S3S_INSTALACION_CONEXION`, `S3S_ADMIN_CORREO` y `S3S_ADMIN_CONTRASENA`, y ejecutar `dotnet run --project tools/Instalacion3S/Instalacion3S.csproj`. La herramienta exige instalación nueva sin usuarios ni operaciones y usa el PasswordHashService existente. No hay contraseña predeterminada. Retirar las variables al finalizar.
5. Configurar ConnectionStrings y JWT mediante configuración privada o variables del servicio. JWT debe tener una clave propia de al menos 32 caracteres. Conservar los valores privados fuera del paquete y del control de versiones. Configurar SMTP y servicios documentales conforme a los contratos actuales.
6. En `Sistema3S.Client`, ejecutar `npm ci` y `npm run build -- --configuration production`. Publicar el backend y el frontend conforme al alojamiento existente. Establecer el origen real y certificado HTTPS, sin cambiar la autenticación ni el guard de permisos.
7. Ingresar como administrador, crear el personal y configurar roles desde Usuarios y roles. BI_VER permite consultar, BI_EXPORTAR exige BI_VER para exportar y BI_REFERENCIA exige BI_VER para la referencia. En una instalación limpia no hay datos de referencia. Los roles no administrativos se inicializan sin asignaciones nuevas; el administrador debe configurar sólo los accesos necesarios.
8. Comprobar catálogo vacío y KPI sin operaciones. Registrar las primeras operaciones válidas en el flujo comercial y actualizar BI; cada consulta lee información persistida. Confirmar backup, restauración y monitoreo en el servidor del cliente antes de ponerlo en servicio.

La instalación comercialmente vacía se verifica con PI-BI-006 sobre SQL de QA y seis endpoints BI. Esto no sustituye la aceptación y configuración del servidor de producción del cliente.

## Actualización de un sistema existente

Para el proyecto de desarrollo existente, aplicar únicamente las fuentes incrementales y las migraciones BI idempotentes `20261012_BI.sql` y `20261013_BI_RelacionesReferencia.sql`, después del respaldo. No usar `01-estructura.sql` ni `02-catalogos.sql`. Conservar appsettings, uploads y datos originales. El historial autorizado del desarrollo permanece exclusivamente en el esquema bi de BD_3S y no se distribuye como información comercial del cliente.

## Operación y rendimiento

Valores BI por defecto: cuatro consultas activas y cuatro en espera. Para capacidad validada del servidor, ajustar `Bi__ConsultasConcurrentes` (1–32) y `Bi__ConsultasEnEspera` (0–64) sin modificar otras políticas. La saturación produce 429 y Retry-After de dos segundos; no se reintenta automáticamente. Los reportes usan timeout SQL de 30 segundos y espera de bloqueo de cinco segundos. Las opciones y detalles usan 15 segundos. Medir nuevamente antes de aumentar la concurrencia.

Ante una reversión de código, conservar el esquema BI y la carga identificada hasta decidir su tratamiento. Restaurar toda la base desde un respaldo descartaría operaciones posteriores; no hacerlo automáticamente. Una retirada de lote requiere un procedimiento específico aprobado y verificado en una copia.
