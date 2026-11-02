# Diccionario de datos del modelo implementado

Generado por `extraer-modelo.mjs` desde el esquema de la base de datos migrada. Los identificadores están en inglés, como en el código; las descripciones, en español. «Clave» indica clave primaria (PK), foránea (FK) o única (UK). Las restricciones de control (CHECK) se listan al pie de cada tabla.

Tablas excluidas por no pertenecer al dominio: `__EFMigrationsHistory` (historial de migraciones de Entity Framework); `spatial_ref_sys` (catálogo de sistemas de referencia de PostGIS); `data_protection_keys` (claves de ASP.NET Core Data Protection (infraestructura)).

## Negocios, identidad y planes

### user_accounts: Cuenta de usuario con credenciales y estado de acceso

| Campo | Tipo | Longitud | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | uuid | 16 bytes | No | PK | Identificador interno de la cuenta. |
| email | character varying(254) | 254 caracteres | No | UK | Correo electrónico de acceso, único en la plataforma. |
| display_name | character varying(120) | 120 caracteres | No | — | Nombre visible del usuario. |
| password_hash | text | Variable | No | — | Hash de la contraseña generado por el componente de identidad del framework; no es la contraseña. |
| is_suspended | boolean | 1 byte | No | — | Indica si el administrador suspendió la cuenta. |
| created_at | timestamp with time zone | 8 bytes | No | — | Fecha y hora de alta en UTC. |
| failed_login_count | integer | 4 bytes | No | — | Intentos fallidos consecutivos de inicio de sesión. |
| locked_until | timestamp with time zone | 8 bytes | Sí | — | Fin del bloqueo temporal tras cinco intentos fallidos; nulo si no hay bloqueo. |

### roles: Rol de la plataforma

| Campo | Tipo | Longitud | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | smallint | 2 bytes | No | PK | Identificador del rol. |
| name | character varying(50) | 50 caracteres | No | UK | Nombre único del rol (Owner o Admin). |

### user_account_roles: Asignación de roles a cuentas

| Campo | Tipo | Longitud | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| user_id | uuid | 16 bytes | No | PK, FK | Cuenta a la que se asigna el rol. |
| role_id | smallint | 2 bytes | No | PK, FK | Rol asignado. |

Claves foráneas: `role_id` → `roles`; `user_id` → `user_accounts`.

### user_sessions: Sesión autenticada y revocable

| Campo | Tipo | Longitud | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | uuid | 16 bytes | No | PK | Identificador interno de la sesión. |
| user_id | uuid | 16 bytes | No | FK | Cuenta autenticada. |
| token_hash | character varying(64) | 64 caracteres | No | UK | Hash SHA-256 del token de sesión enviado en la cookie. |
| expires_at | timestamp with time zone | 8 bytes | No | — | Vencimiento de la sesión. |
| revoked_at | timestamp with time zone | 8 bytes | Sí | — | Momento del cierre de sesión; nulo mientras está activa. |
| created_at | timestamp with time zone | 8 bytes | No | — | Fecha y hora de inicio de la sesión. |

Claves foráneas: `user_id` → `user_accounts`.

### businesses: Negocio con su única ubicación

| Campo | Tipo | Longitud | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | uuid | 16 bytes | No | PK | Identificador interno del negocio. |
| owner_id | uuid | 16 bytes | No | FK | Cuenta responsable del negocio. |
| category_id | smallint | 2 bytes | No | FK | Rubro al que pertenece el negocio. |
| name | character varying(160) | 160 caracteres | No | — | Nombre visible del comercio. |
| description | character varying(2000) | 2000 caracteres | No | — | Descripción de la oferta del negocio. |
| address | character varying(250) | 250 caracteres | No | — | Dirección textual del local. |
| whatsapp | character varying(16) | 16 caracteres | No | — | Número de WhatsApp validado del negocio. |
| location | geography(Point,4326) | 1 punto, 2 coordenadas | No | — | Ubicación geográfica del local (latitud y longitud, SRID 4326). |
| status | character varying(20) | 20 caracteres | No | — | Estado del perfil: borrador o publicado. |
| created_at | timestamp with time zone | 8 bytes | No | — | Fecha y hora de creación en UTC. |
| updated_at | timestamp with time zone | 8 bytes | No | — | Fecha y hora de la última modificación. |
| first_published_at | timestamp with time zone | 8 bytes | Sí | — | Primera publicación del perfil, mantenida por la base de datos. |
| publication_history_unknown | boolean | 1 byte | No | — | Indica que la fecha de primera publicación es desconocida. |
| is_suspended | boolean | 1 byte | No | — | Indica si el administrador suspendió el negocio. |

Claves foráneas: `category_id` → `business_categories`; `owner_id` → `user_accounts`.

Restricciones: `CHECK (((status)::text = ANY ((ARRAY['draft'::character varying, 'published'::character varying])::text[])))`.

### business_categories: Rubro de negocio administrado por la plataforma

| Campo | Tipo | Longitud | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | smallint | 2 bytes | No | PK | Identificador del rubro. |
| name | character varying(80) | 80 caracteres | No | UK | Nombre único del rubro. |
| slug | character varying(80) | 80 caracteres | No | UK | Identificador textual único del rubro para direcciones web. |
| is_active | boolean | 1 byte | No | — | Indica si el rubro admite nuevos negocios. |

### business_photos: Fotografía del perfil del negocio

| Campo | Tipo | Longitud | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| business_id | uuid | 16 bytes | No | PK, FK | Negocio al que pertenece la fotografía. |
| slot | integer | 4 bytes | No | PK | Posición de la fotografía, de 1 a 5. |
| caption | character varying(160) | 160 caracteres | No | — | Texto descriptivo de la fotografía. |
| width | integer | 4 bytes | No | — | Ancho en píxeles tras el procesamiento. |
| height | integer | 4 bytes | No | — | Alto en píxeles tras el procesamiento. |
| content | bytea | hasta 1 MB | No | — | Imagen vuelta a codificar y sin metadatos. |

Claves foráneas: `business_id` → `businesses`.

Restricciones: `CHECK ((octet_length(content) <= 1048576))`; `CHECK (((slot >= 1) AND (slot <= 5)))`.

### business_subscriptions: Período del plan Impulso otorgado a un negocio

| Campo | Tipo | Longitud | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | uuid | 16 bytes | No | PK | Identificador interno del período. |
| business_id | uuid | 16 bytes | No | FK | Negocio que recibe el plan Impulso. |
| starts_at | timestamp with time zone | 8 bytes | No | — | Inicio del período habilitado. |
| ends_at | timestamp with time zone | 8 bytes | No | — | Fin del período; posterior al inicio. |
| granted_by | uuid | 16 bytes | No | FK | Administrador que otorgó el período. |
| granted_at | timestamp with time zone | 8 bytes | No | — | Momento del otorgamiento. |
| reason | character varying(500) | 500 caracteres | No | — | Motivo del otorgamiento. |
| revoked_at | timestamp with time zone | 8 bytes | Sí | — | Momento de la revocación; nulo si sigue vigente. |
| revoked_by | uuid | 16 bytes | Sí | FK | Administrador que revocó el período. |
| revocation_reason | character varying(500) | 500 caracteres | Sí | — | Motivo de la revocación. |

Claves foráneas: `business_id` → `businesses`; `granted_by` → `user_accounts`; `revoked_by` → `user_accounts`.

Restricciones: `CHECK ((ends_at > starts_at))`; `CHECK ((((revoked_at IS NULL) AND (revoked_by IS NULL) AND (revocation_reason IS NULL)) OR ((revoked_at IS NOT NULL) AND (revoked_by IS NOT NULL) AND (revocation_reason IS NOT NULL))))`.

### admin_actions: Auditoría de acciones del administrador

| Campo | Tipo | Longitud | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | bigint | 8 bytes | No | PK | Número secuencial de la acción. |
| actor_id | uuid | 16 bytes | No | FK | Administrador que realizó la acción. |
| target_type | character varying(20) | 20 caracteres | No | — | Tipo de objeto afectado: cuenta, negocio, producto o rubro. |
| target_id | character varying(64) | 64 caracteres | No | — | Identificador del objeto afectado. |
| target_label | character varying(254) | 254 caracteres | No | — | Nombre del objeto al momento de la acción. |
| action | character varying(20) | 20 caracteres | No | — | Acción realizada, por ejemplo suspender o reactivar. |
| reason | character varying(500) | 500 caracteres | Sí | — | Motivo informado por el administrador. |
| created_at | timestamp with time zone | 8 bytes | No | — | Fecha y hora de la acción. |

Claves foráneas: `actor_id` → `user_accounts`.

Restricciones: `CHECK (((target_type)::text = ANY ((ARRAY['account'::character varying, 'business'::character varying, 'product'::character varying, 'category'::character varying])::text[])))`.

## Catálogo, solicitudes, ventas e inventario

### catalog_products: Producto del catálogo de un negocio

| Campo | Tipo | Longitud | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | uuid | 16 bytes | No | PK | Identificador interno del producto. |
| business_id | uuid | 16 bytes | No | FK | Negocio propietario del producto. |
| name | character varying(160) | 160 caracteres | No | — | Nombre visible del producto. |
| description | character varying(2000) | 2000 caracteres | No | — | Descripción del producto. |
| category | character varying(80) | 80 caracteres | No | — | Categoría textual del producto. |
| price | numeric(12,2) | 12 dígitos, 2 decimales | No | — | Precio unitario, mayor o igual que cero. |
| currency | character varying(3) | 3 caracteres | No | — | Moneda del precio: ARS o USD. |
| available | boolean | 1 byte | No | — | Indica si el negocio lo ofrece en este momento. |
| status | character varying(20) | 20 caracteres | No | — | Estado: borrador, publicado o archivado. |
| version | integer | 4 bytes | No | — | Versión para el control de concurrencia optimista. |
| created_at | timestamp with time zone | 8 bytes | No | — | Fecha y hora de creación. |
| updated_at | timestamp with time zone | 8 bytes | No | — | Fecha y hora de la última modificación. |
| is_suspended | boolean | 1 byte | No | — | Indica si el administrador suspendió el producto. |

Claves foráneas: `business_id` → `businesses`.

Restricciones: `CHECK (((currency)::text = ANY ((ARRAY['ARS'::character varying, 'USD'::character varying])::text[])))`; `CHECK ((price >= (0)::numeric))`; `CHECK (((status)::text = ANY ((ARRAY['draft'::character varying, 'published'::character varying, 'archived'::character varying])::text[])))`; `CHECK ((version > 0))`.

### product_photos: Fotografía de un producto

| Campo | Tipo | Longitud | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| product_id | uuid | 16 bytes | No | PK, FK | Producto al que corresponde la fotografía. |
| content | bytea | hasta 1 MB | No | — | Imagen vuelta a codificar y sin metadatos. |

Claves foráneas: `product_id` → `catalog_products`.

Restricciones: `CHECK (((octet_length(content) >= 1) AND (octet_length(content) <= 1048576)))`.

### inventory_balances: Saldo de stock de un producto

| Campo | Tipo | Longitud | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| product_id | uuid | 16 bytes | No | PK, FK | Producto cuyo saldo se registra. |
| quantity | integer | 4 bytes | No | — | Unidades disponibles, entre 0 y 1.000.000. |
| version | integer | 4 bytes | No | — | Versión para el control de concurrencia optimista. |

Claves foráneas: `product_id` → `catalog_products`.

Restricciones: `CHECK ((version > 0))`; `CHECK (((quantity >= 0) AND (quantity <= 1000000)))`.

### inventory_movements: Movimiento de stock con su saldo resultante

| Campo | Tipo | Longitud | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| operation_id | uuid | 16 bytes | No | PK | Identificador de la operación, que evita registrarla dos veces. |
| product_id | uuid | 16 bytes | No | PK, FK | Producto afectado. |
| business_id | uuid | 16 bytes | No | FK | Negocio propietario del producto. |
| actor_id | uuid | 16 bytes | No | FK | Cuenta que registró el movimiento. |
| sale_id | uuid | 16 bytes | Sí | FK | Venta que originó el movimiento, si corresponde. |
| delta | integer | 4 bytes | No | — | Variación de unidades, positiva o negativa. |
| balance | integer | 4 bytes | No | — | Saldo resultante después del movimiento. |
| kind | character varying(20) | 20 caracteres | No | — | Tipo: ajuste, venta o devolución. |
| reason | character varying(300) | 300 caracteres | No | — | Motivo del movimiento. |
| request_hash | character varying(64) | 64 caracteres | No | — | Hash de la solicitud, que detecta reintentos con datos distintos. |
| created_at | timestamp with time zone | 8 bytes | No | — | Fecha y hora del movimiento. |

Claves foráneas: `business_id` → `businesses`; `product_id` → `catalog_products`; `sale_id` → `sales`; `actor_id` → `user_accounts`.

Restricciones: `CHECK (((balance >= 0) AND (balance <= 1000000)))`; `CHECK (((kind)::text = ANY ((ARRAY['adjustment'::character varying, 'sale'::character varying, 'return'::character varying])::text[])))`.

### customer_orders: Solicitud de compra de un consumidor

| Campo | Tipo | Longitud | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | uuid | 16 bytes | No | PK | Identificador interno de la solicitud. |
| business_id | uuid | 16 bytes | No | FK | Negocio que recibe la solicitud. |
| reference | character varying(15) | 15 caracteres | No | UK | Referencia corta y única que se comparte por WhatsApp. |
| customer_name | character varying(80) | 80 caracteres | No | — | Nombre informado por el consumidor. |
| business_name | character varying(160) | 160 caracteres | No | — | Nombre del negocio al momento de la solicitud. |
| whatsapp | character varying(16) | 16 caracteres | No | — | Número de WhatsApp del negocio al momento de la solicitud. |
| access_key_hash | character varying(64) | 64 caracteres | No | — | Hash de la clave privada con la que el consumidor consulta su solicitud. |
| request_hash | character varying(64) | 64 caracteres | No | — | Hash de la solicitud, que detecta reintentos con datos distintos. |
| access_expires_at | timestamp with time zone | 8 bytes | No | — | Vencimiento del acceso del consumidor a la solicitud. |
| created_at | timestamp with time zone | 8 bytes | No | — | Fecha y hora de creación. |
| updated_at | timestamp with time zone | 8 bytes | No | — | Fecha y hora de la última modificación. |
| contact_received_at | timestamp with time zone | 8 bytes | Sí | — | Momento en que el negocio confirmó el contacto por WhatsApp. |
| status | character varying(20) | 20 caracteres | No | — | Estado: pendiente, en atención, cancelada o convertida en venta. |
| source | character varying(20) | 20 caracteres | No | — | Origen: catálogo o campaña. |
| version | integer | 4 bytes | No | — | Versión para el control de concurrencia optimista. |
| campaign_id | uuid | 16 bytes | Sí | FK | Campaña que originó la solicitud; obligatoria solo si el origen es campaña. |

Claves foráneas: `business_id` → `businesses`; `campaign_id` → `campaigns`.

Restricciones: `CHECK ((((access_key_hash)::text ~ '^[0-9a-f]{64}$'::text) AND ((request_hash)::text ~ '^[0-9a-f]{64}$'::text)))`; `CHECK ((version > 0))`; `CHECK (((status)::text = ANY ((ARRAY['pending'::character varying, 'in_attention'::character varying, 'cancelled'::character varying, 'converted'::character varying])::text[])))`; `CHECK (((((source)::text = 'catalog'::text) AND (campaign_id IS NULL)) OR (((source)::text = 'campaign'::text) AND (campaign_id IS NOT NULL))))`.

### order_lines: Producto incluido en una solicitud

| Campo | Tipo | Longitud | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| order_id | uuid | 16 bytes | No | PK, FK | Solicitud a la que pertenece la línea. |
| product_id | uuid | 16 bytes | No | PK, FK | Producto solicitado. |
| name | character varying(160) | 160 caracteres | No | — | Nombre del producto al momento de la solicitud. |
| unit_price | numeric(12,2) | 12 dígitos, 2 decimales | No | — | Precio unitario al momento de la solicitud. |
| currency | character varying(3) | 3 caracteres | No | — | Moneda del precio: ARS o USD. |
| quantity | integer | 4 bytes | No | — | Unidades solicitadas, entre 1 y 99. |
| product_version | integer | 4 bytes | No | — | Versión del producto que vio el consumidor. |

Claves foráneas: `product_id` → `catalog_products`; `order_id` → `customer_orders`.

Restricciones: `CHECK (((currency)::text = ANY ((ARRAY['ARS'::character varying, 'USD'::character varying])::text[])))`; `CHECK ((unit_price >= (0)::numeric))`; `CHECK (((quantity >= 1) AND (quantity <= 99)))`.

### order_changes: Historial de estados de una solicitud

| Campo | Tipo | Longitud | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| order_id | uuid | 16 bytes | No | PK, FK | Solicitud modificada. |
| version | integer | 4 bytes | No | PK | Versión de la solicitud que produjo el cambio. |
| actor_id | uuid | 16 bytes | No | FK | Cuenta que realizó el cambio. |
| occurred_at | timestamp with time zone | 8 bytes | No | — | Fecha y hora del cambio. |
| from_status | character varying(20) | 20 caracteres | No | — | Estado anterior. |
| to_status | character varying(20) | 20 caracteres | No | — | Estado nuevo. |
| contact_received | boolean | 1 byte | No | — | Indica si el cambio confirmó el contacto. |

Claves foráneas: `order_id` → `customer_orders`; `actor_id` → `user_accounts`.

### sales: Venta reportada por el negocio

| Campo | Tipo | Longitud | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | uuid | 16 bytes | No | PK | Identificador interno de la venta. |
| business_id | uuid | 16 bytes | No | FK | Negocio que reporta la venta. |
| order_id | uuid | 16 bytes | Sí | FK, UK | Solicitud convertida en venta; nulo en una venta directa. |
| actor_id | uuid | 16 bytes | No | FK | Cuenta que registró la venta. |
| request_hash | character varying(64) | 64 caracteres | No | — | Hash de la solicitud, que detecta reintentos con datos distintos. |
| created_at | timestamp with time zone | 8 bytes | No | — | Fecha y hora del registro. |
| void_operation_id | uuid | 16 bytes | Sí | — | Identificador de la operación de anulación. |
| void_hash | character varying(64) | 64 caracteres | Sí | — | Hash de la solicitud de anulación. |
| void_reason | character varying(300) | 300 caracteres | Sí | — | Motivo de la anulación. |
| voided_by | uuid | 16 bytes | Sí | FK | Cuenta que anuló la venta. |
| voided_at | timestamp with time zone | 8 bytes | Sí | — | Fecha y hora de la anulación; nulo si la venta está vigente. |

Claves foráneas: `business_id` → `businesses`; `order_id` → `customer_orders`; `actor_id` → `user_accounts`; `voided_by` → `user_accounts`.

### sale_lines: Producto vendido en una venta

| Campo | Tipo | Longitud | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| sale_id | uuid | 16 bytes | No | PK, FK | Venta a la que pertenece la línea. |
| product_id | uuid | 16 bytes | No | PK, FK | Producto vendido. |
| name | character varying(160) | 160 caracteres | No | — | Nombre del producto al momento de la venta. |
| quantity | integer | 4 bytes | No | — | Unidades vendidas, entre 1 y 99. |
| unit_price | numeric(12,2) | 12 dígitos, 2 decimales | No | — | Precio unitario acordado. |
| currency | character varying(3) | 3 caracteres | No | — | Moneda del precio: ARS o USD. |
| origin | character varying(20) | 20 caracteres | No | — | Origen: solicitado, acordado en la conversación o venta directa. |
| returned_quantity | integer | 4 bytes | No | — | Unidades devueltas, sin superar las vendidas. |

Claves foráneas: `product_id` → `catalog_products`; `sale_id` → `sales`.

Restricciones: `CHECK (((origin)::text = ANY ((ARRAY['requested'::character varying, 'conversation'::character varying, 'direct'::character varying])::text[])))`; `CHECK (((unit_price >= (0)::numeric) AND ((currency)::text = ANY ((ARRAY['ARS'::character varying, 'USD'::character varying])::text[]))))`; `CHECK ((((quantity >= 1) AND (quantity <= 99)) AND ((returned_quantity >= 0) AND (returned_quantity <= quantity))))`.

## Ranking, campañas y medición

### ranking_contexts: Contexto de ranking (zona y rubro) con su secuencia

| Campo | Tipo | Longitud | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| context_key | character varying(200) | 200 caracteres | No | PK | Clave del contexto, formada por zona y rubro. |
| sequence | bigint | 8 bytes | No | — | Número de la última oportunidad registrada en el contexto. |

Restricciones: `CHECK ((sequence > 0))`.

### ranking_snapshots: Orden publicado del ranking para una solicitud

| Campo | Tipo | Longitud | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | uuid | 16 bytes | No | PK | Identificador interno del orden publicado. |
| request_id | uuid | 16 bytes | No | UK | Identificador único de la solicitud que pidió el orden. |
| context_key | character varying(200) | 200 caracteres | No | FK | Contexto de ranking al que pertenece. |
| expires_at | timestamp with time zone | 8 bytes | No | — | Vencimiento del orden para la paginación. |
| payload | jsonb | Variable | No | — | Lista ordenada de negocios y huella de la política aplicada. |
| policy_label | character varying(40) | 40 caracteres | Sí | — | Política publicada que produjo el orden, por ejemplo equity-v1. |

Claves foráneas: `context_key` → `ranking_contexts`.

Restricciones: `CHECK ((jsonb_typeof(payload) = 'object'::text))`; `CHECK (((policy_label)::text ~ '^[a-z0-9][a-z0-9.-]{0,39}$'::text))`.

### ranking_opportunities: Oportunidad de exposición auditada

| Campo | Tipo | Longitud | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | uuid | 16 bytes | No | PK | Identificador interno de la oportunidad. |
| context_key | character varying(200) | 200 caracteres | No | FK, UK | Contexto de ranking al que pertenece. |
| sequence | bigint | 8 bytes | No | UK | Posición de la oportunidad en la secuencia del contexto. |
| occurred_at | timestamp with time zone | 8 bytes | No | — | Fecha y hora de la oportunidad. |
| payload | jsonb | Variable | No | — | Traza privada de candidatos, componentes del puntaje y configuración. |
| policy_label | character varying(40) | 40 caracteres | Sí | — | Política publicada aplicada, por ejemplo equity-v1. |

Claves foráneas: `context_key` → `ranking_contexts`.

Restricciones: `CHECK ((sequence > 0))`; `CHECK ((jsonb_typeof(payload) = 'object'::text))`; `CHECK (((policy_label)::text ~ '^[a-z0-9][a-z0-9.-]{0,39}$'::text))`.

### ranking_contributions: Exposición esperada y recibida por negocio

| Campo | Tipo | Longitud | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| opportunity_id | uuid | 16 bytes | No | PK, FK | Oportunidad en la que se midió la exposición. |
| business_id | uuid | 16 bytes | No | PK | Negocio candidato; referencia lógica sin clave foránea declarada. |
| expected | double precision | 8 bytes | No | — | Exposición esperada según el mérito, entre 0 y 1. |
| received | double precision | 8 bytes | No | — | Exposición recibida según la posición, entre 0 y 1. |

Claves foráneas: `opportunity_id` → `ranking_opportunities`.

Restricciones: `CHECK (((expected >= (0)::double precision) AND (expected <= (1)::double precision)))`; `CHECK (((received >= (0)::double precision) AND (received <= (1)::double precision)))`.

### campaigns: Campaña publicitaria pagada

| Campo | Tipo | Longitud | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | uuid | 16 bytes | No | PK | Identificador interno de la campaña. |
| business_id | uuid | 16 bytes | No | FK | Negocio anunciante. |
| product_id | uuid | 16 bytes | Sí | FK | Producto promocionado; nulo si se promociona una fotografía del negocio. |
| photo_slot | integer | 4 bytes | Sí | — | Fotografía del negocio promocionada, de 1 a 5, cuando no hay producto. |
| image | bytea | hasta 1 MB | Sí | — | Imagen propia de la campaña, hasta 1 MB. |
| title | character varying(160) | 160 caracteres | No | — | Título del anuncio. |
| description | character varying(1000) | 1000 caracteres | No | — | Texto del anuncio. |
| conditions | character varying(1000) | 1000 caracteres | No | — | Condiciones de la promoción. |
| zone_id | character varying(32) | 32 caracteres | No | — | Zona de publicación: centro, nueva-cordoba o alberdi. |
| starts_at | timestamp with time zone | 8 bytes | No | — | Inicio de la vigencia. |
| ends_at | timestamp with time zone | 8 bytes | No | — | Fin de la vigencia, hasta 30 días después del inicio. |
| created_at | timestamp with time zone | 8 bytes | No | — | Fecha y hora de creación. |
| status | character varying(24) | 24 caracteres | No | — | Estado: borrador, en revisión, pendiente de pago, programada, pausada o rechazada. |
| version | integer | 4 bytes | No | — | Versión para el control de concurrencia optimista. |
| save_hash | character varying(64) | 64 caracteres | No | — | Hash del último guardado, que detecta reintentos con datos distintos. |
| approved_at | timestamp with time zone | 8 bytes | Sí | — | Momento de la aprobación del administrador. |
| paid_at | timestamp with time zone | 8 bytes | Sí | — | Momento en que se registró el pago. |
| payment_reference | character varying(120) | 120 caracteres | Sí | — | Referencia del pago informado. |

Claves foráneas: `business_id` → `businesses`; `product_id` → `catalog_products`.

Restricciones: `CHECK (((ends_at > starts_at) AND (ends_at <= (starts_at + '30 days'::interval))))`; `CHECK ((((product_id IS NOT NULL) AND (photo_slot IS NULL)) OR ((product_id IS NULL) AND ((photo_slot >= 1) AND (photo_slot <= 5)))))`; `CHECK (((image IS NULL) OR (octet_length(image) <= 1048576)))`; `CHECK (((status)::text = ANY ((ARRAY['draft'::character varying, 'pending_review'::character varying, 'awaiting_payment'::character varying, 'scheduled'::character varying, 'paused'::character varying, 'rejected'::character varying])::text[])))`; `CHECK (((zone_id)::text = ANY ((ARRAY['centro'::character varying, 'nueva-cordoba'::character varying, 'alberdi'::character varying])::text[])))`.

### campaign_changes: Historial de acciones sobre una campaña

| Campo | Tipo | Longitud | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| operation_id | uuid | 16 bytes | No | PK | Identificador de la operación, que evita registrarla dos veces. |
| campaign_id | uuid | 16 bytes | No | FK, UK | Campaña afectada. |
| actor_id | uuid | 16 bytes | No | FK | Cuenta que realizó la acción. |
| version | integer | 4 bytes | No | UK | Versión de la campaña que produjo el cambio. |
| action | character varying(24) | 24 caracteres | No | — | Acción realizada, por ejemplo enviar, aprobar o rechazar. |
| reason | character varying(300) | 300 caracteres | No | — | Motivo informado, visible para el negocio en un rechazo. |
| request_hash | character varying(64) | 64 caracteres | No | — | Hash de la solicitud, que detecta reintentos con datos distintos. |
| created_at | timestamp with time zone | 8 bytes | No | — | Fecha y hora de la acción. |

Claves foráneas: `campaign_id` → `campaigns`; `actor_id` → `user_accounts`.

### campaign_events: Impresión o clic de una campaña

| Campo | Tipo | Longitud | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| campaign_id | uuid | 16 bytes | No | PK, FK | Campaña medida. |
| context_id | uuid | 16 bytes | No | PK | Identificador del contexto de visualización; evita contar dos veces el mismo evento. |
| kind | character varying(16) | 16 caracteres | No | PK | Tipo de evento: impresión o clic. |
| created_at | timestamp with time zone | 8 bytes | No | — | Fecha y hora del evento. |

Claves foráneas: `campaign_id` → `campaigns`.

Restricciones: `CHECK (((kind)::text = ANY ((ARRAY['impression'::character varying, 'click'::character varying])::text[])))`.

### business_activity: Exposición e interacción observada de un negocio

| Campo | Tipo | Longitud | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| visit_id | uuid | 16 bytes | No | PK | Identificador anónimo de la visita. |
| business_id | uuid | 16 bytes | No | PK, FK | Negocio observado. |
| kind | character varying(20) | 20 caracteres | No | PK | Tipo: impresión, vista del perfil o contacto. |
| surface | character varying(20) | 20 caracteres | No | PK | Superficie: guía de comercios o perfil. |
| source | character varying(20) | 20 caracteres | No | — | Origen: guía, búsqueda o desconocido. |
| position | integer | 4 bytes | Sí | — | Posición en la que se mostró el negocio, si se conoce. |
| ranking_version | character varying(64) | 64 caracteres | Sí | — | Versión del orden con el que se mostró el negocio. |
| occurred_at | timestamp with time zone | 8 bytes | No | — | Fecha y hora del evento. |

Claves foráneas: `business_id` → `businesses`.

Restricciones: `CHECK (((kind)::text = ANY ((ARRAY['impression'::character varying, 'profile_view'::character varying, 'contact'::character varying])::text[])))`; `CHECK ((((surface)::text = ANY ((ARRAY['directory'::character varying, 'profile'::character varying])::text[])) AND (((kind)::text <> 'impression'::text) OR ((surface)::text = 'directory'::text)) AND (((kind)::text <> 'profile_view'::text) OR ((surface)::text = 'profile'::text))))`; `CHECK (((((source)::text = ANY ((ARRAY['directory'::character varying, 'search'::character varying])::text[])) AND ("position" IS NOT NULL) AND (ranking_version IS NOT NULL) AND (("position" >= 1) AND ("position" <= 200000)) AND (((ranking_version)::text = ANY ((ARRAY['name-v1'::character varying, 'distance-v1'::character varying])::text[])) OR (((source)::text = 'directory'::text) AND ((ranking_version)::text ~ '^[0-9a-f]{64}$'::text)))) OR (((source)::text = 'unknown'::text) AND ("position" IS NULL) AND (ranking_version IS NULL))))`.

### owner_activity: Actualización sustantiva del responsable

| Campo | Tipo | Longitud | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | bigint | 8 bytes | No | PK | Número secuencial del registro. |
| business_id | uuid | 16 bytes | No | FK | Negocio actualizado. |
| kind | character varying(20) | 20 caracteres | No | — | Tipo: perfil, catálogo o promoción. |
| occurred_at | timestamp with time zone | 8 bytes | No | — | Fecha y hora de la actualización. |

Claves foráneas: `business_id` → `businesses`.

Restricciones: `CHECK (((kind)::text = ANY ((ARRAY['profile_update'::character varying, 'catalog_update'::character varying, 'promotion_update'::character varying])::text[])))`.
