# Actividad del negocio

## Visibilidad y eventos

### Escenario: aparición observable
Con una tarjeta al menos 50 % dentro del viewport, la pestaña visible y un segundo continuo, registrar `impression` orgánica. No registrar al cargar la respuesta, precargar un enlace, pasar rápidamente, ocultar la pestaña o desmontar la tarjeta antes del segundo. Intervalos separados no se suman.

### Escenario: interés y contacto
Registrar `profile_view` sólo después de cargar el perfil con pestaña visible. Registrar `contact` al activar WhatsApp desde tarjeta o perfil. No registrar mensajes, conversaciones ni ventas. El envío no bloquea el enlace y sus fallos no interrumpen el recorrido.

## Integridad y contexto

### Escenario: recarga o concurrencia
UUID por pestaña con vigencia fija de 30 minutos. Deduplicar en cliente y base por sesión/negocio/tipo/superficie, incluso envíos simultáneos. No interpretar UUID como persona. Una búsqueda nueva no vuelve a contar el mismo negocio en la sesión; se conserva el primer contexto observado.

### Escenario: comprobante inválido
El servidor emite comprobantes de 30 minutos a partir de resultados publicados: negocio, posición global, `name-v1`/`distance-v1` y origen `directory`/`search`. Rechazar comprobantes alterados, caducados, UUID vacío, eventos desconocidos o combinaciones inválidas con 400. Un comprobante de perfil sin contexto de lista no autoriza una impresión.

### Escenario: atribución disponible
Al navegar explícitamente de tarjeta a perfil, mantener contexto en la pestaña hasta 5 minutos y en esa entrada del historial. Sin navegación comprobada, usar origen `unknown`. No guardar texto buscado ni coordenadas del visitante en eventos. Los comprobantes acreditan contexto servido, no autenticidad humana de la conducta.

### Escenario: versión de ranking local
La revista emite el hash SHA-256 de política y posición original en su token.
Almacenar los 64 caracteres con source=directory y posición válida no nula;
name-v1/distance-v1 siguen admitidos. Impresión, visita y contacto conservan
deduplicación y exclusión del dueño. No modifican el ledger de exposición modelada.
Downgrade de esquema se rechaza mientras existan eventos con hash; no truncar
versiones ni descartar atribución. Tras retención/purga, Down/Up conserva eventos anteriores.

### Escenario: visibilidad cambia
Antes de insertar, verificar negocio publicado, dueño no suspendido y actor autenticado distinto del dueño. Descartar evento no elegible con 204, sin crear fila. Conflictos de deduplicación también devuelven 204.

## API y permisos

- `POST /public/events`: JSON `token`, `visitId`, `kind`, `surface`. Origen confiable y `X-Requested-With: marketplace`; máximo 4096 bytes incluso sin Content-Length. 403 para origen no autorizado, 413 para exceso; 1200 solicitudes/minuto globales por instancia, sin cola, 429 al superar. Límite inicial del prototipo, no control antifraude completo.
- `GET /businesses/{id}/activity?days=7|30`: sesión obligatoria (401), propiedad comprobada (404 para ajenos); sólo 7/30 (400 para otros). Respuesta no-store con totales, fecha de actualización, zona y serie diaria. Valor por defecto 30.
- `GET /public/businesses` agrega diccionario superior `measurement` por ID; los campos de cada tarjeta se conservan. Perfil agrega `measurementToken`. No serializar metadatos privados ni identidad del dueño.

## Período y retención

Ventanas calendario de 7/30 días, hoy incluido y parcial, zona `America/Argentina/Cordoba`. Días sin eventos explícitamente en cero. Un error nunca se presenta como ceros. Excluir eventos anteriores a 30 días inmediatamente; eliminarlos al inicio y cada hora mediante tarea registrada. Si el servicio está detenido o falla la base, la purga se reintenta al siguiente arranque/ejecución. Sin agregados permanentes. Backups y logs se configuran aparte del almacenamiento de eventos.

## Panel

En Mi negocio, Ver actividad permite elegir comercio y período, seleccionar métrica, consultar evolución y tabla accesible. Mostrar actualización, estado vacío, error/reintento y definiciones. Excluir visitas propias autenticadas, explicar que clics no prueban mensajes/ventas. No inventar porcentajes, tendencias, comparaciones ni rankings a partir de datos escasos.
