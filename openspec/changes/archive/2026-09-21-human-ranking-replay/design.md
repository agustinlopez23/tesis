# Diseño previo

HumanRankingRunner recorre todas las consultas; jamás llama a Judge. Un conjunto
de secuencias identifica la muestra; mapa vacío no solicita oráculo. Distinguir
no seleccionada, faltante/parcial, vacía, ideal cero y juzgada. No calcular nDCG
parcial ni tratar faltantes como grado cero. Validar juicios ajenos/fuera de dominio.

Acumular global, revista, búsqueda y superficie/zona; Gini de superficie sobre
la unión de exposiciones, no media de zonas. Reusar acumulador de métricas del
piloto sin cambiar su salida/contrato. Reportar muestras y denominadores explícitos.

CLI --evaluate-review requiere paquete de calibración, digest fijado, variante
de equidad y archivo explícito de pesos. Revalidar hojas actuales desde el paquete,
no confiar en un JSON de qrels editable. Paquetes vacíos/incompletos producen
reporte de pendientes, sin ejecutar comparación. Entrenamiento/reserva rechazados.
Comparar Proximity y candidata con mismos datos/juicios e historias aisladas.
Solo cambiar Eq/N/rotación dentro de la grilla prevista; mantener mérito/elegibilidad.
Exportar trazas, resúmenes por semilla, medias pareadas con peso igual y hashes.
Un ámbito sin nDCG en alguna semilla conserva null en la media, sin ocultar ausencias.

Tres unidades de revisión <400 líneas cada una: núcleo y pruebas; adaptador CLI/
exportador; pruebas de integración y documentación. No tocar frontend/API/DAL.
