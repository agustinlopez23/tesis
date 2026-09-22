# Protocolo previo a las corridas · 20/09/2026

Diseño exploratorio propio, posterior al piloto; no es un experimento confirmatorio.
Se conserva `business-pilot-v1`, generador, semillas 17/29/43, 200 negocios,
5.000 consultas por semilla, ventana y métricas. No se calibran pesos.

| Variante | Fórmula | Rotación |
| --- | --- | --- |
| Proximity | M | No |
| EquityOnly | 0,90 M + 0,10 Eq | No |
| EquityNewcomer | 0,85 M + 0,10 Eq + 0,05 N | No |
| EquityRotation | 0,90 M + 0,10 Eq | Sí |
| Equity | 0,85 M + 0,10 Eq + 0,05 N | Sí |

Sin rotación: puntaje exacto descendente e id ascendente. Con rotación: mismos
intervalos y SHA-256 del piloto. Al quitar novedad, su peso vuelve a mérito.
Cada celda recibe todos los mismos candidatos y su propio historial desde cero.
Se comparan políticas completas: el historial divergente forma parte del efecto.
La comparación mide novedad frente a mérito y rotación incluyendo discretización;
no identifica por separado el efecto de discretizar y desempatar por hash.

Contrastes pareados por semilla y estrato: Only-Proximity (compensación),
Newcomer-Only y Equity-Rotation (novedad sin/con rotación),
Rotation-Only y Equity-Newcomer (rotación sin/con novedad).
Interacción: Equity-Rotation-Newcomer+Only. No hay inferencia causal comercial,
intervalos de confianza ni umbrales elegidos después de observar resultados.

`--suite pilot` conserva exactamente cuatro variantes y artefactos anteriores;
`--suite ablation` exporta las cinco celdas bajo schema propio. No hay paquetes
nuevos ni cambios DAL. Reversión: usar suite pilot; orden público intacto.

Fundamento revisado: [Equity of Attention](https://arxiv.org/abs/1805.01788)
estudia atención acumulada y calidad; [Fairness of Exposure](https://arxiv.org/abs/1802.07281)
relaciona exposición y utilidad. Nuestra heurística no hereda sus garantías.
