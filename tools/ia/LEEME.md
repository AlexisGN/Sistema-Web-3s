# Inteligencia Artificial de Sistema3S

El módulo estima ventas observadas de productos físicos inventariables y prepara recomendaciones para revisión humana. No registra ventas, compras ni pagos; no modifica existencias. El BI conserva sus cálculos y sus vistas.

## Preparación del proyecto en otro equipo

Consulta [IA/LEEME.md](../../IA/LEEME.md), la guía vigente de instalación y revisión. Los modelos compartidos están en IA/modelos/referencia y el entorno se crea en IA/.venv. IA/preparar.py genera rutas relativas a Sistema3S.Web en ia.local.json; las rutas de un equipo no se comparten.

En una instalación nueva, aplica primero el esquema comercial y BI, y después Database/20261014_IA.sql en tu base autorizada. Configura la conexión SQL local. El preparador de Python no modifica bases ni registra operaciones. El historial operativo sólo se habilita después de comprobar su procedencia. Los modelos de referencia incluidos en Git no sustituyen los datos ni los snapshots SQL de tu base.

## Condiciones para interpretar una estimación

Cada producto necesita al menos 90 días de observación y cuatro días con ventas. Ese mínimo habilita una estimación, no una garantía de precisión. La comparación temporal se calcula sólo cuando existe historia suficiente anterior al corte. La ventana más reciente no participa en la selección del método. Se conservan métodos de referencia para demanda intermitente y se evalúa un modelo neuronal real.

El motor recibe cantidades diarias, número agregado de compradores, calendario y disponibilidad reconstruible desde el kardex. No recibe nombres, correos ni claves SQL. Una disponibilidad desconocida permanece desconocida; la falta de stock puede ocultar demanda que el sistema no registró. Por ello se estiman ventas observadas.

El modelo se reutiliza hasta 28 días si conserva versión, catálogo e historia compatible. Una corrección histórica obliga a reevaluarlo. La extracción se limita a 730 días por producto. No se entrena al abrir una pantalla. Las consultas leen snapshots SQL; el proceso usa bloqueo por procedencia, huella idempotente, tiempo máximo y un reintento controlado. Los fallos de IA se registran de forma independiente.

## Abastecimiento y compras

En Riesgo de inventario verificar proveedor, plazo, cobertura adicional, reserva de seguridad, mínimo, múltiplo y evidencia. Las reservas y futuras entregas se declaran sólo si se comprobaron; no se deducen de una compra ya registrada. Una entrega debe ser posterior a hoy. Cuando llega su fecha, la configuración deja de usarse hasta revisarla para evitar sumar dos veces existencias recibidas.

La necesidad combina demanda del plazo y cobertura, reserva declarada y variabilidad observada, menos la posición verificada de inventario. No extrapola más allá de 90 días. Sin información suficiente no propone una cantidad. La reserva por variabilidad es una regla de planificación basada en desviación histórica; no promete un nivel de servicio estadístico.

Revisar la recomendación y registrar una justificación. Aceptarla permite preparar una solicitud operativa. Se necesita IA_BORRADORES_CREAR y COMPRAS_CREAR además de IA_VER. Abrirla en Compras sólo prellena proveedor y detalle; el usuario debe verificar precio, comprobante y demás datos antes de registrar. La compra conserva las validaciones actuales. La vinculación posterior exige proveedor, producto y cantidad coincidentes y una compra vigente posterior a la solicitud. Un error de vinculación no revierte ni vuelve a registrar la compra.

## Seguimiento y límites

Los avisos internos se deduplican por producto y cambios materiales de prioridad, cantidad o semana de pedido. Revisar un aviso no genera nuevos avisos iguales. El aviso flotante se monta exclusivamente dentro del módulo IA, respeta la procedencia y los permisos, y detiene el sondeo al salir. No se inserta en la cabecera global. No envía correos automáticamente.

Las estimaciones originales no se sobrescriben al evaluar ventas posteriores. Las ventanas aún no cumplidas quedan sin evaluación. Si se anula posteriormente una venta, la observación se reconcilia conservando la predicción original. Confiabilidad de las estimaciones muestra una explicación comercial por producto y horizonte; el backtesting de referencia es sólo evidencia técnica.

Los importes se proyectan netos de IGV, manteniendo el precio ponderado de ventas confirmadas de los últimos 90 días. El costo ponderado de adquisición reciente es una referencia, no un COGS contable valorizado. Si falta precio o costo, no se calcula el importe correspondiente. No se afirma utilidad neta ni pérdida neta. El simulador manual de escenarios fue retirado; permanecen los pronósticos y los rangos calculados a partir de evaluaciones temporales.

## Pruebas y recuperación

Ejecutar la suite .NET y Angular actuales y `python -m pytest tools/ia/test_motor.py`. Las bases de QA están aisladas. `IaDiagnostico` es una herramienta privada fuera de HTTP para extracción anónima y ejecución de referencia; no debe exponerse como API ni ejecutarse contra el cliente.

Respaldar la base con COPY_ONLY y CHECKSUM y probar restauración antes de aplicar el DDL. Guardar las fuentes anteriores. Para deshabilitar el motor, cambiar Habilitada a false y reiniciar; el comercio sigue disponible. Para revertir fuentes, restaurar sólo los archivos respaldados. Las tablas ia son complementarias y pueden conservarse para trazabilidad; no ejecutar DROP ni restaurar una base completa sin evaluar las operaciones posteriores y autorización expresa.
