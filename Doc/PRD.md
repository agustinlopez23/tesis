# PRD — Revista hiperlocal y SaaS para comercios

Versión: 0.4 · Fecha: 2026-09-07 · Estado: borrador para revisión de producto.

Este documento reúne las decisiones del usuario y adapta la propuesta del seminario al producto actual. Los requisitos del MVP son una propuesta concreta de implementación; las decisiones abiertas no se consideran aprobadas. No acredita funcionalidades implementadas ni resultados experimentales.

## 1. Visión y problema

Crear una plataforma para comercios de Córdoba que combine una revista digital de descubrimiento hiperlocal con herramientas SaaS para gestionar catálogo, pedidos y stock. Los consumidores acceden gratis; los negocios tienen presencia gratuita y pueden suscribirse para acceder a catálogo y opciones mejoradas. La publicidad paga constituye otra fuente de ingresos: los negocios pueden contratar campañas para promocionar productos o promociones. La elegibilidad según plan y los posibles beneficios publicitarios de la suscripción quedan por definir.

El consumidor necesita descubrir oferta relevante cerca de su ubicación. El comerciante necesita visibilidad y una forma sencilla de transformar ese interés en ventas, conservando WhatsApp como canal de conversación. La plataforma debe permitir relacionar el descubrimiento, el pedido y la venta registrada.

El aporte tecnológico central de la tesis es un ranking evaluable que combine relevancia, proximidad y exposición equitativa. La actividad del negocio también forma parte de la orientación acordada; su definición y su incorporación al algoritmo requieren evaluación. La equidad busca distribuir oportunidades según mérito, sin prometer idéntica exposición a todos.

## 2. Decisiones confirmadas

| Tema | Decisión |
| --- | --- |
| Producto | Revista online de negocios de la zona, con herramientas SaaS. |
| Actores | Administrador, negocios y usuarios consumidores. |
| Acceso del consumidor | Gratuito. |
| Modelo comercial | Freemium para negocios; suscripción para catálogo y opciones mejoradas, y contratación de publicidad para productos o promociones. |
| Cierre comercial | Contacto por WhatsApp; sin checkout de compra dentro de la plataforma. |
| Trazabilidad | Vincular producto y pedido originado en la plataforma con la venta y el movimiento de stock. |
| Referencias funcionales | Gestión de Kyte y recorrido de pedidos de Pedix. |
| Alcance inicial de pedidos | Un negocio por pedido, con confirmación desde su panel. |
| Diferencial académico | Ranking hiperlocal de exposición equitativa, con evaluación reproducible. |

## 3. Usuarios y responsabilidades

| Actor | Necesidad | Capacidades propuestas para el MVP |
| --- | --- | --- |
| Consumidor | Encontrar comercios y productos cercanos y contactar al vendedor. | Elegir zona, explorar revista, buscar, consultar perfiles y catálogos habilitados, armar y consultar su solicitud de pedido. |
| Responsable del negocio | Dar a conocer su oferta y gestionar oportunidades de venta. | Administrar su propio negocio y publicaciones; según plan, catálogo, pedidos, stock, métricas y publicidad. |
| Administrador | Operar y moderar la plataforma. | Gestionar cuentas, negocios, suscripciones, categorías y publicidad; suspender contenido y consultar evidencia de evaluación. |

Propuesta de simplificación: un responsable y una ubicación por negocio en el MVP. El modelo debe aislar los datos de cada negocio: un responsable no puede consultar ni modificar pedidos, stock o métricas privados de otro.

## 4. Propuesta de valor y planes

Se proponen dos nombres de plan, sin definir precios ni tarifas:

- **Presencia — gratuito:** para que el negocio sea descubierto en su zona y reciba contactos.
- **Impulso — con suscripción:** para transformar esa presencia en pedidos gestionados, ventas registradas y campañas de productos medibles.

La siguiente distribución es la propuesta de producto para el MVP, pendiente de validación del usuario. Los consumidores mantienen acceso gratuito.

| Capacidad | Presencia | Impulso |
| --- | --- | --- |
| Perfil, ubicación y contacto por WhatsApp | Sí | Sí |
| Participación en la revista y ranking orgánico | Sí | Sí, con las mismas reglas |
| Publicaciones de novedades y promociones | Texto, imagen y vigencia | Además, vinculación con productos del catálogo |
| Catálogo de productos | No | Sí, con precios y disponibilidad |
| Solicitudes por producto | No | Sí, con resumen y referencia para WhatsApp |
| Gestión de ventas y stock | No | Sí, vinculada a solicitudes |
| Estadísticas del negocio | Visitas al perfil y clics de contacto | Además, consultas por producto, conversión e importes de ventas reportadas |
| Contratar campañas de promociones | Sí, como contratación adicional | Sí, como contratación adicional |
| Contratar campañas de productos del catálogo | No | Sí, como contratación adicional |
| Métricas publicitarias | Impresiones y clics por campaña | Además, solicitudes y ventas reportadas vinculadas a la campaña |

La ventaja publicitaria de Impulso es poder promocionar productos directamente desde el catálogo y seguir el recorrido hasta la venta registrada. La suscripción no incluye automáticamente campañas: estas se contratan aparte. No se definen descuentos, créditos publicitarios ni precios en este PRD.

Los espacios patrocinados se identifican visualmente y se separan de los resultados orgánicos. Pagar una suscripción o campaña no modifica el puntaje orgánico. Los cupos y la rotación publicitaria se especificarán antes de implementar.

### Publicidad como fuente de ingresos

El negocio puede pagar para dar visibilidad adicional a un producto o a una promoción vigente ante consumidores de una zona. Se distinguen los ingresos por suscripción de los ingresos por campañas publicitarias; no se presupone que suscribirse incluya campañas ilimitadas.

Recorrido propuesto para el MVP:

1. El responsable elige un producto de su catálogo o crea una promoción con título, imagen, descripción, condiciones y vigencia.
2. Define la zona objetivo y el período de difusión del paquete publicitario; este documento no establece tarifas.
3. Solicita la contratación. El administrador revisa el contenido y registra la confirmación del pago antes de activar la campaña.
4. La campaña aparece en espacios identificados como **Patrocinado**, dentro de la zona y las fechas acordadas.
5. El consumidor abre el producto o la promoción y puede contactar al negocio. Si se genera una solicitud, esta conserva el identificador de campaña.
6. El negocio consulta impresiones, clics, solicitudes y ventas reportadas atribuibles a la campaña.

Propuesta comercial inicial: paquetes de duración y zona definidas y activación manual, sin establecer tarifas. Los cupos y condiciones de exposición quedan pendientes; no se promete un número de ventas. No se requiere integrar una pasarela de pago para el prototipo.

Estados propuestos: borrador, pendiente de revisión, pendiente de pago, programada, activa, pausada, finalizada y rechazada. Solo se muestra una campaña aprobada, con pago confirmado, dentro de vigencia y cuyo negocio y contenido sigan habilitados. El negocio puede solicitar o realizar su pausa desde el panel; el administrador puede suspenderla por moderación.

Las campañas de productos requieren catálogo habilitado. Las promociones deben tener un destino público con sus condiciones y contacto. Si se habilita publicidad para negocios gratuitos, una promoción no les concede catálogo ni gestión de pedidos de productos. Antes de implementar se debe fijar qué ocurre con una campaña paga cuando vence el plan, se agota el producto o se suspende el contenido, incluyendo su tratamiento comercial.

La exposición patrocinada y sus ingresos se reportan por separado. Las métricas de ventas son reportadas por el negocio y no equivalen a compras verificadas por WhatsApp. No se vende una mejor posición en el ranking orgánico.

Para el prototipo se propone un único plan pago, activado manualmente por el administrador, sin procesar cobros recurrentes. El precio y la facturación comercial quedan pendientes. Los permisos deben depender del estado del plan, aunque su activación sea manual.

Al vencer una suscripción se propone ocultar el catálogo público y bloquear nuevas solicitudes de productos, conservando el perfil gratuito y el acceso a pedidos existentes para su resolución. No borrar historial ni inventario por vencimiento. La política definitiva debe validarse.

## 5. Alcance del MVP

### Incluido

- Acceso y autorización de los tres roles; exploración pública propuesta sin cuenta.
- Alta y gestión de negocios, ubicación y moderación básica.
- Revista con tarjetas de negocios y novedades, zona seleccionable y categorías.
- Búsqueda de negocios y búsqueda de productos de catálogos habilitados.
- Catálogo con nombre, descripción, categoría, foto, precio y disponibilidad.
- Solicitud de pedido de un solo negocio, mensaje preparado para WhatsApp y seguimiento desde el panel.
- Confirmación de venta, descuento de stock e historial de movimientos relacionados.
- Permisos por plan y gestión manual de suscripciones.
- Contratación de campañas para productos o promociones, segmentación por zona y vigencia, revisión y activación manual, espacios patrocinados identificados y métricas por campaña.
- Registro de exposición, visitas, clics, solicitudes y ventas reportadas.
- Ranking configurable y experimento reproducible para la tesis.

### Fuera del MVP

- Cobro al consumidor, pasarela de pagos, comisiones por compra y logística integrada.
- Carrito con varios negocios, sucursales y múltiples empleados por negocio.
- Lectura automática de conversaciones de WhatsApp o confirmación automática de mensajes enviados.
- Facturación fiscal, contabilidad, integración con cajas externas y cobro recurrente automático del SaaS.
- Reseñas, chat interno, recomendaciones personales avanzadas y subastas publicitarias.
- Gestión de reservas de stock con vencimiento: inicialmente la solicitud no reserva unidades.

La asistencia de IA para borradores de catálogo se incluye en la propuesta documental del prototipo para mantener los objetivos específicos de la primera entrega. Su uso es opcional para el comerciante: ofrece borradores editables, requiere confirmación, permite carga manual y permanece fuera del ranking. Retirarla del alcance exige revisar los objetivos académicos, no solo ocultar la función.

## 6. Recorridos principales

### A. Descubrir un negocio

1. El consumidor permite usar su ubicación o selecciona una zona manualmente.
2. La revista presenta negocios y novedades elegibles de esa zona.
3. Puede filtrar por categoría o buscar; los resultados orgánicos usan el ranking correspondiente.
4. Abre el perfil y contacta al negocio o entra al catálogo, si está habilitado.
5. Un contacto desde perfil se mide como intención de contacto, sin atribuirlo a un producto.

### B. Solicitar productos y contactar por WhatsApp

1. El consumidor selecciona productos y cantidades de un negocio.
2. Revisa el resumen y los precios mostrados; se informa que el negocio debe confirmar disponibilidad y venta.
3. La plataforma valida productos y cantidades y crea una solicitud con referencia única.
4. Prepara el mensaje con referencia y resumen y ofrece abrir WhatsApp.
5. El negocio ve la solicitud en su panel aunque el consumidor no haya enviado el mensaje.
6. Si abrir WhatsApp falla, el consumidor puede volver a intentarlo o copiar la referencia y el resumen sin crear otra solicitud.

Propuesta: no exigir cuenta para explorar. La identidad y los datos mínimos necesarios para crear una solicitud, así como el acceso privado a su estado, deben especificarse antes de implementar este recorrido.

### C. Registrar la venta y descontar stock

1. El negocio localiza la solicitud por referencia y confirma si recibió el contacto.
2. Puede acordar cambios y registrar los productos, cantidades y precios finales, preservando el pedido original.
3. Al registrar la venta, el sistema comprueba disponibilidad y descuenta las cantidades efectivamente vendidas.
4. La venta y sus movimientos de stock conservan la relación con la solicitud y su origen.
5. Si falta stock, la operación no se confirma; el negocio debe ajustar la venta.

Las ventas presenciales y los ajustes de inventario se registran con un origen propio. No se atribuyen a una consulta por coincidir en producto o fecha.

## 7. Pedidos, ventas e inventario

Estados propuestos de solicitud: **pendiente**, **en atención**, **convertida en venta**, **cancelada**. La confirmación de contacto recibido es un dato separado: abrir WhatsApp no lo demuestra.

| Regla | Comportamiento requerido |
| --- | --- |
| Solicitud | Guarda negocio, productos, cantidades, precios de referencia, fecha y origen disponible. |
| WhatsApp | El mensaje preparado incluye la referencia; su envío y recepción no se presumen. |
| Stock | Solicitar o abrir WhatsApp no descuenta ni reserva stock. |
| Confirmación | Registrar venta y descontar stock debe ser una operación atómica. |
| Concurrencia | Dos ventas simultáneas no pueden vender las mismas últimas unidades ni generar stock negativo. |
| Reintentos | Confirmar repetidamente la misma operación no crea ventas ni descuentos duplicados. |
| Cambios | Los artículos finales pueden diferir de los solicitados; ambos se conservan para medir sustituciones. |
| Cancelación | Cancelar una solicitud sin venta no cambia stock. |
| Correcciones | Una venta registrada se corrige mediante una operación auditada; la reposición de unidades requiere registrar su devolución efectiva. |
| Inventario manual | Todo ajuste indica cantidad, motivo, responsable y fecha; no genera conversión. |
| Trazabilidad | Una venta atribuida apunta explícitamente a una solicitud; no se deduce por proximidad temporal. |

Propuesta de límite: una venta por solicitud en el MVP, sin entregas ni conversiones parciales múltiples. Las cantidades finales pueden reducirse al registrar esa venta.

## 8. Ranking: aporte tecnológico

### 8.1 Base del seminario

El documento académico de `Doc/Seminario1 entrega` define un ranking de productos:

`S = 0,40 × Rel + 0,30 × Prox + 0,10 × Disp + 0,20 × Eq`

- **Rel:** coincidencia de consulta con nombre, descripción y categoría, mediante búsqueda textual y similitud de trigramas.
- **Prox:** `exp(-d / λ)`, con distancia `d` en kilómetros y `λ = 1,5 km` inicial.
- **Disp:** puntaje pleno con stock, intermedio con stock bajo y exclusión o relegación sin stock.
- **Eq:** compensación de exposición insuficiente respecto del mérito del negocio, estimado por relevancia y cercanía en búsquedas en las que participa.

Los factores se normalizan entre 0 y 1. Pesos y parámetros son iniciales, configurables y sujetos a calibración; no son resultados validados.

La exposición considera una ventana de 30 días y descuento por posición `1 / log2(posición + 1)`. El seminario agrega bonificación decreciente para negocios nuevos, máximo de dos productos del mismo negocio en los diez primeros y rotación controlada entre puntajes similares.

### 8.2 Adaptación a la revista y al freemium

Se requieren dos contextos explícitos:

| Contexto | Unidad ordenada | Adaptación |
| --- | --- | --- |
| Búsqueda de productos | Producto de un catálogo habilitado | Partir de la fórmula del seminario y limitar concentración por negocio. |
| Revista y búsqueda de negocios | Negocio, acompañado de su contenido | Usar cercanía, pertinencia de categoría o consulta, actividad y equidad; sin exigir catálogo ni stock. |

Para la revista se propone una tarjeta por negocio por tanda de resultados. La ausencia de catálogo en el plan gratuito no debe contarse como falta de actividad. Tener más productos no debe elevar por sí solo el mérito del negocio.

Actividad propuesta: vigencia de la información y novedades sustantivas recientes, con aporte acotado. Repetir publicaciones o editar sin cambios relevantes no debe acumular ventajas ilimitadas. No se propone usar ventas o clics brutos como sinónimo de calidad: podría reforzar a quienes ya reciben más exposición.

La fórmula de negocios, los pesos de actividad y la pertinencia cuando no hay consulta textual quedan pendientes de especificación. No reutilizar silenciosamente la fórmula de productos para la revista.

### 8.3 Reglas y contratos por especificar

- Filtrar negocios suspendidos, contenido no publicado y resultados fuera de la zona elegida antes de ordenar. El radio de búsqueda es distinto de `λ`, que controla el decaimiento de proximidad.
- Propuesta para solicitudes: excluir productos sin stock; fijar umbral de stock bajo antes de implementar.
- Separar exposición orgánica de publicidad tanto al presentar como al medir equidad.
- Precisar qué constituye una impresión visible y deduplicar recargas y eventos repetidos.
- Definir exposición esperada, normalización de Eq, casos sin historial y protección frente a divisiones por cero.
- Fijar duración y máximo de bonificación inicial, tolerancia de empate y semilla de rotación reproducible.
- Evitar que bonificación y equidad eleven contenido ajeno a la consulta: definir elegibilidad y umbral de relevancia.
- Versionar configuraciones y guardar la versión usada en cada experimento.
- Mantener registros suficientes para explicar componentes del puntaje y auditar resultados.

## 9. Evaluación de la tesis

Hipótesis: el algoritmo completo reduce la concentración de exposición y amplía la cobertura de comercios, con una pérdida acotada de relevancia frente a las variantes de referencia.

Base experimental del seminario: aproximadamente 200 comercios, 3.000 productos y 5.000 consultas con ubicaciones distribuidas en Córdoba. Comparar relevancia sola, relevancia más proximidad y algoritmo completo usando el mismo conjunto de datos y consultas.

| Métrica | Qué evalúa |
| --- | --- |
| Gini de exposición orgánica | Concentración de visibilidad entre negocios. |
| Participación de los diez más expuestos | Porción de exposición acumulada por los negocios dominantes. |
| Cobertura en top 10 | Proporción de negocios elegibles con al menos una aparición entre los diez primeros. |
| nDCG@10 | Calidad del orden respecto de juicios de relevancia definidos para el experimento. |
| Desviación respecto de exposición por mérito | Si el reparto respeta oportunidades esperadas según relevancia y cercanía. |

Evaluar además escenarios con negocios nuevos, zonas de distinta densidad y negocios con muchos productos. Para estudiar actividad, agregar una variante con y sin esa señal. Evaluar por separado revista y búsqueda de productos.

Los juicios de relevancia deben establecerse independientemente del puntaje que se evalúa. Fijar semillas, orden temporal de consultas, actualización del historial y configuraciones idénticas entre corridas comparables. Reportar variación entre corridas y límites de los datos sintéticos; no presentar simulación como evidencia de incremento de ventas reales.

Los umbrales de mejora y pérdida máxima aceptable de nDCG deben acordarse antes del experimento final. No basta con obtener un Gini menor si los resultados dejan de ser útiles.

## 10. Métricas de producto y atribución

| Evento / métrica | Interpretación |
| --- | --- |
| Impresión orgánica o patrocinada | Oportunidad de exposición, según contrato de visibilidad. |
| Visita a perfil o producto | Interés en la oferta. |
| Clic de contacto | Intención de abrir WhatsApp; no prueba envío. |
| Solicitud creada | Productos y cantidades registrados en la plataforma. |
| Contacto confirmado por negocio | Recepción reportada del contacto. |
| Venta vinculada | Venta registrada por el negocio y relacionada con una solicitud. |
| Conversión de solicitudes | Solicitudes convertidas / solicitudes creadas de una misma cohorte y ventana de seguimiento. |
| Importe atribuido | Suma de ventas vinculadas, identificado como reportado por negocios. |

Preservar origen disponible: revista, búsqueda, anuncio o acceso directo, además de producto y versión del ranking cuando corresponda. No inventar atribución si falta evidencia. Definir ventana de seguimiento y tratamiento de cancelaciones y devoluciones antes de construir reportes.

## 11. Requisitos de calidad

- Interfaz responsiva, priorizando uso desde celular y textos claros en español.
- Selección manual de zona cuando se deniega o falla la geolocalización.
- Navegación por teclado, etiquetas en formularios y estados de carga y error perceptibles.
- Autorización en servidor por rol, propiedad del recurso y plan; ocultar botones no sustituye controles.
- Pedidos y datos de contacto privados; referencias públicas no deben permitir enumerar información personal.
- Recopilar únicamente datos necesarios; precisar retención de ubicación, contactos y eventos antes de implementación.
- Registro auditado de ventas, ajustes, cambios de plan y moderación, sin exponer secretos ni conversaciones.
- Ante fallos de WhatsApp, conservar la solicitud y permitir recuperar el resumen.
- Objetivo inicial propuesto: respuesta de búsqueda p95 inferior a 1 segundo con el conjunto experimental, reportando infraestructura y carga de prueba. Validar el objetivo en la spec técnica.

## 12. Criterios de aceptación del MVP

1. Un consumidor puede elegir zona manualmente, descubrir un negocio gratuito y contactarlo sin acceder a funciones pagas.
2. Un negocio gratuito no puede publicar catálogo ni recibir nuevas solicitudes de productos mediante API o interfaz.
3. Un negocio suscripto puede publicar productos y recibir una solicitud con referencia y resumen para WhatsApp.
4. Crear una solicitud no altera stock ni se contabiliza como venta o mensaje recibido.
5. El negocio puede registrar una venta vinculada; venta y descuento se guardan juntos, sin duplicados ni stock negativo ante concurrencia.
6. Una venta presencial o un ajuste de stock no se atribuye automáticamente a contactos de la revista.
7. Un responsable no puede acceder a información privada de otro negocio, incluso modificando identificadores en solicitudes HTTP.
8. Un negocio elegible puede solicitar una campaña para un producto o una promoción, con zona y fechas definidas. Solo se muestra tras aprobación y confirmación de pago, mientras siga vigente y habilitada. Los espacios patrocinados son identificables y sus impresiones no se mezclan con la exposición orgánica.
9. El ranking respeta elegibilidad y límites por negocio y produce resultados reproducibles bajo una configuración experimental fija.
10. Se entrega una comparación de variantes con métricas de equidad y relevancia, sin afirmar mejoras no observadas.

## 13. Secuencia de entrega propuesta

1. Validar este PRD y cerrar decisiones necesarias para el primer caso de uso.
2. Identidad, autorización, alta de negocio y capacidades por plan.
3. Perfil público, ubicación, revista básica y registro de exposición.
4. Catálogo, búsqueda y solicitudes con referencia para WhatsApp.
5. Registro de ventas, inventario y atribución desde solicitudes.
6. Ranking completo, actividad definida y evaluación experimental.
7. Publicidad, reportes y revisión integral del MVP.

Cada entrega se descompone en propuestas y especificaciones OpenSpec con criterios observables antes de implementar. Al 2026-09-18 están implementados el scaffold, el onboarding, la guía pública con búsqueda/rubro, selección de zona o ubicación, radio y distancias, y perfiles públicos con galería de hasta cinco fotos administradas por el responsable. La guía ordena por distancia cuando hay centro y por nombre cuando no lo hay. Ranking equitativo, medición de exposición y las demás capacidades continúan pendientes. La evidencia distingue pruebas previas a la implementación de ampliaciones posteriores. Ver [estado técnico](../openspec/project.md).

## 14. Decisiones abiertas

| Decisión | Propuesta inicial / impacto |
| --- | --- |
| Nombre del producto | Pendiente; mantener nombre descriptivo. |
| Rubros piloto | Elegir pocos rubros con productos por unidad; variantes, peso y fraccionamiento requieren alcance adicional. |
| Cuenta del consumidor | Exploración pública; definir identificación y acceso privado a solicitudes. |
| Planes | Validar nombres Presencia e Impulso y capacidades propuestas; los precios quedan fuera del PRD. |
| Vencimiento de plan | Preservar datos y resolver pedidos existentes; validar política de acceso. |
| Actividad | Definir eventos sustantivos, ventana, límites y pesos, también para negocios gratuitos. |
| Revista sin búsqueda | Definir pertinencia y fórmula de negocios; no hay relevancia textual disponible. |
| Ranking de productos | Fijar Eq, bonificación, umbrales, desempates y normalización. |
| Publicidad | Validar elegibilidad propuesta por plan, cupos, segmentación, rotación y tratamiento de campañas interrumpidas o afectadas por vencimiento del plan. |
| Inventario | Validar alcance por unidades y regla de reposición por devolución. |
| Evaluación | Acordar umbrales de éxito, relevancia de referencia y diseño de corridas. |
| IA de catálogo | Se conserva en la propuesta documental por coherencia académica; revisar antes de cualquier reducción de alcance. |

## 15. Fuentes y antecedentes

- [Documentación UML del prototipo](Entrega2/README.md): diagnóstico, requisitos, fichas, secuencias, pantallas, despliegue y validación. Sus parámetros nuevos son propuestas revisables.

- [Modelo de entidades UML y ER](model/README.md): propuesta de entidades, relaciones y restricciones derivada de este PRD.

- Decisiones de producto acordadas con el usuario en la conversación del 2026-09-07.
- Documento académico local en `Doc/Seminario1 entrega`, sección «Diseño del algoritmo de ranking orgánico» y sección de asistencia de IA. El original permanece excluido de Git.
- [ADR de arquitectura](architecture/ADR-001-layered-architecture.md) y [estado técnico](../openspec/project.md).
- [Kyte: recibir pedidos por WhatsApp](https://docs.kyteapp.com/en/articles/9441046-receive-your-orders-on-whatsapp): referencia para pedido estructurado y resumen por mensajería.
- [Kyte: gestión de ventas por WhatsApp](https://www.kyteapp.com/selling/whatsapp-sales): referencia de catálogo, ventas e inventario.
- [Pedix: directorio de comercios](https://tiendas.pedix.app/): antecedente de descubrimiento local y múltiples rubros. La variedad de rubros por sí sola no constituye el diferencial de la tesis.

Estos antecedentes orientan el diseño; no acreditan que sus productos implementen el ranking equitativo aquí propuesto.
