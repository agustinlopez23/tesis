# Diseño

`MagazineRankingCandidates.PrepareAsync` solo acepta ID de zona predefinida,
rubro positivo, semilla y secuencia no negativa. Reutiliza los tres puntos públicos
de `discovery-location.ts`; radio fijo 4 km. Clave canónica invariante:
`magazine/{zone}/category/{id}/radius/4/eligibility/v1`. Modificar puntos/reglas
requiere nueva versión. No recibe ubicación personal ni texto.

Una lectura completa mediante IRankingCandidateRepository; reloj del servidor
después de la lectura para incluir publicaciones concurrentes ya visibles.
PublishedAt nullable representa historia desconocida en el motor; el adaptador
valida la marca de origen y nunca inventa fechas. R=1, actividad=0; la configuración
existente conserva decaimiento de proximidad de 2 km. IDs de dueño se mantienen
en un mapa interno para el futuro control de previsualización propia.

Corrupción, duplicados, fecha futura, distancia fuera de radio o cancelación
abortan el lote. No se calcula deuda ni elige política. Sin migraciones ni paquetes.
Rollback: quitar adaptador/registro/pruebas y revertir PublishedAt nullable y su
tratamiento en BusinessRanker. Borrador experimental congelable intacto.
