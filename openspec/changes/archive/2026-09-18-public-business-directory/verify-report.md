# Verificación — guía pública

Fecha: 2026-09-18. Resultado: aprobado localmente.

## Evidencia TDD observada

- Backend: seis casos de `PublicDirectoryTests` fallaron por HTTP 404 antes de
  agregar `/public/businesses`. La corrida válida del rojo usó PostGIS real;
  las corridas previas bloqueadas por permisos de EventLog no cuentan como rojo.
- Frontend: con página vacía, cuatro casos de guía fallaron por ausencia de la
  interfaz; el caso de proxy falló por 404. Después pasaron los siete casos del
  conjunto dirigido (incluyendo dos casos anteriores del proxy).
- Regresión del panel: usando el campo real `whatsApp` de la API, dos casos
  fallaron porque el teléfono quedaba vacío al editar. Corregir el nombre del
  campo en el estado y el formulario llevó la suite completa a verde.

## Resultado final

| Comprobación | Evidencia |
| --- | --- |
| Build .NET Release, sin restore | 0 errores, 0 advertencias |
| Suite .NET sobre bases temporales PostGIS | 16 aprobadas, 0 omitidas |
| `npm test` | 31 aprobadas, 5 archivos |
| `npm run lint` | Código de salida 0 |
| `npm run build` | Código de salida 0; incluye `/explorar` |
| `node App/tools/verify-onboarding.mjs` | Código de salida 0, 2026-09-18T18:45:19Z |

El recorrido integrado verifica registro, ingreso, alta/edición, guía anónima,
publicación/despublicación, consulta de segunda página, cabecera `no-store`,
ausencia de ownerId, respuesta HTML de `/explorar`, cierre y revocación. El
script inicia API/Next y elimina su base temporal y procesos al terminar.
Resultado local adicional: `.tools/onboarding-smoke-result.json` (ignorado).

Las pruebas HTTP cubren borradores, suspensión, campos públicos exactos, acceso
privado denegado, 21 negocios en dos páginas sin duplicados, desempate por id,
página vacía y cinco parámetros inválidos. Las de interfaz cubren contacto,
anterior/siguiente, vacío, recuperación ante HTTP 503 y fallo de red, y
publicación desde el panel conservando el teléfono.

## Límites

GitHub Actions quedó configurado con PostGIS, pero no se ejecutó una corrida
remota. Sin nuevas dependencias productivas ni migraciones. El orden alfabético
no constituye evaluación del ranking.

## Demostración posterior con Playwright

2026-09-18T19:27:58Z: recorrido real con Playwright 1.63.0 y Microsoft Edge,
API/Next en producción local y una base exclusiva de demostración. Se verificó
registro desde el navegador, alta en borrador, publicación/despublicación,
conservación de WhatsApp y guía en contexto anónimo con tres negocios ficticios.
El enlace de WhatsApp se inspeccionó sin abrirlo ni enviar mensajes.

Capturas inspeccionadas en escritorio 1440 px y móvil 390 px; sin desbordamiento
horizontal en móvil ni errores JavaScript de página. Capturas adicionales de
acceso y panel. Script y evidencia locales, ignorados por Git:
`.tools/browser/show-demo.mjs` y `.tools/browser/artifacts/result.json`.
La demo permanece activa hasta cerrar el navegador controlado; después el
script detiene sus servidores y elimina su base de demostración.

## Cierre del alcance ampliado — rediseño y búsqueda

La evidencia anterior corresponde a la primera guía. El estado final incorpora
búsqueda, filtros y el rediseño solicitado después: frontend 34/34, backend 16/16,
lint y build de producción aprobados. Playwright verificó registro, publicación,
despublicación, búsqueda sin tildes, filtros, recarga, recuperación ante 503,
geolocalización simulada, foco por teclado y anchos 320/390/768/1440.

No se afirma un ciclo rojo previo para todo el rediseño: sus pruebas se
adaptaron/agregaron durante la implementación. El primer intento integrado
detectó una proyección SQL no traducible; se corrigió antes del resultado verde.
Los nuevos incrementos retoman pruebas rojas antes de implementar comportamientos.

Investigación y recursos: `Doc/ux-research-2026-09-18.md`.
La demo actual usa la base local `marketplace`, con tres negocios ficticios.
Los scripts de demostración anteriores son históricos; la demo actual no
se elimina automáticamente al cerrar el navegador.
