# Descripción de casos de uso

Versión 1.0 · 2026-09-07. Actores: consumidor, responsable de negocio y administrador de plataforma. «Usuario» en CU-01 comprende los roles que ingresan; el consumidor puede explorar anónimamente. WhatsApp y el proveedor de IA son participantes externos. El evaluador utiliza autorización de administrador y ejecuta herramientas de evaluación separadas de producción.

Cada caso mostrado en el diagrama general tiene una ficha. El diagrama general no constituye un caso adicional. Las frecuencias son **a relevar**, no cifras inventadas.

## CU-01 — Registrar cuenta y gestionar sesión

| Campo | Descripción |
| --- | --- |
| Versión | 1.0 — 2026-09-07 |
| Objetivos asociados | OP-05; OP-01 |
| Requerimiento principal | RF-01 |
| Actor principal | Usuario |
| Descripción | Registrar cuentas, iniciar y cerrar sesión y rechazar el acceso a cuentas suspendidas. Los permisos se comprobarán en servidor. |
| Precondiciones | Para iniciar sesión debe existir una cuenta habilitada; el registro no requiere sesión. |
| Disparador | El actor inicia la función desde P01. |
| Postcondiciones | Cuenta o sesión gestionada; ningún fallo concede permisos. |
| Frecuencia esperada | A relevar en piloto; ejecución según demanda del actor. |
| Importancia | Alta |
| Comentarios | Modelo propuesto; datos sensibles sujetos a autorización. Pantalla P01. |

**Secuencia normal**

| Paso | Acción |
| --- | --- |
| 1 | El usuario elige registrarse o ingresar e introduce los datos requeridos. |
| 2 | El sistema valida formato y unicidad del correo al registrar, o credenciales al ingresar. |
| 3 | El sistema asigna el rol solicitado permitido; nunca permite autoconcederse administrador. |
| 4 | Se establece una sesión y se muestran las funciones autorizadas. |
| 5 | Al cerrar sesión se invalida el acceso de esa sesión. |

**Cursos alternativos**

| Código | Condición y respuesta |
| --- | --- |
| A1 | Correo duplicado o datos inválidos: informar el campo y conservar datos no sensibles. |
| A2 | Credenciales inválidas o cuenta suspendida: rechazar sin revelar datos de otra cuenta. |

Fuente: elaboración propia.
## CU-02 — Gestionar perfil del negocio

| Campo | Descripción |
| --- | --- |
| Versión | 1.0 — 2026-09-07 |
| Objetivos asociados | OP-02; OP-01 |
| Requerimiento principal | RF-02 |
| Actor principal | Responsable del negocio |
| Descripción | Crear, editar y archivar el perfil propio con nombre, descripción, rubro, ubicación y WhatsApp, validando los datos antes de publicarlo. |
| Precondiciones | Responsable autenticado; para editar debe ser propietario. |
| Disparador | El actor inicia la función desde P02. |
| Postcondiciones | Perfil persistido con estado y actividad correspondientes. |
| Frecuencia esperada | A relevar en piloto; ejecución según demanda del actor. |
| Importancia | Alta |
| Comentarios | Modelo propuesto; datos sensibles sujetos a autorización. Pantalla P02. |

**Secuencia normal**

| Paso | Acción |
| --- | --- |
| 1 | El responsable abre su perfil o solicita crear un negocio. |
| 2 | Ingresa nombre, rubro, descripción, dirección, ubicación y WhatsApp. |
| 3 | El sistema valida los campos y la propiedad del recurso. |
| 4 | Guarda el perfil como borrador o publicado y registra el cambio. |
| 5 | La oferta pública refleja únicamente perfiles habilitados. |

**Cursos alternativos**

| Código | Condición y respuesta |
| --- | --- |
| A1 | Ubicación o WhatsApp inválidos: conservar borrador e indicar corrección. |
| A2 | Recurso de otro negocio: denegar acceso sin modificarlo. |

Fuente: elaboración propia.

## CU-03 — Gestionar novedades y promociones

| Campo | Descripción |
| --- | --- |
| Versión | 1.0 — 2026-09-07 |
| Objetivos asociados | OP-02; OP-01 |
| Requerimiento principal | RF-03 |
| Actor principal | Responsable del negocio |
| Descripción | Crear, editar, publicar y archivar novedades o promociones propias con condiciones y vigencia; vincular productos solo cuando el plan habilite catálogo. |
| Precondiciones | Responsable autenticado con negocio habilitado. |
| Disparador | El actor inicia la función desde P03. |
| Postcondiciones | Publicación guardada y disponible según estado y vigencia. |
| Frecuencia esperada | A relevar en piloto; ejecución según demanda del actor. |
| Importancia | Alta |
| Comentarios | Modelo propuesto; datos sensibles sujetos a autorización. Pantalla P03. |

**Secuencia normal**

| Paso | Acción |
| --- | --- |
| 1 | Abre publicaciones y selecciona novedad o promoción. |
| 2 | Completa contenido y, si es promoción, condiciones y fechas. |
| 3 | Opcionalmente vincula un producto propio si tiene capacidad de catálogo. |
| 4 | El sistema valida vigencia y guarda o publica el contenido. |
| 5 | Registra actividad sustantiva deduplicada; los contenidos vencidos dejan de ofrecerse como vigentes. |

**Cursos alternativos**

| Código | Condición y respuesta |
| --- | --- |
| A1 | Fechas inconsistentes o producto ajeno: rechazar guardado. |
| A2 | Plan sin catálogo: permitir promoción independiente y rechazar vínculo de producto. |

Fuente: elaboración propia.

## CU-04 — Gestionar catálogo y precios

| Campo | Descripción |
| --- | --- |
| Versión | 1.0 — 2026-09-07 |
| Objetivos asociados | OP-02; OP-01 |
| Requerimiento principal | RF-04 |
| Actor principal | Responsable del negocio |
| Descripción | Crear, editar, publicar y archivar productos con nombre, descripción, categoría, imagen, precio, moneda y disponibilidad, preservando los valores históricos de pedidos y ventas. |
| Precondiciones | Responsable autenticado con catálogo habilitado. |
| Disparador | El actor inicia la función desde P04. |
| Postcondiciones | Oferta vigente actualizada; stock cambia únicamente mediante movimientos. |
| Frecuencia esperada | A relevar en piloto; ejecución según demanda del actor. |
| Importancia | Alta |
| Comentarios | Modelo propuesto; datos sensibles sujetos a autorización. Pantalla P04. |

**Secuencia normal**

| Paso | Acción |
| --- | --- |
| 1 | Abre su catálogo y elige un producto o una nueva carga. |
| 2 | Completa los datos comerciales y la categoría. |
| 3 | Revisa y confirma guardar o publicar. |
| 4 | El sistema valida propiedad, capacidad y valores no negativos. |
| 5 | Persiste el producto; en el alta crea saldo cero y conserva históricos existentes. |

**Cursos alternativos**

| Código | Condición y respuesta |
| --- | --- |
| A1 | Plan vencido: bloquear nuevas publicaciones sin borrar datos. |
| A2 | Precio inválido o categoría inexistente: informar el campo y no publicar. |

Fuente: elaboración propia.

## CU-05 — Generar borrador asistido de producto

| Campo | Descripción |
| --- | --- |
| Versión | 1.0 — 2026-09-07 |
| Objetivos asociados | OP-02; OP-01 |
| Requerimiento principal | RF-05 |
| Actor principal | Responsable del negocio |
| Descripción | Solicitar sugerencias de nombre, descripción y categoría desde una imagen y datos mínimos, editarlas o descartarlas, sin publicar automáticamente y permitiendo carga manual ante fallos. |
| Precondiciones | Sesión y catálogo habilitados; imagen válida seleccionada. |
| Disparador | El actor inicia la función desde P05. |
| Postcondiciones | Borrador revisable; no existe publicación sin confirmación. |
| Frecuencia esperada | A relevar en piloto; ejecución según demanda del actor. |
| Importancia | Alta |
| Comentarios | Modelo propuesto; datos sensibles sujetos a autorización. Pantalla P05. |

**Secuencia normal**

| Paso | Acción |
| --- | --- |
| 1 | El responsable selecciona una imagen y solicita asistencia. |
| 2 | El sistema envía solo la imagen y los datos mínimos al proveedor de IA. |
| 3 | Valida que la respuesta estructurada incluya una categoría admitida. |
| 4 | Presenta las sugerencias como borrador editable. |
| 5 | El responsable acepta, corrige o descarta y continúa con CU-04 para guardar. |

**Cursos alternativos**

| Código | Condición y respuesta |
| --- | --- |
| A1 | Proveedor falla, demora o devuelve formato inválido: ofrecer formulario manual sin bloqueo. |
| A2 | Sugerencia incorrecta: permitir editar o descartar sin publicación. |

Fuente: elaboración propia.

## CU-06 — Registrar movimientos de inventario

| Campo | Descripción |
| --- | --- |
| Versión | 1.0 — 2026-09-07 |
| Objetivos asociados | OP-03; OP-01 |
| Requerimiento principal | RF-06 |
| Actor principal | Responsable del negocio |
| Descripción | Registrar entradas y ajustes de stock por producto con cantidad, motivo y responsable, mantener historial y rechazar saldos negativos y operaciones duplicadas. |
| Precondiciones | Sesión y gestión de inventario habilitadas; producto propio existente. |
| Disparador | El actor inicia la función desde P06. |
| Postcondiciones | Saldo conciliable con movimientos; no hay stock negativo. |
| Frecuencia esperada | A relevar en piloto; ejecución según demanda del actor. |
| Importancia | Alta |
| Comentarios | Modelo propuesto; datos sensibles sujetos a autorización. Pantalla P06. |

**Secuencia normal**

| Paso | Acción |
| --- | --- |
| 1 | Consulta saldo e historial de un producto. |
| 2 | Selecciona entrada o ajuste e indica cantidad y motivo. |
| 3 | El sistema valida la clave de operación y la disponibilidad si el ajuste resta. |
| 4 | Registra movimiento y saldo en una transacción con control concurrente. |
| 5 | Muestra el nuevo saldo y el historial. |

**Cursos alternativos**

| Código | Condición y respuesta |
| --- | --- |
| A1 | Operación repetida: devolver el resultado previo sin aplicar otra vez. |
| A2 | Saldo insuficiente o conflicto concurrente: no modificar parcialmente e informar. |

Fuente: elaboración propia.

## CU-07 — Descubrir negocios de la zona

| Campo | Descripción |
| --- | --- |
| Versión | 1.0 — 2026-09-07 |
| Objetivos asociados | OP-04; OP-01 |
| Requerimiento principal | RF-07 |
| Actor principal | Consumidor |
| Descripción | Mostrar al ingresar una revista de negocios ordenados por el ranking hiperlocal, con ubicación manual o del dispositivo, filtros de rubro y búsqueda textual opcional, incluyendo negocios gratuitos elegibles. |
| Precondiciones | No requiere cuenta; debe obtenerse o elegirse una zona antes de consultar resultados locales. |
| Disparador | El actor inicia la función desde P07. |
| Postcondiciones | Resultados correspondientes a la zona, contexto y versión; exposición sin confundir anuncios. |
| Frecuencia esperada | A relevar en piloto; ejecución según demanda del actor. |
| Importancia | Crítica |
| Comentarios | Modelo propuesto; datos sensibles sujetos a autorización. Pantalla P07. |

**Secuencia normal**

| Paso | Acción |
| --- | --- |
| 1 | El consumidor abre la revista y elige zona o permite geolocalización. |
| 2 | Opcionalmente selecciona rubro o escribe una búsqueda. |
| 3 | El sistema filtra negocios publicados de la zona y calcula factores con configuración versionada. |
| 4 | Presenta una tarjeta por negocio en orden orgánico y espacios patrocinados separados. |
| 5 | Registra candidatos y, al cumplirse la regla de visibilidad, impresiones orgánicas. |

**Cursos alternativos**

| Código | Condición y respuesta |
| --- | --- |
| A1 | Ubicación denegada: ofrecer selección manual sin bloquear exploración. |
| A2 | Sin negocios elegibles: mostrar estado vacío y permitir cambiar zona o filtros. |

Fuente: elaboración propia.

## CU-08 — Buscar productos cercanos

| Campo | Descripción |
| --- | --- |
| Versión | 1.0 — 2026-09-07 |
| Objetivos asociados | OP-04; OP-01 |
| Requerimiento principal | RF-08 |
| Actor principal | Consumidor |
| Descripción | Buscar productos de catálogos habilitados por texto, categoría y zona, ordenando con la fórmula de productos y sus límites de exposición por negocio. |
| Precondiciones | Zona seleccionada; no requiere cuenta. |
| Disparador | El actor inicia la función desde P08. |
| Postcondiciones | Resultados trazables al contexto de búsqueda. |
| Frecuencia esperada | A relevar en piloto; ejecución según demanda del actor. |
| Importancia | Alta |
| Comentarios | Modelo propuesto; datos sensibles sujetos a autorización. Pantalla P08. |

**Secuencia normal**

| Paso | Acción |
| --- | --- |
| 1 | El consumidor selecciona búsqueda de productos e ingresa texto o categoría. |
| 2 | El sistema filtra productos publicados, disponibles y de negocios con catálogo habilitado. |
| 3 | Calcula relevancia, proximidad, disponibilidad y equidad con versión registrada. |
| 4 | Presenta resultados respetando el máximo de dos productos por negocio en los primeros diez. |
| 5 | Permite abrir el producto y registra exposición visible. |

**Cursos alternativos**

| Código | Condición y respuesta |
| --- | --- |
| A1 | Sin coincidencias: ofrecer corregir texto o ampliar zona. |
| A2 | Producto pierde disponibilidad al abrirlo: mostrar estado actualizado e impedir solicitar unidades inexistentes. |

Fuente: elaboración propia.

## CU-09 — Consultar oferta y contactar al negocio

| Campo | Descripción |
| --- | --- |
| Versión | 1.0 — 2026-09-07 |
| Objetivos asociados | OP-03; OP-01 |
| Requerimiento principal | RF-09 |
| Actor principal | Consumidor |
| Descripción | Consultar perfil, catálogo o promoción vigente y abrir WhatsApp con contexto del negocio, registrando intención de contacto sin considerarla mensaje recibido ni venta. |
| Precondiciones | Destino público habilitado. |
| Disparador | El actor inicia la función desde P09. |
| Postcondiciones | Visita o intención registrada; no se crea venta. |
| Frecuencia esperada | A relevar en piloto; ejecución según demanda del actor. |
| Importancia | Alta |
| Comentarios | Modelo propuesto; datos sensibles sujetos a autorización. Pantalla P09. |

**Secuencia normal**

| Paso | Acción |
| --- | --- |
| 1 | Abre una tarjeta de negocio, producto o promoción. |
| 2 | El sistema muestra información vigente y registra la visita con su origen disponible. |
| 3 | El consumidor elige contactar al negocio. |
| 4 | Se registra el clic y se prepara el enlace a WhatsApp. |
| 5 | El consumidor decide si envía el mensaje en WhatsApp. |

**Cursos alternativos**

| Código | Condición y respuesta |
| --- | --- |
| A1 | Destino archivado: informar que ya no está disponible. |
| A2 | WhatsApp no abre: ofrecer copiar número y resumen; no simular envío exitoso. |

Fuente: elaboración propia.

## CU-10 — Crear solicitud para WhatsApp

| Campo | Descripción |
| --- | --- |
| Versión | 1.0 — 2026-09-07 |
| Objetivos asociados | OP-03; OP-01 |
| Requerimiento principal | RF-10 |
| Actor principal | Consumidor |
| Descripción | Crear una solicitud de productos de un solo negocio con referencia única, copia de cantidades y precios y resumen para WhatsApp, sin reservar stock y sin duplicar la solicitud al reintentar. |
| Precondiciones | Productos publicados de un negocio con catálogo; nombre de contacto y cantidades válidas. |
| Disparador | El actor inicia la función desde P10. |
| Postcondiciones | Solicitud pendiente persistida con origen disponible y stock intacto. |
| Frecuencia esperada | A relevar en piloto; ejecución según demanda del actor. |
| Importancia | Crítica |
| Comentarios | Modelo propuesto; datos sensibles sujetos a autorización. Pantalla P10. |

**Secuencia normal**

| Paso | Acción |
| --- | --- |
| 1 | El consumidor agrega productos de un negocio y revisa el resumen. |
| 2 | Confirma nombre de contacto y cantidades; el teléfono es opcional. |
| 3 | El sistema revalida productos, precios y capacidad del negocio. |
| 4 | Guarda solicitud y líneas con clave de idempotencia y acceso privado. |
| 5 | Entrega referencia y resumen y ofrece abrir WhatsApp. |

**Cursos alternativos**

| Código | Condición y respuesta |
| --- | --- |
| A1 | Cambió el precio o disponibilidad: solicitar nueva revisión antes de confirmar. |
| A2 | Reintento: devolver la solicitud existente; si falla WhatsApp conservarla. |

Fuente: elaboración propia.

## CU-11 — Consultar estado de una solicitud

| Campo | Descripción |
| --- | --- |
| Versión | 1.0 — 2026-09-07 |
| Objetivos asociados | OP-03; OP-01 |
| Requerimiento principal | RF-11 |
| Actor principal | Consumidor |
| Descripción | Permitir consultar el resumen y estado de una solicitud mediante acceso privado autorizado, sin permitir acceso por la referencia pública aislada. |
| Precondiciones | Enlace privado válido o cuenta propietaria de la solicitud. |
| Disparador | El actor inicia la función desde P11. |
| Postcondiciones | Estado consultado sin alterar stock ni venta. |
| Frecuencia esperada | A relevar en piloto; ejecución según demanda del actor. |
| Importancia | Alta |
| Comentarios | Modelo propuesto; datos sensibles sujetos a autorización. Pantalla P11. |

**Secuencia normal**

| Paso | Acción |
| --- | --- |
| 1 | El consumidor abre el acceso privado recibido al crear la solicitud. |
| 2 | El sistema valida credencial y asociación con la solicitud. |
| 3 | Muestra referencia, detalle original y estado reportado. |
| 4 | Distingue solicitud creada de contacto confirmado por el negocio. |
| 5 | Permite recuperar el resumen para WhatsApp sin generar otra solicitud. |

**Cursos alternativos**

| Código | Condición y respuesta |
| --- | --- |
| A1 | Referencia sin credencial o token inválido: denegar datos privados. |
| A2 | Solicitud cancelada o convertida: mostrar su estado sin permitir duplicar conversión. |

Fuente: elaboración propia.

## CU-12 — Atender solicitudes del negocio

| Campo | Descripción |
| --- | --- |
| Versión | 1.0 — 2026-09-07 |
| Objetivos asociados | OP-03; OP-01 |
| Requerimiento principal | RF-12 |
| Actor principal | Responsable del negocio |
| Descripción | Listar y localizar solicitudes propias por referencia, confirmar recepción del contacto, pasar a atención o cancelar, preservando el contenido original. |
| Precondiciones | Responsable autenticado; solicitud del propio negocio. |
| Disparador | El actor inicia la función desde P12. |
| Postcondiciones | Estado y recepción reportada actualizados con auditoría. |
| Frecuencia esperada | A relevar en piloto; ejecución según demanda del actor. |
| Importancia | Alta |
| Comentarios | Modelo propuesto; datos sensibles sujetos a autorización. Pantalla P12. |

**Secuencia normal**

| Paso | Acción |
| --- | --- |
| 1 | Abre la bandeja y busca una referencia. |
| 2 | Consulta el detalle original y su estado. |
| 3 | Confirma si recibió el contacto y pasa a atención. |
| 4 | Puede cancelar con motivo o continuar al registro de venta. |
| 5 | El sistema valida la transición y registra actor e instante. |

**Cursos alternativos**

| Código | Condición y respuesta |
| --- | --- |
| A1 | Solicitud ya convertida: no permitir cancelarla como pendiente; usar corrección de venta. |
| A2 | Suscripción vencida: permitir resolver solicitudes anteriores sin admitir nuevas. |

Fuente: elaboración propia.

## CU-13 — Registrar venta y descontar stock

| Campo | Descripción |
| --- | --- |
| Versión | 1.0 — 2026-09-07 |
| Objetivos asociados | OP-03; OP-01 |
| Requerimiento principal | RF-13 |
| Actor principal | Responsable del negocio |
| Descripción | Registrar una venta vinculada a solicitud o presencial, conservar los artículos finales con el origen de cada línea y descontar stock atómicamente, impidiendo conversiones duplicadas y stock negativo. |
| Precondiciones | Sesión autorizada; productos propios; solicitud no cancelada ni convertida cuando corresponda. |
| Disparador | El actor inicia la función desde P13. |
| Postcondiciones | Venta reportada y saldo consistentes; como máximo una venta por solicitud, con el origen declarado en cada línea. |
| Frecuencia esperada | A relevar en piloto; ejecución según demanda del actor. |
| Importancia | Crítica |
| Comentarios | Modelo propuesto; datos sensibles sujetos a autorización. Pantalla P13. |

**Secuencia normal**

| Paso | Acción |
| --- | --- |
| 1 | Abre una solicitud o inicia una venta presencial. |
| 2 | Confirma los productos, cantidades y precios efectivamente acordados, quitando, cambiando o agregando líneas respecto de la solicitud. |
| 3 | El sistema valida idempotencia, propiedad y disponibilidad con control concurrente. |
| 4 | Guarda venta, líneas con su origen, movimientos y saldo dentro de una transacción. |
| 5 | Si existe solicitud la marca convertida y conserva la relación de origen. |

**Cursos alternativos**

| Código | Condición y respuesta |
| --- | --- |
| A1 | Stock insuficiente: rechazar toda la operación y permitir ajustar cantidades. |
| A2 | Solicitud ya convertida o reintento: recuperar venta existente sin nuevo descuento. |

Fuente: elaboración propia.

## CU-14 — Corregir venta y registrar devolución física

| Campo | Descripción |
| --- | --- |
| Versión | 1.0 — 2026-09-07 |
| Objetivos asociados | OP-03; OP-01 |
| Requerimiento principal | RF-14 |
| Actor principal | Responsable del negocio |
| Descripción | Anular una venta con motivo y auditoría sin reponer stock automáticamente; registrar devoluciones físicas relacionadas con líneas vendidas sin exceder las cantidades originales. |
| Precondiciones | Venta propia registrada y responsable autorizado. |
| Disparador | El actor inicia la función desde P14. |
| Postcondiciones | Corrección trazable; no se gestiona reintegro monetario. |
| Frecuencia esperada | A relevar en piloto; ejecución según demanda del actor. |
| Importancia | Alta |
| Comentarios | Modelo propuesto; datos sensibles sujetos a autorización. Pantalla P14. |

**Secuencia normal**

| Paso | Acción |
| --- | --- |
| 1 | Abre la venta y elige anular o registrar devolución física. |
| 2 | Indica motivo y, para devolución, líneas y unidades recibidas. |
| 3 | El sistema valida estado, cantidades acumuladas y clave de operación. |
| 4 | Guarda la auditoría y, solo si hay devolución física, movimiento y saldo atómicos. |
| 5 | Muestra el historial sin borrar la venta original. |

**Cursos alternativos**

| Código | Condición y respuesta |
| --- | --- |
| A1 | Devolución excede lo vendido: rechazar. |
| A2 | Anulación o devolución repetida: no duplicar cambio ni unidades. |

Fuente: elaboración propia.

## CU-15 — Consultar y habilitar suscripción

| Campo | Descripción |
| --- | --- |
| Versión | 1.0 — 2026-09-07 |
| Objetivos asociados | OP-05; OP-01 |
| Requerimiento principal | RF-15 |
| Actor principal | Responsable del negocio; Administrador |
| Descripción | Mostrar ventajas de Presencia e Impulso sin precios, permitir al administrador habilitar períodos no superpuestos y aplicar capacidades según vigencia, conservando historial al vencer. |
| Precondiciones | Responsable autenticado para consultar; administrador autenticado para habilitar. |
| Disparador | El actor inicia la función desde P15. |
| Postcondiciones | Período auditable y permisos derivados de la vigencia. |
| Frecuencia esperada | A relevar en piloto; ejecución según demanda del actor. |
| Importancia | Alta |
| Comentarios | Modelo propuesto; datos sensibles sujetos a autorización. Pantalla P15. |

**Secuencia normal**

| Paso | Acción |
| --- | --- |
| 1 | El responsable consulta su plan y las capacidades disponibles. |
| 2 | La contratación se acuerda fuera del prototipo. |
| 3 | El administrador selecciona negocio, plan e intervalo de vigencia. |
| 4 | El sistema valida que no exista superposición y registra la activación. |
| 5 | Durante el período aplica Impulso; al vencer aplica Presencia preservando datos y resolución de pedidos anteriores. |

**Cursos alternativos**

| Código | Condición y respuesta |
| --- | --- |
| A1 | Intervalos superpuestos: rechazar activación. |
| A2 | Actor no administrador: rechazar cambio de suscripción. |

Fuente: elaboración propia.

## CU-16 — Gestionar campañas publicitarias

| Campo | Descripción |
| --- | --- |
| Versión | 1.0 — 2026-09-07 |
| Objetivos asociados | OP-05; OP-01 |
| Requerimiento principal | RF-16 |
| Actor principal | Responsable del negocio |
| Descripción | Crear y consultar campañas para una promoción o un producto propio elegible con zona y fechas, enviarlas a revisión y pausarlas; la suscripción no compra posición orgánica. |
| Precondiciones | Negocio habilitado y capacidad según destino; producto o promoción vigente. |
| Disparador | El actor inicia la función desde P16. |
| Postcondiciones | Campaña registrada; no se entrega publicidad sin revisión y pago confirmado. |
| Frecuencia esperada | A relevar en piloto; ejecución según demanda del actor. |
| Importancia | Alta |
| Comentarios | Modelo propuesto; datos sensibles sujetos a autorización. Pantalla P16. |

**Secuencia normal**

| Paso | Acción |
| --- | --- |
| 1 | El responsable elige promocionar una promoción o producto propio. |
| 2 | Define título, zona y fechas de la campaña. |
| 3 | El sistema comprueba que haya exactamente un destino y las capacidades del plan. |
| 4 | Guarda borrador o envía a revisión. |
| 5 | El responsable consulta estado y puede pausar la campaña. |

**Cursos alternativos**

| Código | Condición y respuesta |
| --- | --- |
| A1 | Producto sin catálogo habilitado: rechazar destino y ofrecer promoción independiente. |
| A2 | Fechas inválidas o destino ajeno: indicar corrección sin activar. |

Fuente: elaboración propia.

## CU-17 — Revisar y activar publicidad

| Campo | Descripción |
| --- | --- |
| Versión | 1.0 — 2026-09-07 |
| Objetivos asociados | OP-05; OP-01 |
| Requerimiento principal | RF-17 |
| Actor principal | Administrador |
| Descripción | Aprobar o rechazar campañas, registrar confirmación manual del pago y habilitar entrega solo con fechas, zona, negocio y destino válidos, identificándola como patrocinada. |
| Precondiciones | Administrador autenticado; campaña presentada. |
| Disparador | El actor inicia la función desde P17. |
| Postcondiciones | Campaña con decisión auditable; ningún anuncio modifica exposición orgánica. |
| Frecuencia esperada | A relevar en piloto; ejecución según demanda del actor. |
| Importancia | Alta |
| Comentarios | Modelo propuesto; datos sensibles sujetos a autorización. Pantalla P17. |

**Secuencia normal**

| Paso | Acción |
| --- | --- |
| 1 | Consulta la campaña y su destino. |
| 2 | Aprueba o rechaza con motivo. |
| 3 | Si corresponde registra confirmación del pago acordado externamente. |
| 4 | Programa o activa según fechas y elegibilidad. |
| 5 | La entrega vuelve a verificar condiciones y registra impresiones patrocinadas separadas. |

**Cursos alternativos**

| Código | Condición y respuesta |
| --- | --- |
| A1 | Sin pago confirmado: mantener pendiente aunque esté aprobada. |
| A2 | Destino suspendido, sin stock o plan vencido: suspender entrega; registrar motivo para resolución comercial externa. |

Fuente: elaboración propia.

## CU-18 — Consultar métricas del negocio

| Campo | Descripción |
| --- | --- |
| Versión | 1.0 — 2026-09-07 |
| Objetivos asociados | OP-03; OP-01 |
| Requerimiento principal | RF-18 |
| Actor principal | Responsable del negocio |
| Descripción | Mostrar métricas por período y capacidades: visitas y clics para Presencia y además solicitudes, ventas reportadas y atribución por producto o campaña para Impulso, separando exposición orgánica y paga. |
| Precondiciones | Responsable autenticado y propietario. |
| Disparador | El actor inicia la función desde P18. |
| Postcondiciones | Reporte reproducible por período, sin inferir ventas desde clics. |
| Frecuencia esperada | A relevar en piloto; ejecución según demanda del actor. |
| Importancia | Alta |
| Comentarios | Modelo propuesto; datos sensibles sujetos a autorización. Pantalla P18. |

**Secuencia normal**

| Paso | Acción |
| --- | --- |
| 1 | Selecciona período y, si está habilitado, producto o campaña. |
| 2 | El sistema determina métricas autorizadas por plan. |
| 3 | Agrega eventos deduplicados y solicitudes de una misma cohorte. |
| 4 | Calcula conversión e importe de ventas reportadas excluyendo anuladas y explicando devoluciones físicas. |
| 5 | Muestra resultados y definiciones, incluyendo estados sin datos. |

**Cursos alternativos**

| Código | Condición y respuesta |
| --- | --- |
| A1 | Sin eventos: mostrar cero o no aplicable cuando el denominador sea cero. |
| A2 | Filtro ajeno o métrica no habilitada: rechazar sin exponer datos. |

Fuente: elaboración propia.

## CU-19 — Administrar cuentas, contenido y categorías

| Campo | Descripción |
| --- | --- |
| Versión | 1.0 — 2026-09-07 |
| Objetivos asociados | OP-05; OP-01 |
| Requerimiento principal | RF-19 |
| Actor principal | Administrador |
| Descripción | Gestionar categorías y suspender o rehabilitar cuentas, negocios y publicaciones con motivo y auditoría, preservando historial comercial. |
| Precondiciones | Administrador autenticado. |
| Disparador | El actor inicia la función desde P19. |
| Postcondiciones | Recursos moderados con autor, instante y motivo. |
| Frecuencia esperada | A relevar en piloto; ejecución según demanda del actor. |
| Importancia | Alta |
| Comentarios | Modelo propuesto; datos sensibles sujetos a autorización. Pantalla P19. |

**Secuencia normal**

| Paso | Acción |
| --- | --- |
| 1 | Selecciona cuenta, negocio, contenido o categoría. |
| 2 | Consulta su estado y referencias relevantes. |
| 3 | Elige modificación permitida e indica motivo. |
| 4 | El sistema valida dependencias y registra cambio y auditoría. |
| 5 | Las consultas públicas aplican el nuevo estado sin eliminar historial. |

**Cursos alternativos**

| Código | Condición y respuesta |
| --- | --- |
| A1 | Categoría referenciada: impedir borrado destructivo y permitir corrección de nombre. |
| A2 | Intento de quitar el último administrador habilitado: rechazar. |

Fuente: elaboración propia.

## CU-20 — Configurar y evaluar el prototipo

| Campo | Descripción |
| --- | --- |
| Versión | 1.0 — 2026-09-07 |
| Objetivos asociados | OP-04; OP-01 |
| Requerimiento principal | RF-20 |
| Actor principal | Administrador / evaluador académico |
| Descripción | Versionar parámetros y ejecutar evaluación reproducible del ranking con datos sintéticos y de la asistencia de catálogo con una muestra documentada, exportando métricas sin alterar evidencia de producción. |
| Precondiciones | Evaluador autorizado; conjunto, juicios de relevancia y configuración preparados. |
| Disparador | El actor inicia la función desde P20 o la herramienta de evaluación. |
| Postcondiciones | Artefactos experimentales separados de datos operativos; sin afirmaciones de mejora anticipadas. |
| Frecuencia esperada | A relevar en piloto; ejecución según demanda del actor. |
| Importancia | Crítica |
| Comentarios | Modelo propuesto; datos sensibles sujetos a autorización. Pantalla P20. |

**Secuencia normal**

| Paso | Acción |
| --- | --- |
| 1 | El evaluador selecciona contexto, configuración y conjunto de datos. |
| 2 | Registra semilla, versión y parámetros de las variantes de referencia. |
| 3 | Ejecuta consultas en el mismo orden y registra candidatos y exposición según protocolo. |
| 4 | Calcula equidad, cobertura y nDCG; en la muestra de IA registra aceptación, correcciones y tiempos. |
| 5 | Exporta informe y archivos de evidencia con limitaciones y resultados observados. |

**Cursos alternativos**

| Código | Condición y respuesta |
| --- | --- |
| A1 | Configuración inválida o juicios ausentes: no presentar métricas como válidas. |
| A2 | Proveedor IA indisponible: registrar fallo de asistencia y evaluar la alternativa manual. |

Fuente: elaboración propia.
