# Diagnóstico y propuesta

Versión 1.0 · 2026-09-07 · Borrador académico para revisión.

El diagnóstico se elabora a partir del relevamiento funcional de la organización modelada presentado en la primera entrega. Se conservan los nombres de sus tres procesos y se identifican problemas y causas operativas. Las relaciones causales son interpretaciones del relevamiento documental; no se agregan entrevistas, frecuencias ni mediciones de campo que no estén acreditadas. La numeración de tablas es local a este apartado y deberá ajustarse al integrar el trabajo final.

**Tabla 1. Diagnóstico del proceso incorporación y publicación de productos**

Nombre del proceso: **incorporación y publicación de productos**.

| Problemas | Causas |
| --- | --- |
| 1. La oferta publicada contiene información heterogénea e incompleta que dificulta identificar y comparar los productos. | 1. Las publicaciones se preparan mediante fotografías ocasionales y texto libre, sin una pauta común para registrar nombre, descripción, categoría y disponibilidad. |
| 2. La preparación de publicaciones exige repetir tareas de registro de información. | 2. El precio se anota primero en papel o planilla y luego se transcribe al elaborar la publicación. El dueño concentra estas tareas junto con la atención del local y la reposición. |
| 3. Las publicaciones ofrecen escaso contexto para descubrir productos pertinentes de comercios cercanos. | 3. La oferta se distribuye en redes sociales y estados de WhatsApp sin una clasificación uniforme que relacione producto, rubro y ubicación del comercio. |

Fuente: elaboración propia a partir del relevamiento funcional de la primera entrega.

**Tabla 2. Diagnóstico del proceso actualización de precios y disponibilidad**

Nombre del proceso: **actualización de precios y disponibilidad**.

| Problemas | Causas |
| --- | --- |
| 1. Los precios de publicaciones anteriores pueden diferir del precio vigente. | 1. La corrección se realiza en la lista de precios, mientras que las publicaciones difundidas conservan los valores anteriores. Cada soporte exige una actualización independiente. |
| 2. El consumidor puede consultar productos que ya no están disponibles. | 2. Los faltantes se comunican verbalmente o por mensajería cuando llega una consulta, sin actualizar sistemáticamente la oferta publicada. |
| 3. La disponibilidad depende de verificaciones reiteradas del comerciante. | 3. Se comprueba visualmente la góndola o el depósito; el proceso relevado no registra entradas, ventas y ajustes en una secuencia vinculada al producto. |

Fuente: elaboración propia a partir del relevamiento funcional de la primera entrega.

**Tabla 3. Diagnóstico del proceso atención de consultas y venta**

Nombre del proceso: **atención de consultas y venta**.

| Problemas | Causas |
| --- | --- |
| 1. La atención exige volver a confirmar precio y existencia para cada consulta. | 1. El consumidor consulta sobre publicaciones potencialmente incompletas o desactualizadas y el comerciante verifica físicamente la disponibilidad antes de responder. |
| 2. La consulta inicial y la venta concretada no quedan relacionadas explícitamente. | 2. La conversación transcurre en el local o en WhatsApp y la venta se anota en un cuaderno, sin una referencia compartida que conserve lo consultado y lo vendido. |
| 3. Resulta difícil atribuir ventas al interés generado por la oferta digital. | 3. Los registros descritos no vinculan canal de origen, consulta y venta, por lo que una anotación de venta aislada no permite reconstruir ese recorrido. |

Fuente: elaboración propia a partir del relevamiento funcional de la primera entrega.

El diagnóstico no introduce «ranking» ni «suscripciones» como procesos relevados: son elementos de la solución propuesta. La asimetría de visibilidad se retoma del área problemática de la primera entrega y se aborda mediante el descubrimiento de la oferta, sin afirmar que se haya medido su magnitud en comercios reales.

## Propuesta

Se propone desarrollar una plataforma web hiperlocal para pequeños comercios y PyMEs de Córdoba. El consumidor accederá a una revista digital que presente negocios de su zona ordenados mediante un algoritmo de relevancia, proximidad y exposición equitativa. Podrá consultar perfiles, novedades y promociones y, cuando el negocio disponga de catálogo, encontrar productos y generar una solicitud para continuar la conversación por WhatsApp.

Para la incorporación y publicación de productos, la solución ofrecerá un catálogo estructurado y asistencia opcional de inteligencia artificial para sugerir nombre, descripción y categoría a partir de una imagen y datos mínimos. Las sugerencias se presentarán como borradores editables y ninguna se publicará sin confirmación del responsable. La carga manual estará siempre disponible. Esta función mantiene coherencia con los objetivos específicos de la primera entrega.

Para la actualización de precios y disponibilidad, el negocio podrá mantener la oferta vigente y registrar movimientos básicos de inventario. Para la atención de consultas y venta, cada solicitud conservará una referencia y el detalle original. El comerciante podrá registrar lo efectivamente vendido, vinculándolo con la solicitud y actualizando el stock en una misma operación. Se distinguirán el clic de contacto, la recepción reportada y la venta informada: abrir WhatsApp no demostrará que se envió un mensaje ni que se concretó una compra.

La plataforma adoptará un modelo SaaS freemium: Presencia ofrecerá perfil y contacto, mientras que Impulso habilitará catálogo, pedidos, stock y métricas de conversión. Las campañas publicitarias se contratarán por separado y se identificarán como patrocinadas, sin modificar el puntaje orgánico. Los precios comerciales no forman parte de esta documentación. El prototipo utilizará habilitación manual de suscripciones y campañas para demostrar su funcionamiento sin integrar cobros recurrentes.

El algoritmo se evaluará mediante simulaciones con datos sintéticos, comparando distribución de exposición, cobertura y relevancia con variantes de referencia. La asistencia de catálogo se evaluará mediante calidad de las sugerencias y tiempo de carga. Estos son resultados esperados a verificar, no beneficios cuantificados ya obtenidos.

# Objetivo, límites y alcances del prototipo

## Objetivo del prototipo

**OP-01.** Implementar y validar un prototipo web que permita a comercios de proximidad de Córdoba publicar y actualizar su oferta, a consumidores descubrir negocios y productos cercanos mediante un ranking de exposición equitativa, y a responsables de negocios relacionar solicitudes para WhatsApp con ventas reportadas y movimientos de inventario, incorporando asistencia opcional para la creación de catálogo.

Objetivos operativos para vincular los casos de uso:

- **OP-02:** reducir tareas repetidas de publicación mediante información estructurada y borradores asistidos sujetos a revisión.
- **OP-03:** mantener trazabilidad entre solicitudes, ventas e inventario y diferenciar contacto de conversión reportada.
- **OP-04:** medir la distribución de visibilidad y la relevancia del descubrimiento local mediante experimentos reproducibles.
- **OP-05:** demostrar acceso por roles y capacidades del SaaS, publicidad identificada y administración del prototipo.

## Límites

El prototipo comprende desde el registro del negocio y la preparación de su oferta hasta el descubrimiento por zona, la creación de solicitudes y el registro de su eventual conversión en venta, con actualización del stock y consulta de métricas. La conversación, el acuerdo comercial, el pago del comprador y la entrega ocurren fuera de la plataforma. Se modela una ubicación y un responsable por negocio, productos por unidad y un negocio por solicitud.

La habilitación comercial de suscripciones y campañas se registra manualmente. La evaluación sintética permite estudiar el ranking, pero no demostrar incremento real de facturación ni adopción sin un estudio adicional.

## Alcances

1. Registro y acceso de cuentas con roles de consumidor, responsable de negocio y administrador de plataforma; exploración pública sin cuenta.
2. Perfiles, ubicación, rubros, publicaciones y promociones de negocios.
3. Catálogo, precios, disponibilidad y asistencia opcional para borradores con alternativa manual.
4. Entradas, ventas, ajustes y devoluciones físicas de inventario, con responsable y motivo.
5. Revista de negocios ordenados desde el ingreso, selección manual o asistida de zona y búsqueda de productos.
6. Registro de candidatos y exposición orgánica para medir equidad, separado de publicidad.
7. Solicitudes identificadas y resumen para WhatsApp, seguimiento y registro de venta.
8. Planes Presencia e Impulso y habilitación manual de períodos de suscripción.
9. Campañas para promociones o productos, revisión y visualización como patrocinadas.
10. Métricas de visitas, contactos y ventas reportadas; configuración y evaluación reproducible del ranking y evaluación de asistencia de catálogo.

Se excluyen pagos y logística del comprador, facturación fiscal, integración con cajas externas, lectura automática de WhatsApp, sucursales, variantes, fraccionamiento, reseñas, chat interno y personalización avanzada. Las devoluciones físicas no incluyen reembolsos monetarios. No se reservan unidades al crear una solicitud.

## Metodología y coherencia

Se mantiene UML y el desarrollo iterativo e incremental de la primera entrega. Los requisitos se representan mediante casos de uso, fichas, secuencias, clases y despliegue. El DER complementa el modelo orientado a objetos porque la persistencia será relacional. OpenSpec y TDD organizan la implementación técnica, sin sustituir estos entregables académicos por Scrum.

Las interfaces son maquetas estáticas sin lógica de negocio. Las decisiones propuestas y los parámetros todavía no calibrados se identifican en [decisiones y validación](07-decisiones-y-validacion.md). El documento no certifica aprobación del tutor ni implementación de las funciones.
