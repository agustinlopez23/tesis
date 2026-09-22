# Verificación documental · 21/09/2026

Protocolo, rúbrica, plan JSON, cuatro CSV y contrato de integración preparados.
Estado de criterios: propuestos; consulta al usuario pendiente al escribir esta
evidencia. No se ejecutó evaluación final ni se fabricaron juicios humanos.

Actualización posterior: el usuario aceptó los criterios principales para preparar
el experimento. Acuerdo en `Doc/evaluation/business-ranking-final/plan.json`;
importador implementado en el incremento `2026-09-21-human-ranking-review`.

Comprobaciones con `.tools/check-ranking-protocol.py`: JSON válido, semillas
disjuntas, muestra 30/60 consistente, 18 configuraciones de calibración, límites
numéricos consistentes, CSV de solo encabezados sin campos duplicados y enlaces
locales existentes. `git diff --check` sin errores; advertencias previas CRLF.

Revisión contra código: BusinessRepository recorta candidatos antes de BLL,
BusinessProfile carece de primera publicación, tokens de actividad no contienen
snapshot y ledger del piloto es en memoria. Se documentaron esas brechas.

Dos unidades de revisión documental menores a 400 líneas; no se crearon PRs.
Sin cambios de runtime, dependencias ni migraciones: no se repitieron pruebas
backend/frontend ni Playwright. Evidencia ejecutable previa: 31 pruebas de
laboratorio aprobadas, registrada en las ablaciones. Sin commit/push/despliegue.
