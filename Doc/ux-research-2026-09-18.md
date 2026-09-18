# Investigación y rediseño de la guía local

Fecha: 2026-09-18. Alcance: investigación documental, revisión de la interfaz
existente, implementación y verificación. No se realizaron entrevistas ni
pruebas con participantes; no se atribuyen mejoras de conversión medidas.

## Diagnóstico

La portada anterior priorizaba registro y disponibilidad de la API. No ofrecía
una entrada orientada a la persona que busca un negocio. La guía tenía tarjetas
con textos de similar peso, sin búsqueda ni filtros. El panel no confirmaba
guardados ni distinguía visualmente borradores y publicaciones.

## Fuentes y decisiones

| Fuente consultada | Hallazgo aplicable | Cambio implementado |
| --- | --- | --- |
| [NN/g: The State of Ecommerce Search](https://www.nngroup.com/articles/state-ecommerce-search/) | La búsqueda debe ayudar a encontrar oferta, con resultados pertinentes y formas de acotarlos. | Búsqueda por nombre, descripción y dirección, combinada con rubro. Filtrado en servidor antes de paginar. |
| [Baymard: Display Applied Filters in an Overview](https://baymard.com/research-articles/how-to-design-applied-filters) | La falta de una vista de filtros activos desorienta y dificulta refinar resultados. | Filtros seleccionados, etiquetas removibles, limpiar filtros y búsqueda conservada en URL e historial. |
| [Airbnb: actividades en Córdoba](https://www.airbnb.com/cordoba-argentina/things-to-do) | Referencia de descubrimiento local centrado en propuestas concretas. | Separación entre explorar y administrar, y jerarquía consistente de nombre, rubro, dirección y contacto. No se incorporan calificaciones sin datos. |

La adaptación de estos principios a una guía de comercios es una decisión de
diseño, no una conclusión experimental sobre este producto.

## Dirección visual

**Cerca** es un nombre de trabajo para la propuesta visual, no una decisión
comercial de marca. Paleta de papel claro, verde oscuro y terracota; serif
editorial en titulares y tipografía de sistema para controles. Espaciado,
bordes, estados de foco y botones compartidos entre guía y panel.

La portada usa una escena ilustrativa, identificada como tal. Los comercios
usan portadas tipográficas por categoría; no se simulan fotografías propias,
distancias, horarios, reseñas ni posiciones de relevancia.

## Flujos

- `/` y `/explorar`: guía pública, búsqueda, rubros, estado vacío, reintento,
  paginación y contacto directo por WhatsApp.
- `/mi-negocio`: registro/ingreso, lista de negocios con estado, formulario
  dividido en identidad, contacto/ubicación y publicación, confirmación de
  guardado y cancelación de edición.
- `/estado`: diagnóstico de conectividad conservado fuera del flujo principal.

La búsqueda ignora mayúsculas y tildes de vocales. Los comodines SQL se tratan
como texto literal. Se mantiene la exclusión de borradores y responsables
suspendidos, el orden alfabético y el límite de 20 resultados por página.

La ubicación del dispositivo se solicita solo al pulsar el botón, con una
indicación de usarla cuando se está en el local. Se conservan coordenadas
manuales como alternativa. Los campos obligatorios ocultos se revelan al
validar. No hay geocodificación automática de direcciones en este corte.

## Accesibilidad y estados

- Enlace para saltar al contenido, navegación y regiones etiquetadas.
- Foco visible, botones de rubro con `aria-pressed`, resultados anunciados y
  confirmaciones con `role=status`.
- Errores de conexión diferenciados de búsquedas sin resultados.
- Carga visual, botones pendientes deshabilitados y respeto a movimiento reducido.
- Formularios con autocompletado, mostrar contraseña y ayuda de formato.
- Descripciones extensas expandibles y diseño adaptable sin ancho fijo en móvil.

## Imagen editorial

Generada con la herramienta integrada `imagegen`. Recurso final:
[`neighborhood-editorial.webp`](../App/frontend/public/images/neighborhood-editorial.webp),
1100 × 733, 190.460 bytes. Original conservado por la herramienta;
la app usa el archivo optimizado dentro del repositorio.

Prompt utilizado:

> Create one polished editorial photograph for a local independent-business discovery website in Córdoba, Argentina. Landscape 3:2 composition. A warm, quiet street-level view of a charming independent corner café and neighborhood grocery with muted terracotta stucco walls, tall dark olive green wooden doors and shop windows, traditional pale fabric awning, two small café tables, potted native plants, subtle dappled afternoon sunlight. Authentic low-rise Argentine neighborhood feel, no European landmarks, no identifiable real business, no readable signs or text, no logos, no people in foreground. Thoughtful architectural travel magazine photography, real textures, subtle film grain, warm but restrained color grading, sophisticated natural lighting. The scene should feel lived-in and local rather than luxury. Frame facade tightly, eye level, overall earthy burnt orange, cream, dark green palette. This is an illustrative editorial scene, not a photo of a listed business. Output only the image.

## Verificación

- Vitest: 34 pruebas aprobadas; interfaz, filtros, restauración desde URL/historial, errores y sesión.
- 16 pruebas de integración .NET/PostgreSQL aprobadas: publicación, suspensión, privacidad,
  paginación, búsqueda anterior a paginado, tildes y escape de comodines.
- ESLint y compilación de producción con TypeScript.
- MCP Playwright: registro, alta y edición, publicación/despublicación,
  búsqueda por descripción, rubros, sin resultados, limpieza, recarga,
  recuperación ante un HTTP 503 simulado y geolocalización simulada.
- Revisión visual en escritorio y móvil; medición de desbordamiento con
  anchos de 320, 390, 768 y 1440 píxeles.

Los tres comercios de la demo son ficticios. Los enlaces de WhatsApp se revisan
sin enviar mensajes. Capturas locales en `.playwright-mcp/cerca-*.png`.

## Pendiente de validar con personas

Comprensión de la propuesta y del nombre de trabajo, tiempo para encontrar un
negocio, recuperación de búsquedas vacías y comprensión del cambio entre
borrador/publicado. Fotografías por comercio, mapa y ranking de exposición
equitativa requieren sus propios datos y flujos; este rediseño no los simula.
