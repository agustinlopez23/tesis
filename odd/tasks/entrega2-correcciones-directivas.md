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

- [ ] T3.1 `Doc/model/model.json`: campo `item_origin` en `SaleItem`, entrada en
      `descriptions` y marca en `optional.SaleItem`.
- [ ] T3.2 `Doc/model/data-dictionary.md`: restriccion `UNIQUE` y valores del campo.
- [ ] T3.3 `Doc/model/README.md`: decisiones 4 y la nota de venta presencial.
- [ ] T3.4 `Doc/PRD.md`: seccion 6 recorrido C, seccion 7 (tabla y limite: "pueden
      diferir" en lugar de "pueden reducirse") y criterio de aceptacion de la seccion 12.
- [ ] T3.5 `catalog.json` caso 13, `screens.json` P13 y `sequences.json` S04.
- [ ] T3.6 `02-requerimientos.md` RF-13, `03-casos-de-uso.md` CU-13 y
      `06-trazabilidad.md`, manteniendo la coincidencia literal que exige `verify.py`.
- [ ] T3.7 `contenido-entrega2.mjs`: RF-13 y criterios de aceptacion de HU-13.
- [ ] T3.8 Actualizar los conteos de campos codificados a mano (203 a 204) en
      `Doc/model/README.md`, `Doc/documentation-checklist.md`,
      `Doc/Seminario_Entrega_2/README.md` y `04-diagramas.md`.
- [ ] T3.9 Regenerar modelo, figuras, BPMN, reporte y el docx de Scrum.
- [ ] T3.10 Ejecutar `verify.py` y commit de unidad de trabajo.

## Criterios de aceptacion

- `verify.py` pasa sin fallos tras cada unidad de trabajo.
- `Entrega2-Scrum.docx` contiene una seccion de referencias con las seis fuentes en
  ingles y sus citas correspondientes en el cuerpo.
- Ningun documento describe la venta presencial como destino de articulos acordados
  sobre una solicitud existente.
- El conteo de campos del modelo es consistente entre el generador y la prosa.
- Los archivos generados no se editan a mano.

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

## Proximo paso

T3.1 — origen por linea de venta en el modelo.
