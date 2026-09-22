# Ablaciones de ranking · 21/09/2026

Se separaron compensación, novedad y rotación con los mismos datos y pesos del
piloto. [Reporte completo](report.md), [configuración](configuration.json),
[resumen por semilla](summary.json) y [SHA-256](sha256.json).
El [protocolo previo](../../../openspec/changes/archive/2026-09-21-business-ranking-ablations/design.md)
fija las cinco variantes. Es exploración sintética posterior al piloto, no
validación del experimento final ni elección de una política pública.

## Resultado global

Medias de las semillas 17/29/43; menor Gini indica menor concentración.

| Variante | Gini | nDCG@10 | Desviación por mérito |
| --- | --- | --- | --- |
| Relevancia + proximidad | 0,4806 | 0,9333 | 0,2603 |
| Compensación sola | 0,4203 | 0,9013 | 0,2113 |
| Compensación + novedad | 0,4131 | 0,8972 | 0,2075 |
| Compensación + rotación | 0,4178 | 0,8990 | 0,2092 |
| Compensación + ambas | 0,4106 | 0,8946 | 0,2054 |

La compensación sola produce la mayor parte de la reducción global de Gini y
también la mayor parte de la pérdida de nDCG. Novedad y rotación agregan mejoras
pequeñas en concentración con pérdidas adicionales de relevancia. Las cuatro
celdas de compensación alcanzan cobertura global 1; eso oculta diferencias locales.

Contrastes **pareados por semilla**: primero se resta dentro de cada semilla,
luego se calcula media ± desviación estándar muestral (n=3). No son intervalos
de confianza ni pruebas de significación. Diferencias en puntos, no porcentajes.

| Contraste | Δ Gini | Δ nDCG |
| --- | --- | --- |
| Compensación − referencia | −0,060294 ± 0,006808 | −0,032051 ± 0,002189 |
| Novedad, sin rotación | −0,007243 ± 0,000205 | −0,004039 ± 0,000291 |
| Novedad, con rotación | −0,007193 ± 0,000357 | −0,004430 ± 0,000067 |
| Rotación, sin novedad | −0,002529 ± 0,000030 | −0,002280 ± 0,000028 |
| Rotación, con novedad | −0,002479 ± 0,000215 | −0,002670 ± 0,000323 |
| Interacción | +0,000050 ± 0,000204 | −0,000390 ± 0,000349 |

Para reproducir cada contraste, usar `runs[].strata.all` en `summary.json`.
Novedad sin/con rotación = EquityNewcomer−EquityOnly / Equity−EquityRotation;
rotación sin/con novedad = EquityRotation−EquityOnly / Equity−EquityNewcomer.
Interacción = Equity−EquityRotation−EquityNewcomer+EquityOnly. La misma operación
en cada clave `magazine/0..2` y `search/0..2` da los contrastes por estrato.

## Diferencias que impiden elegir solo por el promedio

- Revista/Centro: compensación sola pierde 0,068838 de nDCG frente a referencia;
  búsqueda/Centro pierde 0,003660. El oráculo de revista sigue favoreciendo
  proximidad por construcción; no son juicios humanos.
- Revista/Alberdi: cobertura de negocios nuevos pasa de 0,7790 con compensación
  sola a 0,8710 con novedad y a 0,8895 con ambas.
- Búsqueda/Alberdi: agregar rotación a novedad baja cobertura de nuevos de
  1,0000 a 0,9815; rotación sin novedad empeora ligeramente desviación por mérito
  (+0,000294 de media). Una mejora global no garantiza mejora en cada estrato.

Las zonas se superponen. Hay solo tres semillas y un generador. El historial
es independiente por variante: se compara la política completa, incluido su
efecto sobre exposiciones futuras. Retirar novedad devuelve peso a mérito;
retirar rotación quita discretización y hash juntos. No se identifica cuál de
estas dos partes de rotación explica el efecto. Actividad está excluida.

## Reproducción y verificaciones

Desde la raíz, con el SDK local configurado según `App/README.md`:

```powershell
dotnet run --project App/backend/tools/Marketplace.Evaluation --configuration Release -- --suite ablation --output .tools/ranking-ablation-reproduction
```

Usar directorio nuevo o vacío. `--suite pilot` (también el valor por defecto)
conserva las cuatro variantes anteriores. Una suite desconocida se rechaza.

Se ejecutaron dos veces 200 negocios × 5.000 consultas × tres semillas × cinco
variantes: **75.000 rankings por ejecución**, con todos los hashes idénticos.
Se reprodujo además el piloto de cuatro variantes y sus diez hashes coinciden
con el manifiesto del 19/09. Proximity y Equity coinciden exactamente en ambas
suites. Las 75.000 trazas verificadas comparten candidatos, juicios y exposición
esperada por consulta. Entorno: Windows, .NET SDK 10.0.400.

Artefactos completos locales: `.tools/ranking-ablation-2026-09-21/` y `...-repeat/`;
compatibilidad: `.tools/ranking-pilot-compatibility-2026-09-21/`. Se conservan aquí
reporte, resumen, configuración y manifiesto; inputs y trazas se regeneran.
Pruebas BLL/evaluación: **31/31**, build del laboratorio sin advertencias.

## Continuación

Antes del experimento final: fijar umbrales globales **y por superficie**, juicios
humanos independientes y conjuntos separados para calibrar/evaluar. No deducir
un umbral de aceptación de estos resultados. Después, especificar candidatos y
oportunidades históricos, contexto sin ubicación personal persistida y snapshot
estable para paginar. El catálogo sigue pendiente para evaluar productos.
La guía pública conserva distancia/nombre/id; no se activó ranking experimental.
