# Verificación — 05/10/2026

HTTP: prueba inicial 404 donde se esperaba autorización → verde con endpoint
privado. Adaptador vacío produjo null en contrato exitoso → implementación verde.
UI import inexistente → consentimiento, borrador editable/aplicación y fallback
verdes. No hubo llamadas externas ni se configuraron credenciales reales.

Suite completa: **148/148 API/HTTP/PostGIS/adaptador**, **99/99 BLL/evaluación** y
**117/117 frontend**. Lint y compilación Next correctos, sin nuevas dependencias.
Pruebas API con proveedor sustituido validan que no se crea producto al generar;
rechazan salida demasiado larga y fallo. Pruebas del cliente HTTP verifican
Responses, store:false, esquema y hechos como único input, rechazos, respuesta
incompleta/malformada, deshabilitación sin red y tamaño máximo. Fixtures fuerzan
deshabilitación para no heredar credenciales/habilitación del entorno.

`node App/tools/verify-catalog.mjs --campaigns --serve`: flujo comercial completo
correcto, disponibilidad IA false y POST 503 a través del proxy real. Playwright
en móvil: fallback visible y producto manual guardado. Segundo recorrido con
respuesta IA simulada en navegador: conserva texto original hasta confirmar,
permite editar sugerencia, conserva precio 4200 y estado draft, luego guarda por
API real. No acredita calidad ni conectividad del modelo real. Base eliminada al
cerrar; demo intacta. Artefactos ignorados en `.tools/campaigns/`.

Durante cierre se corrigió por separado resolución comercial de campaña vencida:
prueba roja 409 → 200 y seis pruebas CampaignTests verdes. Evidencia y commit
`7a3567f`; no afecta al adaptador ni a la evaluación.

Se volvió a recoger el paquete humano de calibración existente con su hash fijado,
escribiendo en `.tools/review-calibration-status-2026-10-05-close/`. Estado incomplete:
30 consultas, 1143 pares, 2286 juicios pendientes y ninguna pareja comparada. No se
alteraron hojas, paquetes ni borrador congelable. Ranking productivo permanece
cerrado; no hay validación humana ni selección de política que declarar.
