# Ranking de negocios: protocolo del piloto

Estado: diseño experimental inicial; no es aprobación del ranking público ni
del experimento final. Se implementa primero un laboratorio local, sin IA.

## Fuentes primarias consultadas

- Singh y Joachims, [Fairness of Exposure in Rankings](https://arxiv.org/abs/1802.07281)
  (2018): relaciona asignación de exposición y utilidad de los resultados.
- Biega, Gummadi y Weikum, [Equity of Attention](https://arxiv.org/abs/1805.01788)
  (2018): propone estudiar atención acumulada en secuencias de rankings.
- Järvelin y Kekäläinen, [Cumulated Gain-based Evaluation of IR Techniques](https://researchportal.tuni.fi/en/publications/cumulated-gain-based-evaluation-of-ir-techniques)
  (2002): fundamento de evaluación con juicios graduados y ganancia descontada.

La implementación siguiente es una heurística propia para explorar el PRD;
no reproduce los optimizadores ni hereda garantías de estos trabajos.

## Contrato previo a las corridas

Unidad: negocio, una tarjeta por negocio. Se comparan relevancia, relevancia
más proximidad, equidad sin actividad y equidad con actividad. La revista sin
texto usa pertinencia de rubro = 1; búsqueda usa coincidencia sintética de
etiquetas. Nunca se exige catálogo, plan pago, ventas ni clics para participar.

Antes de ordenar: publicado, responsable habilitado, fecha de publicación no
futura, coincidencia mínima R >= 0,2 y distancia dentro del radio si hay centro.
Se recibe el conjunto completo; nunca se recorta al top 20 antes de rankear.
Duplicados e inputs no finitos o fuera de dominio se rechazan.

Configuración versionada `business-pilot-v1`, inicial y sin calibrar:

- Proximidad P = exp(-distanciaKm / 2). Radio y decaimiento son independientes.
- Mérito M = 0,7 R + 0,3 P; sin centro, M = R.
- Déficit Eq = clamp((esperada - recibida) / max(esperada, 1), 0, 1).
- Novedad N = max(0, 1 - edadEnDías / 14), sin reiniciar por edición.
- Relevancia sola: R. Relevancia/proximidad: M.
- Equidad: 0,85 M + 0,10 Eq + 0,05 N.
- Con actividad: 0,80 M + 0,10 Eq + 0,05 N + 0,05 A.
- A está acotada a [0,1], es exógena y exclusivamente sintética en el piloto.
  No se conecta `UpdatedAt`, impresiones ni clics al ranking de la aplicación.
- Rotación solo en variantes de equidad: intervalos fijos de 0,01 de puntaje,
  SHA-256 de semilla/contexto/secuencia/id y finalmente id. No se usa hash de
  proceso ni un comparador no transitivo de «diferencia menor a epsilon».

Exposición simulada: top 10 con descuento 1/log2(posición+1). Todas las
posiciones del top 10 reciben esa atención modelada; no equivale a la impresión
visible instrumentada en el navegador. La exposición esperada reparte el mismo
presupuesto entre todos los elegibles, proporcional a M, con tope 1 por negocio
y redistribución del remanente. El tope evita exigir más que la primera posición.

Historial separado por variante y contexto (superficie, zona, rubro e intención),
ventana móvil (t-30 días, t]. Primero expira historial, luego ordena y finalmente
actualiza exposición esperada y recibida. No se consulta el futuro. El piloto
empieza sin historial y conserva ceros para todos los elegibles. Los negocios
nuevos ingresan a mitad de la secuencia. No se transfieren deudas entre zonas.

## Relevancia independiente y métricas

El generador define necesidades y capacidades sintéticas. Un oráculo separado
asigna grados 0..3 por adecuación de oferta y bandas de distancia; estos grados
no se entregan al motor. La señal R tiene ruido independiente del juicio, por lo
que nDCG no se calcula contra el propio puntaje. Es independencia de fórmula,
no validación humana: los supuestos sintéticos siguen condicionando el resultado.

Por cada superficie/zona y también globalmente:

- Gini sobre exposición de la unión de candidatos elegibles, incluidos ceros.
- Participación de los diez negocios más expuestos (no las diez posiciones).
- Cobertura: elegibles con exposición / elegibles únicos.
- nDCG@10: ganancia 2^grado-1 y descuento logarítmico; ideal calculado sobre
  todos los candidatos elegibles. Sin ganancia ideal: null, fuera del promedio,
  con contador explícito. Consultas vacías se cuentan por separado.
- Desviación por mérito: 0,5 * suma |exposición/suma - esperada/suma|.
  Si no hay exposición, métricas de distribución = null, no éxito artificial.

Se reportan métricas acumuladas de la corrida; el historial utilizado por Eq
sí expira a 30 días. No confundir ambas ventanas.

## Dataset y límites

200 negocios y 5.000 consultas cronológicas durante 60 días por semilla,
semillas 17, 29 y 43. Zonas sintéticas alrededor de Centro, Nueva Córdoba y
Alberdi, con densidades distintas. Mitad revista, mitad búsqueda de negocios.
Archivo reproducible de inputs, configuración y resultados con SHA-256;
trazas top 10 y exposición esperada por candidato permiten auditar el cálculo.
Resumen por semilla y media/desviación estándar muestral entre semillas.

Sin productos: los 3.000 productos y el escenario de concentración de catálogos
del PRD requieren el incremento de catálogo. Tampoco se prueba latencia HTTP,
causalidad, ventas, calidad real ni resistencia a manipulación de actividad.
No se define umbral de éxito después de ver los resultados. Antes del experimento
final deben acordarse pérdida máxima de nDCG y mejora exigida, incorporar juicios
humanos y separar conjuntos de calibración y evaluación. El piloto informa
resultados favorables y desfavorables sin declarar validada la hipótesis.
