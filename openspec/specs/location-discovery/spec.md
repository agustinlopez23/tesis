# Ubicación de búsqueda

## Requisito: radio y distancia

`GET /public/businesses` acepta conjuntamente `lat`, `lon`, `radiusKm`.
Coordenadas finitas dentro de [-90,90] y [-180,180], radio entre 0.1 y 50 km.
Un conjunto incompleto o inválido responde 400. Sin parámetros espaciales
se conserva el contrato y orden anteriores, sin distancia inventada.

Con centro se filtra por distancia geográfica en metros antes de paginar;
se devuelven hasta 20 negocios y `hasMore`. Cada resultado agrega `distanceKm`.
Orden: distancia ascendente, nombre e id. Elegibilidad, texto y rubro se
aplican conjuntamente. No se exponen coordenadas ni información de cuentas.

## Requisito: selección y alternativa manual

El visitante puede elegir un punto de referencia de Córdoba y un radio.
Los puntos se describen como referencias aproximadas, no polígonos barriales.
El permiso de geolocalización se solicita solo al pulsar “Usar mi ubicación”.
Si falla, la selección manual y la guía general siguen disponibles.
Se descarta una respuesta tardía de geolocalización si el usuario cambia a
una zona manual o quita el filtro. Cambiar centro o radio reinicia página.

## Requisito: persistencia y privacidad de la URL

Las zonas manuales y el radio se conservan en la URL. La ubicación del
dispositivo se conserva en el estado de la entrada del historial del navegador,
no en la URL compartible ni en localStorage o la base de datos. El navegador
controla la retención de su historial. Recargar conserva
la ubicación de esa entrada del historial; una URL copiada no la contiene.
Las respuestas y las solicitudes de búsqueda son no-store. No se guarda
historial de ubicaciones en la base ni se solicita ubicación automáticamente.

## Requisito: estados accesibles

Zona, radio, distancia aproximada y orden son visibles. Se puede quitar
el filtro espacial. Un vacío con radio mantiene los controles para ampliarlo.
Pendiente/error de ubicación se anuncia y no bloquea búsqueda textual.
