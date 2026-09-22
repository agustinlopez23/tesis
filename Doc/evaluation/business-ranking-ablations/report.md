# Ablaciones sintéticas de ranking de negocios

Configuración `business-pilot-v1`. 200 negocios y 5000 consultas durante 60 días por semilla: 17, 29, 43.

Media ± desviación estándar muestral entre semillas; no es un intervalo de confianza. Sin umbral de aceptación: estos resultados no validan la hipótesis ni prueban ventas.

Exposición modelada en top 10, distinta de impresiones visibles reales. Gini y desviación por mérito: menor valor indica menor desigualdad según esa métrica. Cobertura y nDCG: mayor valor. La relevancia se juzga mediante un oráculo sintético independiente del puntaje.

## Global

| Variante | Gini | Top 10 negocios | Cobertura | nDCG@10 | Desviación mérito | Cobertura nuevos |
| --- | --- | --- | --- | --- | --- | --- |
| Relevancia + proximidad | 0.4806 ± 0.0205 | 0.1717 ± 0.0136 | 0.9896 ± 0.0052 | 0.9333 ± 0.0153 | 0.2603 ± 0.0162 | 0.9833 ± 0.0289 |
| Solo compensación | 0.4203 ± 0.0144 | 0.1613 ± 0.0115 | 1.0000 ± 0.0000 | 0.9013 ± 0.0170 | 0.2113 ± 0.0119 | 1.0000 ± 0.0000 |
| Compensación + novedad | 0.4131 ± 0.0142 | 0.1596 ± 0.0114 | 1.0000 ± 0.0000 | 0.8972 ± 0.0170 | 0.2075 ± 0.0117 | 1.0000 ± 0.0000 |
| Compensación + rotación | 0.4178 ± 0.0144 | 0.1606 ± 0.0119 | 1.0000 ± 0.0000 | 0.8990 ± 0.0170 | 0.2092 ± 0.0119 | 1.0000 ± 0.0000 |
| Equidad | 0.4106 ± 0.0141 | 0.1587 ± 0.0115 | 1.0000 ± 0.0000 | 0.8946 ± 0.0170 | 0.2054 ± 0.0114 | 1.0000 ± 0.0000 |

## Revista · Centro

| Variante | Gini | Top 10 negocios | Cobertura | nDCG@10 | Desviación mérito | Cobertura nuevos |
| --- | --- | --- | --- | --- | --- | --- |
| Relevancia + proximidad | 0.7810 ± 0.0029 | 0.3501 ± 0.0123 | 0.4886 ± 0.0124 | 1.0000 ± 0.0000 | 0.6099 ± 0.0035 | 0.3895 ± 0.0182 |
| Solo compensación | 0.6007 ± 0.0162 | 0.3212 ± 0.0161 | 0.9632 ± 0.0211 | 0.9312 ± 0.0013 | 0.4076 ± 0.0125 | 0.9833 ± 0.0289 |
| Compensación + novedad | 0.5866 ± 0.0158 | 0.3162 ± 0.0165 | 0.9755 ± 0.0132 | 0.9233 ± 0.0013 | 0.3974 ± 0.0129 | 0.9833 ± 0.0289 |
| Compensación + rotación | 0.5933 ± 0.0164 | 0.3185 ± 0.0174 | 0.9755 ± 0.0132 | 0.9260 ± 0.0015 | 0.4026 ± 0.0126 | 0.9833 ± 0.0289 |
| Equidad | 0.5799 ± 0.0157 | 0.3137 ± 0.0168 | 0.9807 ± 0.0080 | 0.9183 ± 0.0018 | 0.3931 ± 0.0125 | 0.9833 ± 0.0289 |

## Revista · Nueva Córdoba

| Variante | Gini | Top 10 negocios | Cobertura | nDCG@10 | Desviación mérito | Cobertura nuevos |
| --- | --- | --- | --- | --- | --- | --- |
| Relevancia + proximidad | 0.7797 ± 0.0158 | 0.3605 ± 0.0079 | 0.4929 ± 0.0100 | 1.0000 ± 0.0000 | 0.5972 ± 0.0258 | 0.4043 ± 0.0382 |
| Solo compensación | 0.6129 ± 0.0280 | 0.3393 ± 0.0105 | 0.9325 ± 0.0400 | 0.9375 ± 0.0112 | 0.4047 ± 0.0248 | 0.9130 ± 0.0778 |
| Compensación + novedad | 0.6005 ± 0.0284 | 0.3348 ± 0.0111 | 0.9485 ± 0.0349 | 0.9305 ± 0.0124 | 0.3956 ± 0.0238 | 0.9667 ± 0.0577 |
| Compensación + rotación | 0.6066 ± 0.0286 | 0.3378 ± 0.0115 | 0.9467 ± 0.0344 | 0.9330 ± 0.0116 | 0.4009 ± 0.0248 | 0.9315 ± 0.0758 |
| Equidad | 0.5940 ± 0.0289 | 0.3326 ± 0.0119 | 0.9592 ± 0.0264 | 0.9252 ± 0.0118 | 0.3915 ± 0.0240 | 0.9500 ± 0.0866 |

## Revista · Alberdi

| Variante | Gini | Top 10 negocios | Cobertura | nDCG@10 | Desviación mérito | Cobertura nuevos |
| --- | --- | --- | --- | --- | --- | --- |
| Relevancia + proximidad | 0.7857 ± 0.0099 | 0.3535 ± 0.0140 | 0.4093 ± 0.0333 | 1.0000 ± 0.0000 | 0.5815 ± 0.0037 | 0.4191 ± 0.0708 |
| Solo compensación | 0.6763 ± 0.0231 | 0.3413 ± 0.0130 | 0.8350 ± 0.0486 | 0.9496 ± 0.0088 | 0.4277 ± 0.0201 | 0.7790 ± 0.0954 |
| Compensación + novedad | 0.6646 ± 0.0237 | 0.3374 ± 0.0132 | 0.8767 ± 0.0352 | 0.9431 ± 0.0087 | 0.4176 ± 0.0200 | 0.8710 ± 0.0578 |
| Compensación + rotación | 0.6711 ± 0.0242 | 0.3398 ± 0.0126 | 0.8597 ± 0.0457 | 0.9465 ± 0.0086 | 0.4239 ± 0.0215 | 0.8359 ± 0.1004 |
| Equidad | 0.6589 ± 0.0244 | 0.3364 ± 0.0121 | 0.8918 ± 0.0287 | 0.9392 ± 0.0079 | 0.4136 ± 0.0213 | 0.8895 ± 0.0477 |

## Búsqueda de negocios · Centro

| Variante | Gini | Top 10 negocios | Cobertura | nDCG@10 | Desviación mérito | Cobertura nuevos |
| --- | --- | --- | --- | --- | --- | --- |
| Relevancia + proximidad | 0.4067 ± 0.0406 | 0.1502 ± 0.0109 | 0.9387 ± 0.0079 | 0.8691 ± 0.0407 | 0.2036 ± 0.0313 | 0.9658 ± 0.0297 |
| Solo compensación | 0.3855 ± 0.0222 | 0.1487 ± 0.0098 | 0.9913 ± 0.0060 | 0.8654 ± 0.0389 | 0.1922 ± 0.0146 | 1.0000 ± 0.0000 |
| Compensación + novedad | 0.3810 ± 0.0214 | 0.1482 ± 0.0099 | 0.9913 ± 0.0060 | 0.8640 ± 0.0386 | 0.1917 ± 0.0138 | 1.0000 ± 0.0000 |
| Compensación + rotación | 0.3838 ± 0.0219 | 0.1483 ± 0.0101 | 0.9913 ± 0.0060 | 0.8654 ± 0.0387 | 0.1910 ± 0.0145 | 1.0000 ± 0.0000 |
| Equidad | 0.3792 ± 0.0210 | 0.1477 ± 0.0101 | 0.9913 ± 0.0060 | 0.8638 ± 0.0389 | 0.1903 ± 0.0138 | 1.0000 ± 0.0000 |

## Búsqueda de negocios · Nueva Córdoba

| Variante | Gini | Top 10 negocios | Cobertura | nDCG@10 | Desviación mérito | Cobertura nuevos |
| --- | --- | --- | --- | --- | --- | --- |
| Relevancia + proximidad | 0.4045 ± 0.0315 | 0.1524 ± 0.0048 | 0.9210 ± 0.0325 | 0.8618 ± 0.0401 | 0.1930 ± 0.0179 | 0.8807 ± 0.1054 |
| Solo compensación | 0.3837 ± 0.0211 | 0.1499 ± 0.0065 | 0.9892 ± 0.0054 | 0.8585 ± 0.0391 | 0.1821 ± 0.0116 | 0.9833 ± 0.0289 |
| Compensación + novedad | 0.3794 ± 0.0209 | 0.1489 ± 0.0065 | 0.9892 ± 0.0054 | 0.8577 ± 0.0391 | 0.1819 ± 0.0111 | 0.9833 ± 0.0289 |
| Compensación + rotación | 0.3823 ± 0.0212 | 0.1493 ± 0.0070 | 0.9892 ± 0.0054 | 0.8581 ± 0.0396 | 0.1814 ± 0.0114 | 0.9833 ± 0.0289 |
| Equidad | 0.3777 ± 0.0211 | 0.1479 ± 0.0067 | 0.9892 ± 0.0054 | 0.8568 ± 0.0397 | 0.1807 ± 0.0112 | 0.9833 ± 0.0289 |

## Búsqueda de negocios · Alberdi

| Variante | Gini | Top 10 negocios | Cobertura | nDCG@10 | Desviación mérito | Cobertura nuevos |
| --- | --- | --- | --- | --- | --- | --- |
| Relevancia + proximidad | 0.4504 ± 0.0277 | 0.1657 ± 0.0030 | 0.9186 ± 0.0355 | 0.8690 ± 0.0127 | 0.1749 ± 0.0084 | 0.9038 ± 0.0384 |
| Solo compensación | 0.4364 ± 0.0224 | 0.1647 ± 0.0032 | 0.9679 ± 0.0210 | 0.8659 ± 0.0104 | 0.1639 ± 0.0091 | 0.9815 ± 0.0321 |
| Compensación + novedad | 0.4332 ± 0.0222 | 0.1643 ± 0.0035 | 0.9679 ± 0.0210 | 0.8653 ± 0.0104 | 0.1628 ± 0.0086 | 1.0000 ± 0.0000 |
| Compensación + rotación | 0.4363 ± 0.0226 | 0.1649 ± 0.0028 | 0.9660 ± 0.0242 | 0.8654 ± 0.0101 | 0.1642 ± 0.0093 | 0.9815 ± 0.0321 |
| Equidad | 0.4330 ± 0.0224 | 0.1642 ± 0.0034 | 0.9660 ± 0.0242 | 0.8646 ± 0.0100 | 0.1628 ± 0.0085 | 0.9815 ± 0.0321 |

## Corridas individuales: global

| Semilla | Variante | Consultas | Vacías | Con juicio útil | Elegibles | Gini | nDCG |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 17 | Relevancia + proximidad | 5000 | 0 | 5000 | 192 | 0.5043 | 0.9458 |
| 17 | Solo compensación | 5000 | 0 | 5000 | 192 | 0.4365 | 0.9163 |
| 17 | Compensación + novedad | 5000 | 0 | 5000 | 192 | 0.4290 | 0.9124 |
| 17 | Compensación + rotación | 5000 | 0 | 5000 | 192 | 0.4339 | 0.9140 |
| 17 | Equidad | 5000 | 0 | 5000 | 192 | 0.4263 | 0.9095 |
| 29 | Relevancia + proximidad | 5000 | 0 | 5000 | 192 | 0.4671 | 0.9163 |
| 29 | Solo compensación | 5000 | 0 | 5000 | 192 | 0.4086 | 0.8828 |
| 29 | Compensación + novedad | 5000 | 0 | 5000 | 192 | 0.4016 | 0.8789 |
| 29 | Compensación + rotación | 5000 | 0 | 5000 | 192 | 0.4060 | 0.8805 |
| 29 | Equidad | 5000 | 0 | 5000 | 192 | 0.3991 | 0.8761 |
| 43 | Relevancia + proximidad | 5000 | 0 | 5000 | 192 | 0.4705 | 0.9379 |
| 43 | Solo compensación | 5000 | 0 | 5000 | 192 | 0.4160 | 0.9048 |
| 43 | Compensación + novedad | 5000 | 0 | 5000 | 192 | 0.4087 | 0.9004 |
| 43 | Compensación + rotación | 5000 | 0 | 5000 | 192 | 0.4135 | 0.9025 |
| 43 | Equidad | 5000 | 0 | 5000 | 192 | 0.4065 | 0.8981 |

## Lectura y límites

El agregado mezcla oportunidades de zonas y rubros: revisar también cada estrato. El historial del motor expira a 30 días; las métricas acumulan los 60 días de simulación. Nuevos: negocios publicados después del inicio que llegaron a ser elegibles. Las cuatro celdas de compensación separan novedad y rotación; Equidad incluye ambas. Sin novedad, su peso vuelve a mérito. Rotación incluye discretización y desempate. Actividad excluida.

No incluye productos, juicios humanos, tráfico real, medición HTTP ni causalidad comercial. El ranking público continúa sin cambios. Inputs, configuración, trazas y hashes acompañan este reporte. Cada traza conserva todos los candidatos elegibles con sus juicios y exposición esperada, más los componentes del top 10.
