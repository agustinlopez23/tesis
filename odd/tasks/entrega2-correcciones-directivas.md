# Entrega 2 — correcciones de directivas y origen por linea de venta

Feature: `entrega2-correcciones-directivas`
Rama: `docs/entrega2-scrum`
Creado: 2026-09-25
Modo TDD: no aplica a documentacion; la verificacion de referencia es
`python Doc/Seminario_Entrega_2/tools/verify.py` mas la regeneracion de artefactos.
Estrategia de entrega: `ask-on-risk`.

## Objetivo

Cerrar el unico item descubierto del checklist de la catedra (citas y referencias) y
documentar el origen por linea de venta, de modo que lo acordado por mensajeria entre
en el sistema sin partir la venta ni degradar la atribucion.

## Problema

1. `Entrega2-Scrum.docx` cumple 10 de 11 items de `directivas_entrega_2.md`. El item 5
   (citas y referencias) no esta atendido: `contenido-entrega2.mjs` no contiene ninguna
   cita ni seccion de referencias.
2. Un pedido nace en la web y la conversacion sigue por mensajeria, donde el consumidor
   puede sumar articulos. Hoy el corpus es ambiguo: `PRD.md` limita la venta a reducir
   cantidades, mientras `CU-13` admite confirmar lo efectivamente acordado. Registrar los
   extras como venta presencial separada rompe la atribucion, que es el aporte de la tesis.
3. El pipeline de regeneracion documental apunta a `Doc/Entrega2`, carpeta que ya no
   existe (se renombro a `Doc/Seminario_Entrega_2`). Bloquea cualquier regeneracion.

## Por que

La atribucion explicita entre solicitud, producto y venta es el diferencial academico
declarado en `PRD.md`. Partir un unico evento comercial en dos registros inflaria el
conteo de conversiones y desinflaria el importe atribuido. El origen por linea conserva
un evento, un registro y una atribucion, y ademas habilita una metrica nueva: la venta
incremental acordada durante la conversacion.

## Alcance autorizado

Documentacion y generadores documentales. No se toca `App/` ni `openspec/`: el concepto
`SaleItem` todavia no tiene implementacion.

## Decisiones de diseno

- Campo nuevo `SaleItem.item_origin`, `varchar(64)`, opcional condicional (`Si*`).
  Valores: `requested` (venia en la solicitud) y `agreed_in_conversation` (se acordo por
  mensajeria). Nulo cuando `Sale.origin = in_store`, porque no hay solicitud contra la
  cual comparar.
- `Sale.origin` (`platform` / `in_store`) se conserva sin cambios: es ortogonal. El
  origen presencial queda reservado a ventas sin solicitud.
- La restriccion `UNIQUE(sale_id, product_id)` pasa a
  `UNIQUE(sale_id, product_id, item_origin)`, para que un mismo producto pueda aparecer
  como linea solicitada y como linea agregada en la conversacion sin perder la traza.
- Metricas de importe separadas: importe de lineas solicitadas e importe incremental
  acordado en conversacion. No se publica un unico importe mezclado.
- Identificadores en ingles y descripciones en espanol, segun la convencion del modelo.

## Tareas

### T1 — Reparar el pipeline documental (prerrequisito)

- [ ] T1.1 Corregir la ruta `Doc/Entrega2` a `Doc/Seminario_Entrega_2` en
      `build-report.mjs:6`, `render-figures.mjs:6`, `build-bpmn.mjs:6` y `verify.py:10`.
- [ ] T1.2 Corregir las instrucciones de `Doc/Seminario_Entrega_2/README.md`
      (seccion "Regeneracion local") que citan la carpeta inexistente.
- [ ] T1.3 Ejecutar `node Doc/Seminario_Entrega_2/tools/build-all.mjs` y
      `python Doc/Seminario_Entrega_2/tools/verify.py` para fijar una linea base verde
      antes de cualquier cambio de contenido.
- [ ] T1.4 Commit de unidad de trabajo.

### T2 — Citas y referencias (item 5 del checklist)

- [ ] T2.1 Agregar el export `referencias` a `contenido-entrega2.mjs` con las seis
      fuentes verificadas, en su idioma original (ingles).
- [ ] T2.2 Insertar las citas en el cuerpo: Scrum en los artefactos, historias de
      usuario, criterios Dado/cuando/entonces, arquitectura en capas y las dos fuentes
      de ranking.
- [ ] T2.3 Renderizar la seccion "Referencias" en `build-entrega2-scrum.mjs`,
      respetando el formato de la catedra (Times New Roman 12, interlineado 1,5).
- [ ] T2.4 Regenerar `Entrega2-Scrum.docx` y verificar.
- [ ] T2.5 Commit de unidad de trabajo.

### T3 — Origen por linea de venta

- [x] T3.1 `Doc/model/model.json`: campo `item_origin` en `SaleItem`, entrada en
      `descriptions` y marca en `optional.SaleItem`.
- [x] T3.2 `Doc/model/data-dictionary.md`: restriccion `UNIQUE` y valores del campo.
- [x] T3.3 `Doc/model/README.md`: decisiones 4 y la nota de venta presencial.
- [x] T3.4 `Doc/PRD.md`: seccion 6 recorrido C, seccion 7 (tabla y limite: "pueden
      diferir" en lugar de "pueden reducirse") y criterio de aceptacion de la seccion 12.
- [x] T3.5 `catalog.json` caso 13, `screens.json` P13 y `sequences.json` S04.
- [x] T3.6 `02-requerimientos.md` RF-13, `03-casos-de-uso.md` CU-13 y
      `06-trazabilidad.md`, manteniendo la coincidencia literal que exige `verify.py`.
- [x] T3.7 `contenido-entrega2.mjs`: RF-13 y criterios de aceptacion de HU-13.
- [x] T3.8 Actualizar los conteos de campos codificados a mano (203 a 204) en
      `Doc/model/README.md`, `Doc/documentation-checklist.md`,
      `Doc/Seminario_Entrega_2/README.md` y `04-diagramas.md`.
- [x] T3.9 Regenerar modelo, figuras, BPMN, reporte y el docx de Scrum.
- [x] T3.10 Ejecutar `verify.py` y commit de unidad de trabajo.

### T4 — Trazabilidad de la entrega de Scrum

La catedra cierra las directivas pidiendo que cada requerimiento funcional pueda
rastrearse contra el alcance, una historia de usuario, la estructura de datos y al menos
una parte del prototipo. El documento de Scrum no tenia esa seccion.

- [x] T4.1 Codificar los alcances (`A-01` a `A-16`) para que la matriz pueda referirlos.
- [x] T4.2 Agregar los dos alcances faltantes: moderacion de la plataforma (cubre RF-19) y
      registro de auditoria (cubre RF-23). Ambos describen funcionalidad ya exigida por
      esos requerimientos; el alcance estaba incompleto, no los requerimientos.
- [x] T4.3 Export `trazabilidad` con las 23 filas RF -> alcance -> HU -> entidades ->
      prototipo, derivadas de `catalog.json` (casos y `rfextra`).
- [x] T4.4 Renderizar la seccion en `build-entrega2-scrum.mjs`.
- [x] T4.5 Regenerar, verificar y commit de unidad de trabajo.

## Criterios de aceptacion

- `verify.py` pasa sin fallos tras cada unidad de trabajo.
- `Entrega2-Scrum.docx` contiene una seccion de referencias con las seis fuentes en
  ingles y sus citas correspondientes en el cuerpo.
- Ningun documento describe la venta presencial como destino de articulos acordados
  sobre una solicitud existente.
- El conteo de campos del modelo es consistente entre el generador y la prosa.
- Los archivos generados no se editan a mano.
- Cada uno de los 23 RF aparece exactamente una vez en la matriz de trazabilidad, con
  alcance, historia, entidades y prototipo cuando corresponde.
- Todo alcance declarado tiene al menos un RF que lo cubre, y todo RF tiene alcance.

## Verificacion

- `node Doc/Seminario_Entrega_2/tools/build-all.mjs`
- `node Doc/Seminario_Entrega_2/tools/build-entrega2-scrum.mjs`
- `python Doc/Seminario_Entrega_2/tools/verify.py`

## Progreso

- T1 cerrado en ea74cf5. build-all.mjs completa los cuatro generadores; verify.py sale 0.
- T2 cerrado: seis referencias en ingles verificadas contra sus fuentes y citadas en el cuerpo
  (Scrum en la descripcion del sistema, Cohn 2005 en el product backlog, Cohn 2004 y North 2006
  en historias de usuario, Fowler 2002 y Singh y Joachims 2018 en arquitectura). Comprobado por
  extraccion de word/document.xml: los catorce marcadores de cita y referencia estan presentes.
  verify.py sale 0.
- T3 cerrado: campo item_origin en SaleItem (204 campos), unicidad por origen, PRD secciones 6, 7
  y 12, caso 13, pantalla P13, secuencia S04, RF-13, CU-13, trazabilidad, RF-13 y HU-13 del
  documento de Scrum, SaleItem agregado al diccionario curado y etiqueta BPMN. Conteos de campos
  escritos a mano actualizados en cuatro documentos. verify.py sale 0 con 204 campos.
- T4 cerrado: la entrega efectiva es la de Scrum, decidido por el usuario. Alcances codificados
  A-01 a A-16 con dos agregados (moderacion y auditoria) que cubren RF-19 y RF-23, hasta ahora
  sin alcance declarado. Matriz de 23 filas RF -> alcance -> HU -> entidades -> prototipo,
  validada por script: los 23 RF aparecen una sola vez, los 16 alcances tienen cobertura y no
  hay referencias colgadas. verify.py sale 0.

## Proximo paso

Revision manual de la paginacion del docx, que verify.py no cubre. Queda por decidir que se
hace con el corpus UML paralelo: verify.py lo valida, de modo que borrarlo romperia la puerta
de verificacion.
