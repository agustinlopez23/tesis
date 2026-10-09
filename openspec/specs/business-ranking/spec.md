# Ranking experimental de negocios

## Requisitos

1. El motor DEBE filtrar elegibilidad antes de ordenar y devolver una tarjeta
   por negocio. Duplicados y valores inválidos DEBEN fallar explícitamente.
   Escenario: un suspendido, borrador, irrelevante, futuro o fuera del radio
   nunca se beneficia de deuda de exposición ni novedad.
   Historia desconocida (PublishedAt=null) DEBE seguir elegible sin bonificación
   de novedad; no equivale a fecha futura ni publicación reciente.
2. El motor DEBE exponer los componentes y versión, usar proximidad separada
   del radio y prescindir de IA y plan. Sin ubicación DEBE usar mérito textual.
3. La compensación DEBE usar solo historia del mismo contexto dentro de 30 días.
   Escenario: al cumplir 30 días la contribución expira; otra zona no hereda deuda.
4. La exposición esperada DEBE conservar el presupuesto del top 10, estar entre
   0 y 1 por candidato y cubrir también candidatos no mostrados.
5. La misma entrada, configuración, historial, secuencia y semilla DEBEN generar
   idéntico orden. Invertir el orden de entrada no DEBE modificar el resultado.
6. La evaluación DEBE separar los juicios del motor, contar ceros y consultas
   vacías, calcular el ideal sobre todos los elegibles y exportar trazas.
7. Cada variante DEBE recibir el mismo dataset y su propio historial. El runner
   DEBE exportar versión/configuración/semillas/hashes y métricas por estrato.
8. El piloto DEBE identificarse como sintético y no habilitar cambios públicos.
9. La suite de ablaciones DEBE separar las cuatro combinaciones de novedad y
   rotación con compensación, más proximidad. Quitar novedad devuelve su peso
   a mérito; quitar rotación ordena por puntaje exacto e id sin usar secuencia.
10. `--suite pilot` (por defecto) DEBE conservar los artefactos originales;
    `--suite ablation` DEBE exportar cinco variantes con schema propio e inputs
    idénticos. Suites desconocidas DEBEN rechazarse antes de escribir salida.
11. El replay humano DEBE conservar todas las consultas cronológicas con historia
    independiente por política. nDCG DEBE usar solo juicios de la muestra completa,
    nunca completar con oráculo ni cero. DEBE distinguir no seleccionada, faltante,
    vacía e ideal cero, y exportar denominadores por semilla/ámbito.
12. Las métricas humanas DEBEN incluir global, superficies y superficies/zonas.
    Gini de superficie DEBE calcularse uniendo exposiciones. Las medias DEBEN dar
    igual peso a semillas, con diferencias pareadas y null ante semillas sin datos.
13. `--evaluate-review` DEBE verificar paquete, digest externo y hojas originales,
    aceptar solo calibración y pesos de la grilla con mérito/elegibilidad congelados.
    Escenario: hojas incompletas producen código 2 y pendientes, sin ranking;
    hojas completas producen comparación y hashes, sin decidir aceptación.
    Entrenamiento/reserva, opciones mezcladas y sobrescritura DEBEN rechazarse.
14. El comprobador DEBE separar criterios acordados y salvaguardas propuestas,
    incluir igualdad sin redondear, exigir cero si la referencia relativa es cero,
    y declarar inconclusive ante semillas/ámbitos/denominadores/juicios insuficientes.
    Calibración requiere 5 consultas por estrato, >=4 útiles y >=1 por semilla.
    Fallos locales o por semilla NO DEBEN ocultarse detrás de un promedio global.
15. `--calibrate-review` DEBE recorrer las 18 celdas fijas de calibración con
    baseline una vez por semilla e historias independientes. La selección DEBE
    exigir todas las celdas evaluables y cumplir criterios combinados; desempatar
    por menor desviación por mérito, mayor nDCG, menor Eq/N y sin rotación.
    DEBE exportar trazas/hashes/estados y declarar provisional la sugerencia.
    Sin aptas NO DEBE fabricar ganadora; sin juicios NO DEBE ejecutar rankings.
    No DEBE congelar automáticamente política ni abrir evaluación reservada.
16. El preparador DEBE copiar fuentes/protocolo y bytes originales verificados
    de las hojas, identificar binarios y ejecutar calibración sobre la copia.
    DEBE declarar draft, política no congelada y evaluación no autorizada, con
    registros humanos vacíos y pendientes explícitos. Sin juicios, política null.
17. El verificador del borrador DEBE exigir SHA-256 externo y comprobar inventario
    exacto de archivos/hashes. Cambios, faltantes, extras, enlaces, escapes de ruta
    y sobrescritura DEBEN rechazarse. Integridad NO DEBE equivaler a aprobación.
    Metadatos de build en directorios intermedios DEBEN incluirse en la huella de fuentes.
18. La comprobación de reconstrucción DEBE verificar primero el digest externo y
    compilar una copia de fuentes con el SDK fijado. DEBE comparar hashes exactos
    del runtime y salida de calibración, registrar condiciones/comandos y mantener
    intacto el borrador. Cambios o fallos DEBEN reportarse, sin aprobar por omisión.
    Un replay de pendientes NO DEBE presentarse como evaluación humana de rankings.
