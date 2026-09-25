# Decisiones de diseño y plan de validación

Versión 1.0 · 2026-09-07. Las siguientes decisiones concretan un **borrador revisable**, sin afirmar aprobación del tutor ni resultados de implementación. Los precios de planes y campañas quedan fuera de este documento.

## Coherencia con la primera entrega

- Se conservan los tres procesos relevados, el objetivo de digitalización y el aporte de ranking equitativo.
- Se mantiene la asistencia opcional de IA para borradores, porque figura en los objetivos específicos originales. Es opcional para el comerciante, pero una función incluida en este borrador del prototipo. Eliminarla requiere reconciliar objetivos y alcance.
- La revista inicial ordena negocios; la fórmula original de productos se conserva en su contexto y se propone una adaptación explícita para la revista.
- Solicitudes, ventas reportadas, planes y campañas concretan decisiones posteriores del usuario. Su alcance se mantiene acotado a habilitación manual, un negocio por solicitud y sin pagos del comprador.
- Presencia e Impulso son nombres propuestos, no el nombre definitivo del producto.

## Contratos propuestos para cerrar el diseño

| Decisión | Propuesta documentada |
| --- | --- |
| Piloto | Productos por unidad, sin talles, colores o fraccionamiento. Rubros concretos a elegir con el usuario. |
| Zona | Radio inicial de 3 km y opciones 1, 3 y 5 km, sujeto a evaluación de densidad. Selección manual disponible. No confundir radio con λ=1,5 km de la fórmula. |
| Consumidor | Explorar sin cuenta. Solicitar con nombre; teléfono opcional. Acceso privado mediante credencial, nunca solo referencia. |
| Identidad | Sesiones revocables y hash de credenciales gestionado por componente de autenticación; no inventar criptografía propia. |
| Publicidad | Ambos planes pueden contratar promociones; Impulso permite además productos y atribución de ventas. Validar comercialmente esta distribución antes de lanzamiento. |
| Plan vencido | Ocultar catálogo y bloquear solicitudes nuevas; conservar historial y permitir resolver solicitudes anteriores. |
| Campaña inelegible | Pausar entrega si destino, plan o negocio dejan de cumplir condiciones. Resolver compensación comercial fuera del prototipo. |
| IA | Timeout propuesto de 15 s; respuesta validada contra taxonomía y formulario manual siempre disponible. |
| Impresión visible | Propuesta: al menos 50 % de la tarjeta visible durante 1 s. Una impresión por candidato y sesión en una consulta; la repetición técnica no suma. Validar antes del experimento. |
| Atribución comercial | Vinculación explícita solicitud→venta. Para informes, cohorte por creación de solicitud y seguimiento de 30 días; evitar mezclar solicitudes recientes aún abiertas con cohortes maduras. |
| Devoluciones | Reportar unidades devueltas; no deducir reembolsos monetarios sin información registrada. Excluir ventas anuladas de los importes reportados. |

## Ranking: base conservada y adaptación propuesta

En productos se conserva la fórmula inicial de la entrega: `S = 0,40 Rel + 0,30 Prox + 0,10 Disp + 0,20 Eq`. Los productos sin stock se excluyen de solicitudes y de esta búsqueda. Proximidad: `Prox = exp(-d/1,5)` con d en kilómetros. Disponibilidad propuesta: 1 por encima del umbral de stock bajo y 0,5 cuando hay stock bajo positivo.

Para negocios se propone evaluar `S = 0,40 Rel + 0,30 Prox + 0,10 Act + 0,20 Eq`. Cuando no hay texto, Rel vale 1 entre negocios elegibles por rubro/zona, de modo que no se inventa una coincidencia textual. Los factores restantes distinguen posiciones. Con texto, Rel considera nombre, descripción y rubro. La actividad reemplaza disponibilidad solo en este contexto; no se aplica como si un negocio gratuito tuviera productos sin stock.

Actividad propuesta: número de días con al menos una novedad o cambio sustantivo de perfil en los últimos 14 días, dividido por 7 y limitado a 1. Se cuenta como máximo un día de actividad por negocio y día UTC. Las reediciones equivalentes no crean eventos. Esta definición es una hipótesis evaluable y puede cambiar con la versión del algoritmo.

Para Eq se propone contrastar exposición histórica observada con exposición esperada por mérito dentro de 30 días. En cada consulta se calcula un mérito por negocio a partir de relevancia y proximidad, agregando los candidatos del negocio una sola vez; se normaliza entre negocios elegibles. La cuota esperada se aplica al presupuesto total de exposición orgánica efectivamente observado en esa consulta. Con esto no se exige repartir una exposición que nunca ocurrió ni se premia tener más productos por duplicar cuotas.

Propuesta de compensación: `Eq = max(0, min(1, (esperada - observada) / max(esperada, ε)))`, con ε positivo versionado y Eq=0 cuando no hay historial esperado. Los negocios nuevos reciben una bonificación separada de hasta 0,05, decreciente linealmente durante 14 días. Se conserva un máximo de dos productos del mismo negocio entre los primeros diez, una tarjeta por negocio en la revista y desempate por semilla dentro de una tolerancia de puntaje fijada por versión.

Estas fórmulas de adaptación y normalización **no están calibradas ni demuestran mejora por sí mismas**. La versión del algoritmo debe fijar ε, umbral de relevancia, tolerancia de empate y estrategia de agregación de mérito antes de ejecutar el experimento final. Se registran como parámetros pendientes, no se les asigna un valor oculto en las tablas.

## Validación académica

| Objeto | Diseño de evaluación | Evidencia a conservar |
| --- | --- | --- |
| Ranking de productos | Aproximadamente 200 negocios, 3000 productos y 5000 consultas sintéticas; relevancia sola, relevancia+cercanía y algoritmo completo sobre iguales datos y orden temporal. | Semillas, configuración, conjunto, juicios independientes de relevancia, candidatos, impresiones simuladas y métricas. |
| Revista de negocios | Contextos sin texto y con rubro, negocios nuevos y zonas de distinta densidad; comparar cercanía con variante completa y con/sin actividad. | Cobertura, Gini, concentración de los diez más expuestos y relevancia según juicios adecuados al contexto. |
| Calidad de ranking | nDCG@10 con juicios independientes del puntaje evaluado; métricas por segmento y variación entre corridas. | No calcular nDCG usando el mismo score como verdad de referencia. |
| IA | Misma muestra de imágenes y taxonomía, carga manual y asistida con orden contrabalanceado propuesto. | Aceptación sin cambios, campos editados, categoría correcta y tiempo por producto; registrar errores y límites de muestra. |
| Recorridos funcionales | Ejecutar normales y alternativas de CU-01 a CU-20. | Resultado observado, fecha, versión, captura y defecto si corresponde. |
| Seguridad e inventario | Casos cruzados entre negocios, reintentos y concurrencia sobre PostgreSQL/PostGIS real. | Evidencia de rechazo, atomicidad y ausencia de doble descuento. |

Antes de la evaluación final se acordarán umbrales de mejora de exposición y pérdida máxima admisible de nDCG. Sin esos umbrales fijados previamente se reportarán diferencias descriptivas, no cumplimiento retrospectivo de un objetivo inventado.

## Pendientes reales

Revisión del usuario y tutor; rubros piloto; parámetros restantes del ranking; política de retención para datos reales; proveedor de despliegue y dimensionamiento; ejecución de pruebas funcionales y experimentos. Ningún pendiente implica que se hayan medido resultados ni que el prototipo esté implementado.

Fuente: elaboración propia a partir de la primera entrega, la consigna y el PRD.
