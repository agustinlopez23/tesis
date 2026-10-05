# Capacidad acotada de la previsualización

Rechazar creaciones que excedan los límites operativos sin truncar candidatos ni
registrar exposición parcial. Acotar las lecturas históricas y las solicitudes
simultáneas por proceso; comprobar recuperación y carga sobre PostGIS temporal.

Incluye DAL, controles HTTP, pruebas y documentación. Depende del incremento
HTTP local terminado. No cambia política, UI, migraciones ni habilitación.
Éxito: frontera exacta aceptada, exceso rechazado con rollback; concurrencia sin
cola; lecturas independientes de creación y evidencia de carga reproducible.
