# Diseño

Protocolo congelado antes de ejecutar: `Doc/ranking-research-2026-09-19.md`.
BLL contiene tipos puros, ranking y libro de exposición en memoria. No referencia
DAL, HTTP, EF ni el runner. `backend/tools/Marketplace.Evaluation` es consola .NET
sin paquetes nuevos, dependiente de BLL; genera datos y métricas offline.
`Marketplace.BLL.Tests` usa las versiones xUnit ya fijadas y prueba ambos proyectos.

La consola exporta JSON de inputs, trazas JSONL y resumen JSON/Markdown a un
directorio explícito. No accede a datos reales, credenciales ni la API. Hashes
de archivos identifican el dataset y resultados; la configuración acompaña cada
reporte. Los tiempos de ejecución no forman parte del resultado determinista.

La guía y sus tokens conservan la versión del orden actual. Activar el motor
necesitará contrato de candidatos/contextos históricos, estabilidad de páginas y
snapshot del orden; reutilizar únicamente eventos visibles no permite reconstruir
los candidatos que nunca aparecieron.

Unidades de revisión previstas (hasta 400 líneas por unidad cuando sea posible):
protocolo; motor y pruebas; historial y pruebas; métricas y pruebas; generador;
runner/reproducibilidad; reporte y continuidad. No se abre PR ni publica nada.
Rollback: retirar los proyectos de evaluación; no hay migración o estado público.
