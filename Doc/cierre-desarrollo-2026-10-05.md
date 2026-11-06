# Estado de cierre local — 05/10/2026

**Actualización del 06/10:** contraste corregido y navegación de productos visible
en móvil/escritorio. Suite frontend 126/126; lint/build y 30 controles comerciales
en tres motores aprobados. Axe: 18 análisis sin violaciones/desbordamiento y dos
comprobaciones de teclado. Conserva inconclusos para revisión humana; no acredita
conformidad WCAG. Demo local actualizada. Backend sin cambios en esta iteración.

El núcleo comercial está implementado y probado localmente. La asistencia de IA
está preparada, desactivada hasta configurar un modelo y credenciales. La tesis
todavía necesita evaluación humana; este cierre técnico no demuestra su hipótesis.

**Demo actualizada:** http://localhost:3000. Migraciones aplicadas con respaldo
restaurado y datos conservados; [estado y reinicio](demo-local-2026-10-05.md).

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

Suite base: **370 pruebas aprobadas**: 148 API/HTTP/PostGIS/
adaptador, 99 BLL/evaluación y 123 frontend, sin omitidas. API/BLL corresponden al
último incremento; la revisión Playwright repitió frontend y harness comercial.
Lint y compilación Next
correctos. Después de la suite completa se amplió la regresión de resolución de
campaña vencida: seis pruebas de campañas aprobadas tras corregirla. Recorridos
reales Next/API/PostGIS, reintentos y pantallas 1440/390/320 comprobados. IA usa
dobles explícitos; no se hicieron llamadas pagas ni se acreditó calidad factual.

Verificaciones posteriores: 3/3 pruebas de integración del seed y matriz
Chromium/Firefox/WebKit con 10 comprobaciones por motor aprobadas. El recorrido
comercial se puede repetir con [un comando](../App/tools/README.md), en base aislada.
No sustituye la prueba en dispositivos físicos ni una auditoría de accesibilidad.

Los cambios están en commits locales de las ramas encadenadas hasta
`fix/playwright-commerce-flow`. La [revisión Playwright](qa-playwright-2026-10-05.md)
corrigió paneles obsoletos, selección con precio viejo y mensajes de acceso/sesión.
Sin push ni despliegue público. Después de esa revisión se actualizó y encendió
la demo existente, con respaldo verificado y las siete migraciones pendientes.
Las bases temporales de pruebas y restauración fueron eliminadas.

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

