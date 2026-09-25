# Diccionario de datos por campo

Versión 1.0 · Tipos y longitudes propuestos para el prototipo. Identificadores en inglés y descripción en español conforme a la consigna. No se han ejecutado migraciones. «Sí*» significa opcional físicamente pero sujeto a reglas condicionales del [diccionario de restricciones](data-dictionary.md). Los tipos de tamaño fijo se expresan en bytes, no en caracteres.

Las operaciones de autenticación utilizarán un componente mantenido del framework; los hashes no son contraseñas ni tokens utilizables. Longitudes de almacenamiento geográfico/JSON no se equiparan a longitud de texto.

## UserAccount — Cuenta de usuario

| Campo | Longitud / precisión | Tipo propuesto | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | 16 bytes; 36 caracteres de representación | uuid | No | PK | Identificador interno de la entidad. |
| email | 254 caracteres | varchar(254) | No | UK | Correo normalizado de la cuenta. |
| display_name | 120 caracteres | varchar(120) | No | — | Nombre visible de la persona. |
| status | 64 caracteres | varchar(64) | No | — | Estado del recurso según su ciclo de vida. |
| created_at | 8 bytes; fecha y hora UTC | timestamptz | No | — | Fecha y hora UTC de creación. |
| password_hash | 512 caracteres | varchar(512) | No | — | Hash de contraseña generado y verificado por el componente de autenticación; nunca contraseña en claro. |

Fuente: elaboración propia.

## UserRole — Rol de una cuenta

| Campo | Longitud / precisión | Tipo propuesto | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| user_id | 16 bytes; 36 caracteres de representación | uuid | No | PK,FK | Cuenta de usuario relacionada. |
| role | 64 caracteres | varchar(64) | No | PK | Rol asignado a la cuenta. |

Fuente: elaboración propia.

## BusinessCategory — Rubro de negocio

| Campo | Longitud / precisión | Tipo propuesto | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | 16 bytes; 36 caracteres de representación | uuid | No | PK | Identificador interno de la entidad. |
| name | 160 caracteres | varchar(160) | No | UK | Nombre visible. |

Fuente: elaboración propia.

## Business — Negocio y su única ubicación

| Campo | Longitud / precisión | Tipo propuesto | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | 16 bytes; 36 caracteres de representación | uuid | No | PK | Identificador interno de la entidad. |
| owner_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Cuenta responsable del negocio. |
| category_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Categoría o rubro de la entidad. |
| name | 160 caracteres | varchar(160) | No | — | Nombre visible. |
| description | 2000 caracteres | varchar(2000) | Sí* | — | Descripción de la oferta o del negocio. |
| address | 250 caracteres | varchar(250) | Sí* | — | Dirección textual del local. |
| location | 1 punto, 2 coordenadas; sin longitud textual | geography(Point,4326) | No | — | Ubicación geográfica del negocio. |
| whatsapp_number | 16 caracteres | varchar(16) | No | — | Número del negocio validado para contacto. |
| status | 64 caracteres | varchar(64) | No | — | Estado del recurso según su ciclo de vida. |
| created_at | 8 bytes; fecha y hora UTC | timestamptz | No | — | Fecha y hora UTC de creación. |

Fuente: elaboración propia.

## Plan — Plan sin precios definidos

| Campo | Longitud / precisión | Tipo propuesto | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | 16 bytes; 36 caracteres de representación | uuid | No | PK | Identificador interno de la entidad. |
| code | 64 caracteres | varchar(64) | No | UK | Código estable del plan. |
| name | 160 caracteres | varchar(160) | No | — | Nombre visible. |
| is_free | 1 byte | boolean | No | — | Indica si el plan corresponde al acceso gratuito. |

Fuente: elaboración propia.

## PlanCapability — Permisos del plan

| Campo | Longitud / precisión | Tipo propuesto | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| plan_id | 16 bytes; 36 caracteres de representación | uuid | No | PK,FK | Plan relacionado. |
| capability | 64 caracteres | varchar(64) | No | PK | Código de capacidad funcional. |
| enabled | 1 byte | boolean | No | — | Indica si la capacidad está habilitada. |

Fuente: elaboración propia.

## Subscription — Período de suscripción

| Campo | Longitud / precisión | Tipo propuesto | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | 16 bytes; 36 caracteres de representación | uuid | No | PK | Identificador interno de la entidad. |
| business_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Negocio propietario o contexto del registro. |
| plan_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Plan relacionado. |
| status | 64 caracteres | varchar(64) | No | — | Estado del recurso según su ciclo de vida. |
| starts_at | 8 bytes; fecha y hora UTC | timestamptz | No | — | Inicio de vigencia UTC, inclusivo. |
| ends_at | 8 bytes; fecha y hora UTC | timestamptz | Sí* | — | Fin de vigencia UTC, exclusivo. |
| activated_by | 16 bytes; 36 caracteres de representación | uuid | Sí* | FK | Administrador que habilita el período. |

Fuente: elaboración propia.

## Publication — Novedad o promoción

| Campo | Longitud / precisión | Tipo propuesto | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | 16 bytes; 36 caracteres de representación | uuid | No | PK | Identificador interno de la entidad. |
| business_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Negocio propietario o contexto del registro. |
| product_id | 16 bytes; 36 caracteres de representación | uuid | Sí* | FK | Producto relacionado, perteneciente al negocio. |
| kind | 64 caracteres | varchar(64) | No | — | Tipo de evento, contenido o movimiento. |
| title | 160 caracteres | varchar(160) | No | — | Título visible del contenido. |
| body | 4000 caracteres | varchar(4000) | No | — | Texto de la publicación. |
| image_url | 2048 caracteres | varchar(2048) | Sí* | — | Referencia a la imagen almacenada. |
| terms | 2000 caracteres | varchar(2000) | Sí* | — | Condiciones de la promoción. |
| status | 64 caracteres | varchar(64) | No | — | Estado del recurso según su ciclo de vida. |
| starts_at | 8 bytes; fecha y hora UTC | timestamptz | Sí* | — | Inicio de vigencia UTC, inclusivo. |
| ends_at | 8 bytes; fecha y hora UTC | timestamptz | Sí* | — | Fin de vigencia UTC, exclusivo. |
| published_at | 8 bytes; fecha y hora UTC | timestamptz | Sí* | — | Fecha y hora de publicación. |

Fuente: elaboración propia.

## BusinessActivity — Actividad sustantiva auditable

| Campo | Longitud / precisión | Tipo propuesto | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | 16 bytes; 36 caracteres de representación | uuid | No | PK | Identificador interno de la entidad. |
| business_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Negocio propietario o contexto del registro. |
| actor_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Cuenta que realiza la operación. |
| publication_id | 16 bytes; 36 caracteres de representación | uuid | Sí* | FK | Publicación relacionada. |
| kind | 64 caracteres | varchar(64) | No | — | Tipo de evento, contenido o movimiento. |
| occurred_at | 8 bytes; fecha y hora UTC | timestamptz | No | — | Fecha y hora UTC del evento. |
| deduplication_key | 128 caracteres | varchar(128) | No | UK | Clave estable para evitar duplicación de eventos. |

Fuente: elaboración propia.

## UserSession — Sesión autenticada revocable

| Campo | Longitud / precisión | Tipo propuesto | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | 16 bytes; 36 caracteres de representación | uuid | No | PK | Identificador interno de la entidad. |
| user_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Cuenta de usuario relacionada. |
| token_hash | 128 caracteres | varchar(128) | No | UK | Hash del token de sesión; nunca el token utilizable. |
| created_at | 8 bytes; fecha y hora UTC | timestamptz | No | — | Fecha y hora UTC de creación. |
| expires_at | 8 bytes; fecha y hora UTC | timestamptz | No | — | Fecha y hora UTC de vencimiento de la sesión. |
| revoked_at | 8 bytes; fecha y hora UTC | timestamptz | Sí* | — | Fecha y hora de revocación explícita. |

Fuente: elaboración propia.

## AuditEvent — Auditoría inmutable de cambios sensibles

| Campo | Longitud / precisión | Tipo propuesto | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | 16 bytes; 36 caracteres de representación | uuid | No | PK | Identificador interno de la entidad. |
| actor_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Cuenta que realiza la operación. |
| business_id | 16 bytes; 36 caracteres de representación | uuid | Sí* | FK | Negocio propietario o contexto del registro. |
| target_type | 64 caracteres | varchar(64) | No | — | Tipo de recurso auditado; lista cerrada. |
| target_id | 16 bytes; 36 caracteres de representación | uuid | No | — | Identificador del recurso auditado; referencia lógica preservada. |
| action | 64 caracteres | varchar(64) | No | — | Acción auditada. |
| previous_state | 64 caracteres | varchar(64) | Sí* | — | Estado anterior, si aplica. |
| new_state | 64 caracteres | varchar(64) | Sí* | — | Estado nuevo, si aplica. |
| reason | 1000 caracteres | varchar(1000) | No | — | Motivo de ajuste, corrección o decisión. |
| occurred_at | 8 bytes; fecha y hora UTC | timestamptz | No | — | Fecha y hora UTC del evento. |

Fuente: elaboración propia.

## ProductCategory — Categoría de producto

| Campo | Longitud / precisión | Tipo propuesto | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | 16 bytes; 36 caracteres de representación | uuid | No | PK | Identificador interno de la entidad. |
| name | 160 caracteres | varchar(160) | No | UK | Nombre visible. |

Fuente: elaboración propia.

## Product — Producto por unidad

| Campo | Longitud / precisión | Tipo propuesto | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | 16 bytes; 36 caracteres de representación | uuid | No | PK | Identificador interno de la entidad. |
| business_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Negocio propietario o contexto del registro. |
| category_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Categoría o rubro de la entidad. |
| name | 160 caracteres | varchar(160) | No | — | Nombre visible. |
| description | 2000 caracteres | varchar(2000) | Sí* | — | Descripción de la oferta o del negocio. |
| image_url | 2048 caracteres | varchar(2048) | Sí* | — | Referencia a la imagen almacenada. |
| price | 12 dígitos, 2 decimales | numeric(12,2) | No | — | Precio actual del producto. |
| currency | 3 caracteres | varchar(3) | No | — | Código de moneda. |
| status | 64 caracteres | varchar(64) | No | — | Estado del recurso según su ciclo de vida. |

Fuente: elaboración propia.

## Inventory — Saldo de stock

| Campo | Longitud / precisión | Tipo propuesto | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| product_id | 16 bytes; 36 caracteres de representación | uuid | No | PK,FK | Producto relacionado, perteneciente al negocio. |
| business_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Negocio propietario o contexto del registro. |
| quantity | 4 bytes; entero de 32 bits | integer | No | — | Cantidad entera de unidades. |
| low_stock_threshold | 4 bytes; entero de 32 bits | integer | No | — | Umbral de disponibilidad baja. |
| version | 8 bytes; entero de 64 bits | bigint | No | — | Versión de concurrencia o identificación del algoritmo. |

Fuente: elaboración propia.

## OrderRequest — Solicitud previa al contacto

| Campo | Longitud / precisión | Tipo propuesto | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | 16 bytes; 36 caracteres de representación | uuid | No | PK | Identificador interno de la entidad. |
| business_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Negocio propietario o contexto del registro. |
| consumer_id | 16 bytes; 36 caracteres de representación | uuid | Sí* | FK | Cuenta del consumidor, cuando está autenticado. |
| source_interaction_id | 16 bytes; 36 caracteres de representación | uuid | Sí* | FK | Interacción comprobada que origina la solicitud. |
| reference | 32 caracteres | varchar(32) | No | UK | Referencia pública de solicitud; no autoriza acceso privado. |
| access_token_hash | 128 caracteres | varchar(128) | No | — | Hash del token privado para consultar la solicitud. |
| contact_name | 120 caracteres | varchar(120) | No | — | Nombre proporcionado para atender la solicitud. |
| contact_phone | 16 caracteres | varchar(16) | Sí* | — | Teléfono opcional proporcionado por el consumidor. |
| status | 64 caracteres | varchar(64) | No | — | Estado del recurso según su ciclo de vida. |
| origin | 64 caracteres | varchar(64) | No | — | Canal de origen conocido o sin atribución. |
| idempotency_key | 128 caracteres | varchar(128) | No | — | Clave de operación que evita repetir sus efectos. |
| created_at | 8 bytes; fecha y hora UTC | timestamptz | No | — | Fecha y hora UTC de creación. |
| contact_confirmed_at | 8 bytes; fecha y hora UTC | timestamptz | Sí* | — | Instante en que el negocio reportó recepción del contacto. |
| contact_confirmed_by | 16 bytes; 36 caracteres de representación | uuid | Sí* | FK | Responsable que confirmó recepción del contacto. |

Fuente: elaboración propia.

## OrderItem — Copia de lo solicitado

| Campo | Longitud / precisión | Tipo propuesto | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | 16 bytes; 36 caracteres de representación | uuid | No | PK | Identificador interno de la entidad. |
| business_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Negocio propietario o contexto del registro. |
| order_request_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Solicitud vinculada. |
| product_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Producto relacionado, perteneciente al negocio. |
| product_name_snapshot | 160 caracteres | varchar(160) | No | — | Copia del nombre del producto al registrar la operación. |
| quantity | 4 bytes; entero de 32 bits | integer | No | — | Cantidad entera de unidades. |
| unit_price_snapshot | 12 dígitos, 2 decimales | numeric(12,2) | No | — | Copia del precio unitario solicitado. |
| currency | 3 caracteres | varchar(3) | No | — | Código de moneda. |

Fuente: elaboración propia.

## Sale — Venta reportada

| Campo | Longitud / precisión | Tipo propuesto | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | 16 bytes; 36 caracteres de representación | uuid | No | PK | Identificador interno de la entidad. |
| business_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Negocio propietario o contexto del registro. |
| order_request_id | 16 bytes; 36 caracteres de representación | uuid | Sí* | FK | Solicitud vinculada. |
| recorded_by | 16 bytes; 36 caracteres de representación | uuid | No | FK | Responsable que registra la venta. |
| origin | 64 caracteres | varchar(64) | No | — | Canal de origen conocido o sin atribución. |
| status | 64 caracteres | varchar(64) | No | — | Estado del recurso según su ciclo de vida. |
| idempotency_key | 128 caracteres | varchar(128) | No | — | Clave de operación que evita repetir sus efectos. |
| recorded_at | 8 bytes; fecha y hora UTC | timestamptz | No | — | Fecha y hora del registro de venta. |

Fuente: elaboración propia.

## SaleItem — Copia de lo efectivamente vendido

| Campo | Longitud / precisión | Tipo propuesto | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | 16 bytes; 36 caracteres de representación | uuid | No | PK | Identificador interno de la entidad. |
| business_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Negocio propietario o contexto del registro. |
| sale_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Venta a la que pertenece la línea. |
| product_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Producto relacionado, perteneciente al negocio. |
| product_name_snapshot | 160 caracteres | varchar(160) | No | — | Copia del nombre del producto al registrar la operación. |
| item_origin | 64 caracteres | varchar(64) | Sí* | — | Origen de la línea: solicitada por el consumidor o acordada durante la conversación. |
| quantity | 4 bytes; entero de 32 bits | integer | No | — | Cantidad entera de unidades. |
| unit_price | 12 dígitos, 2 decimales | numeric(12,2) | No | — | Precio unitario efectivamente acordado. |
| currency | 3 caracteres | varchar(3) | No | — | Código de moneda. |

Fuente: elaboración propia.

## StockMovement — Movimiento firmado de unidades

| Campo | Longitud / precisión | Tipo propuesto | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | 16 bytes; 36 caracteres de representación | uuid | No | PK | Identificador interno de la entidad. |
| business_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Negocio propietario o contexto del registro. |
| product_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Producto relacionado, perteneciente al negocio. |
| sale_item_id | 16 bytes; 36 caracteres de representación | uuid | Sí* | FK | Línea de venta que explica la salida o devolución. |
| actor_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Cuenta que realiza la operación. |
| quantity_delta | 4 bytes; entero de 32 bits | integer | No | — | Variación de unidades con signo. |
| kind | 64 caracteres | varchar(64) | No | — | Tipo de evento, contenido o movimiento. |
| reason | 1000 caracteres | varchar(1000) | No | — | Motivo de ajuste, corrección o decisión. |
| operation_key | 128 caracteres | varchar(128) | No | — | Clave de operación de inventario. |
| occurred_at | 8 bytes; fecha y hora UTC | timestamptz | No | — | Fecha y hora UTC del evento. |

Fuente: elaboración propia.

## RankingConfiguration — Versión inmutable del algoritmo

| Campo | Longitud / precisión | Tipo propuesto | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | 16 bytes; 36 caracteres de representación | uuid | No | PK | Identificador interno de la entidad. |
| version | 64 caracteres | varchar(64) | No | UK | Versión de concurrencia o identificación del algoritmo. |
| context | 64 caracteres | varchar(64) | No | — | Contexto del ranking: revista, búsqueda de negocios o productos. |
| parameters | Variable; límite propuesto de 16 KiB por documento | jsonb | No | — | Parámetros y reglas de una versión inmutable. |
| created_at | 8 bytes; fecha y hora UTC | timestamptz | No | — | Fecha y hora UTC de creación. |

Fuente: elaboración propia.

## DiscoveryRequest — Consulta o entrada a la revista

| Campo | Longitud / precisión | Tipo propuesto | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | 16 bytes; 36 caracteres de representación | uuid | No | PK | Identificador interno de la entidad. |
| configuration_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Versión del ranking utilizada. |
| consumer_id | 16 bytes; 36 caracteres de representación | uuid | Sí* | FK | Cuenta del consumidor, cuando está autenticado. |
| session_key | 128 caracteres | varchar(128) | No | — | Identificador seudónimo de sesión de exploración. |
| context | 64 caracteres | varchar(64) | No | — | Contexto del ranking: revista, búsqueda de negocios o productos. |
| query_text | 250 caracteres | varchar(250) | Sí* | — | Texto de búsqueda; puede estar ausente al entrar. |
| business_category_id | 16 bytes; 36 caracteres de representación | uuid | Sí* | FK | Rubro usado como filtro. |
| product_category_id | 16 bytes; 36 caracteres de representación | uuid | Sí* | FK | Categoría de producto usada como filtro. |
| search_center | 1 punto, 2 coordenadas; sin longitud textual | geography(Point,4326) | No | — | Centro geográfico de la consulta. |
| radius_meters | 4 bytes; entero de 32 bits | integer | No | — | Radio de búsqueda o campaña en metros. |
| random_seed | 8 bytes; entero de 64 bits | bigint | No | — | Semilla que permite repetir desempates controlados. |
| created_at | 8 bytes; fecha y hora UTC | timestamptz | No | — | Fecha y hora UTC de creación. |

Fuente: elaboración propia.

## RankingCandidate — Candidato evaluado, visible o no

| Campo | Longitud / precisión | Tipo propuesto | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | 16 bytes; 36 caracteres de representación | uuid | No | PK | Identificador interno de la entidad. |
| request_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Consulta de descubrimiento evaluada. |
| business_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Negocio propietario o contexto del registro. |
| product_id | 16 bytes; 36 caracteres de representación | uuid | Sí* | FK | Producto relacionado, perteneciente al negocio. |
| relevance | 14 dígitos, 8 decimales | numeric(14,8) | No | — | Relevancia normalizada del candidato. |
| proximity | 14 dígitos, 8 decimales | numeric(14,8) | No | — | Proximidad normalizada a la zona de consulta. |
| availability | 14 dígitos, 8 decimales | numeric(14,8) | Sí* | — | Disponibilidad normalizada, si aplica al contexto. |
| activity | 14 dígitos, 8 decimales | numeric(14,8) | Sí* | — | Actividad sustantiva normalizada, si aplica al contexto. |
| equity | 14 dígitos, 8 decimales | numeric(14,8) | No | — | Factor normalizado de compensación de exposición. |
| new_business_bonus | 14 dígitos, 8 decimales | numeric(14,8) | No | — | Bonificación temporal para negocio nuevo. |
| merit | 14 dígitos, 8 decimales | numeric(14,8) | No | — | Mérito utilizado para estimar oportunidades esperadas. |
| expected_exposure_share | 14 dígitos, 8 decimales | numeric(14,8) | No | — | Participación esperada del negocio en la consulta; no sumar una vez por producto. |
| final_score | 14 dígitos, 8 decimales | numeric(14,8) | No | — | Puntaje final de esta evaluación. |
| position | 4 bytes; entero de 32 bits | integer | Sí* | — | Posición orgánica, comenzando en uno. |

Fuente: elaboración propia.

## OrganicImpression — Exposición orgánica observada

| Campo | Longitud / precisión | Tipo propuesto | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | 16 bytes; 36 caracteres de representación | uuid | No | PK | Identificador interno de la entidad. |
| candidate_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Candidato cuya tarjeta fue observada. |
| position | 4 bytes; entero de 32 bits | integer | No | — | Posición orgánica, comenzando en uno. |
| exposure_weight | 14 dígitos, 8 decimales | numeric(14,8) | No | — | Peso de exposición según posición. |
| deduplication_key | 128 caracteres | varchar(128) | No | UK | Clave estable para evitar duplicación de eventos. |
| viewed_at | 8 bytes; fecha y hora UTC | timestamptz | No | — | Instante en que se cumple el contrato de visibilidad. |

Fuente: elaboración propia.

## AdCampaign — Campaña de producto o promoción

| Campo | Longitud / precisión | Tipo propuesto | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | 16 bytes; 36 caracteres de representación | uuid | No | PK | Identificador interno de la entidad. |
| business_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Negocio propietario o contexto del registro. |
| product_id | 16 bytes; 36 caracteres de representación | uuid | Sí* | FK | Producto relacionado, perteneciente al negocio. |
| publication_id | 16 bytes; 36 caracteres de representación | uuid | Sí* | FK | Publicación relacionada. |
| title | 160 caracteres | varchar(160) | No | — | Título visible del contenido. |
| target_center | 1 punto, 2 coordenadas; sin longitud textual | geography(Point,4326) | No | — | Centro de la zona objetivo de campaña. |
| radius_meters | 4 bytes; entero de 32 bits | integer | No | — | Radio de búsqueda o campaña en metros. |
| starts_at | 8 bytes; fecha y hora UTC | timestamptz | No | — | Inicio de vigencia UTC, inclusivo. |
| ends_at | 8 bytes; fecha y hora UTC | timestamptz | No | — | Fin de vigencia UTC, exclusivo. |
| status | 64 caracteres | varchar(64) | No | — | Estado del recurso según su ciclo de vida. |
| approved_at | 8 bytes; fecha y hora UTC | timestamptz | Sí* | — | Instante de aprobación de campaña. |
| approved_by | 16 bytes; 36 caracteres de representación | uuid | Sí* | FK | Administrador que aprueba. |
| payment_confirmed_at | 8 bytes; fecha y hora UTC | timestamptz | Sí* | — | Instante de confirmación manual del pago. |
| payment_confirmed_by | 16 bytes; 36 caracteres de representación | uuid | Sí* | FK | Administrador que confirma el pago externo. |

Fuente: elaboración propia.

## AdImpression — Exposición patrocinada observada

| Campo | Longitud / precisión | Tipo propuesto | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | 16 bytes; 36 caracteres de representación | uuid | No | PK | Identificador interno de la entidad. |
| campaign_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Campaña publicitaria relacionada. |
| discovery_request_id | 16 bytes; 36 caracteres de representación | uuid | Sí* | FK | Consulta donde aparece el anuncio, si existe. |
| placement | 64 caracteres | varchar(64) | No | — | Espacio publicitario utilizado. |
| session_key | 128 caracteres | varchar(128) | No | — | Identificador seudónimo de sesión de exploración. |
| deduplication_key | 128 caracteres | varchar(128) | No | UK | Clave estable para evitar duplicación de eventos. |
| viewed_at | 8 bytes; fecha y hora UTC | timestamptz | No | — | Instante en que se cumple el contrato de visibilidad. |

Fuente: elaboración propia.

## Interaction — Visita o intención de contacto

| Campo | Longitud / precisión | Tipo propuesto | Nulo | Clave | Descripción |
| --- | --- | --- | --- | --- | --- |
| id | 16 bytes; 36 caracteres de representación | uuid | No | PK | Identificador interno de la entidad. |
| business_id | 16 bytes; 36 caracteres de representación | uuid | No | FK | Negocio propietario o contexto del registro. |
| product_id | 16 bytes; 36 caracteres de representación | uuid | Sí* | FK | Producto relacionado, perteneciente al negocio. |
| publication_id | 16 bytes; 36 caracteres de representación | uuid | Sí* | FK | Publicación relacionada. |
| organic_impression_id | 16 bytes; 36 caracteres de representación | uuid | Sí* | FK | Impresión orgánica que origina la interacción. |
| ad_impression_id | 16 bytes; 36 caracteres de representación | uuid | Sí* | FK | Impresión patrocinada que origina la interacción. |
| consumer_id | 16 bytes; 36 caracteres de representación | uuid | Sí* | FK | Cuenta del consumidor, cuando está autenticado. |
| session_key | 128 caracteres | varchar(128) | No | — | Identificador seudónimo de sesión de exploración. |
| kind | 64 caracteres | varchar(64) | No | — | Tipo de evento, contenido o movimiento. |
| origin | 64 caracteres | varchar(64) | No | — | Canal de origen conocido o sin atribución. |
| deduplication_key | 128 caracteres | varchar(128) | No | UK | Clave estable para evitar duplicación de eventos. |
| occurred_at | 8 bytes; fecha y hora UTC | timestamptz | No | — | Fecha y hora UTC del evento. |

Fuente: elaboración propia.
