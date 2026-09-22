# Diseño previo

Entrenamiento: semilla 61, dos consultas por cada uno de seis estratos (12).
Calibración: 101/103/107, una por semilla/estrato más dos por estrato (30).
Elegir por SHA-256 review-v1|seed|sequence antes de mirar candidatos o juicios.
Conservar vacías. Generador actual de 200 negocios/5.000 consultas sin alterarlo.

Un vocabulario semántico versionado asigna a las capacidades numéricas ofertas
ficticias en cuatro rubros. Describe capacidades primarias/secundarias como hechos,
sin convertir el grado del oráculo en texto. Distancias vienen del dataset.
No se llama Judge, no se ejecutan variantes ni se generan notas humanas.
Es material de preparación a revisar; no una colección real o final independiente.

Paquete: inputs completos, queries/candidates CSV, hojas separadas E1/E2 con
contexto y oferta y campos grado/razón vacíos, resolutions CSV y manifest.json.
Cada hoja se ordena por hash específico del evaluador, sin puntajes/posiciones.
Manifiesto fija hashes de inputs/fichas. Su digest se guarda fuera del paquete;
collect requiere ese digest y reextrae desde generador versionado para verificar
selección y todos los elegibles. Hashes no prueban procedencia humana.

Solo distribuir la hoja de cada evaluador y rúbrica, nunca inputs/coordinador.
Collect permite editar solo grado/razón; faltar filas deja revisión incompleta.
Errores y modificaciones de hechos fallan antes de escribir. Sobrescritura prohibida.
Tres unidades de revisión <400 líneas: selección/semántica; exportación/recolección
y adaptador; pruebas/evidencia/documentación (dividir esta última si supera límite).
