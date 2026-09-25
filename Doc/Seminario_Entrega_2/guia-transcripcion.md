# Guía de transcripción de la Entrega 2

Nota de trabajo, no entregable. Sirve para pasar `Entrega2-Scrum.docx` al documento de la Entrega 1 conservando una voz uniforme entre ambas entregas.

El documento generado tiene dos registros distintos y **solo uno necesita reescritura**.

| Bloque | Palabras | Qué hacer |
| --- | --- | --- |
| Prosa narrativa | ~1.350 | Reescribir con voz propia |
| Requerimientos, historias y criterios de aceptación | ~3.366 | **No tocar** |

Los requerimientos usan «El sistema deberá permitir» y los criterios usan «dado / cuando / entonces». Ambos formatos los prescribe la cátedra en la lectura *Definición del prototipo tecnológico*. Reescribirlos con voz personal es un error, no una mejora.

## Orden de prioridad

| Pasaje | Palabras | Prioridad |
| --- | --- | --- |
| Diagrama de arquitectura | 373 | Alta |
| Propuesta | 219 | Alta |
| Pantallas: introducción y cuatro descripciones | 272 | Media |
| Estructura de datos: introducción | 225 | Media |
| Sprint backlog: texto de encabezado y cierre | 105 | Baja |
| Diccionario de datos: introducción | 66 | Baja |
| Objetivo del prototipo y Límites | 87 | Ninguna: formato prescripto |

El objetivo debe ser una sola oración con verbo en infinitivo y los límites deben expresarse «Desde… hasta…». Esa forma la exige la consigna, así que conviene dejarlos como están.

## Marcas de estilo propias que el borrador no reproduce

Tres rasgos aparecen de forma sostenida en la Entrega 1 corregida y están ausentes en el texto generado.

**1. La oración cierra con la consecuencia.**

> «Publicar un producto les exige fotografiarlo, redactar un título y una descripción, definir una categoría y subirlo a cada canal, y esa tarea compite con la atención del mostrador, la reposición y la administración diaria, que suelen concentrarse en la misma persona. **El resultado es que** la mayor parte de la oferta nunca se publica.»

El borrador describe el problema y se detiene. Falta el cierre que convierte la descripción en argumento.

**2. Simetría retórica entre las dos caras del problema.**

> «Del lado del consumidor, **el problema es espejo**: no tiene un medio para saber qué productos hay disponibles en los comercios de su zona.»

No hay ninguna construcción equivalente en el texto generado.

**3. La enumeración va dentro de la prosa, no en viñetas.**

> «…comercios minoristas de barrio (almacenes, kioscos, venta de insumos para el hogar, tiendas de indumentaria, librerías, ferreterías, verdulerías, entre otros) que comparten el mismo problema…»

El borrador tiende a cortar en listas donde la Entrega 1 encadena dentro del párrafo.

Otros pivotes recurrentes de la Entrega 1 que conviene reintroducir: «es decir», «De allí», «La diferencia con este proyecto es», «cada opción tiene un límite».

## Diagrama de arquitectura: reusar el párrafo propio

Esta sección **ya está escrita** en el Diseño Metodológico de la Entrega 1. Conviene partir de ahí y extenderla, no reescribir sobre el borrador.

Versión propia, Entrega 1:

> «Esa aplicación se comunica por medio de una API REST con el backend desarrollado en C# con ASP.NET Core, organizado en N capas: la capa de presentación expone los servicios, la capa de lógica de negocio concentra las reglas del sistema —entre ellas, el cálculo del ranking orgánico (objetivo específico 2)— y la capa de acceso a datos utiliza Entity Framework Core para traducir los objetos del dominio a la base de datos.»

Versión del borrador:

> «El backend está desarrollado en C# con ASP.NET Core y se organiza en N capas. La capa de presentación expone los servicios y resuelve la autenticación y la comprobación de permisos. La capa de lógica de negocio concentra las reglas del sistema, entre ellas el motor de ranking orgánico…»

Tres diferencias concretas:

1. La versión propia encadena con dos puntos y guiones largos; el borrador corta en oraciones independientes.
2. La versión propia **ancla cada componente a un objetivo específico** del proyecto: «(objetivo específico 2)», «(objetivos específicos 3 y 5)», «(objetivos específicos 2 y 4)». El borrador eliminó todas esas referencias, y con ellas la trazabilidad entre arquitectura y objetivos.
3. La versión propia mantiene la cadena causal en una sola respiración.

Al transcribir, conviene recuperar las referencias a objetivos específicos. Son una marca de coherencia que el corrector puede rastrear entre entregas.

## Propuesta: el pasaje que más se lee

Es la respuesta directa al diagnóstico, así que se lee con más atención que el resto. Requiere argumentar, no describir.

El borrador dice, en su apertura:

> «Se propone el desarrollo de un sistema web de marketplace hiperlocal que centralice la oferta del comercio de proximidad en un catálogo estructurado y la ponga a disposición del consumidor de la zona mediante búsqueda por cercanía.»

Funciona como enunciado, pero le falta el cierre consecuencial del estilo propio. Conviene que cada movimiento de la propuesta termine indicando qué problema del diagnóstico resuelve, del mismo modo en que la Entrega 1 cierra sus párrafos.

La consigna pide además que la propuesta se redacte como un relato que permita entender qué se construirá, siguiendo el modelo «El sistema que se…». El borrador ya respeta esa forma.

## Qué no cambiar

- Los nombres de los tres procesos diagnosticados. Deben coincidir literalmente con el relevamiento de la Entrega 1: *incorporación y publicación de productos*, *actualización de precios y disponibilidad*, *atención de consultas y venta*.
- La numeración de tablas y figuras. El documento arranca en Tabla 5 y Figura 3 para continuar donde termina la Entrega 1.
- Las causas del diagnóstico. Están redactadas evitando la formulación que la cátedra marca como incorrecta: la causa de un problema nunca es la inexistencia de un sistema informático.
- El formato del cuerpo: Times New Roman 12 pt, interlineado 1,5 líneas, sangría de primera línea de 1,27 cm, alineación justificada. Títulos en 14 pt negrita centrados y subtítulos en 12 pt cursiva sin negrita alineados a la izquierda.

## Sobre las maquetas de pantalla

Las cuatro imágenes incluidas son wireframes generados desde `tools/screens.json`, no capturas de la aplicación. Es lo que corresponde en esta instancia: la consigna pide prototipos «sin codificar» y aclara que el código de las interfaces se entrega en el Entregable N.º 4. El texto del documento las describe como «prototipos de baja fidelidad con datos ficticios» y no afirma que el producto esté implementado. Conviene no modificar esa formulación al transcribir.

## Regeneración

```powershell
node Doc/Seminario_Entrega_2/tools/render-arquitectura.mjs
node Doc/Seminario_Entrega_2/tools/build-entrega2-scrum.mjs
```

El contenido académico está en `tools/contenido-entrega2.mjs` y el formato en `tools/build-entrega2-scrum.mjs`. Editar el contenido en el primero y regenerar; no conviene editar el `.docx` a mano si después se vuelve a generar.
