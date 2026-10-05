# Campañas patrocinadas

## Preparación y revisión

El titular MUST preparar un anuncio privado de producto con plan vigente o una
promoción con foto existente de su galería, aun con plan gratuito. Título 3–160,
descripción/condiciones 3–1000 caracteres, zona Centro/Nueva Córdoba/Alberdi,
duración hasta 30 días e inicio como máximo 180 días adelante. Hasta 100 campañas
por negocio. La imagen MUST copiarse al anuncio; editar la galería no cambia el
contenido aprobado. Borrador/rechazo admiten nueva edición versionada y revisión.

Escenario: editar con versión anterior devuelve 409; otro titular recibe 404.
Guardar nuevamente exactamente el mismo intento recupera la campaña.

El administrador MUST aprobar contenido antes de registrar un pago verificado
con referencia. Cada decisión exige UUID estable, versión y motivo auditado con
actor/fecha. Aprobar no equivale a cobrar ni activa publicidad. El prototipo no
procesa pagos. El titular puede pausar; administración puede pausar/reanudar y
registrar una resolución comercial. Datos aprobados no admiten edición directa.

## Difusión separada

La contratación MUST reservar como máximo tres campañas cuyos períodos se
superponen con el período solicitado por zona, también entre distintos negocios
con pagos concurrentes. Las pausadas conservan reserva. La lista rota su orden
cada minuto; no promete impresiones ni ventas ni modifica el ranking orgánico.

Solo campañas aprobadas/pagadas, programadas, vigentes y elegibles MUST aparecer.
Se revalidan publicación del negocio y titular habilitado. Para producto se exige
publicación, disponibilidad, plan y saldo positivo o desconocido. Una interrupción
oculta el anuncio en la siguiente consulta y conserva historia/pago; no extiende
fechas ni genera compensación automática. Resolución administrativa con motivo.

Escenario: stock cero, plan vencido o pausa retiran anuncio e imagen públicos;
titular/administrador todavía pueden consultar el contenido para gestionarlo.

Las pantallas MUST identificar Patrocinado y mostrarlo separado de resultados
orgánicos, solo al elegir una zona manual. La promoción tiene destino público
con condiciones, vigencia y WhatsApp. No habilita catálogo a un negocio gratuito.

## Atribución y resultados

El contexto MUST estar protegido y vencer a los 30 minutos. Aparición visible
requiere 50 % de tarjeta durante un segundo continuo con pestaña activa. Eventos
deduplicados por campaña/contexto/tipo, excluyendo titular autenticado; retención
30 días y purga al iniciar/cada hora. No almacena coordenadas ni texto de búsqueda.

Una solicitud MUST conservar campaña solo con contexto explícito, válido y del
mismo negocio; para anuncio de producto debe incluirlo. Reintentar una solicitud
ya guardada MUST devolverla aunque campaña/contexto dejen de ser elegibles.
Las solicitudes orgánicas existentes mantienen su huella original.

Métricas privadas: eventos últimos 30 días, solicitudes históricas y ventas
vigentes reportadas vinculadas, importes separados por origen/moneda y últimos
100 cambios. Abrir WhatsApp no acredita mensaje enviado, cobro ni compra.
Rutas no-store, autorización por sesión/rol, CSRF, JSON hasta 16 KiB; eventos con
límite de actividad. Down MUST fallar si existe historia o atribución de campañas.
