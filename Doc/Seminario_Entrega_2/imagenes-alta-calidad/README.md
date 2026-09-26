# Imagenes de la Entrega 2 en alta calidad

Generado con `node Doc/Seminario_Entrega_2/tools/export-imagenes.mjs 4400`.

Cada figura del documento `Entrega2-Scrum.docx` se entrega en dos formatos. El PNG esta
re-renderizado desde su fuente SVG a 4400 pixeles de ancho, muy por encima de la
resolucion que consume el documento. El SVG es vectorial: no pierde definicion en ninguna
escala y es el formato preferible para imprimir, proyectar o insertar en otro documento.

La numeracion coincide con la del documento, donde la Entrega 1 cierra en la Figura 2.

| Figura | Titulo | Archivo | PNG | Fuente |
| --- | --- | --- | --- | --- |
| 3 | Diagrama de clases del prototipo | `figura-03-diagrama-de-clases` | 4400x5239, 1559 KB | `Doc/model/figures/UML-completo.svg` |
| 4 | Diagrama de entidad-relacion del prototipo | `figura-04-diagrama-entidad-relacion` | 4400x5005, 1562 KB | `Doc/model/figures/ER-completo.svg` |
| 5 | Prototipo de interfaz: Asistencia de catalogo | `figura-05-prototipo-P05-asistencia-de-catalogo` | 4400x3400, 348 KB | `Doc/Seminario_Entrega_2/pantallas/P05.svg` |
| 6 | Prototipo de interfaz: Descubri tu zona | `figura-06-prototipo-P07-descubri-tu-zona` | 4400x3400, 364 KB | `Doc/Seminario_Entrega_2/pantallas/P07.svg` |
| 7 | Prototipo de interfaz: Tu solicitud | `figura-07-prototipo-P10-tu-solicitud` | 4400x3400, 336 KB | `Doc/Seminario_Entrega_2/pantallas/P10.svg` |
| 8 | Prototipo de interfaz: Registrar venta | `figura-08-prototipo-P13-registrar-venta` | 4400x3400, 371 KB | `Doc/Seminario_Entrega_2/pantallas/P13.svg` |
| 9 | Diagrama de arquitectura del prototipo | `figura-09-diagrama-de-arquitectura` | 4400x4450, 657 KB | `Doc/Seminario_Entrega_2/figuras/arquitectura.svg` |

Esta carpeta es un derivado: se regenera con el script y no se edita a mano. Si cambia una
maqueta o el modelo, primero se corre `build-all.mjs` y despues este script.

Fuente: elaboracion propia.
