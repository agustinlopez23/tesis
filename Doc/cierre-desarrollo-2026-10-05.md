# Estado de cierre local — 05/10/2026

El núcleo comercial está implementado y probado localmente. La asistencia de IA
está preparada, desactivada hasta configurar un modelo y credenciales. La tesis
todavía necesita evaluación humana; este cierre técnico no demuestra su hipótesis.

| Recorrido | Dónde usarlo | Estado |
| --- | --- | --- |
| Negocios y productos por texto, rubro y cercanía | `/explorar`, `/productos` | Implementado; orden alfabético/distancia |
| Perfil, galería, catálogo y solicitudes | `/negocios/{id}` | Implementado; catálogo sujeto a plan |
| Atención, ventas, inventario y métricas | Mi negocio → Editar | Implementado; operaciones auditadas e idempotentes |
| Planes manuales | `/administracion/planes` | Implementado; exige rol administrador |
| Campañas de producto/promoción | Mi negocio → Editar → Ver campañas | Implementado; revisión y pago manual |
| Revisión y difusión publicitaria | `/administracion/campanas` | Implementado; cupos, pausa y resolución auditada |
| Redacción asistida | Editor de producto → Ayuda para redactar | Implementación preparada; proveedor real pendiente |
| Ranking experimental | `/revista` en desarrollo con flags | Solo previsualización; producción cerrada |

Verificación acumulada actual: **370 pruebas aprobadas**: 148 API/HTTP/PostGIS/
adaptador, 99 BLL/evaluación y 123 frontend, sin omitidas. API/BLL corresponden al
último incremento; la revisión Playwright repitió frontend y harness comercial.
Lint y compilación Next
correctos. Después de la suite completa se amplió la regresión de resolución de
campaña vencida: seis pruebas de campañas aprobadas tras corregirla. Recorridos
reales Next/API/PostGIS, reintentos y pantallas 1440/390/320 comprobados. IA usa
dobles explícitos; no se hicieron llamadas pagas ni se acreditó calidad factual.

Los cambios están en commits locales de las ramas encadenadas hasta
`fix/playwright-commerce-flow`. La [revisión Playwright](qa-playwright-2026-10-05.md)
corrigió paneles obsoletos, selección con precio viejo y mensajes de acceso/sesión.
Sin push, despliegue, migración ni reinicio de la
demo existente. Las bases generadas para verificar fueron eliminadas. Para correr
la versión nueva hay que aplicar las migraciones y recompilar según [App/README](../App/README.md).

## Pendientes que impiden declarar todo terminado

1. Configurar proveedor, modelo y clave de IA en el servidor, luego probar el
   servicio real y revisar calidad. Adaptador OpenAI disponible; modelo sin elegir.
2. Completar entrenamiento/anotaciones de evaluadores humanos y desacuerdos. La
   recolección comprobada hoy sigue mostrando **2286 juicios de calibración
   pendientes**, en 1143 pares/30 consultas. [Paquetes y procedimiento](evaluation/business-ranking-final/packages/README.md).
3. Con esos juicios, seleccionar/congelar política, preparar reserva y ejecutar
   evaluación final; desarrollar/validar el ranking experimental de productos y
   cerrar resultados de la tesis. La búsqueda comercial actual no sustituye esa
   evaluación ni permite afirmar equidad/relevancia demostradas.

El prototipo no cobra suscripciones/campañas, no valida pagos bancarios ni acredita
compras en WhatsApp. La administración registra verificaciones externas. Hosting
y publicación siguen sin contratar/configurar.

Evidencia: [campañas](../openspec/changes/archive/2026-10-05-sponsored-campaigns/verify-report.md)
y [asistencia](../openspec/changes/archive/2026-10-05-catalog-draft-assistance/verify-report.md).
