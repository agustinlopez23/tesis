# Evidencia del piloto de ranking · 19/09/2026

[Resultados completos](report.md), [configuración](configuration.json) y
[hashes SHA-256](sha256.json). El [protocolo](../../ranking-research-2026-09-19.md)
se escribió antes de ejecutar; no se ajustaron pesos tras observar resultados.

Se implementó el motor en BLL y el laboratorio de negocios previsto en ADR-001.
El motor todavía no interviene en `/explorar`; sus resultados son simulados.

## Hallazgos

Frente a relevancia + proximidad, la variante de equidad cambia las medias de
las tres semillas:

| Métrica | Referencia | Equidad |
| --- | --- | --- |
| Gini | 0,4806 | 0,4106 |
| Participación de los diez negocios más expuestos | 0,1717 | 0,1587 |
| Cobertura | 0,9896 | 1,0000 |
| nDCG@10 | 0,9333 | 0,8946 |
| Desviación de exposición por mérito | 0,2603 | 0,2054 |

La pérdida media de nDCG es 0,0387 puntos (aproximadamente 4,1 % relativo).
No existe aún un umbral aprobado que permita llamarla aceptable. En revista,
la referencia por proximidad alcanza nDCG = 1 por construcción del oráculo de
bandas de distancia: ese resultado no acredita perfección en una revista real.
En Centro/revista la cobertura sube de 0,4886 a 0,9807 y nDCG baja de 1 a 0,9183.
La agregación global oculta diferencias considerables entre superficies.

Con actividad sintética, Gini = 0,4194 y nDCG = 0,8856: ambos empeoran globalmente
respecto de equidad sin actividad. A es aleatoria y no informa utilidad por
construcción; esto no demuestra que toda señal real de actividad sea perjudicial.
Se mantiene fuera del ranking público hasta definir eventos sustantivos.

La variante de equidad combina déficit, novedad y rotación. Esta comparación
no identifica cuál de esos componentes causa cada cambio; faltan ablaciones
separadas. Los radios de las tres zonas se superponen: las muestras geográficas
no son poblaciones independientes. Todos los negocios elegibles aparecen en
algún momento globalmente, pero eso no implica cobertura total en cada contexto.

## Reproducción

Desde la raíz, con el SDK configurado según `App/README.md`:

```powershell
dotnet run --project App/backend/tools/Marketplace.Evaluation --configuration Release -- --output .tools/ranking-reproduction
```

Usar un directorio nuevo o vacío; se rechaza sobrescribir evidencia existente.
Por defecto: 200 negocios, 5.000 consultas por semilla, 60 días, semillas
17/29/43 y cuatro variantes. Se exportan inputs JSON, trazas JSONL comprimidas,
configuración, resumen JSON, reporte Markdown y manifiesto SHA-256.

`--queries 100 --seeds 17` permite una comprobación breve.
`--config Doc/evaluation/business-ranking-pilot/configuration.json` permite
reutilizar la configuración. Los pesos modificados se exportan en cada corrida;
no alterar la evidencia registrada para presentar una calibración como el piloto.

En esta sesión se ejecutaron dos veces las 12 corridas completas: 15.000 consultas
distintas y 60.000 rankings por ejecución. Todos los hashes coincidieron entre
ejecuciones. Entorno: Windows, .NET SDK 10.0.400. Compresión y coma flotante pueden
variar entre runtimes/plataformas: no se afirma igualdad binaria universal.

Artefactos voluminosos locales: `.tools/ranking-pilot-2026-09-19/`, con duplicado
en `.tools/ranking-pilot-2026-09-19-repeat/`. Solo se conservan en Git el reporte,
la configuración y los hashes; el comando regenera inputs y trazas. El resumen
JSON contiene todas las métricas por semilla/estrato, además del reporte legible.

## Trabajo siguiente

Actualización 21/09: las [ablaciones de novedad y rotación](../business-ranking-ablations/README.md)
ya están completas; los resultados originales de este piloto se conservan.

1. Definir umbrales del experimento final antes de evaluarlo, juicios humanos y
   separación de calibración/evaluación; estudiar ablaciones de novedad/rotación.
2. Especificar integración pública: candidatos y oportunidades históricos,
   contexto sin ubicación personal persistida y snapshot estable para paginar.
   Los eventos actuales no registran candidatos no mostrados, por lo que no
   alcanzan para reconstruir el mérito esperado de todas las oportunidades.
3. Catálogo habilitará la evaluación separada de productos, sus 3.000 ítems
   y concentración por negocio. No se considera completada la evaluación §9 del PRD.
