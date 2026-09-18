# Diseño

DTO público separado; BLL compone respuesta desde contrato de repositorio.
DAL filtra estado y suspensión en SQL y obtiene 21 filas para calcular `hasMore`.
API valida página y usa el manejo existente de fallos de base de datos.
Sin migraciones ni modificaciones en permisos de administración de negocios.

Página cliente accesible mediante enlace desde portada; proxy permite solo GET
de la ruta pública y conserva el parámetro de página. Usa `no-store`; el cliente
descarta solicitudes anteriores al cambiar página o desmontarse.

Pruebas primero: HTTP con base temporal PostGIS; Vitest para tarjetas, WhatsApp,
paginación, vacío, error y proxy. Build/lint y smoke del recorrido real.
Unidades de revisión previstas: backend + contrato; frontend + proxy; integración
y documentación, cada una inferior a 400 líneas. No se crean PRs en este turno.
Rollback: revertir estas unidades; no requiere restaurar datos ni esquema.

## Diseño final del rediseño

La guía también es la portada. El panel está en `/mi-negocio` y el diagnóstico
en `/estado`. Componentes de navegación/iconos compartidos y CSS sin paquetes
adicionales. Imagen editorial local WebP, identificada como ilustrativa.
Consulta textual normalizada en SQL antes de paginar; rubro exacto.
La API conserva el contrato público y el proxy propaga parámetros.
La URL, el historial y el estado de búsqueda se sincronizan sin persistir datos
de visitantes. Capturas de Playwright y comprobaciones en el informe UX.
