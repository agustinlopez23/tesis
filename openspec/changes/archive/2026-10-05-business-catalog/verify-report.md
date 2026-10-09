# Verificación — 05/10/2026

## Resultado

Catálogo por negocio y permisos por plan completados. API real y Next compilado
permiten administrar productos, mostrarlos en perfil y activar/revocar períodos.
No se migró ni reinició la demo; sin push/PR ni activación del ranking productivo.

## Evidencia

- API/HTTP/PostgreSQL/PostGIS: **110/110**, sin omitidas ni advertencias en la
  última ejecución. Incluye 8 escenarios del catálogo y uno de migración.
- Frontend: **90/90**, sin omitidas. Tras separar los archivos por recorrido,
  los siete casos nuevos pasan de nuevo: owner 3, public 2, admin 2.
- `npm run build`, `npm run lint` y `git diff --check`: correctos.
- `node App/tools/verify-catalog.mjs`: dos ejecuciones correctas, la última con
  el backend final. Base generada, migración real, Next production y proxy.
  Comprueba autorización, plan, alta sin duplicación, búsqueda, archivo,
  revocación, conservación privada y reactivación. Limpieza final correcta.
- Playwright con API real: acceso de responsable, alta/publicación, precio
  decimal, foto válida procesada, perfil y filtro de categoría; administración,
  revocación/activación con motivo e historial. 1440/390/320 px sin desborde.
  Dos tarjetas públicas y una tras filtrar Sandwiches; imagen descargada válida.
  Capturas locales en `.tools/catalog/` (solo datos temporales). Sin errores JS
  observados; 401 inicial de sesión y 400 de PNG inválido son respuestas esperadas.

Los primeros intentos del script de navegador fallaron por importación no
disponible en su contexto y por un selector de select; se ajustó el harness,
no la funcionalidad. Se probó un PNG inválido y luego un PNG válido. Una compilación
coincidió con la API local abierta y Windows bloqueó la DLL: se cerró el harness,
se reconstruyó y repitió la suite, sin errores pendientes.

## Casos sensibles

Propiedad, roles actuales, origen, fechas futuras/vencidas, contigüidad,
solapamiento concurrente, identidad del reintento, precisión UTC, revocación
auditada, dueño suspendido, negocio borrador, productos archivados, fotos privadas,
versiones de edición y conservación ante downgrade. No se usan bases InMemory.

## Límites y siguiente incremento

Catálogo local al negocio, alfabético y con disponibilidad declarada. Quedan
pedidos, stock/ventas, búsqueda global y ranking de productos, IA opcional,
publicidad y evaluación final. La administración implementada gestiona planes;
no completa la moderación general. Cobro y precios comerciales no implementados.

## Revisión y rollback

| Unidad | Commit | Líneas agregadas + eliminadas |
| --- | --- | --- |
| Almacenamiento/migración | `0c5cb25` | 1.187 (incluye 1.027 de migración/designer/snapshot) |
| API y pruebas HTTP | `fc0ffee` | 650 |
| Catálogo público/proxy | `13d1baa` | 155 |
| Paneles de productos/planes | `e3ba78c` | 352 |

El cierre agrega harness y evidencia; no abre PR. Cadena de dependencia:
almacenamiento → API → público/proxy → paneles → cierre.

Cortes y dependencia en `design.md`. Identidades y conteos de commits en Git.
Migración generada y unidad API superan el presupuesto nominal: se recomienda
`size:exception` al preparar PR; se mantienen íntegros código/pruebas.
Rollback por unidad: retirar UI/proxy y API conserva los datos; revertir esquema
solo en vacío. La demo no recibió asignaciones de administrador ni períodos.
