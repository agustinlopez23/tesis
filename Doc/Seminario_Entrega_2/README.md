# Documentación UML del prototipo

Versión 1.0 · 2026-09-07 · **Borrador completo para revisión**, no entrega académica aprobada ni evidencia de implementación.

## Leer y revisar

- [Documento unificado para navegador e impresión](documentacion.html).
- [Documento Word editable](Entrega2-documentacion.docx).
- [Galería de 20 maquetas enlazadas](pantallas/index.html).

La portada no incorpora datos personales del autor. Al integrar el trabajo final corresponde aplicar su plantilla institucional, numeración global de tablas/figuras y ajustes del tutor. Las figuras amplias también se entregan como SVG y PNG independientes para ampliar su lectura; no dependen de un servicio externo.

## Contenido

| Entregable | Archivo |
| --- | --- |
| Diagnóstico de los tres procesos originales, propuesta, objetivo, límites y alcances | [01-diagnostico-y-alcance.md](01-diagnostico-y-alcance.md) |
| 23 RF y 13 RNF con verificación prevista | [02-requerimientos.md](02-requerimientos.md) |
| 20 fichas de casos de uso | [03-casos-de-uso.md](03-casos-de-uso.md) |
| Casos de uso, 8 secuencias, BPMN, componentes y despliegue con explicación | [04-diagramas.md](04-diagramas.md) |
| Clases con operaciones | [Modelo de clases](../model/classes.md) |
| DER por módulos y completo | [Modelo relacional](../model/ER.md) |
| Diccionario de 204 campos, tipos y longitudes | [Diccionario por campo](../model/field-dictionary.md) |
| Reglas de integridad y decisiones de persistencia | [Diccionario de restricciones](../model/data-dictionary.md) |
| Pantallas y encadenamiento | [05-pantallas.md](05-pantallas.md) |
| RF → CU → pantalla/secuencia → entidades → verificación | [06-trazabilidad.md](06-trazabilidad.md) |
| Decisiones propuestas y evaluación académica | [07-decisiones-y-validacion.md](07-decisiones-y-validacion.md) |

Las fuentes de diagramas están en `diagramas/` (PlantUML y BPMN) y `../model/` (PlantUML, Mermaid y JSON). Las figuras SVG/PNG y sus fuentes DOT se encuentran en `figuras/` y `../model/figures/`. Las vistas por actor usan el mismo conjunto de casos del diagrama general.

## Decisiones que requieren revisión

La documentación conserva IA opcional para el comerciante por coherencia con los objetivos de la primera entrega. Los nombres de planes, distribución publicitaria, radio inicial, actividad y adaptación de la fórmula a negocios están identificados como propuestas. Los parámetros pendientes y umbrales de éxito deben cerrarse antes de implementar y evaluar; no se ha supuesto aprobación del usuario o tutor a esos valores.

La plantilla P20 representa el recorrido de una herramienta académica aislada. No implica exponer controles experimentales a consumidores ni ejecutar datos sintéticos en producción. Las maquetas son de baja fidelidad y muestran campos, acciones y estados; no son el diseño visual final.

## Regeneración local

Las dependencias de documentación se instalan en `.tools/documentation`, separadas de la aplicación y excluidas de Git. Las versiones observadas se fijan en [tools/package.json](tools/package.json), con lockfile adjunto. Preparación desde la raíz del repositorio en PowerShell:

```powershell
New-Item -ItemType Directory -Force .tools/documentation
Copy-Item Doc/Seminario_Entrega_2/tools/package.json .tools/documentation/package.json
Copy-Item Doc/Seminario_Entrega_2/tools/package-lock.json .tools/documentation/package-lock.json
npm ci --prefix .tools/documentation --no-audit --no-fund
node Doc/Seminario_Entrega_2/tools/build-all.mjs
node Doc/Seminario_Entrega_2/tools/render-arquitectura.mjs
node Doc/Seminario_Entrega_2/tools/build-entrega2-scrum.mjs
python Doc/Seminario_Entrega_2/tools/verify.py
```

Editar el texto académico en Markdown. `tools/catalog.json` contiene la matriz de requisitos/casos usada para comprobar trazabilidad; si cambia una ficha, actualizar también esa definición. `tools/screens.json` controla las maquetas, `tools/sequences.json` las secuencias y `../model/model.json` el modelo y diccionario por campo. Los generadores reemplazan únicamente sus artefactos derivados.

## Verificación y límites

Se comprueban correspondencia de entidades y campos UML/ER, cobertura de RF/CU/pantallas, enlaces locales, sintaxis XML de SVG/BPMN y estructura del DOCX. Las figuras se renderizan localmente y se inspeccionan muestras del modelo, casos de uso, secuencias y pantallas.

No se ejecutaron funciones de negocio, migraciones, experimentos del ranking ni pruebas de compatibilidad del producto. Las fuentes PlantUML/Mermaid no se validaron con sus renderizadores específicos; las figuras visuales provienen de generadores locales equivalentes. El XML BPMN no se importó en un editor BPMN y el Word requiere revisión final de paginación en el editor del usuario.
