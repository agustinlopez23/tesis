# Verificación · 21/09/2026

Paquetes preparados, sin calificaciones humanas: entrenamiento tiene 12 consultas
con 521 pares por evaluador; calibración, 30 consultas con 1.143 pares por evaluador.
Son 1.042/2.286 juicios pendientes, no resultados de evaluación.

- TDD inicial: ocho pruebas rojas con NotImplementedException; luego 53/53.
- Revisión: comprobación por consulta de todos los candidatos elegibles contra
  inputs y prueba de filas ausentes/hechos alterados. Protección de LEEME: una
  prueba roja detectó que las instrucciones no estaban fijadas; corregida.
- TDD de auditoría: se amplió round-trip para exigir hash de la respuesta original;
  falló por campo ausente, se implementó. Resultado final **55/55**.
- Compilación Release sin advertencias, cero dependencias nuevas.
- CLI: dos generaciones completas por split; todos los bytes coinciden. Los
  hashes de manifiestos se verificaron contra cada archivo inmutable.
- Recolección real de ambos paquetes: código 2, todos los juicios pendientes,
  sin judgments-final.json. Evaluación reservada, sobrescritura y opciones
  incompatibles: código 1; sin salida de evaluación ni mezcla de semillas.
- ZIP de E1/E2 contienen solo su hoja en blanco y LEEME. No se distribuyeron.
- Las pruebas que rellenan grados son fixtures de software y se eliminan; nunca
  se guardaron como juicios humanos de los paquetes entregados.
- JSON, semillas disjuntas, tamaños, enlaces y diff --check verificados.

Comandos: `dotnet test App/backend/tests/Marketplace.BLL.Tests --configuration
Release --no-restore -m:1`; reproducción/uso en `Doc/evaluation/business-ranking-final/packages/README.md`.
Helper local de preparación/evidencia: `.tools/prepare-review-packages.py`.

El vocabulario es sintético y solo prepara entrenamiento/calibración; todavía
no prueba independencia semántica de la colección final. El digest debe fijarse
fuera del paquete; no acredita identidad humana. Collection reextrae usando la
versión del generador instalada, no promete compatibilidad binaria universal.
No se ejecutó el oráculo ni se midieron rankings en los paquetes; se reutilizó
solo el motor para filtrar el conjunto completo sin devolver su orden al evaluador.
No cambiaron API/DAL/frontend: no se repitieron HTTP/PostGIS/Playwright ni las
simulaciones completas. Sin migraciones, commit, push o despliegue.
