# Tareas

- [x] Definir replay humano, ámbitos, faltantes y límites de calibración.
- [x] TDD núcleo (requisitos 11/12): cuatro rojas antes del runner; pasar con
  cronología completa, estados separados y exposición acumulada por superficie.
- [x] TDD exportación/comparación (12/13): cuatro rojas por stubs; implementar
  paquete verificado, pesos congelados, tres semillas y medias pareadas.
- [x] CLI (13): opción inicialmente rechazada; implementar y verificar códigos
  0/2/1 mediante pruebas de exportación y recorrido real de pendientes/errores.
- [x] Refactorizar lectura verificada compartida con recolección sin cambiar Build.
- [x] Ejecutar suite completa, preservar hashes/hojas reales y documentar continuidad.

Comando reproducible: `dotnet test App/backend/tests/Marketplace.BLL.Tests
--configuration Release --no-restore -m:1`. CLI y configuración de ejemplo en
`Doc/evaluation/business-ranking-final/human-ranking.md`.

Pendientes fuera del incremento: anotación humana, comprobador de criterios y
suficiencia, barrido de las 18 celdas, selección/congelamiento y evaluación reservada.
