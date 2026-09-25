# Documentación del prototipo — Lista de control

Referencia: consigna del Seminario Final compartida por el usuario el 2026-09-07.
Metodología elegida en la primera entrega: **UML**. Estado actualizado: se preparó el [borrador documental completo](Entrega2/README.md), con [Word editable](Entrega2/Entrega2-documentacion.docx), figuras y maquetas. Esto no indica aprobación académica ni implementación.

## Diagramas y representaciones visuales

| Entregable | Estado actual | Trabajo pendiente |
| --- | --- | --- |
| Diagrama general de casos de uso | Preparado: 20 casos | Revisar con el usuario; todos tienen ficha y RF relacionado. |
| Diagramas de casos de uso por módulo, cuando ayuden a la lectura | Preparadas tres vistas por actor | Mantener las mismas funciones del diagrama general. |
| Diagramas de secuencia | Preparadas 8 secuencias con alternativas | Revisar contratos al especificar cada implementación. |
| Diagrama de clases | Preparadas 26 entidades con operaciones y multiplicidades | Validar decisiones de dominio antes de implementar. |
| Diagrama entidad-relación (DER) | Completo y por módulos, con figuras | Validar restricciones físicas antes de migraciones. |
| Prototipos de interfaces de pantallas | Preparadas 20 maquetas estáticas enlazadas | Revisar recorridos y luego desarrollar diseño visual final. |
| Diagrama de despliegue | Preparado con explicación | Elegir proveedores y dimensionamiento antes de desplegar. |

Fuentes: [clases en PlantUML](model/entities.puml), [DER por módulos](model/ER.md), [DER completo en Mermaid](model/database.mmd) y [guía del modelo](model/README.md). Se entregan figuras SVG/PNG y se inspeccionaron muestras gráficas; sigue pendiente la revisión académica. El [índice de diagramas](Entrega2/04-diagramas.md) incluye también componentes y BPMN.

### Secuencias candidatas, a confirmar con los casos de uso

- Entrada a la revista: selección de zona, cálculo del ranking de negocios y registro de exposición.
- Búsqueda de productos y consulta del catálogo.
- Creación de solicitud y preparación del mensaje de WhatsApp.
- Confirmación de venta y descuento transaccional de stock, incluyendo insuficiencia y reintentos.
- Publicación de catálogo asistida por IA, si se conserva el alcance de la primera entrega: revisión humana y alternativa manual.
- Contratación y activación de campaña, diferenciando publicidad y ranking orgánico.
- Activación de suscripción y control de capacidades, si se requiere explicar su interacción.

Esta selección es una propuesta de cobertura, no una exigencia de un diagrama por cada caso de uso. La consigna sí exige una ficha por cada caso de uso representado; el diagrama general no necesita una ficha adicional por ser un diagrama.

## Documentación que acompaña los diagramas

| Entregable | Estado / criterio de preparación |
| --- | --- |
| Diagnóstico y propuesta | Redactados para los tres procesos, con problemas y causas operativas derivados del relevamiento. |
| Objetivo, límites y alcances del prototipo | Redactados y vinculados con objetivos de la primera entrega. |
| Requerimientos funcionales (RF) | 23 RF codificados con trazabilidad a casos de uso. |
| Requerimientos no funcionales (RNF) | 13 RNF con verificación prevista; no se presentan como pruebas realizadas. |
| Fichas de casos de uso | 20 fichas con precondiciones, flujo normal, alternativas y postcondiciones. Frecuencias pendientes de relevamiento, sin cifras inventadas. |
| Justificación de estructura de datos | Redactada: UML orientado a objetos y DER para persistencia relacional. |
| Diccionario de datos | Completados 204 campos con descripción en español, longitud/precisión y tipo propuesto. |
| Explicación del despliegue | Redactada junto al diagrama; proveedores aún pendientes. |

Procesos relevados en la primera entrega, cuyos nombres deben conservarse:

1. **incorporación y publicación de productos**.
2. **actualización de precios y disponibilidad**.
3. **atención de consultas y venta**.

## Orden de trabajo sugerido

1. Revisar coherencia entre primera entrega y PRD y fijar el alcance documentable.
2. Codificar RF y RNF.
3. Dibujar casos de uso y redactar sus fichas.
4. Ajustar clases, DER y diccionario usando los casos de uso.
5. Elaborar secuencias y maquetas de pantallas coordinadas con esos recorridos.
6. Preparar despliegue, explicación y figuras exportables.
7. Revisar trazabilidad: alcance → RF → caso de uso → pantalla/secuencia → entidades → verificación.

El diagnóstico y la propuesta se redactan conservando el relevamiento original. La numeración de tablas y figuras y sus leyendas «Fuente: elaboración propia» se ajustarán al ensamblar la entrega.

## Límites de esta lista

- Por haber elegido UML, no se requieren product backlog, historias de usuario y sprint backlog como sustitutos de los artefactos UML de esta consigna.
- No se requiere un diagrama NoSQL para la base relacional elegida.
- El ADR de arquitectura existente sirve de insumo, pero no sustituye el diagrama de despliegue requerido.
- La primera entrega también menciona componentes y BPMN: ambos se incorporaron al borrador, aunque esta consigna no los exija expresamente.
- La asistencia de IA se conserva como opción del comerciante en el borrador por coherencia con los objetivos originales. Cualquier reducción posterior debe conciliar esos objetivos.
- Los ejemplos de la consigna orientan el formato; sus funciones, cifras y tecnologías no se trasladan automáticamente a este proyecto.
- Documentar solo funciones que se asumirán para la entrega final; identificar cualquier propuesta pendiente antes de convertirla en requisito.
