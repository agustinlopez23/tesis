# Tareas

- [x] Definir borrador, copia de evidencia, integridad y requisitos aún pendientes.
- [x] TDD 16/17: siete casos nuevos y ampliación del fixture integral fallan por stub.
- [x] Implementar captura acotada, originales sin reconstruir, calibración sobre copia,
  manifiesto draft y verificación por pin externo; siete pruebas rápidas verdes.
- [x] CLI: registrar rechazo inicial de --prepare-freeze y habilitar comandos estrictos.
- [x] Verificar suite completa 80/80 y revisar captura de metadatos intermedios.
- [x] TDD adicional: reproducir omisión de Directory.Build.props intermedio, corregir;
  ejecutar suite final 81/81 incluyendo calibración completa y verificación de trazas.
- [x] Preparar borrador real, guardar referencia externa, comprobar hashes/pending y
  rechazo de sobrescritura/pin incorrecto/opciones huérfanas. Documentar continuidad.

Comando: `dotnet test App/backend/tests/Marketplace.BLL.Tests --configuration
Release --no-restore -m:1`. CLI: `Doc/evaluation/business-ranking-final/freeze.md`.

Fuera de alcance: autoría/fechas humanas, cierre metodológico, equivalencia demostrada
de fuentes/build, congelamiento efectivo, materiales reservados y evaluación final.
