# Modelo de datos implementado

Esta carpeta documenta el modelo de datos **tal como está implementado** en el prototipo. Se obtiene del esquema de la base de datos migrada (PostgreSQL con PostGIS) y de las clases de C# del backend. Reemplaza, en el documento final, al modelo de diseño de la Entrega 2, que se conserva sin cambios en la carpeta superior ([`Doc/model/`](../README.md)).

## Contenido

| Archivo | Uso |
| --- | --- |
| [extraer-modelo.mjs](extraer-modelo.mjs) | Lee el esquema real y las clases de C# y genera el modelo y el diccionario. |
| [descripciones.json](descripciones.json) | Descripciones en español de tablas, campos, clases y relaciones. |
| [modelo-implementado.json](modelo-implementado.json) | Modelo generado: tablas por módulo, relaciones, clases, operaciones y diccionario. |
| [diccionario-datos.md](diccionario-datos.md) | Diccionario completo: tipo, longitud, nulabilidad, claves, restricciones y descripción de cada campo. |
| [figures/](figures/) | Diagramas de clases (`UML-*`) y de entidad-relación (`ER-*`) en DOT, SVG y PNG. |

## Módulos

| Módulo | Tablas | Figuras |
| --- | --- | --- |
| Negocios, identidad y planes | `user_accounts`, `roles`, `user_account_roles`, `user_sessions`, `businesses`, `business_categories`, `business_photos`, `business_subscriptions`, `admin_actions` | [Clases](figures/UML-01-negocios.svg), [ER](figures/ER-01-negocios.svg) |
| Catálogo, solicitudes, ventas e inventario | `catalog_products`, `product_photos`, `inventory_balances`, `inventory_movements`, `customer_orders`, `order_lines`, `order_changes`, `sales`, `sale_lines` | [Clases](figures/UML-02-comercio.svg), [ER](figures/ER-02-comercio.svg) |
| Ranking, campañas y medición | `ranking_contexts`, `ranking_snapshots`, `ranking_opportunities`, `ranking_contributions`, `campaigns`, `campaign_changes`, `campaign_events`, `business_activity`, `owner_activity` | [Clases](figures/UML-03-ranking.svg), [ER](figures/ER-03-ranking.svg) |

También se generan las vistas completas [ER-completo](figures/ER-completo.svg) y [UML-completo](figures/UML-completo.svg).

Se excluyen las tablas que no pertenecen al dominio: `__EFMigrationsHistory` (historial de migraciones), `spatial_ref_sys` (catálogo de PostGIS) y `data_protection_keys` (claves de ASP.NET Core Data Protection).

## Criterios

- **Fuente de verdad.** Las tablas, columnas, tipos, longitudes, nulabilidad y claves primarias, foráneas, únicas y de control se leen de `information_schema` y `pg_catalog`. Las tablas del ranking se crean con migraciones SQL y no con Entity Framework; por eso la base de datos, y no el código, define el esquema.
- **Diagrama de entidad-relación.** Usa notación pie de cuervo. La multiplicidad se deriva de cada clave foránea: el extremo del padre es `1` u `0..1` según la nulabilidad de la columna, y el del hijo es `0..1` si la columna es única y `0..*` en otro caso. Las claves foráneas hacia otro módulo se indican en la fila, sin trazar la relación.
- **Diagrama de clases.** Usa las clases de `Marketplace.Models` y, para el ranking, los registros de `Marketplace.BLL` que representan esas tablas. Se omiten identificadores, claves foráneas y propiedades de navegación. Las entidades no contienen comportamiento, por lo que las operaciones son métodos de los servicios de la capa de lógica de negocio que actúan sobre cada clase; el script verifica que cada uno exista en el código. Las clases de otro paquete se muestran solo con su nombre y con borde discontinuo.
- **Referencias lógicas.** `ranking_contributions.business_id`, `campaign_events.context_id` y `campaigns.photo_slot` son referencias sin clave foránea declarada; se describen en el diccionario, pero no se dibujan como relaciones.

## Cómo regenerarlo

Requisitos: la base de datos local levantada y migrada (contenedor `app-database-1`) y las dependencias de `.tools/documentation`. El script consulta el esquema con `psql` dentro del contenedor, por lo que no necesita la contraseña.

```bash
node Doc/model/implementacion/extraer-modelo.mjs
node Doc/Seminario_Entrega_2/tools/build-model.mjs --model Doc/model/implementacion/modelo-implementado.json --out Doc/model/implementacion
```

El primer comando acepta `--container`, `--user` y `--db` para otra instancia. El segundo reutiliza el generador de la Entrega 2; sin argumentos, ese generador sigue produciendo el modelo de diseño original.
