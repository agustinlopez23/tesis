# Diseño y límites

BLL construye una oportunidad completa sin IDs de dueño/visitante: entradas,
componentes, orden, saldo anterior, objetivos y asignación top 10. Hash SHA-256
incluye esquema, configuración completa, variante y semilla. Previsualización con
negocio propio anula toda la contribución, preservando traza y orden.

DAL usará tablas internas SQL administradas por una migración explícita, sin
entidades EF: contexto/secuencia, snapshot efímero, oportunidad y contribuciones.
No tienen FK hacia cuentas/negocios: la historia debe sobrevivir a cambios del
catálogo. Oportunidades no contienen requestId ni dueño. Snapshot guarda vínculo
efímero requestId→lista y contexto. La lista no almacena DTOs públicos.

Una transacción bloquea primero requestId (advisory lock) y luego la fila del
contexto, incrementando secuencia con RETURNING. Reintento valida contexto antes
de construir; nunca suma exposición. El callback BLL corre dentro de esa misma
transacción y conexión, obtiene candidatos/saldos, calcula y devuelve la traza.
Fallos cancelan también el avance de secuencia. Read Committed permite ver el
commit previo después de esperar el bloqueo del contexto.

TTL 30 minutos sin renovación, historial (ahora−30 días, ahora] y purga de filas
vencidas al arrancar/cada hora. Contextos conservan secuencia tras purgar historia.
Una lectura de página revalida los 20 IDs originales contra datos actuales; no
rellena huecos. Endpoints, cursores HTTP, carga/abuso, UI y política evaluada son
trabajo posterior. No se habilita el ranking público por registrar servicios.

Rollback: retirar servicios/worker y revertir la migración elimina únicamente
tablas de ranking nuevas; no afecta publicaciones, actividad ni guía actual.
