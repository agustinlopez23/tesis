# Recuperación interna de candidatos

`IRankingCandidateRepository` obtiene todos los negocios publicados con dueño
habilitado, del rubro y radio indicados. La categoría debe ser positiva y el área
cumplir DiscoveryArea.IsValid; categoría inexistente o sin coincidencias da vacío.

Una sentencia parametrizada PostgreSQL/PostGIS recupera ID de negocio, ID de dueño
interno, distancia en km, FirstPublishedAt y PublicationHistoryUnknown. No pagina
ni trunca; IDs ordenados solo para estabilidad técnica, sin calcular puntuación.
La respuesta es de solo lectura y conserva historia desconocida como null/true.

Entrada inválida, integridad inválida (IDs vacíos/duplicados, distancia no finita
o negativa, fecha/marca incoherentes) y cancelación rechazan la operación sin
devolver una lista parcial. Nuevas lecturas reflejan elegibilidad vigente; una
lista anterior no se actualiza ni constituye un snapshot persistente.

La guía pública mantiene sus contratos, orden y paginación. Este contrato interno
no activa ranking ni amplía superficies públicas. El adaptador de revista restringe
a zonas predefinidas y 4 km; conserva historia desconocida para que el motor dé N=0.
El límite operativo predeterminado es 1.000 candidatos, configurable de 1 a 10.000.
DAL obtiene como máximo límite+1 filas en la misma sentencia; detectar exceso
rechaza explícitamente toda la operación. Nunca devuelve un prefijo al motor.
