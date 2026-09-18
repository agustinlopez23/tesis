# Guía pública

## Requisito: acceso anónimo y elegibilidad

`GET /public/businesses?page=1` devuelve `items` y `hasMore`, sin sesión.
Cada elemento contiene únicamente id, nombre, descripción, dirección, rubro y
WhatsApp del negocio. Se excluyen borradores y responsables suspendidos.

- Publicar un negocio lo incorpora; volver a borrador lo retira en la siguiente consulta.
- Suspender al responsable oculta sus negocios publicados.
- Los endpoints privados siguen requiriendo sesión y propiedad.

## Requisito: consulta limitada

Se devuelven hasta 20 elementos ordenados por nombre e id como desempate estable.
La página predeterminada es 1; valores no enteros o fuera de 1..10000 dan HTTP 400.
Una página sin resultados devuelve lista vacía y `hasMore=false`.
La paginación no representa ranking ni exposición equitativa.

## Requisito: recorrido web

Desde la portada se accede a `/explorar`. La guía muestra carga, tarjetas, páginas
anterior/siguiente, vacío o un error con reintento. El enlace `wa.me` usa el número
internacional sin `+`; abrirlo no registra un pedido, mensaje enviado ni venta.
Una API caída no se presenta como lista vacía y las respuestas no se cachean.

## Requisito: búsqueda y rubro

`q` (hasta 150 caracteres) busca en nombre, descripción y dirección, sin
distinguir mayúsculas ni tildes de vocales. `category` (hasta 100 caracteres)
filtra por el nombre exacto del rubro. Ambos filtros se aplican antes de paginar.
Los comodines se interpretan literalmente; los límites inválidos devuelven 400.
La URL conserva búsqueda, rubro y página; recargar y navegar por historial
restaura los controles. Las etiquetas activas permiten eliminar filtros.

## Requisito: recorridos separados

`/` y `/explorar` presentan la guía. `/mi-negocio` contiene acceso y gestión.
`/estado` conserva el diagnóstico técnico. El panel identifica borradores y
publicados, confirma guardados y no simula un cierre de sesión si falla.
La ubicación del dispositivo se solicita únicamente con una acción explícita;
se conserva carga manual. La portada editorial se identifica como ilustrativa.
No se simulan fotos, distancias, calificaciones ni horarios de cada comercio.

## Requisito: accesibilidad y adaptación

Se puede saltar al contenido con teclado. Los filtros exponen su estado,
los campos tienen etiquetas y los mensajes informan carga, error y éxito.
La guía y el panel se adaptan a anchos de 320 a 1440 px sin desbordamiento.
