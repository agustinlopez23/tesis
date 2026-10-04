# Verificación — 04/10/2026

RED: 14 fallos compilables, 13 contra adaptador sin implementar y uno porque
el motor excluía historia desconocida. GREEN y regresión: **95/95 BLL/evaluación**,
0 omitidas, 46 s. **62/62 HTTP/PostGIS**, 0 omitidas, 6 s, bases temporales reales.
Compilación Release de ambos recorridos sin advertencias.

Comandos desde raíz, DOTNET_ROOT=.tools/dotnet y DOTNET_CLI_HOME=.tools/cli:

```powershell
& .tools/dotnet/dotnet.exe test App/backend/tests/Marketplace.BLL.Tests --no-restore --configuration Release -m:1 -p:UseSharedCompilation=false
& .tools/test-publication-history.ps1 -Filter 'FullyQualifiedName~Marketplace.Api.Tests'
```

Para reproducir RED se usó el filtro
`FullyQualifiedName~MagazineRankingCandidatesTests|FullyQualifiedName~Unknown_publication_history`.
El helper HTTP carga la conexión local sin imprimir credenciales. La fixture crea,
migra y elimina bases aisladas. Recorrido runtime: IoC → PostGIS → adaptador →
motor conserva 45 candidatos; HTTP publica/despublica/republica históricos y el
motor mantiene N=0. Regresión de guía incluida. Sin migración de la base de demo.

Revisión de código: claves invariantes y sin coordenadas personales, colección y
mapa de dueños de solo lectura, ninguna referencia del flujo público al adaptador.
No se repitieron experimentos completos ni se tocaron paquetes/borrador previos.
Sin UI nueva: revisión visual y pruebas frontend N/A. Faltan snapshots/ledger y
política evaluada para integrar públicamente; este cambio no los activa.
Rollback delimitado en design.md, sin cambios de esquema.
