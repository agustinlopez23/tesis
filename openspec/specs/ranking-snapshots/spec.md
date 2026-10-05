# Snapshots y exposición persistente de revista

## Creación atómica e idempotente

BLL adapta todos los candidatos y genera traza reproducible con entradas,
componentes, saldos previos, posición, objetivo y exposición modelada top 10.
SHA-256 identifica configuración, variante, semilla y esquema. Sin dueño,
visitante ni requestId en oportunidades. Si el actor posee un candidato,
toda la oportunidad contribuye cero, con razón own-business-preview.

En una transacción PostgreSQL se guarda snapshot, oportunidad, contribuciones
y secuencia. Tres reintentos concurrentes del mismo requestId/contexto producen
una oportunidad; otro contexto con igual requestId produce Conflict. Distintos
requestId en un contexto se serializan y leen los saldos ya confirmados.
Una falla después de insertar snapshot revierte todo, incluida la secuencia.

## Lectura y retención

TTL de 30 minutos sin renovación; reintento vencido produce Expired mientras
exista el vínculo. Tras purgarlo no se promete deduplicación. Secuencia conservada.
Historial por contexto en (ahora−30 días, ahora], incluso antes de la purga.
Purga al iniciar y cada hora; falla temporal se reintenta en la siguiente hora.

Páginas internas de 20 posiciones, orden inmutable. Cada lectura revalida dueño,
publicación, rubro y distancia en una consulta y devuelve datos públicos actuales.
Retirados dejan huecos; nuevos esperan otra lista. Conserva posiciones originales
y avance de 20 aunque una página quede vacía. Relectura no suma exposición.
Lista desconocida/vencida no entrega página. Offsets negativos/no múltiplos de
20/fuera de lista son inválidos. Lista vacía admite offset 0 sin siguiente página.

La lectura histórica materializa como máximo límite+1 saldos agregados por negocio,
incluidos los ya inelegibles. Límite predeterminado 10.000, configurable 1–100.000.
Excederlo rechaza toda la creación, revierte la secuencia y permite reintentar.
Replays vigentes no reconstruyen candidatos/saldos. No se acota el trabajo SQL
necesario para agregar contribuciones de los 30 días del contexto.

Servicios expuestos solo mediante previsualización local, según `ranking-http`;
sin cambio del orden de la guía. La activación requiere política evaluada y UI.
