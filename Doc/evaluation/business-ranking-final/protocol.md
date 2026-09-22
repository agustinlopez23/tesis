# Protocolo de evaluación de negocios · 21/09/2026

**Estado: criterios principales acordados con el usuario el 21/09/2026; experimento no ejecutado.**
Respuesta registrada: «Usar estos criterios para preparar el experimento».
El [plan legible por herramientas](plan.json) conserva ese acuerdo. Las cifras
son tolerancias propias, no estándares publicados ni umbrales inferidos para
aprobar el piloto. El diseño metodológico continúa en preparación. Cambiar límites exige nueva versión antes
de abrir resultados de evaluación. El piloto y sus ablaciones son exploratorios.

## Pregunta y alcance

¿Una política sin IA reduce concentración de exposición respecto de relevancia
y proximidad conservando utilidad según juicios humanos independientes?
Unidad: una tarjeta por negocio. Comparador principal: Proximity; Relevance y
las ablaciones son controles secundarios. La actividad queda excluida hasta
definir y validar una señal sustantiva. Plan pago, clics y ventas no otorgan mérito.

Esta fase cubre negocios. No completa PRD §9: faltan catálogo, 3.000 productos,
límites por comercio y escenario de concentración de catálogos. Juicios humanos
sobre fichas sintéticas siguen siendo una evaluación de escenarios simulados,
no validación con clientes reales ni evidencia de ventas.

## Separación y congelamiento

| Uso | Semillas propuestas | Permitido |
| --- | --- | --- |
| Exploración histórica | 17, 29, 43 | Entender defectos; nunca prueba final |
| Entrenamiento de rúbrica | 61 | Dos consultas por estrato, sin usar sus grados en métricas |
| Calibración | 101, 103, 107 | Afinar pesos con sus propios juicios |
| Evaluación reservada | 211, 223, 227, 229, 233 | Una política congelada frente al comparador |

Cada dataset: 200 negocios, 5.000 consultas cronológicas, 60 días; historial
inicial vacío y ventana de 30 días independiente por política/contexto. No se
divide aleatoriamente una misma secuencia: evitar compartir historial o entidades
entre conjuntos. IDs compuestos `datasetVersion/seed/localId` evitan confundir
los GUID locales reutilizados por el generador. Mismo dataset entre variantes.

Antes de generar evaluación: fijar versión del generador, fichas semánticas de
oferta/necesidad, reglas de elegibilidad, rubricado y selección de consultas.
El generador base usa necesidades numéricas; `business-review-cases-v1` ya las
describe como ofertas/necesidades ficticias en cuatro rubros para entrenamiento
y calibración. [Paquetes preparados](packages/README.md). Revisar comprensión
humana antes de congelar; la semántica final reservada aún no está preparada.
Las ofertas deben describirse con hechos comprensibles; no traducir el grado del
oráculo a una descripción ni mostrarlo al evaluador. Reservar autores/fichas de
evaluación respecto de calibración; verificar solapamiento exacto y semántico.

Muestra humana propuesta: 30 consultas de calibración (una por cada combinación
semilla × seis estratos, más doce por selección balanceada) y 60 de evaluación
(dos por semilla × seis estratos). Para calibración, tomar primero la consulta
con menor SHA-256 de `review-v1|seed|sequence` por celda; luego las dos siguientes
de cada estrato entre sus tres semillas. Evaluación usa las dos menores por celda.
Estratos = revista/búsqueda × Centro/Nueva Córdoba/Alberdi. Selección independiente
del puntaje, número de resultados, existencia de juicios y métrica obtenida.
Consulta vacía se conserva. Una celda insuficiente impide cerrar el dataset;
no se reemplaza una consulta por haber resultado difícil o desfavorable.

Calibración: explorar Eq ∈ {0,025; 0,05; 0,10}, N ∈ {0; 0,025; 0,05}, rotación
activada/desactivada (18 configuraciones). Mérito recibe el peso restante;
mantener proximidad, radio, ventana y discretización del piloto. No evaluar
actividad. Elegir entre configuraciones que cumplan todos los criterios en
calibración: menor desviación por mérito global, luego mayor nDCG humano global,
luego menor Eq, menor N y rotación desactivada. Sin ganadora: revisar diseño
antes de evaluación; no abrir la reserva para encontrar una configuración.

Congelar archivo de parámetros, política, commit o árbol de fuentes SHA-256,
protocolo, fichas, consultas elegidas, juicios adjudicados y método de agregación.
El manifiesto debe incluir fechas y quién emitió los juicios. Una persona que no
calibre custodia los resultados reservados hasta ese punto. Si no es posible,
declarar falta de cegamiento; no llamarlo evaluación ciega. No se generaron todavía
los datasets reservados ni se encargaron juicios a personas.

El [preparador de borradores](freeze.md) ya conserva copias y hashes de fuentes,
protocolo, paquete original, binarios y calibración. Su verificador comprueba
integridad; no certifica autores/fechas ni cierra los requisitos metodológicos.

## Criterios principales acordados y salvaguardas del diseño

Comparar política congelada menos Proximity sobre los mismos casos. Para Gini
y desviación por mérito, mejora relativa = (referencia − política) / referencia;
si referencia = 0, exigir política = 0 y reportar mejora relativa no aplicable.
Los límites incluyen igualdad y se aplican sin redondear datos.

| Medida | Global y cada superficie | Cada superficie × zona |
| --- | --- | --- |
| Pérdida absoluta de nDCG@10 humano | ≤ 0,02 puntos | ≤ 0,03 puntos |
| Reducción relativa de Gini | ≥ 10 % | No aumentar más de 0,01 puntos |
| Reducción relativa de desviación por mérito | ≥ 5 % | No aumentar más de 0,01 puntos |
| Cobertura de elegibles | No caer más de 0,01 puntos | Igual límite |
| Cobertura de negocios nuevos elegibles | No caer más de 0,01 puntos | Igual límite |

Como salvaguarda adicional de diseño, cada semilla global debe cumplir pérdida humana ≤ 0,03, no empeorar
Gini ni desviación por mérito y no perder más de 0,01 en ambas coberturas.
El límite de relevancia protege utilidad; las mejoras mínimas evitan aceptar
cambios insignificantes solo por tener signo favorable. Los límites principales
de relevancia, mejora global/por superficie y cobertura fueron aceptados para
preparar el experimento. Los controles adicionales por semilla y de empeoramiento
local son salvaguardas metodológicas propuestas, pendientes de congelar con el diseño.
Se privilegia conservar utilidad incluso si eso descarta todas las variantes.

## Métricas, faltantes y decisión

Exposición: atención **modelada** top 10, 1/log2(posición+1); no impresiones visibles.
Gini, cobertura, participación top diez y desviación se calculan sobre todos los
elegibles, incluidos ceros. Reportar global, por superficie y por seis estratos;
agregar exposiciones antes de calcular Gini, nunca promediar Gini de zonas.
Desviación por mérito usa M operacional del motor: no es prueba de equidad
respecto de preferencias humanas. Las zonas superpuestas no son réplicas independientes.

nDCG humano: ganancia 2^grado−1, ideal sobre **todos** los elegibles de cada consulta
seleccionada. Reproducir historial completo, medir nDCG solo en la muestra; no
reiniciar el ranking para cada ficha. No rellenar consultas no anotadas con el
oráculo. Su nDCG sintético puede ir en un anexo separado sin decidir aceptación.
Promediar consultas con ideal positivo dentro de cada semilla/estrato, después
promediar semillas con el mismo peso. Hacer restas pareadas antes de resumir.

Registrar consultas vacías, sin ganancia ideal, faltantes y nuevos elegibles.
Para decidir: al menos una consulta con ideal positivo por semilla × estrato,
y al menos ocho de las diez consultas de evaluación por estrato con ideal positivo.
En calibración, exigir al menos cuatro de sus cinco consultas por estrato y
mantener la condición de una consulta útil por semilla × estrato.
Todos los pares elegibles deben tener juicio final; no evaluados ≠ irrelevantes.
Ausencia de exposición, denominadores o nuevos elegibles en un ámbito obligatorio
produce resultado no concluyente, nunca aprobación artificial por cero o null.

Reportar media, desviación estándar muestral, valores por semilla y diferencias
pareadas; con cinco semillas no se afirma generalización estadística al mercado.
Estados: **cumple en esta colección** si todos los criterios y datos están completos;
**no cumple** si datos completos incumplen algún límite; **no concluyente** si falta
evidencia requerida. No aceptar por promedio global cuando falla un estrato.
Publicar también resultados desfavorables. Tras abrir reserva, cualquier ajuste
es exploratorio y requiere otra colección reservada para una nueva confirmación.

## Trabajo que habilita ejecutarlo

1. Límites principales acordados. Confirmar recursos de anotación y cerrar diseño.
2. Paquetes de entrenamiento/calibración preparados; hacer entrenamiento de rúbrica
   con personas y cerrar materiales semánticos de evaluación por separado.
3. Importación, preparación/recolección y comparación de calibración implementadas,
   con historial completo, juicios verificados y agregados por superficie.
4. Barrido y comprobador de calibración implementados; ejecutar con juicios
   humanos, cerrar diseño y congelar manifiesto/política antes de abrir evaluación.
5. Implementar y verificar el [contrato de integración](../../ranking-public-integration.md)
   antes de considerar activación. Aprobar un experimento offline no despliega nada.

El [comparador humano](human-ranking.md) ejecuta una candidata explícita frente a
Proximity en calibración. El [barrido completo](calibration.md) ya comprueba
criterios y suficiencia y sugiere una candidata provisional; distingue límites
acordados de salvaguardas propuestas. Falta evaluación reservada:
**no ejecuta todavía el protocolo final ni decide aceptación final**.
Las suites pilot/ablation conservan el oráculo sintético. `plan.json` no se pasa
como `--config`: ese argumento acepta exclusivamente pesos del motor.

## Fuentes y decisiones propias

[Biega et al.](https://arxiv.org/abs/1805.01788) fundamenta estudiar atención
acumulada junto con calidad. [Singh y Joachims](https://arxiv.org/abs/1802.07281)
relaciona exposición y utilidad con criterios de equidad definidos explícitamente.
Nuestra heurística no reproduce sus optimizadores ni hereda sus garantías.
[TREC](https://trec.nist.gov/data/reljudge_eng.html) exige correspondencia entre
colección y juicios; [Buckley et al., NIST](https://www.nist.gov/publications/bias-and-limits-pooling-large-collections)
documenta sesgos de conjuntos incompletos de juicios. Por eso aquí se propone
evaluar todos los elegibles de menos consultas, en vez de tratar faltantes como cero.
Cantidades, límites y rúbrica son decisiones de esta propuesta.
