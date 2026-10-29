# Modelo de referencia compartido

Procedencia: demostración comercial identificada de Sistema3S. Corte del entrenamiento: 13 de octubre de 2026. Versión del motor: 3s-ia-3.

`model.keras` contiene la red neuronal entrenada y sus pesos. `metadata.json` incluye medias, escalas y resultados de evaluación temporal necesarios para reproducir su uso. El origen es una demostración y no certifica precisión comercial sobre operaciones auténticas.

El vector de entrada tiene 14 variables: cantidades de los días anteriores 1, 7, 14 y 28; media y desviación de 28 días; frecuencia de días con ventas; media de compradores; media de siete días; seno y coseno del día semanal; seno y coseno del calendario anual; y disponibilidad previa cuando puede reconstruirse. La normalización se ajusta sólo al pasado.

La red utiliza capas densas de 32 y 16 unidades y salida no negativa. El motor compara también media reciente, repetición semanal y SBA; cada producto puede utilizar un método distinto según la evaluación temporal. Las estimaciones de las pantallas se leen de SQL Server, no directamente de este archivo.

Para comprobar que el artefacto se puede cargar, ejecutar IA/verificar_modelo.py con el Python preparado en IA/.venv. Esa comprobación no acredita calidad predictiva futura. La disponibilidad de historial y de datos financieros continúa validándose por producto.
