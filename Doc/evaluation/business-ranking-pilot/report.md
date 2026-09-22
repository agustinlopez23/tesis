# Piloto sintético de ranking de negocios

Configuración `business-pilot-v1`. 200 negocios y 5000 consultas durante 60 días por semilla: 17, 29, 43.

Media ± desviación estándar muestral entre semillas; no es un intervalo de confianza. Sin umbral de aceptación: estos resultados no validan la hipótesis ni prueban ventas.

Exposición modelada en top 10, distinta de impresiones visibles reales. Gini y desviación por mérito: menor valor indica menor desigualdad según esa métrica. Cobertura y nDCG: mayor valor. La relevancia se juzga mediante un oráculo sintético independiente del puntaje.

## Global

| Variante | Gini | Top 10 negocios | Cobertura | nDCG@10 | Desviación mérito | Cobertura nuevos |
| --- | --- | --- | --- | --- | --- | --- |
| Relevancia | 0.5913 ± 0.0212 | 0.2614 ± 0.0201 | 0.9757 ± 0.0167 | 0.6529 ± 0.0146 | 0.4526 ± 0.0097 | 0.9833 ± 0.0289 |
| Relevancia + proximidad | 0.4806 ± 0.0205 | 0.1717 ± 0.0136 | 0.9896 ± 0.0052 | 0.9333 ± 0.0153 | 0.2603 ± 0.0162 | 0.9833 ± 0.0289 |
| Equidad | 0.4106 ± 0.0141 | 0.1587 ± 0.0115 | 1.0000 ± 0.0000 | 0.8946 ± 0.0170 | 0.2054 ± 0.0114 | 1.0000 ± 0.0000 |
| Equidad + actividad | 0.4194 ± 0.0143 | 0.1616 ± 0.0117 | 1.0000 ± 0.0000 | 0.8856 ± 0.0160 | 0.2114 ± 0.0098 | 1.0000 ± 0.0000 |

## Revista · Centro

| Variante | Gini | Top 10 negocios | Cobertura | nDCG@10 | Desviación mérito | Cobertura nuevos |
| --- | --- | --- | --- | --- | --- | --- |
| Relevancia | 0.8302 ± 0.0024 | 0.4113 ± 0.0060 | 0.2522 ± 0.0059 | 0.5491 ± 0.0219 | 0.7452 ± 0.0033 | 0.2035 ± 0.0061 |
| Relevancia + proximidad | 0.7810 ± 0.0029 | 0.3501 ± 0.0123 | 0.4886 ± 0.0124 | 1.0000 ± 0.0000 | 0.6099 ± 0.0035 | 0.3895 ± 0.0182 |
| Equidad | 0.5799 ± 0.0157 | 0.3137 ± 0.0168 | 0.9807 ± 0.0080 | 0.9183 ± 0.0018 | 0.3931 ± 0.0125 | 0.9833 ± 0.0289 |
| Equidad + actividad | 0.5803 ± 0.0227 | 0.3122 ± 0.0313 | 0.9702 ± 0.0132 | 0.9044 ± 0.0144 | 0.3838 ± 0.0230 | 0.9833 ± 0.0289 |

## Revista · Nueva Córdoba

| Variante | Gini | Top 10 negocios | Cobertura | nDCG@10 | Desviación mérito | Cobertura nuevos |
| --- | --- | --- | --- | --- | --- | --- |
| Relevancia | 0.8203 ± 0.0039 | 0.4061 ± 0.0021 | 0.2964 ± 0.0215 | 0.5723 ± 0.0275 | 0.7207 ± 0.0131 | 0.2276 ± 0.0203 |
| Relevancia + proximidad | 0.7797 ± 0.0158 | 0.3605 ± 0.0079 | 0.4929 ± 0.0100 | 1.0000 ± 0.0000 | 0.5972 ± 0.0258 | 0.4043 ± 0.0382 |
| Equidad | 0.5940 ± 0.0289 | 0.3326 ± 0.0119 | 0.9592 ± 0.0264 | 0.9252 ± 0.0118 | 0.3915 ± 0.0240 | 0.9500 ± 0.0866 |
| Equidad + actividad | 0.5990 ± 0.0371 | 0.3307 ± 0.0212 | 0.9484 ± 0.0263 | 0.9166 ± 0.0130 | 0.3958 ± 0.0236 | 0.9500 ± 0.0866 |

## Revista · Alberdi

| Variante | Gini | Top 10 negocios | Cobertura | nDCG@10 | Desviación mérito | Cobertura nuevos |
| --- | --- | --- | --- | --- | --- | --- |
| Relevancia | 0.8038 ± 0.0139 | 0.3940 ± 0.0216 | 0.3535 ± 0.0170 | 0.5777 ± 0.0405 | 0.6719 ± 0.0055 | 0.2878 ± 0.0827 |
| Relevancia + proximidad | 0.7857 ± 0.0099 | 0.3535 ± 0.0140 | 0.4093 ± 0.0333 | 1.0000 ± 0.0000 | 0.5815 ± 0.0037 | 0.4191 ± 0.0708 |
| Equidad | 0.6589 ± 0.0244 | 0.3364 ± 0.0121 | 0.8918 ± 0.0287 | 0.9392 ± 0.0079 | 0.4136 ± 0.0213 | 0.8895 ± 0.0477 |
| Equidad + actividad | 0.6556 ± 0.0338 | 0.3355 ± 0.0188 | 0.8847 ± 0.0496 | 0.9276 ± 0.0087 | 0.4064 ± 0.0312 | 0.8525 ± 0.0804 |

## Búsqueda de negocios · Centro

| Variante | Gini | Top 10 negocios | Cobertura | nDCG@10 | Desviación mérito | Cobertura nuevos |
| --- | --- | --- | --- | --- | --- | --- |
| Relevancia | 0.4251 ± 0.0471 | 0.1676 ± 0.0068 | 0.8862 ± 0.0529 | 0.7215 ± 0.0204 | 0.2569 ± 0.0316 | 0.8649 ± 0.0563 |
| Relevancia + proximidad | 0.4067 ± 0.0406 | 0.1502 ± 0.0109 | 0.9387 ± 0.0079 | 0.8691 ± 0.0407 | 0.2036 ± 0.0313 | 0.9658 ± 0.0297 |
| Equidad | 0.3792 ± 0.0210 | 0.1477 ± 0.0101 | 0.9913 ± 0.0060 | 0.8638 ± 0.0389 | 0.1903 ± 0.0138 | 1.0000 ± 0.0000 |
| Equidad + actividad | 0.3861 ± 0.0211 | 0.1482 ± 0.0111 | 0.9912 ± 0.0030 | 0.8579 ± 0.0374 | 0.1958 ± 0.0129 | 1.0000 ± 0.0000 |

## Búsqueda de negocios · Nueva Córdoba

| Variante | Gini | Top 10 negocios | Cobertura | nDCG@10 | Desviación mérito | Cobertura nuevos |
| --- | --- | --- | --- | --- | --- | --- |
| Relevancia | 0.4102 ± 0.0257 | 0.1671 ± 0.0042 | 0.9101 ± 0.0223 | 0.7469 ± 0.0068 | 0.2483 ± 0.0226 | 0.9297 ± 0.0304 |
| Relevancia + proximidad | 0.4045 ± 0.0315 | 0.1524 ± 0.0048 | 0.9210 ± 0.0325 | 0.8618 ± 0.0401 | 0.1930 ± 0.0179 | 0.8807 ± 0.1054 |
| Equidad | 0.3777 ± 0.0211 | 0.1479 ± 0.0067 | 0.9892 ± 0.0054 | 0.8568 ± 0.0397 | 0.1807 ± 0.0112 | 0.9833 ± 0.0289 |
| Equidad + actividad | 0.3839 ± 0.0228 | 0.1476 ± 0.0036 | 0.9892 ± 0.0054 | 0.8492 ± 0.0332 | 0.1874 ± 0.0119 | 0.9833 ± 0.0289 |

## Búsqueda de negocios · Alberdi

| Variante | Gini | Top 10 negocios | Cobertura | nDCG@10 | Desviación mérito | Cobertura nuevos |
| --- | --- | --- | --- | --- | --- | --- |
| Relevancia | 0.4282 ± 0.0331 | 0.1752 ± 0.0109 | 0.9580 ± 0.0218 | 0.7505 ± 0.0217 | 0.2268 ± 0.0047 | 0.9792 ± 0.0361 |
| Relevancia + proximidad | 0.4504 ± 0.0277 | 0.1657 ± 0.0030 | 0.9186 ± 0.0355 | 0.8690 ± 0.0127 | 0.1749 ± 0.0084 | 0.9038 ± 0.0384 |
| Equidad | 0.4330 ± 0.0224 | 0.1642 ± 0.0034 | 0.9660 ± 0.0242 | 0.8646 ± 0.0100 | 0.1628 ± 0.0085 | 0.9815 ± 0.0321 |
| Equidad + actividad | 0.4316 ± 0.0220 | 0.1631 ± 0.0014 | 0.9697 ± 0.0225 | 0.8585 ± 0.0081 | 0.1637 ± 0.0123 | 0.9630 ± 0.0642 |

## Corridas individuales: global

| Semilla | Variante | Consultas | Vacías | Con juicio útil | Elegibles | Gini | nDCG |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 17 | Relevancia | 5000 | 0 | 5000 | 192 | 0.6156 | 0.6689 |
| 17 | Relevancia + proximidad | 5000 | 0 | 5000 | 192 | 0.5043 | 0.9458 |
| 17 | Equidad | 5000 | 0 | 5000 | 192 | 0.4263 | 0.9095 |
| 17 | Equidad + actividad | 5000 | 0 | 5000 | 192 | 0.4355 | 0.9000 |
| 29 | Relevancia | 5000 | 0 | 5000 | 192 | 0.5764 | 0.6496 |
| 29 | Relevancia + proximidad | 5000 | 0 | 5000 | 192 | 0.4671 | 0.9163 |
| 29 | Equidad | 5000 | 0 | 5000 | 192 | 0.3991 | 0.8761 |
| 29 | Equidad + actividad | 5000 | 0 | 5000 | 192 | 0.4084 | 0.8683 |
| 43 | Relevancia | 5000 | 0 | 5000 | 192 | 0.5819 | 0.6402 |
| 43 | Relevancia + proximidad | 5000 | 0 | 5000 | 192 | 0.4705 | 0.9379 |
| 43 | Equidad | 5000 | 0 | 5000 | 192 | 0.4065 | 0.8981 |
| 43 | Equidad + actividad | 5000 | 0 | 5000 | 192 | 0.4142 | 0.8886 |

## Lectura y límites

El agregado mezcla oportunidades de zonas y rubros: revisar también cada estrato. El historial del motor expira a 30 días; las métricas acumulan los 60 días de simulación. Nuevos: negocios publicados después del inicio que llegaron a ser elegibles. La ablación de actividad es exógena y sintética, no una señal real aprobada.

No incluye productos, juicios humanos, tráfico real, medición HTTP ni causalidad comercial. El ranking público continúa sin cambios. Inputs, configuración, trazas y hashes acompañan este reporte. Cada traza conserva todos los candidatos elegibles con sus juicios y exposición esperada, más los componentes del top 10.
