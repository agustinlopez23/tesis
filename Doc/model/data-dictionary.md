# Diccionario y restricciones del modelo

Versión 1.0 · Propuesta complementaria a UML y ER. **Los atributos son obligatorios salvo los indicados como opcionales aquí o en el [diccionario por campo](field-dictionary.md).** PK = clave primaria, FK = referencia, UK = unicidad. La notación Mermaid no expresa todas las restricciones compuestas y condicionales.

## Convenciones físicas propuestas

- `uuid` → UUID; `string` → varchar con longitud definida por campo; `datetime` → timestamptz UTC; `decimal` monetario → numeric(12,2); factores → numeric(14,8); `long` → bigint; `json` → jsonb. Ver las longitudes propuestas en el diccionario por campo.
- `geography` → geography(Point,4326), coordenadas en orden longitud/latitud. Radios en metros, distancia de la fórmula convertida a kilómetros.
- Productos y cantidades por unidad entera; precios no negativos, cantidades solicitadas/vendidas positivas, saldo no negativo. Una moneda por solicitud y venta, consistente en sus líneas.
- Los identificadores de negocio redundantes son deliberados para integridad entre propietarios. Cada tabla con `id` y `business_id` expone UNIQUE(business_id,id). Las referencias entre recursos del negocio usan FK compuestas, además de la FK de negocio correspondiente.
- Las bajas operativas archivan o suspenden. RESTRICT para borrar productos, negocios o documentos referenciados por ventas. La eliminación/anonimización de datos personales requiere una política de retención antes del piloto con datos reales.
- No se guardan contraseñas ni tokens en claro. UserAccount incorpora password_hash para autenticación local propuesta, y UserSession representa sesiones revocables. El componente de autenticación definitivo debe respetar ese contrato o documentar el mapeo a sus tablas propias.

## Identidad, negocio y SaaS

| Entidad | Significado y restricciones |
| --- | --- |
| UserAccount | Identidad interna. Email único normalizado; status controla habilitación. PasswordHash generado por el componente de autenticación, no criptografía propia. |
| UserSession | UserId referencia la cuenta. TokenHash único; ExpiresAt posterior a CreatedAt. RevokedAt opcional. Una sesión válida requiere cuenta habilitada, no estar revocada y no haber vencido. |
| AuditEvent | ActorId obligatorio. BusinessId opcional para operaciones globales. TargetType y TargetId identifican lógicamente el recurso: no es una FK polimórfica física. Action, Reason y OccurredAt obligatorios; estados anterior/nuevo opcionales. Registro inmutable, validado por el servicio; conservar aunque el recurso se archive. No copiar contactos, hashes ni secretos en el motivo. |
| UserRole | PK(user_id,role). Roles propuestos: consumer, business_owner, administrator; una cuenta puede tener varios. |
| BusinessCategory | Rubro de negocio, independiente de categorías de producto. |
| Business | OwnerId referencia UserAccount; CategoryId referencia BusinessCategory. Description y address opcionales; location y whatsapp_number requeridos para publicar. Propuesta: un rubro principal por negocio. Estados: draft, published, suspended, archived. |
| Plan | Code único: presence o boost; nombres visibles Presencia e Impulso. IsFree distingue el acceso gratuito. Sin campo de precio. |
| PlanCapability | PK(plan_id,capability). Códigos propuestos: catalog, orders, inventory, product_analytics, promotion_ads, product_ads, sales_attribution. Enabled determina habilitación; no concede acceso a datos ajenos. |
| Subscription | BusinessId y PlanId obligatorios. EndsAt y ActivatedBy opcionales para registros aún no activados; Impulso activo requiere fin y actor de activación. Intervalos [starts_at,ends_at), fin posterior al inicio. No superponer períodos habilitados de un mismo negocio. Sin período vigente, aplicar Presencia. |
| Publication | ProductId opcional, con FK(business_id,product_id) a Product. Kind: news o promotion. ImageUrl y PublishedAt opcionales. Terms, StartsAt y EndsAt opcionales para news, obligatorios para promotion publicada, con fin posterior a inicio. Status: draft, published, suspended, archived. |
| BusinessActivity | ActorId obligatorio; PublicationId opcional y del mismo negocio. Kind identifica el cambio sustantivo. DeduplicationKey único. El servicio define qué cuenta como actividad y aplica límites, no cada actualización de fila. |

Capacidades propuestas: ambos planes permiten promotion_ads; solo Impulso permite catalog, orders, inventory, product_analytics, product_ads y sales_attribution. Las campañas son contratación adicional. La elegibilidad comercial sigue propuesta, no decisión confirmada por este modelo.

## Catálogo, pedidos y ventas

| Entidad | Significado y restricciones |
| --- | --- |
| ProductCategory | Clasificación del catálogo; no requiere coincidir con BusinessCategory. |
| Product | Pertenece a un negocio. Description e ImageUrl opcionales en borrador. Price y currency guardan la oferta actual; el historial comercial usa copias. Status: draft, published, archived. |
| Inventory | PK product_id; FK(business_id,product_id) a Product. Exactamente un saldo por producto, creado junto con él. Quantity y LowStockThreshold >= 0. Version para concurrencia optimista o alternativa de bloqueo a decidir. |
| OrderRequest | ConsumerId, SourceInteractionId, ContactPhone, ContactConfirmedAt y ContactConfirmedBy opcionales. ContactName obligatorio propuesto. Token de acceso privado obligatorio propuesto; se guarda solo su hash. Reference única no es credencial. UNIQUE(business_id,idempotency_key). Origin: feed, search, advertisement, direct, unknown. ContactConfirmedAt y su actor se completan juntos. Status: pending, in_progress, converted, cancelled. |
| OrderItem | OrderRequestId y ProductId del mismo negocio mediante FK compuestas. Al menos una línea por solicitud; UNIQUE(order_request_id,product_id) mientras no haya variantes. Quantity > 0. Nombre, precio y moneda son copias inmutables de lo solicitado. |
| Sale | OrderRequestId opcional para venta presencial; UNIQUE(order_request_id) para no convertir dos veces. FK(business_id,order_request_id) y UNIQUE(business_id,idempotency_key). Origin: platform o in_store; platform requiere solicitud, in_store no la admite. RecordedBy obligatorio. Status: recorded o voided, con corrección auditada. |
| SaleItem | FK compuestas hacia Sale y Product; al menos una línea por venta. UNIQUE(sale_id,product_id,item_origin). Quantity > 0. Copia de nombre, precio y moneda finales; no se obtiene el importe histórico del precio actual. ItemOrigin: requested o agreed_in_conversation; obligatorio cuando la venta tiene solicitud, ausente cuando Origin es in_store. |
| StockMovement | FK(business_id,product_id) a Inventory y ActorId a UserAccount. SaleItemId opcional para entrada/ajuste, obligatorio en venta o devolución. QuantityDelta distinto de cero: negativo para venta, positivo para devolución; ajuste con signo según motivo. Reason obligatorio. UNIQUE(business_id,operation_key). |

SourceInteractionId debe apuntar a una interacción del mismo negocio; origin debe coincidir con la atribución guardada. Para publicidad se recorre Interaction → AdImpression → AdCampaign. Si no existe evidencia, no fabricar la relación.

Un mismo producto puede ocupar dos líneas de la misma venta cuando parte de las unidades venía en la solicitud y el resto se acordó durante la conversación. Por eso la unicidad incluye ItemOrigin. El descuento de stock opera sobre las líneas finales, sin distinguir origen; la separación existe para medir cuánto del importe fue solicitado y cuánto se acordó después, sin atribuir ese incremento a una venta presencial.

StockMovement con SaleItemId usa FK(business_id,product_id,sale_item_id) hacia UNIQUE(business_id,product_id,id) de SaleItem. Así no puede descontar un producto distinto al vendido. Un índice único parcial sobre sale_item_id para kind=sale evita descuentos de venta duplicados; las devoluciones pueden generar varios movimientos, sin exceder unidades vendidas.

Registrar Sale, SaleItem, movimientos y nuevo saldo en una transacción, comprobando stock concurrentemente. El saldo inicial también es un movimiento. La suma de movimientos debe reconciliar con Inventory.quantity. Un CHECK aislado no puede asegurar esa igualdad entre filas: se implementa con el servicio transaccional y su verificación.

Anular una venta conserva sus líneas y requiere motivo y actor en AuditEvent; no repone stock automáticamente. Una devolución física genera su propio movimiento. No se modela contabilidad de reintegros monetarios. No usar StockMovement para simular una corrección sin movimiento físico.

## Descubrimiento y exposición

| Entidad | Significado y restricciones |
| --- | --- |
| RankingConfiguration | Version única e inmutable. Context: business_feed, business_search, product_search. Parameters contiene pesos, normalización, ventana, bonificación, umbrales y reglas versionadas. No es permiso para cambiar una versión ya usada. |
| DiscoveryRequest | ConsumerId, QueryText, BusinessCategoryId y ProductCategoryId opcionales. BusinessFeed acepta texto ausente. Configuración y contexto deben coincidir. Radio > 0. SessionKey seudónimo; ubicación de búsqueda sujeta a minimización y retención. |
| RankingCandidate | RequestId y BusinessId obligatorios; ProductId solo para product_search y del mismo negocio. Availability y Activity opcionales si no aplican al contexto; no reemplazarlas silenciosamente por cero. Position opcional para candidatos no seleccionados. Resto de componentes requerido. Factores normalizados [0,1]; rango de bonificación y puntaje según configuración. |
| OrganicImpression | CandidateId obligatorio. Position > 0, consistente con posición presentada; ExposureWeight = 1/log2(position+1). Solo se registra al cumplir contrato de visibilidad. DeduplicationKey evita duplicados técnicos; la ventana de deduplicación aún debe especificarse. |

Unicidad de candidatos: índice parcial UNIQUE(request_id,business_id) cuando product_id es nulo; UNIQUE(request_id,product_id) cuando no es nulo. Position única por request_id cuando no es nula. Las restricciones de contexto entre tablas requieren validación transaccional o trigger, no un CHECK local.

ExpectedExposureShare representa la proporción esperada **del negocio en esa consulta**, entre 0 y 1. En búsqueda de productos se repite como copia en los candidatos del mismo negocio y se agrega una sola vez por (request_id,business_id), nunca por producto. Su valor debe ser idéntico en ese grupo y normalizar entre negocios elegibles. El mérito que la determina se especifica con el algoritmo. No constituye una fórmula de Eq ya aprobada.

Para la equidad histórica se agrega OrganicImpression por negocio dentro de la ventana y se contrasta con oportunidades esperadas deduplicadas por negocio/consulta. La publicidad no entra en esa suma. Los agregados diarios son derivados, no entidades obligatorias del MVP. Candidatos sin impresión conservan evidencia de elegibilidad, no exposición ficticia.

## Publicidad e interacciones

| Entidad | Significado y restricciones |
| --- | --- |
| AdCampaign | ProductId y PublicationId opcionales individualmente, pero exactamente uno obligatorio (XOR). FK compuestas aseguran mismo negocio. Publication debe ser promotion. StartsAt < EndsAt y radio > 0. ApprovedAt/By y PaymentConfirmedAt/By opcionales hasta completar revisión/pago y obligatorios para activar. Actores referencian UserAccount con permiso administrador. Sin importes ni precios. |
| AdImpression | CampaignId obligatorio; DiscoveryRequestId opcional para otros espacios públicos. SessionKey identifica sesión seudónima; Placement distingue ubicación del anuncio. Solo campañas elegibles generan nuevas impresiones. DeduplicationKey único. |
| Interaction | ProductId, PublicationId, OrganicImpressionId, AdImpressionId y ConsumerId opcionales. Como máximo un destino específico Product o Publication; ambos nulos representan perfil de negocio. Como máximo una impresión origen orgánica o publicitaria; ambas nulas para acceso directo o sin evidencia. Kind: profile_view, product_view, promotion_view, whatsapp_click. |

Las impresiones vinculadas deben corresponder al mismo negocio que Interaction. Un clic a producto desde la tarjeta de negocio puede diferir del destino original, pero nunca cambiar de negocio sin iniciar una nueva atribución. Las FK simples a impresiones no garantizan esto: validar el recorrido en transacción o definir triggers antes de migraciones. No confiar en identificadores de campaña enviados por el cliente sin comprobarlos.

AdCampaign.status usa draft, pending_review, pending_payment, scheduled, active, paused, finished, rejected. La elegibilidad de entrega también comprueba fechas, zona, estado del negocio, contenido y capacidades vigentes. Un estado active aislado no basta.

## Índices y controles previstos

- GiST en Business.location y AdCampaign.target_center; filtros por estado, vigencia y radio además de búsqueda espacial.
- Índices de FK, particularmente (business_id,status,created_at) en solicitudes y (business_id,recorded_at) en ventas.
- Búsqueda textual indexada de negocio/producto y trigramas donde se justifique; definición exacta en diseño físico.
- RankingCandidate(request_id,business_id), OrganicImpression(viewed_at,candidate_id) y AdImpression(campaign_id,viewed_at) para evaluación y métricas.
- Exclusión o control transaccional de superposición de suscripciones; índices únicos parciales de pedidos/ventas y candidatos descritos arriba.
- Reglas que no caben en FK/CHECK (mínimo una línea, actor autorizado, estados, plan, consistencia de atribución) se documentan y prueban como contratos transaccionales.

## Pendientes de diseño físico

Componente definitivo de autenticación, retención y anonimización; política de modificación de planes y calibración del ranking. Auditoría y sesiones están representadas en el modelo. No aplicar migraciones hasta revisar estos contratos. Las cardinalidades UML y ER describen estados válidos del dominio, incluidos mínimos que necesitan transacciones para cumplirse.
