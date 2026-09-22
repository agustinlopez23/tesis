# Ranking experimental de negocios

## Requisitos

1. El motor DEBE filtrar elegibilidad antes de ordenar y devolver una tarjeta
   por negocio. Duplicados y valores inválidos DEBEN fallar explícitamente.
   Escenario: un suspendido, borrador, irrelevante, futuro o fuera del radio
   nunca se beneficia de deuda de exposición ni novedad.
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
