# Verificación · 21/09/2026

Resultado: importador/validador CSV disponible y criterios principales registrados
como acordados. No se recogieron juicios ni se ejecutó evaluación final.

- TDD núcleo: cuatro pruebas rojas con NotImplementedException; después verdes.
- TDD archivos: nueve rojas, mientras las cuatro del núcleo pasaban; después 44/44
  en la suite completa, incluidos los 31 casos anteriores del laboratorio.
- TDD pendientes: una roja por lista de tareas vacía; después **45/45**.
- Compilación Release durante las pruebas sin advertencias ni dependencias nuevas.
- CLI real con fixtures explícitos de software: 0 completo, 2 plantillas vacías,
  1 ante duplicados, mezcla con opciones del piloto y sobrescritura. Inputs
  inválidos no crearon directorio; vacíos no exportaron juicios finales.
- Los cuatro SHA-256 del reporte se cotejaron contra bytes originales.
- JSON del protocolo, semillas disjuntas, muestras y enlaces: verificados.
- `git diff --check`: sin errores, con advertencias CRLF ya existentes.

Comando: `dotnet test App/backend/tests/Marketplace.BLL.Tests --configuration
Release --no-restore -m:1`. Helpers locales: `.tools/verify-human-review-cli.py`
y `.tools/check-ranking-protocol.py`. Fixtures bajo `.tools/human-review-cli-2026-09-21/`
son exclusivamente prueba de software, no evidencia humana o de utilidad.

Cobertura del importador = pares declarados en candidates.csv, no certificación
de completitud del dataset original. Tampoco acredita identidades humanas o
autenticidad de las fichas. Falta extracción/manifiesto congelado, preparación de
paquetes, evaluación con juicios y métricas por superficie. No cambiaron API,
DAL, frontend ni ranking del piloto; no se repitieron HTTP/PostGIS/Playwright ni
las simulaciones completas. Sin commit, push, migraciones o despliegue.
