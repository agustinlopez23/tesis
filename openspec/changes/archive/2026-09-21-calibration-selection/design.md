# Diseño previo

CalibrationCriteria valida tres semillas 101/103/107, nueve ámbitos y muestra
de 30 consultas (5 por estrato, >=4 útiles y >=1 útil por semilla/estrato).
Exige juicios completos, denominadores coherentes, exposición/nuevos elegibles
y métricas finitas. Datos insuficientes predominan sobre cualquier fallo de límite.
Comparar medias con peso igual por semilla, sin redondeo ni epsilon añadido.
Límites relativos: candidata <= referencia*(1-reducción); referencia cero exige cero.
Exportar cada comprobación con ámbito, valores, límite, regla y procedencia.

Estado principal independiente del estado de salvaguardas propuestas; estado de
diseño combinado para selección provisional. No presentarlo como acuerdo humano.
La selección solo ocurre con las 18 políticas, evaluables, y una o más aptas;
orden mérito global ascendente, nDCG descendente, Eq/N ascendentes, rotación falsa.
Si faltan datos en alguna candidata, no elegir entre un subconjunto sesgado.

CLI --calibrate-review PAQUETE --package-sha256 HASH --output VACÍO.
Verificar paquete una vez, generar solo calibración y ejecutar baseline una vez
por semilla; luego 18 historias independientes sobre iguales consultas/juicios.
Exportar configuración por celda, corridas, diferencias, criterios, sugerencia
provisional, trazas comprimidas con policyId y hashes. No usar oráculo.
Faltantes => código 2, sin métricas/selección. Inválido =>1. Barrido terminado =>0,
aunque ninguna política cumpla. Cero no significa aprobación experimental.

Unidades encadenables <400 líneas: criterios y sus pruebas; grilla/selección y
pruebas; exportador/CLI y pruebas; documentación. Sin dependencias nuevas.
