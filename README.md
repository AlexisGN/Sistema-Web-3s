# Sistema3S

Proyecto ASP.NET Core, Angular y SQL Server. Configura conexión, JWT y servicios por equipo; no publiques credenciales.

1. Prepara Python3.11 una vez: `py -3.11 IA/preparar.py` en Windows o `python3.11 IA/preparar.py` en Linux. Dependencias fijadas: TensorFlow/Keras2.15.
2. Instala la base mediante las instrucciones funcionales de [tools/instalacion/LEEME.md](tools/instalacion/LEEME.md), sus migraciones BI/IA y la migración aditiva `20261016_IA_MotorAdministrado.sql`.
3. Ejecuta el backend y Angular como siempre. La inferencia y reentrenamiento los administra el backend; no se ejecutan dentro de solicitudes HTTP.

Parámetros de referencia sin secretos: [tools/ia/ia.local.ejemplo.json](tools/ia/ia.local.ejemplo.json). El repositorio incluye una referencia técnica y el motor; un modelo de producción sólo se activa después de aprobar WAPE≤15% por producto y horizonte. Los artefactos privados y la BD no viajan automáticamente en Git. Si no hay candidato aprobado, se conserva el historial y se limita la recomendación predictiva de compra.

Para empaquetar el backend y motor juntos: `python IA/publicar.py --destino RUTA_FUERA_DEL_REPOSITORIO`. Configura los secretos del servidor, su entorno Python y el proceso residente. Angular conserva su despliegue habitual.
