# Acceso HTTP verificable a listas persistentes

Completar creación y lectura HTTP de snapshots con cursores protegidos y errores
explícitos. Depende de la persistencia terminada en `02af9d6`. La previsualización
solo podrá habilitarse explícitamente en Development/Testing; producción sigue
cerrada porque aún no existe una política evaluada. No modifica la guía ni la UI.

Incluye validación estricta de entrada, cuerpo acotado, origen confiable, reloj de
servidor, reintentos, estados 400/409/410, no-store y límites básicos de tasa.
Quedan fuera la aprobación metodológica, activación productiva, pruebas de carga,
límites de candidatos/memoria y frontend. Sin push ni PR en este incremento.

Éxito: pruebas HTTP/PostGIS reales comprueban 20/20/5, repetición sin exposición
adicional, vencimiento/manipulación, filtros incompatibles y previsualización
propia con presupuesto cero. Se preservan los paquetes de evaluación.
