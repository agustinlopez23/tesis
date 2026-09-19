# Investigación: actividad para comercios

Consulta: 19/09/2026. Fuentes primarias; análisis de patrones documentados, no acceso a paneles privados ni comparación exhaustiva del mercado.

| Referencia | Hallazgo | Aplicación en Cerca |
| --- | --- | --- |
| [Google Business Profile](https://support.google.com/business/answer/9918094) | Distingue vistas, solicitudes de indicaciones y clics de llamada/web; explica deduplicación de vistas. | Tres métricas con significado explícito; repetir/recargar no equivale a nuevo interés. |
| [Airbnb: conversión](https://www.airbnb.com/help/article/2714) | Separa impresión en búsqueda, acceso al anuncio y reserva. | Mostrar etapas distintas, sin llamar conversión a un clic de contacto. |
| [Etsy Stats](https://help.etsy.com/hc/en-us/articles/115015774268-How-to-Use-Etsy-Stats-for-Your-Shop) | Período seleccionable, gráfica según métrica, fuentes de tráfico; diferencia visitas/vistas y explica filtrado de bots. | Métricas seleccionables y evolución diaria; no afirmar personas únicas ni filtrado avanzado que no implementamos. |
| [GA4 ecommerce](https://developers.google.com/analytics/devguides/collection/ga4/ecommerce) | Eventos distintos para lista, selección y detalle; admite contexto de lista e índice. | Contexto verificable del orden y origen junto al evento. No integrar Google Analytics para esta función. |
| [IAB, visibilidad display](https://www.iab.com/news/viewability-has-arrived-what-you-need-to-know-to-see-through-this-sea-change/) | Referencia histórica: 50 % de píxeles durante un segundo para display. | Adaptar ese umbral a tarjetas orgánicas. No afirmar certificación MRC ni que es una regla universal de marketplaces. |

## Criterio de producto

El panel responde qué pasó, cuándo y qué significa. Tarjetas de resumen elegibles, gráfico diario con alternativa tabular, período y negocio explícitos, actualización manual y estados vacíos útiles. Mantener identidad Cerca, jerarquía tipográfica y espacio; evitar porcentajes verdes, comparativas o recomendaciones causales con datos insuficientes. Tus visitas autenticadas se excluyen por decisión propia de Cerca.

La instrumentación es propia y mínima. Las señales no son calidad intrínseca: usar clics crudos para mejorar posiciones reforzaría la exposición previa. La evaluación de equidad requiere un experimento separado, candidatos y juicios independientes de relevancia; este incremento no resuelve esa evaluación.

## Referencias técnicas

- [MDN: medir visibilidad](https://developer.mozilla.org/en-US/docs/Web/API/Intersection_Observer_API/Timing_element_visibility): IntersectionObserver no detecta por sí solo una pestaña oculta; combinar Page Visibility y cancelar intervalos.
- [ASP.NET Core: comprobantes con caducidad](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/limited-lifetime-payloads?view=aspnetcore-10.0): usar protección de datos con propósito y tiempo de vida, sin criptografía casera.
- Next.js: guía local instalada de Server and Client Components, leída antes de editar; navegador/estado en componentes cliente.

## Límites asumidos

Sesión de pestaña ≠ persona. Observación del cliente ≠ prueba humana. Recarga deduplicada y primera posición ≠ todas las oportunidades de un ranking. Sin volumen suficiente no hay conclusiones comerciales. No se compró ni conectó un proveedor externo.
