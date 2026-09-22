# Verificación · 21/09/2026

Incremento offline terminado. Protocolo fijado el 20/09 antes de ejecutar;
evidencia en `Doc/evaluation/business-ranking-ablations/`.

- TDD: al agregar contratos y pruebas, 7 fallos/21 aciertos. Seis fallos nuevos
  expusieron fórmulas, orden, edad y exportación aún no implementados; uno previo
  detectó que enumerar todo el enum rompía las cuatro variantes del piloto.
  Dos casos de control ya pasaban; no se cuentan como pruebas rojas.
- Tras implementar: 28/28. Se amplió la prueba existente de candidatos e historial
  a las tres variantes nuevas; resultado final **31/31**.
- Build Release del laboratorio: cero errores y advertencias, sin paquetes nuevos.
- Dos ejecuciones completas de cinco variantes: 75.000 rankings cada una,
  manifiestos y hashes calculados de todos los archivos idénticos.
- Reejecución completa del piloto: sus diez hashes coinciden con el 19/09.
- Resúmenes completos de las variantes comunes coinciden entre suites.
- Se leyeron las 75.000 trazas: mismo contexto, candidatos, juicios y objetivos
  de exposición por consulta en las cinco variantes.
- CLI rechaza suite desconocida antes de crear salida y rechaza sobrescritura.
- Verificador local: `.tools/verify-ranking-ablation.py`; calcula también
  contrastes pareados en los siete estratos a partir de los resúmenes.

Comandos: `dotnet test App/backend/tests/Marketplace.BLL.Tests --configuration
Release --no-restore -m:1`; reproducción en README de evidencia.
El primer arranque de MSBuild sin `-m:1` se interrumpió tras no producir salida;
las verificaciones se completaron con un solo nodo.

No cambiaron API, DAL ni frontend; no se repitieron HTTP/PostGIS ni frontend o
Playwright. Las 46 HTTP y 56 frontend documentadas antes son evidencia previa.
No hay nueva UI, migraciones, commit, push ni despliegue.
