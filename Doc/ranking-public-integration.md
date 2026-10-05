# Contrato propuesto de integración del ranking · 21/09/2026

**Persistencia interna implementada al 04/10/2026:** historial, lector, adaptador,
snapshots/ledger transaccionales, paginación revalidada y purga automática.
**HTTP local implementado el 04/10/2026:** POST/GET, cursores protegidos, validación,
no-store, errores y cuotas por proceso. 100/100 API/HTTP/PostGIS. Solo Development/
Testing con `Ranking:PreviewEnabled=true`; Production permanece cerrado incluso
con esa opción. Política local de prueba EquityOnly/default/17, sin aprobación.
**Capacidad local verificada el 04/10/2026:** candidatos y saldos acotados,
concurrencia separada POST/GET y carga con 1.000 negocios. Proxy/interfaz pendientes;
el dimensionamiento productivo todavía requiere medición en su despliegue real.
Activar depende de una política evaluada y versionada. BLL define interfaces y reglas,
DAL implementa transacciones/PostGIS, API valida HTTP/protege tokens y Next
administra navegación. Se mantiene ADR-001. Activación deshabilitada por defecto.

## Brechas comprobadas y primer alcance

`BusinessRepository.GetPublishedAsync` aplica Skip/Take antes de devolver a BLL;
no sirve como fuente completa del ranking. `BusinessProfile` ahora incorpora
`FirstPublishedAt` y `PublicationHistoryUnknown`, persistidos atómicamente; la
migración marca todos los registros anteriores como desconocidos, también borradores.
`ActivityContext` identifica negocio/posición/versión, pero no todos los elegibles.
Los eventos de actividad actuales no permiten reconstruir oportunidades históricas.
`IRankingCandidateRepository` ya aporta una lectura interna completa separada de
la guía. `MagazineRankingCandidates` la adapta al motor; `RankingSnapshotService`
coordina persistencia y `RankingSnapshotPages` revalida páginas actuales.

Primer corte propuesto: **revista**, rubro obligatorio, zonas predefinidas y radio
fijo 4 km, sin texto ni coordenadas del dispositivo. R=1 para los elegibles del
rubro; P conserva decaimiento de 2 km. El contexto es superficie/zona/rubro/radio/
versión de elegibilidad. Referencias geográficas públicas, no ubicación personal.
Búsqueda libre, GPS, otros radios y ausencia de ubicación conservan la guía actual.
Ampliarlos exige adapter de relevancia y contrato de privacidad evaluados; el
generador de etiquetas sintéticas no es un buscador textual productivo.

`FirstPublishedAt` se escribe una sola vez al pasar a publicado; editar,
despublicar y republicar no reinician edad. Históricos sin evidencia se marcan de
edad desconocida; el adaptador conserva PublishedAt=null y el motor asigna N=0. No usar CreatedAt.
Conservar esa marca también al republicarlos: null no significa «nunca publicado».
El motor/adaptador representa esa ausencia explícitamente. Para nuevos
borradores sí registrar el instante de su primera publicación futura.

## Candidatos y oportunidad histórica

Interfaz BLL implementada obtiene **todos** los elegibles: publicado, dueño habilitado,
rubro y radio válidos; sin corte top 20 previo. Un SELECT DAL/PostGIS mantiene una
vista consistente. IDs únicos y valores finitos; fallo de integridad aborta la lectura.
Devuelve ID de negocio/dueño interno, distancia y fecha/marca histórica. Su orden
por ID es técnico; no calcula ranking ni guarda oportunidades. El lector admite
DiscoveryArea válida; el adaptador restringe revista a zonas y 4 km.
El adaptador usa centro/nueva-cordoba/alberdi, con los mismos puntos públicos de
la guía. Clave invariante: `magazine/{zone}/category/{id}/radius/4/eligibility/v1`.
Cambiar puntos o reglas requiere versionar elegibilidad. R=1 y actividad=0;
conserva dueños internamente, sin DTO público. Toma hora del servidor tras leer
y aborta ante historia incoherente, fecha futura, IDs inválidos/duplicados,
distancia fuera de radio o cancelación. No elige política ni actualiza exposición.
Evidencia: 95/95 BLL/evaluación y 62/62 HTTP/PostGIS el 04/10, incluidos
45 candidatos y desconocidos republicados.
Un límite operativo de candidatos no autoriza truncamiento silencioso: devolver
error recuperable o ofrecer guía vigente con modo y versión distintos.

Por oportunidad guardar, sin identidad del visitante: ID aleatorio, instante del
servidor, contexto canónico, versión/hash de parámetros, secuencia por contexto,
conjunto completo de candidatos y componentes necesarios para reproducir:
R, P, mérito, edad conocida/desconocida, balance previo esperado/recibido,
bonificación, puntaje, orden final y objetivo de exposición por candidato.
Guardar también ceros y candidatos no seleccionados. El registro histórico es
privado; no contiene textos buscados, coordenadas personales, IP, VisitId ni cuenta.

La exposición de este ledger es **asignación modelada del top 10 al generar la
lista**, no atención observada ni garantía de entrega HTTP. Presupuesto igual
al piloto, incluido tope 1 por negocio; la página puede mostrar 20 pero posiciones
11+ no reciben atención en este modelo. Medición visible y reportes del dueño
continúan separados, con su deduplicación actual. No mezclar numeradores visibles
y objetivos modelados ni usar clics para liquidar deuda.

Cada nueva lista actualiza el ledger una sola vez; releer páginas no actualiza
presupuesto ni secuencia. Publicación posterior no modifica una oportunidad
pasada. Retirar contenido después tampoco reescribe la asignación histórica.
Peticiones de un responsable autenticado que incluyan un negocio suyo no aportan
al ledger: presupuesto cero para toda esa oportunidad, razón own-business-preview;
no quitar solo su tarjeta y desbalancear el presupuesto. No es protección contra bots.

## Snapshot, idempotencia y paginación

Implementación interna: cuatro tablas SQL administradas por `AddRankingSnapshots`,
fuera del modelo de entidades EF. DAL toma bloqueo transaccional por requestId y
luego actualiza la secuencia del contexto; el callback BLL consulta/calcula sobre
la misma conexión/transacción. Read Committed ve los saldos confirmados después
de esperar el contexto. Fallos/cancelación revierten también secuencia.
Snapshot conserva IDs y vínculo efímero; oportunidad JSON conserva la traza sin
requestId ni dueño, con contribuciones consultables por contexto/tiempo.
Reintento conserva la política original aunque cambie la configuración del servidor.
Pruebas: 99/99 BLL previas; 83/83 API/HTTP/PostGIS actuales. El contrato HTTP
siguiente está implementado para previsualización local; la guía conserva su orden.

HTTP local: `POST /public/ranking-snapshots` recibe zoneId, categoryId,
radiusKm=4 y requestId aleatorio estable durante reintentos de esa consulta.
Validar Origin, encabezado X-Requested-With, tamaño y tipos como otras mutaciones.
Respuesta contiene items públicos, measurement, expiresAt, rankingVersion,
hasMore y nextCursor. Creación 201, reintento 200. Política no configurable por
el cliente; rechaza propiedades desconocidas, rubros inexistentes y cuerpo >4096.
`GET /public/ranking-snapshots/page?cursor=...` devuelve la siguiente página.
Ambos no-store. La ruta GET actual continúa disponible para el orden vigente.

Orden completo de IDs inmutable durante **30 minutos sin renovación**. Cursor
protegido: snapshotId, offset de posiciones, tamaño fijo 20, versión y vencimiento;
sin filtros ni ubicación. Rechazar combinación con filtros nuevos (400). Token
alterado/inválido: 400; snapshot válido pero vencido o ya purgado: 410 y acción
visible «Actualizar resultados». Nunca mezclar página vieja con lista recalculada.
Una búsqueda/filtro nuevo crea otro snapshot y comienza en la primera página.

Antes de servir cada página, revalidar publicación, dueño y pertenencia al rubro/
radio de los IDs en sus 20 posiciones originales. Excluir los que dejaron de
cumplir; no rellenar huecos trayendo posiciones futuras ni volver a ordenar.
Leer datos públicos actuales, no DTOs antiguos que expongan contenido retirado.
Conservar posición original en tokens de medición; nextCursor avanza 20 posiciones
aunque queden menos tarjetas. hasMore indica posiciones por examinar; una página
vacía intermedia permite seguir o actualizar. Negocios nuevos esperan otra lista.

Crear snapshot, oportunidad, contribuciones y secuencia **en una transacción**.
El ledger productivo reside en DAL; el ExposureLedger en memoria del laboratorio
no se convierte en un singleton compartido entre peticiones ni instancias.
Serializar actualizaciones por contexto; índice único para requestId. Reintento
con igual ID y filtros devuelve el mismo snapshot sin duplicar exposición;
reutilización con otros filtros: 409. Validar asociación antes de devolver datos.
Guardar vínculo requestId→snapshot solo durante su TTL, sin copiarlo al ledger.
Tras expirar, un requestId conocido responde 410; tras purgar la asociación no
puede garantizarse deduplicación: el cliente debe emitir ID nuevo, no reintentar
una operación vencida. Límite explícito, no garantía de exactamente una vez ilimitada.

Cuotas iniciales por proceso: 60 POST/minuto y 600 GET/minuto. Cupos simultáneos
independientes: 4 POST y 16 GET, sin cola; exceso 429 no-store. Se liberan al
completar, fallar o cancelar, incluida la serialización de la respuesta.
Máximos por creación: 1.000 candidatos y 10.000 saldos históricos por contexto,
incluidos negocios ya inelegibles. DAL materializa límite+1 para detectar exceso;
si lo hay, rechaza todo con 503 y rollback, sin truncar ni avanzar secuencia.
Reintentar el mismo requestId es seguro; un replay vigente no reconstruye la lista.
La agregación SQL de saldos sigue procesando el historial de 30 días del contexto;
el límite de filas materializadas no acota ese trabajo de base ni la retención total.
Configuración y rangos en `App/README.md`; valores inválidos impiden el arranque.
Carga local verificada con 1.000 negocios, 17 listas (16 abandonadas tras página 1),
4 creaciones/16 lecturas simultáneas, paginación completa y replay sin nuevos aportes.
Ver [evidencia](../openspec/changes/archive/2026-10-04-ranking-capacity/verify-report.md).
No hay limitación distribuida; antes de activar, dimensionar latencia y memoria en
el despliegue real. Snapshot y tokens no sustituyen controles de abuso; asignación
modelada puede manipularse creando nuevas consultas.
Fallo transaccional: no devolver una lista experimental parcialmente registrada.
UI permite reintentar o usar guía vigente, identificada con su versión real.

## Retención, compatibilidad y verificación futura

Ledger: excluir aportes de (ahora−30 días) o anteriores inmediatamente; purgar
al arranque y cada hora. Snapshot: TTL 30 minutos, borrado periódico y rechazo
inmediato al vencer. No registrar cuerpos, cursores, requestId ni datos personales
en logs del nuevo flujo; revisar también proxy/backups. Tokens v2 de actividad
pueden contener snapshotId y posición; aceptar v1 hasta su vencimiento máximo de
30 minutos, sin inventar asociaciones históricas para ellos. La actividad privada
mantiene su definición y no expone candidatos, componentes ni ledger.

En previsualización, apagar `Ranking:PreviewEnabled` cierra POST con 404;
cursores ya emitidos siguen legibles. `Ranking:WithdrawPages` cierra lectura y
creación habilitada con 410. En producción ambas rutas permanecen cerradas.
El siguiente comportamiento de UI aún está propuesto:
al apagar la función, nuevas búsquedas usan guía vigente; cursores ya emitidos
pueden terminar su TTL si el servicio está sano. Si hay retirada de emergencia,
responder 410 y ofrecer reinicio, sin reordenar páginas en silencio.

Pruebas requeridas, con PostgreSQL/PostGIS temporal cuando corresponda:

| Caso | Resultado esperado |
| --- | --- |
| 45 elegibles; paginar 20/20/5 y repetir página | Sin duplicados ni nuevas contribuciones |
| Dos creaciones concurrentes con mismo requestId | Una oportunidad y un presupuesto |
| Distintos requestId en igual contexto | Secuencia e historial serializados |
| Fallo entre guardado de snapshot y ledger | Rollback de ambos |
| Despublicar/suspender/mover/cambiar rubro antes de página 2 | No exponer contenido retirado; huecos sin reordenamiento |
| Vencimiento, token alterado, mezcla de filtros | 410/400, sin reinicio silencioso |
| Candidato fuera del top 20 | Conservado en oportunidades y exposición esperada |
| Editar y republicar; edad histórica desconocida | No reiniciar novedad; desconocido recibe N=0 |
| Purga, apagado, own-business-preview | Ventanas y presupuesto según contrato |

[RFC 9110 §15.5.11](https://www.rfc-editor.org/rfc/rfc9110.html#name-410-gone)
define 410 para recursos retirados; TTL, rutas y semántica de snapshot anteriores
son decisiones propuestas para Cerca. Este documento no autoriza activación.
