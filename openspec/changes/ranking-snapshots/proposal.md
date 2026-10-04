# Listas persistentes y exposición modelada

Completar la persistencia del ranking de revista para conservar orden al releer,
deduplicar reintentos y recordar exposición durante 30 días. La creación debe
guardar lista, oportunidad, contribuciones y secuencia en una sola transacción.
Incluye lectura interna con TTL de 30 minutos, revalidación y purga periódica.
Sin endpoints ni UI de ranking hasta cerrar política/evaluación y controles de carga.

Unidades locales encadenadas: cálculo/traza → esquema → almacenamiento atómico →
coordinación/paginación/retención. Cada unidad mantiene sus pruebas y evidencia.
Se conservarán límites de revisión de 400 líneas cuando la unidad sea separable;
ningún PR se publica en este tramo. La entrega académica ya fue realizada.
