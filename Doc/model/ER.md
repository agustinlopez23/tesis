# Diagramas entidad-relación

Modelo propuesto orientado a PostgreSQL/PostGIS. Las FK compuestas y restricciones condicionales se detallan en el [diccionario](data-dictionary.md). El [diccionario por campo](field-dictionary.md) fija tipos y longitudes propuestos. Las figuras DOT/SVG y las fuentes Mermaid se generan desde [model.json](model.json); no son migraciones ejecutadas.

## Negocios, identidad y planes

![DER Negocios, identidad y planes](figures/ER-01-negocios.svg)

Fuente: elaboración propia. Se omiten aristas externas al módulo para facilitar lectura; ver el modelo completo.

```mermaid
erDiagram
    UserAccount {
        uuid id PK
        string email UK
        string display_name
        string status
        datetime created_at
        string password_hash
    }
    UserRole {
        uuid user_id PK,FK
        string role PK
    }
    BusinessCategory {
        uuid id PK
        string name UK
    }
    Business {
        uuid id PK
        uuid owner_id FK
        uuid category_id FK
        string name
        string description
        string address
        geography location
        string whatsapp_number
        string status
        datetime created_at
    }
    Plan {
        uuid id PK
        string code UK
        string name
        boolean is_free
    }
    PlanCapability {
        uuid plan_id PK,FK
        string capability PK
        boolean enabled
    }
    Subscription {
        uuid id PK
        uuid business_id FK
        uuid plan_id FK
        string status
        datetime starts_at
        datetime ends_at
        uuid activated_by FK
    }
    Publication {
        uuid id PK
        uuid business_id FK
        uuid product_id FK
        string kind
        string title
        string body
        string image_url
        string terms
        string status
        datetime starts_at
        datetime ends_at
        datetime published_at
    }
    BusinessActivity {
        uuid id PK
        uuid business_id FK
        uuid actor_id FK
        uuid publication_id FK
        string kind
        datetime occurred_at
        string deduplication_key UK
    }
    UserSession {
        uuid id PK
        uuid user_id FK
        string token_hash UK
        datetime created_at
        datetime expires_at
        datetime revoked_at
    }
    AuditEvent {
        uuid id PK
        uuid actor_id FK
        uuid business_id FK
        string target_type
        uuid target_id
        string action
        string previous_state
        string new_state
        string reason
        datetime occurred_at
    }
    UserAccount ||--o{ UserRole : posee
    UserAccount ||--o{ Business : administra
    BusinessCategory ||--o{ Business : clasifica
    Plan ||--o{ PlanCapability : habilita
    Business ||--o{ Subscription : contrata
    Plan ||--o{ Subscription : define
    UserAccount |o--o{ Subscription : activa
    Business ||--o{ Publication : publica
    Business ||--o{ BusinessActivity : registra
    UserAccount ||--o{ BusinessActivity : realiza
    Publication |o--o{ BusinessActivity : origina
    UserAccount ||--o{ UserSession : inicia
    UserAccount ||--o{ AuditEvent : ejecuta
    Business |o--o{ AuditEvent : contextualiza
```

## Catálogo, solicitudes, ventas e inventario

![DER Catálogo, solicitudes, ventas e inventario](figures/ER-02-comercio.svg)

Fuente: elaboración propia. Se omiten aristas externas al módulo para facilitar lectura; ver el modelo completo.

```mermaid
erDiagram
    ProductCategory {
        uuid id PK
        string name UK
    }
    Product {
        uuid id PK
        uuid business_id FK
        uuid category_id FK
        string name
        string description
        string image_url
        decimal price
        string currency
        string status
    }
    Inventory {
        uuid product_id PK,FK
        uuid business_id FK
        int quantity
        int low_stock_threshold
        long version
    }
    OrderRequest {
        uuid id PK
        uuid business_id FK
        uuid consumer_id FK
        uuid source_interaction_id FK
        string reference UK
        string access_token_hash
        string contact_name
        string contact_phone
        string status
        string origin
        string idempotency_key
        datetime created_at
        datetime contact_confirmed_at
        uuid contact_confirmed_by FK
    }
    OrderItem {
        uuid id PK
        uuid business_id FK
        uuid order_request_id FK
        uuid product_id FK
        string product_name_snapshot
        int quantity
        decimal unit_price_snapshot
        string currency
    }
    Sale {
        uuid id PK
        uuid business_id FK
        uuid order_request_id FK
        uuid recorded_by FK
        string origin
        string status
        string idempotency_key
        datetime recorded_at
    }
    SaleItem {
        uuid id PK
        uuid business_id FK
        uuid sale_id FK
        uuid product_id FK
        string product_name_snapshot
        string item_origin
        int quantity
        decimal unit_price
        string currency
    }
    StockMovement {
        uuid id PK
        uuid business_id FK
        uuid product_id FK
        uuid sale_item_id FK
        uuid actor_id FK
        int quantity_delta
        string kind
        string reason
        string operation_key
        datetime occurred_at
    }
    ProductCategory ||--o{ Product : clasifica
    Product ||--|| Inventory : mantiene
    OrderRequest ||--|{ OrderItem : contiene
    Product ||--o{ OrderItem : solicitado
    OrderRequest |o--o| Sale : se_convierte
    Sale ||--|{ SaleItem : contiene
    Product ||--o{ SaleItem : vendido
    Inventory ||--o{ StockMovement : historial
    SaleItem |o--o{ StockMovement : origina
```

## Ranking, campañas y medición

![DER Ranking, campañas y medición](figures/ER-03-descubrimiento.svg)

Fuente: elaboración propia. Se omiten aristas externas al módulo para facilitar lectura; ver el modelo completo.

```mermaid
erDiagram
    RankingConfiguration {
        uuid id PK
        string version UK
        string context
        json parameters
        datetime created_at
    }
    DiscoveryRequest {
        uuid id PK
        uuid configuration_id FK
        uuid consumer_id FK
        string session_key
        string context
        string query_text
        uuid business_category_id FK
        uuid product_category_id FK
        geography search_center
        int radius_meters
        long random_seed
        datetime created_at
    }
    RankingCandidate {
        uuid id PK
        uuid request_id FK
        uuid business_id FK
        uuid product_id FK
        decimal relevance
        decimal proximity
        decimal availability
        decimal activity
        decimal equity
        decimal new_business_bonus
        decimal merit
        decimal expected_exposure_share
        decimal final_score
        int position
    }
    OrganicImpression {
        uuid id PK
        uuid candidate_id FK
        int position
        decimal exposure_weight
        string deduplication_key UK
        datetime viewed_at
    }
    AdCampaign {
        uuid id PK
        uuid business_id FK
        uuid product_id FK
        uuid publication_id FK
        string title
        geography target_center
        int radius_meters
        datetime starts_at
        datetime ends_at
        string status
        datetime approved_at
        uuid approved_by FK
        datetime payment_confirmed_at
        uuid payment_confirmed_by FK
    }
    AdImpression {
        uuid id PK
        uuid campaign_id FK
        uuid discovery_request_id FK
        string placement
        string session_key
        string deduplication_key UK
        datetime viewed_at
    }
    Interaction {
        uuid id PK
        uuid business_id FK
        uuid product_id FK
        uuid publication_id FK
        uuid organic_impression_id FK
        uuid ad_impression_id FK
        uuid consumer_id FK
        string session_key
        string kind
        string origin
        string deduplication_key UK
        datetime occurred_at
    }
    RankingConfiguration ||--o{ DiscoveryRequest : configura
    DiscoveryRequest ||--o{ RankingCandidate : evalua
    RankingCandidate ||--o{ OrganicImpression : recibe
    AdCampaign ||--o{ AdImpression : recibe
    DiscoveryRequest |o--o{ AdImpression : aloja
    OrganicImpression |o--o{ Interaction : origina
    AdImpression |o--o{ Interaction : origina
```
