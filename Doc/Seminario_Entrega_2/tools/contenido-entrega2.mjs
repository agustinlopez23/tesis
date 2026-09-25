// Contenido académico de la Entrega 2 (metodología Scrum).
// Fuentes: directivas_entrega_2.md, "Definición del prototipo tecnológico. Entrega 2-3.pdf"
// y la Entrega 1 corregida (Diseño Metodológico, Relevamiento y Procesos de Negocios).
// Los nombres de los procesos diagnosticados se conservan tal como fueron relevados.

export const diagnostico = [
  {
    proceso: 'incorporación y publicación de productos',
    filas: [
      {
        problema:
          'La mayor parte de la mercadería que ingresa al comercio nunca llega a publicarse en un canal digital.',
        causas: [
          'Publicar exige fotografiar el producto, redactar un título y una descripción y asignarle una categoría, tareas que compiten con la atención del mostrador, la reposición y la administración, concentradas en una sola persona.',
          'El dueño-administrador redacta cada publicación desde cero, porque la lista de precios en papel o planilla solo registra denominación y precio, sin descripción ni categoría reutilizables.',
        ],
      },
      {
        problema:
          'Las publicaciones que sí se realizan carecen de estructura de catálogo: no indican categoría ni disponibilidad, y el precio queda fijado en el momento de publicar.',
        causas: [
          'Los canales utilizados, redes sociales y estados de mensajería, organizan el contenido por fecha de publicación y no admiten atributos de producto.',
          'La decisión de qué publicar se toma producto por producto, sin un criterio de clasificación previo que ordene la oferta.',
        ],
      },
    ],
  },
  {
    proceso: 'actualización de precios y disponibilidad',
    filas: [
      {
        problema:
          'Los precios que el consumidor ve publicados difieren de los vigentes en el comercio.',
        causas: [
          'La corrección se aplica únicamente sobre la lista de precios interna, mientras que cada publicación anterior conserva el precio con el que fue creada y debería editarse de a una.',
          'El cambio se comunica de manera reactiva, solo cuando un cliente pregunta, de modo que las publicaciones ya emitidas quedan sin revisar.',
        ],
      },
      {
        problema:
          'Se ofrecen productos sin existencias y permanece sin difundir la mercadería que sí está disponible.',
        causas: [
          'La disponibilidad se verifica por control visual de góndola o depósito en el momento de la consulta, y ese dato no queda asentado en ningún soporte.',
          'El empleado de mostrador comunica los faltantes verbalmente al dueño-administrador, sin un registro que permita reflejarlos en los canales de publicación.',
        ],
      },
    ],
  },
  {
    proceso: 'atención de consultas y venta',
    filas: [
      {
        problema:
          'Una parte de las consultas recibidas se responde con demora o queda sin responder.',
        causas: [
          'Cada consulta obliga a interrumpir la atención presencial para verificar precio y existencias antes de poder contestar.',
          'Las consultas llegan por canales dispersos, mostrador y mensajería, sin una cola común que permita ordenarlas y retomarlas.',
        ],
      },
      {
        problema:
          'El comercio no puede establecer cuántas consultas terminaron en venta ni qué productos las originaron.',
        causas: [
          'La venta se asienta en el cuaderno de ventas sin referencia a la consulta que la motivó.',
          'Las consultas no concretadas no dejan registro, de modo que la demanda insatisfecha se pierde.',
        ],
      },
      {
        problema:
          'El consumidor de la zona no encuentra la oferta del comercio cuando busca un producto.',
        causas: [
          'La información está dispersa en publicaciones sueltas que no admiten búsqueda por producto ni por cercanía.',
          'La visibilidad de cada publicación depende de la red de contactos del comercio y de la publicidad que pueda costear, y no de lo que efectivamente vende y tiene en stock.',
        ],
      },
    ],
  },
];

export const propuesta = [
  'Se propone el desarrollo de un sistema web de marketplace hiperlocal que centralice la oferta del comercio de proximidad en un catálogo estructurado y la ponga a disposición del consumidor de la zona mediante búsqueda por cercanía. El comerciante cargará cada producto a partir de una fotografía y un conjunto mínimo de datos, sobre los cuales el sistema propondrá nombre, descripción y categoría con asistencia de inteligencia artificial generativa, quedando la publicación sujeta a su revisión. El precio y la disponibilidad se mantendrán en un único lugar, de modo que su corrección se refleje de inmediato en todo lo publicado y no requiera editar cada pieza por separado.',
  'Del lado del consumidor, el sistema presentará los negocios y productos de su zona ordenados por un algoritmo que pondera relevancia, proximidad, disponibilidad y exposición histórica, con el fin de que la visibilidad no dependa de la capacidad de pago del comercio. Cada consulta podrá formalizarse como una solicitud con referencia única que el comerciante recibirá y atenderá por mensajería, y que al concretarse quedará registrada como venta con el descuento automático del inventario. De este modo el comercio dispondrá de una traza entre la consulta recibida, el producto involucrado y la venta efectivamente reportada, información que hoy no conserva, y podrá consultar métricas de visitas, contactos y ventas por período.',
];

export const objetivoPrototipo =
  'Desarrollar un prototipo web de marketplace hiperlocal que permita a los comercios de proximidad de la ciudad de Córdoba publicar y mantener actualizada su oferta con asistencia de inteligencia artificial generativa, y a los consumidores descubrirla por cercanía mediante un ranking de exposición equitativa.';

export const limites =
  'Desde que el responsable del negocio registra su comercio y prepara su oferta, hasta que la solicitud de un consumidor se registra como venta y se descuenta del inventario. La conversación, el acuerdo comercial, el pago y la entrega ocurren fuera del sistema.';

export const alcances = [
  'Registro y acceso de cuentas con roles de consumidor, responsable del negocio y administrador de plataforma.',
  'Gestión del perfil, el rubro y la ubicación del negocio.',
  'Gestión de novedades y promociones del negocio.',
  'Gestión del catálogo, los precios y la disponibilidad de productos.',
  'Asistencia de inteligencia artificial generativa para la creación de productos.',
  'Registro de movimientos de inventario con motivo y responsable.',
  'Descubrimiento de negocios de la zona mediante ranking hiperlocal.',
  'Búsqueda de productos cercanos por texto, categoría y zona.',
  'Consulta de la oferta y contacto con el negocio por mensajería.',
  'Creación y seguimiento de solicitudes de productos.',
  'Registro de ventas, corrección de ventas y devoluciones físicas.',
  'Habilitación de planes de suscripción y de campañas publicitarias identificadas.',
  'Consulta de métricas de visitas, contactos y ventas reportadas.',
  'Configuración y evaluación reproducible del algoritmo de ranking.',
];

export const requerimientosFuncionales = [
  ['RF-01', 'Registrar cuentas, iniciar y cerrar sesión y rechazar el acceso a cuentas suspendidas, comprobando los permisos en el servidor.'],
  ['RF-02', 'Crear, editar y archivar el perfil del negocio con nombre, descripción, rubro, ubicación y número de contacto, validando los datos antes de publicarlo.'],
  ['RF-03', 'Crear, editar, publicar y archivar novedades o promociones propias con sus condiciones y vigencia.'],
  ['RF-04', 'Crear, editar, publicar y archivar productos con nombre, descripción, categoría, imagen, precio, moneda y disponibilidad, preservando los valores históricos de solicitudes y ventas.'],
  ['RF-05', 'Proponer nombre, descripción y categoría de un producto a partir de una imagen y datos mínimos, permitiendo editarlos o descartarlos, sin publicar de forma automática y admitiendo la carga manual ante fallos.'],
  ['RF-06', 'Registrar entradas y ajustes de stock por producto con cantidad, motivo y responsable, manteniendo el historial y rechazando saldos negativos y operaciones duplicadas.'],
  ['RF-07', 'Mostrar al ingresar un listado de negocios de la zona ordenado por el ranking hiperlocal, con ubicación manual o del dispositivo, filtros de rubro y búsqueda textual opcional.'],
  ['RF-08', 'Buscar productos por texto, categoría y zona, ordenando los resultados con la fórmula de ranking y sus límites de exposición por negocio.'],
  ['RF-09', 'Consultar el perfil, el catálogo o una promoción vigente y abrir la mensajería con el contexto del negocio, registrando la intención de contacto sin considerarla venta.'],
  ['RF-10', 'Crear una solicitud de productos de un único negocio con referencia única, copia de cantidades y precios y resumen para mensajería, sin reservar stock y sin duplicarla al reintentar.'],
  ['RF-11', 'Permitir consultar el resumen y el estado de una solicitud mediante acceso privado autorizado, sin habilitar el acceso por la referencia pública aislada.'],
  ['RF-12', 'Listar y localizar las solicitudes propias por referencia, confirmar la recepción del contacto, pasarlas a atención o cancelarlas, preservando su contenido original.'],
  ['RF-13', 'Registrar una venta vinculada a una solicitud o presencial, conservar los artículos finales y descontar el stock de forma atómica, impidiendo conversiones duplicadas y saldos negativos.'],
  ['RF-14', 'Anular una venta con motivo y registro de auditoría sin reponer stock de forma automática, y registrar devoluciones físicas asociadas a las líneas vendidas sin exceder las cantidades originales.'],
  ['RF-15', 'Mostrar las ventajas de los planes Presencia e Impulso, permitir al administrador habilitar períodos de suscripción no superpuestos y aplicar las capacidades según su vigencia.'],
  ['RF-16', 'Crear y consultar campañas publicitarias sobre una promoción o un producto propio con zona y fechas, enviarlas a revisión y pausarlas, sin que la suscripción compre posición orgánica.'],
  ['RF-17', 'Aprobar o rechazar campañas, registrar la confirmación manual del pago y habilitar su entrega solo con fechas, zona, negocio y destino válidos, identificándola como patrocinada.'],
  ['RF-18', 'Mostrar métricas por período según las capacidades del plan, separando la exposición orgánica de la paga.'],
  ['RF-19', 'Gestionar categorías y suspender o rehabilitar cuentas, negocios y publicaciones con motivo y auditoría, preservando el historial comercial.'],
  ['RF-20', 'Versionar los parámetros del ranking y ejecutar una evaluación reproducible con datos sintéticos, exportando las métricas sin alterar la evidencia de producción.'],
  ['RF-21', 'Registrar los candidatos elegibles, la versión del ranking y las impresiones visibles deduplicadas, conservando la exposición orgánica y la patrocinada en registros separados.'],
  ['RF-22', 'Registrar la actividad sustantiva del negocio con fecha y deduplicación, aplicando su aporte acotado al ranking según la configuración vigente.'],
  ['RF-23', 'Registrar los cambios sensibles de estado, plan, moderación y venta con actor, instante y motivo, sin reemplazar el historial comercial.'],
];

export const requerimientosNoFuncionales = [
  ['RNF-01', 'Autorización', 'Toda operación privada comprueba identidad, rol, propiedad del recurso y capacidad del plan en el servidor.'],
  ['RNF-02', 'Integridad y concurrencia', 'La venta, sus líneas, los movimientos de stock y el saldo se confirman en una única transacción; el sistema nunca admite stock negativo ni doble conversión.'],
  ['RNF-03', 'Idempotencia', 'Repetir una misma operación de solicitud, venta o ajuste con idéntica clave conserva un único efecto.'],
  ['RNF-04', 'Usabilidad y accesibilidad', 'Los recorridos principales son utilizables a 360 y 1280 píxeles de ancho, con teclado, etiquetas asociadas y mensajes de error vinculados a su campo.'],
  ['RNF-05', 'Tiempo de respuesta', 'Se propone como objetivo un percentil 95 inferior a un segundo en la interfaz de descubrimiento con 200 negocios y 3000 productos, sujeto a validación sobre la infraestructura definitiva.'],
  ['RNF-06', 'Tolerancia a fallos externos', 'La geolocalización denegada admite selección manual de zona, el fallo de la mensajería conserva la solicitud y el fallo del proveedor de inteligencia artificial habilita la carga manual.'],
  ['RNF-07', 'Privacidad', 'La referencia pública de una solicitud no autoriza su acceso; el sistema almacena el hash del token privado y excluye secretos, mensajes completos y coordenadas de los registros generales.'],
  ['RNF-08', 'Reproducibilidad', 'El mismo conjunto de datos, secuencia temporal, versión de parámetros y semilla producen idéntico resultado de evaluación del ranking.'],
  ['RNF-09', 'Auditabilidad', 'Los cambios sensibles conservan actor, fecha en UTC, transición y motivo, y no pueden modificarse desde los paneles ordinarios.'],
  ['RNF-10', 'Compatibilidad', 'La interfaz es web y responsiva, sin aplicación nativa obligatoria, y se valida al menos sobre los motores Chromium y Firefox en las versiones registradas.'],
  ['RNF-11', 'Recuperación', 'El sistema conserva respaldo de los datos de demostración y un procedimiento de restauración verificable, sin comprometer disponibilidad continua.'],
  ['RNF-12', 'Mantenibilidad', 'Se mantiene la separación entre capa de presentación, lógica de negocio y acceso a datos con inversión de dependencias, de modo que las reglas de ranking y de ventas sean verificables sin proveedores externos.'],
  ['RNF-13', 'Seguridad de entradas', 'El sistema valida longitudes, cantidades, imágenes y destinos externos, y no acepta contenido ejecutable ni enlaces arbitrarios como destino de contacto.'],
];

// Product backlog. Puntos en escala de Fibonacci.
export const productBacklog = [
  ['HU-01', 'Registro y acceso a la plataforma', 'Alta', 8, '—'],
  ['HU-02', 'Perfil y ubicación del negocio', 'Alta', 8, 'HU-01'],
  ['HU-03', 'Novedades y promociones del negocio', 'Media', 5, 'HU-02'],
  ['HU-04', 'Catálogo y precios del negocio', 'Alta', 13, 'HU-02'],
  ['HU-05', 'Borrador de producto asistido por inteligencia artificial', 'Media', 13, 'HU-04'],
  ['HU-06', 'Movimientos de inventario', 'Alta', 8, 'HU-04'],
  ['HU-07', 'Descubrimiento de negocios de la zona', 'Alta', 13, 'HU-02, HU-21'],
  ['HU-08', 'Búsqueda de productos cercanos', 'Alta', 13, 'HU-04, HU-21'],
  ['HU-09', 'Consulta de la oferta y contacto con el negocio', 'Alta', 5, 'HU-02'],
  ['HU-10', 'Creación de una solicitud para mensajería', 'Alta', 13, 'HU-04'],
  ['HU-11', 'Consulta del estado de la solicitud', 'Media', 5, 'HU-10'],
  ['HU-12', 'Atención de las solicitudes recibidas', 'Media', 8, 'HU-10'],
  ['HU-13', 'Registro de venta y descuento de stock', 'Alta', 13, 'HU-06, HU-12'],
  ['HU-14', 'Corrección de venta y devolución física', 'Media', 8, 'HU-13'],
  ['HU-15', 'Consulta y habilitación de la suscripción', 'Baja', 5, 'HU-02'],
  ['HU-16', 'Gestión de campañas publicitarias', 'Baja', 8, 'HU-03, HU-15'],
  ['HU-17', 'Revisión y activación de la publicidad', 'Baja', 5, 'HU-16'],
  ['HU-18', 'Métricas del negocio', 'Media', 8, 'HU-09, HU-13'],
  ['HU-19', 'Administración de cuentas, contenido y categorías', 'Baja', 8, 'HU-01'],
  ['HU-20', 'Configuración y evaluación del prototipo', 'Baja', 13, 'HU-07, HU-08'],
  ['HU-21', 'Registro de la exposición orgánica y patrocinada', 'Alta', 8, '—'],
  ['HU-22', 'Registro de la actividad del negocio', 'Media', 5, 'HU-02'],
  ['HU-23', 'Auditoría de cambios sensibles', 'Media', 5, 'HU-01'],
];

export const historiasUsuario = [
  {
    id: 'HU-01', nombre: 'Registro y acceso a la plataforma', prioridad: 'Alta', puntos: 8,
    descripcion: 'Como persona interesada en usar la plataforma, quiero registrarme e iniciar sesión, para acceder a las funciones propias de mi rol.',
    criterios: [
      'Dado un correo electrónico ya registrado, cuando la persona intenta crear una cuenta con él, entonces el sistema rechaza el alta e informa el motivo sin revelar datos de la cuenta existente.',
      'Dados datos de acceso válidos, cuando la persona inicia sesión, entonces el sistema abre una sesión y habilita únicamente las funciones de los roles asignados a esa cuenta.',
      'Dada una cuenta suspendida, cuando se intenta iniciar sesión, entonces el sistema rechaza el acceso e indica que la cuenta no está habilitada.',
      'Dada una petición a una función privada sin sesión válida, cuando llega al servidor, entonces el sistema la rechaza aunque la interfaz hubiera ofrecido la acción.',
    ],
  },
  {
    id: 'HU-02', nombre: 'Perfil y ubicación del negocio', prioridad: 'Alta', puntos: 8,
    descripcion: 'Como responsable del negocio, quiero crear y mantener el perfil de mi comercio con su rubro y su ubicación, para que los consumidores de mi zona puedan encontrarlo e identificarlo.',
    criterios: [
      'Dado un perfil con nombre, rubro, ubicación y número de contacto válidos, cuando el responsable lo publica, entonces el negocio queda visible en el descubrimiento de su zona.',
      'Dado un perfil al que le falta la ubicación o el número de contacto, cuando el responsable intenta publicarlo, entonces el sistema impide la publicación e indica qué campo falta.',
      'Dado un número de contacto con formato inválido, cuando se guarda el perfil, entonces el sistema lo rechaza e informa el formato esperado.',
      'Dado un perfil publicado, cuando el responsable lo archiva, entonces el negocio deja de aparecer en el descubrimiento y conserva su información histórica.',
    ],
  },
  {
    id: 'HU-03', nombre: 'Novedades y promociones del negocio', prioridad: 'Media', puntos: 5,
    descripcion: 'Como responsable del negocio, quiero publicar novedades y promociones con sus condiciones y vigencia, para comunicar mi oferta puntual a los consumidores de la zona.',
    criterios: [
      'Dada una promoción con condiciones y fechas de vigencia válidas, cuando el responsable la publica, entonces queda visible únicamente dentro de ese período.',
      'Dada una fecha de fin anterior a la de inicio, cuando se intenta guardar la promoción, entonces el sistema rechaza la operación e informa la inconsistencia.',
      'Dada una promoción cuya vigencia venció, cuando un consumidor accede al perfil del negocio, entonces la promoción no se muestra y el historial la conserva.',
    ],
  },
  {
    id: 'HU-04', nombre: 'Catálogo y precios del negocio', prioridad: 'Alta', puntos: 13,
    descripcion: 'Como responsable del negocio, quiero cargar y mantener mis productos con su precio y disponibilidad en un único lugar, para que la información publicada esté siempre vigente sin tener que corregirla canal por canal.',
    criterios: [
      'Dado un producto con nombre, categoría, precio y moneda válidos, cuando el responsable lo publica, entonces queda disponible para la búsqueda de productos de su zona.',
      'Dado un precio negativo o con más de dos decimales, cuando se intenta guardar el producto, entonces el sistema rechaza la operación e informa el formato admitido.',
      'Dado un producto que ya fue solicitado o vendido, cuando el responsable modifica su precio, entonces las solicitudes y ventas anteriores conservan el precio con el que fueron registradas.',
      'Dado un producto publicado, cuando el responsable lo archiva, entonces deja de aparecer en la búsqueda y su historial de ventas permanece intacto.',
    ],
  },
  {
    id: 'HU-05', nombre: 'Borrador de producto asistido por inteligencia artificial', prioridad: 'Media', puntos: 13,
    descripcion: 'Como responsable del negocio, quiero que el sistema me proponga el nombre, la descripción y la categoría de un producto a partir de su fotografía, para reducir el tiempo que dedico a cargar cada artículo.',
    criterios: [
      'Dada una fotografía y los datos mínimos del producto, cuando el responsable solicita la asistencia, entonces el sistema presenta un borrador editable de nombre, descripción y categoría sin publicarlo.',
      'Dado un borrador propuesto, cuando el responsable lo descarta, entonces el producto no se crea y no queda rastro del borrador en el catálogo.',
      'Dado un fallo o una demora del proveedor de inteligencia artificial, cuando se solicita la asistencia, entonces el sistema informa la situación y habilita la carga manual del producto.',
      'Dado un borrador aceptado, cuando el responsable lo publica, entonces el producto queda registrado con los valores que el responsable confirmó, no con los propuestos originalmente.',
    ],
  },
  {
    id: 'HU-06', nombre: 'Movimientos de inventario', prioridad: 'Alta', puntos: 8,
    descripcion: 'Como responsable del negocio, quiero registrar las entradas y los ajustes de stock indicando el motivo, para que la disponibilidad publicada refleje lo que realmente tengo.',
    criterios: [
      'Dada una entrada de stock con cantidad y motivo, cuando el responsable la registra, entonces el saldo del producto aumenta y el movimiento queda en el historial con su responsable y su fecha.',
      'Dado un ajuste que dejaría el saldo por debajo de cero, cuando se intenta registrar, entonces el sistema rechaza la operación e informa el saldo disponible.',
      'Dado un mismo movimiento enviado más de una vez con idéntica clave de operación, cuando el sistema lo procesa, entonces el saldo se modifica una sola vez.',
      'Dado un producto cuyo saldo queda por debajo del umbral configurado, cuando se consulta el catálogo, entonces el producto se muestra con disponibilidad baja.',
    ],
  },
  {
    id: 'HU-07', nombre: 'Descubrimiento de negocios de la zona', prioridad: 'Alta', puntos: 13,
    descripcion: 'Como consumidor, quiero ver al ingresar los negocios de mi zona ordenados por cercanía y relevancia, para descubrir comercios próximos que hoy no conozco.',
    criterios: [
      'Dada una ubicación tomada del dispositivo, cuando el consumidor ingresa, entonces el sistema muestra los negocios de esa zona ordenados por el ranking hiperlocal vigente.',
      'Dado un permiso de geolocalización denegado, cuando el consumidor ingresa, entonces el sistema le permite seleccionar la zona manualmente y muestra el listado igualmente.',
      'Dado un conjunto de negocios de mérito equivalente, cuando se repite la misma consulta, entonces el orden rota de forma controlada y ningún negocio ocupa más de dos posiciones entre los diez primeros resultados.',
      'Dado un negocio con plan gratuito que cumple los criterios de zona y rubro, cuando se arma el listado, entonces el negocio es elegible y puede aparecer en él.',
    ],
  },
  {
    id: 'HU-08', nombre: 'Búsqueda de productos cercanos', prioridad: 'Alta', puntos: 13,
    descripcion: 'Como consumidor, quiero buscar un producto y ver en qué comercios cercanos está disponible, para resolver mi compra en el barrio sin recorrer local por local.',
    criterios: [
      'Dado un texto de búsqueda y una zona, cuando el consumidor busca, entonces el sistema devuelve los productos publicados de esa zona ordenados por la fórmula de ranking.',
      'Dado un texto con errores de tipeo, cuando el consumidor busca, entonces el sistema devuelve igualmente los productos cuyo nombre o descripción se aproximan al término ingresado.',
      'Dado un producto sin existencias, cuando aparece entre los resultados, entonces se muestra al final del listado o se excluye según la configuración vigente, y nunca como disponible.',
      'Dada una búsqueda sin coincidencias en la zona, cuando se ejecuta, entonces el sistema informa la ausencia de resultados y ofrece ampliar el radio.',
    ],
  },
  {
    id: 'HU-09', nombre: 'Consulta de la oferta y contacto con el negocio', prioridad: 'Alta', puntos: 5,
    descripcion: 'Como consumidor, quiero consultar el perfil y el catálogo de un negocio y escribirle por mensajería, para preguntar por un producto con el contexto ya cargado.',
    criterios: [
      'Dado un negocio publicado, cuando el consumidor abre su perfil, entonces el sistema muestra su información, su catálogo habilitado y sus promociones vigentes.',
      'Dado un negocio con número de contacto validado, cuando el consumidor pulsa contactar, entonces el sistema abre la mensajería con un mensaje que incluye el contexto del negocio o del producto.',
      'Dado un clic de contacto, cuando el sistema lo registra, entonces queda asentado como intención de contacto y no como mensaje recibido ni como venta.',
      'Dado un mismo clic de contacto repetido en la misma sesión, cuando se registra, entonces el sistema lo deduplica y no lo contabiliza dos veces.',
    ],
  },
  {
    id: 'HU-10', nombre: 'Creación de una solicitud para mensajería', prioridad: 'Alta', puntos: 13,
    descripcion: 'Como consumidor, quiero armar una solicitud con los productos que me interesan y enviarla por mensajería, para que el comercio reciba mi pedido ordenado y con precios claros.',
    criterios: [
      'Dados uno o más productos de un mismo negocio, cuando el consumidor confirma la solicitud, entonces el sistema la registra con una referencia única y genera el resumen para la mensajería.',
      'Dados productos de dos negocios distintos, cuando el consumidor intenta incluirlos en una misma solicitud, entonces el sistema lo impide e indica que la solicitud corresponde a un único negocio.',
      'Dada una solicitud creada, cuando el consumidor reintenta el envío con la misma clave de operación, entonces el sistema conserva una única solicitud y no la duplica.',
      'Dada una solicitud confirmada, cuando el sistema la registra, entonces conserva copia de las cantidades y los precios vigentes en ese momento y no reserva unidades del inventario.',
    ],
  },
  {
    id: 'HU-11', nombre: 'Consulta del estado de la solicitud', prioridad: 'Media', puntos: 5,
    descripcion: 'Como consumidor, quiero consultar el estado de la solicitud que envié, para saber si el comercio la recibió y en qué situación se encuentra.',
    criterios: [
      'Dado un enlace privado autorizado, cuando el consumidor lo abre, entonces el sistema muestra el resumen y el estado actual de esa solicitud.',
      'Dada únicamente la referencia pública de una solicitud, cuando se intenta acceder con ella, entonces el sistema deniega el acceso al detalle.',
      'Dado un token privado vencido o inválido, cuando se utiliza, entonces el sistema deniega el acceso sin revelar la existencia de la solicitud.',
    ],
  },
  {
    id: 'HU-12', nombre: 'Atención de las solicitudes recibidas', prioridad: 'Media', puntos: 8,
    descripcion: 'Como responsable del negocio, quiero ver y gestionar las solicitudes que recibí, para atenderlas en orden y no perder oportunidades de venta.',
    criterios: [
      'Dadas las solicitudes recibidas, cuando el responsable abre el panel, entonces el sistema las lista con su referencia, su fecha y su estado.',
      'Dada una referencia de solicitud, cuando el responsable la busca, entonces el sistema localiza la solicitud correspondiente a su negocio y solo a ese.',
      'Dada una solicitud pendiente, cuando el responsable confirma la recepción del contacto, entonces el sistema registra quién confirmó y cuándo, y pasa la solicitud a atención.',
      'Dada una solicitud cancelada, cuando se consulta posteriormente, entonces conserva su contenido original sin modificaciones.',
    ],
  },
  {
    id: 'HU-13', nombre: 'Registro de venta y descuento de stock', prioridad: 'Alta', puntos: 13,
    descripcion: 'Como responsable del negocio, quiero registrar la venta concretada y que el stock se descuente solo, para no tener que llevar el control por separado ni vender lo que ya no tengo.',
    criterios: [
      'Dada una solicitud en atención, cuando el responsable registra la venta, entonces el sistema la asienta, descuenta el stock de cada producto y confirma ambas operaciones de forma conjunta.',
      'Dada una última unidad disponible y dos registros de venta simultáneos, cuando ambos se procesan, entonces solo uno descuenta el stock y el otro es rechazado.',
      'Dada una solicitud ya convertida en venta, cuando se intenta convertirla nuevamente, entonces el sistema rechaza la operación e indica la venta existente.',
      'Dado un fallo en cualquier punto del registro, cuando la operación se interrumpe, entonces el sistema revierte la venta y el descuento de stock por completo.',
      'Dada una venta presencial sin solicitud previa, cuando el responsable la registra, entonces el sistema la asienta indicando su origen y descuenta el stock igualmente.',
    ],
  },
  {
    id: 'HU-14', nombre: 'Corrección de venta y devolución física', prioridad: 'Media', puntos: 8,
    descripcion: 'Como responsable del negocio, quiero anular una venta mal registrada y asentar las devoluciones físicas, para que mi historial y mi stock reflejen lo que realmente ocurrió.',
    criterios: [
      'Dada una venta registrada, cuando el responsable la anula indicando el motivo, entonces el sistema la marca como anulada, conserva sus líneas y deja registro de auditoría.',
      'Dada una venta anulada, cuando se completa la anulación, entonces el sistema no repone el stock de forma automática y exige un movimiento de inventario explícito.',
      'Dada una devolución física de mayor cantidad que la vendida, cuando se intenta registrar, entonces el sistema la rechaza e informa la cantidad máxima admitida.',
      'Dada una devolución física válida, cuando se registra, entonces queda asociada a la línea de venta correspondiente con su motivo y su responsable.',
    ],
  },
  {
    id: 'HU-15', nombre: 'Consulta y habilitación de la suscripción', prioridad: 'Baja', puntos: 5,
    descripcion: 'Como responsable del negocio, quiero conocer las ventajas de cada plan y que el administrador habilite el que contrato, para acceder a las funciones correspondientes durante su vigencia.',
    criterios: [
      'Dados los planes disponibles, cuando el responsable los consulta, entonces el sistema muestra las capacidades de Presencia e Impulso sin exhibir precios.',
      'Dado un período de suscripción que se superpone con otro vigente del mismo negocio, cuando el administrador intenta habilitarlo, entonces el sistema rechaza la operación.',
      'Dada una suscripción vigente, cuando el responsable utiliza una función del plan, entonces el sistema la habilita según las capacidades de ese plan.',
      'Dada una suscripción vencida, cuando se consulta el negocio, entonces las capacidades adicionales quedan deshabilitadas y el historial generado se conserva.',
    ],
  },
  {
    id: 'HU-16', nombre: 'Gestión de campañas publicitarias', prioridad: 'Baja', puntos: 8,
    descripcion: 'Como responsable del negocio, quiero crear campañas sobre una promoción o un producto propio indicando zona y fechas, para ampliar mi difusión sin alterar mi posición orgánica.',
    criterios: [
      'Dada una promoción o un producto propio elegible, cuando el responsable crea la campaña con zona y fechas válidas, entonces el sistema la registra y la envía a revisión.',
      'Dado un producto que no pertenece al negocio, cuando se intenta asociarlo a una campaña, entonces el sistema rechaza la operación.',
      'Dada una campaña activa, cuando el responsable la pausa, entonces deja de entregarse y conserva las métricas acumuladas.',
      'Dada una campaña contratada, cuando el sistema calcula el ranking orgánico, entonces la campaña no modifica el puntaje orgánico del negocio.',
    ],
  },
  {
    id: 'HU-17', nombre: 'Revisión y activación de la publicidad', prioridad: 'Baja', puntos: 5,
    descripcion: 'Como administrador de la plataforma, quiero revisar y activar las campañas, para que solo se entregue publicidad válida y claramente identificada.',
    criterios: [
      'Dada una campaña en revisión, cuando el administrador la aprueba y confirma el pago, entonces el sistema habilita su entrega dentro de las fechas y la zona definidas.',
      'Dada una campaña con zona, fechas o destino inválidos, cuando el administrador intenta activarla, entonces el sistema impide la activación e indica el dato observado.',
      'Dada una campaña activa, cuando un consumidor la visualiza, entonces el sistema la presenta identificada como patrocinada y registra su impresión separada de la orgánica.',
      'Dada una campaña rechazada, cuando se consulta posteriormente, entonces conserva el motivo del rechazo y su registro de auditoría.',
    ],
  },
  {
    id: 'HU-18', nombre: 'Métricas del negocio', prioridad: 'Media', puntos: 8,
    descripcion: 'Como responsable del negocio, quiero ver cuántas visitas, contactos y ventas tuve en un período, para saber si mi presencia en la plataforma me está sirviendo.',
    criterios: [
      'Dado un período seleccionado, cuando el responsable consulta las métricas, entonces el sistema muestra las visitas y los clics de contacto de ese período.',
      'Dado un negocio con plan Impulso, cuando consulta las métricas, entonces el sistema añade las solicitudes, las ventas reportadas y su atribución por producto o campaña.',
      'Dado un negocio con plan Presencia, cuando consulta las métricas, entonces el sistema no muestra las métricas exclusivas del plan Impulso.',
      'Dadas impresiones orgánicas y patrocinadas en el mismo período, cuando se presentan las métricas, entonces el sistema las informa por separado.',
    ],
  },
  {
    id: 'HU-19', nombre: 'Administración de cuentas, contenido y categorías', prioridad: 'Baja', puntos: 8,
    descripcion: 'Como administrador de la plataforma, quiero gestionar las categorías y suspender o rehabilitar cuentas, negocios y publicaciones, para mantener el contenido dentro de las reglas de uso.',
    criterios: [
      'Dada una cuenta o un negocio en infracción, cuando el administrador lo suspende indicando el motivo, entonces deja de estar visible y queda registro de auditoría con actor y fecha.',
      'Dado un negocio suspendido, cuando se lo rehabilita, entonces recupera su visibilidad conservando íntegro su historial comercial.',
      'Dada una categoría en uso, cuando el administrador intenta eliminarla, entonces el sistema lo impide e indica cuántos registros la utilizan.',
    ],
  },
  {
    id: 'HU-20', nombre: 'Configuración y evaluación del prototipo', prioridad: 'Baja', puntos: 13,
    descripcion: 'Como evaluador académico, quiero versionar los parámetros del ranking y ejecutar evaluaciones reproducibles, para medir la equidad de exposición y la relevancia del descubrimiento.',
    criterios: [
      'Dado un conjunto de parámetros, cuando el evaluador lo guarda, entonces el sistema lo registra con una versión inmutable e identificable.',
      'Dados el mismo conjunto de datos, la misma versión de parámetros y la misma semilla, cuando la evaluación se ejecuta dos veces, entonces ambas corridas producen idéntico orden y métricas equivalentes dentro de la tolerancia documentada.',
      'Dada una evaluación finalizada, cuando el evaluador exporta sus métricas, entonces el sistema entrega el resultado sin modificar los registros de producción.',
      'Dada una evaluación en curso, cuando se ejecuta, entonces opera sobre datos sintéticos aislados y no sobre la base de datos del prototipo en uso.',
    ],
  },
  {
    id: 'HU-21', nombre: 'Registro de la exposición orgánica y patrocinada', prioridad: 'Alta', puntos: 8,
    descripcion: 'Como administrador de la plataforma, quiero que el sistema registre qué negocios fueron mostrados y en qué posición, para poder medir la equidad de exposición del ranking.',
    criterios: [
      'Dada una consulta de descubrimiento, cuando el sistema arma el resultado, entonces registra los candidatos elegibles, la versión del ranking aplicada y la posición asignada a cada uno.',
      'Dada una tarjeta efectivamente visible para el consumidor, cuando se muestra, entonces el sistema registra la impresión ponderada por su posición.',
      'Dada una misma impresión detectada más de una vez en la misma sesión, cuando se registra, entonces el sistema la deduplica.',
      'Dadas impresiones orgánicas y patrocinadas, cuando se almacenan, entonces quedan en registros separados y no se mezclan en el cálculo de equidad.',
    ],
  },
  {
    id: 'HU-22', nombre: 'Registro de la actividad del negocio', prioridad: 'Media', puntos: 5,
    descripcion: 'Como administrador de la plataforma, quiero que el sistema registre la actividad sustantiva de cada negocio, para que el ranking pueda premiar a los comercios que mantienen su oferta al día.',
    criterios: [
      'Dada una actualización sustantiva del perfil, del catálogo o de una promoción, cuando el responsable la realiza, entonces el sistema la registra con su fecha y su tipo.',
      'Dada una misma acción repetida dentro de la ventana de deduplicación, cuando se registra, entonces el sistema la contabiliza una sola vez.',
      'Dado un negocio con actividad muy frecuente, cuando se calcula su aporte al ranking, entonces el sistema aplica el tope configurado y la ventaja no crece de forma ilimitada.',
    ],
  },
  {
    id: 'HU-23', nombre: 'Auditoría de cambios sensibles', prioridad: 'Media', puntos: 5,
    descripcion: 'Como administrador de la plataforma, quiero que los cambios sensibles queden auditados, para poder reconstruir qué ocurrió, quién lo hizo y por qué.',
    criterios: [
      'Dado un cambio de estado, de plan, de moderación o de venta, cuando se produce, entonces el sistema registra el actor, el instante en UTC, la transición y el motivo.',
      'Dado un registro de auditoría existente, cuando se intenta modificarlo desde los paneles ordinarios, entonces el sistema rechaza la operación.',
      'Dado un registro de auditoría, cuando se almacena, entonces no contiene contraseñas, tokens ni mensajes completos de los consumidores.',
    ],
  },
];

export const sprintBacklog = {
  encabezado:
    'Se planifica el primer sprint con una duración de catorce días corridos y una dedicación máxima de ocho horas diarias. Se seleccionan las tres historias de mayor prioridad que no dependen de otras pendientes y que habilitan el resto del backlog: el acceso a la plataforma, el perfil del negocio y el catálogo. El estado corresponde al momento de redacción de este entregable.',
  filas: [
    ['1', 'HU-01 Registro y acceso a la plataforma', 'T-01', 'Modelar las entidades de cuenta, rol y sesión.', 'Alta', '1 día', 'Hecho'],
    ['', '', 'T-02', 'Implementar el registro, el inicio y el cierre de sesión en la capa de lógica de negocio.', 'Alta', '2 días', 'Hecho'],
    ['', '', 'T-03', 'Exponer los servicios de autenticación y la comprobación de permisos en el servidor.', 'Alta', '1 día', 'En curso'],
    ['', '', 'T-04', 'Diseñar e implementar la pantalla de acceso.', 'Media', '1 día', 'Por hacer'],
    ['', '', 'T-05', 'Escribir las pruebas de reglas de acceso y de cuenta suspendida.', 'Alta', '1 día', 'Por hacer'],
    ['1', 'HU-02 Perfil y ubicación del negocio', 'T-06', 'Modelar el negocio y su rubro con la columna geográfica.', 'Alta', '1 día', 'Por hacer'],
    ['', '', 'T-07', 'Implementar el alta, la edición y el archivado del perfil con validación previa a la publicación.', 'Alta', '2 días', 'Por hacer'],
    ['', '', 'T-08', 'Diseñar e implementar la pantalla de perfil con selección de ubicación en el mapa.', 'Media', '1 día', 'Por hacer'],
    ['1', 'HU-04 Catálogo y precios del negocio', 'T-09', 'Modelar el producto, su categoría y su saldo de inventario.', 'Alta', '1 día', 'Por hacer'],
    ['', '', 'T-10', 'Implementar el alta, la edición, la publicación y el archivado de productos preservando los valores históricos.', 'Alta', '2 días', 'Por hacer'],
    ['', '', 'T-11', 'Diseñar e implementar la pantalla de catálogo.', 'Media', '1 día', 'Por hacer'],
  ],
  cierre:
    'La suma de las estimaciones asciende a catorce días, dentro del límite fijado para el sprint. Las historias HU-03, HU-05 a HU-14 y HU-21 a HU-23 quedan planificadas para los sprints siguientes, respetando el orden de dependencias registrado en el product backlog.',
};

export const estructuraDatosIntro = [
  'El prototipo se construye con un modelo de dominio orientado a objetos sobre una base de datos relacional, de modo que se presentan los dos diagramas que corresponden a esa combinación: el diagrama de clases, que expresa la estructura del dominio con sus atributos y relaciones, y el diagrama de entidad-relación, que expresa cómo esa estructura se normaliza en tablas con sus claves primarias y foráneas. No se incorpora un diagrama para base de datos no relacional porque el prototipo no utiliza un motor de esa naturaleza.',
  'El modelo comprende veintiséis entidades. Tres de ellas almacenan información geográfica mediante la extensión PostGIS, con el tipo geography(Point,4326): la ubicación del negocio, el centro de una consulta de descubrimiento y el centro de destino de una campaña publicitaria. Esa capacidad geoespacial es la que hace posible la búsqueda por cercanía y el factor de proximidad del ranking.',
  'El modelo contempla además datos no estructurados. Las fotografías de productos y publicaciones se conservan en un almacenamiento de objetos externo y la base de datos guarda únicamente su referencia. Los parámetros de cada versión del algoritmo de ranking se almacenan en un campo de tipo JSON, lo que permite versionar pesos y umbrales sin alterar el esquema relacional. Los textos libres de publicaciones, motivos de auditoría y motivos de movimiento de inventario se conservan como texto sin estructura interna.',
];

export const diccionarioIntro =
  'Los identificadores de entidades y campos del modelo están expresados en inglés, conforme a la convención de nomenclatura del código fuente. Por ese motivo se incorpora el diccionario de datos de las entidades del núcleo del negocio, con el nombre del campo, su longitud, su tipo de dato y su descripción en español. Los tipos de tamaño fijo se expresan en bytes y no en caracteres.';

export const diccionarioDatos = [
  {
    entidad: 'Business',
    descripcion: 'Negocio y su única ubicación',
    campos: [
      ['id', '16 bytes', 'uuid', 'Identificador interno del negocio.'],
      ['owner_id', '16 bytes', 'uuid', 'Cuenta responsable del negocio.'],
      ['category_id', '16 bytes', 'uuid', 'Rubro al que pertenece el negocio.'],
      ['name', '160 caracteres', 'varchar(160)', 'Nombre visible del comercio.'],
      ['description', '2000 caracteres', 'varchar(2000)', 'Descripción de la oferta del negocio.'],
      ['address', '250 caracteres', 'varchar(250)', 'Dirección textual del local.'],
      ['location', '1 punto, 2 coordenadas', 'geography(Point,4326)', 'Ubicación geográfica del local.'],
      ['whatsapp_number', '16 caracteres', 'varchar(16)', 'Número de contacto validado del negocio.'],
      ['status', '64 caracteres', 'varchar(64)', 'Estado del negocio en su ciclo de vida.'],
      ['created_at', '8 bytes', 'timestamptz', 'Fecha y hora de creación en UTC.'],
    ],
  },
  {
    entidad: 'Product',
    descripcion: 'Producto del catálogo',
    campos: [
      ['id', '16 bytes', 'uuid', 'Identificador interno del producto.'],
      ['business_id', '16 bytes', 'uuid', 'Negocio propietario del producto.'],
      ['category_id', '16 bytes', 'uuid', 'Categoría del producto.'],
      ['name', '160 caracteres', 'varchar(160)', 'Nombre visible del producto.'],
      ['description', '2000 caracteres', 'varchar(2000)', 'Descripción del producto.'],
      ['image_url', '2048 caracteres', 'varchar(2048)', 'Referencia a la imagen almacenada.'],
      ['price', '12 dígitos, 2 decimales', 'numeric(12,2)', 'Precio actual del producto.'],
      ['currency', '3 caracteres', 'varchar(3)', 'Código de moneda.'],
      ['status', '64 caracteres', 'varchar(64)', 'Estado del producto en su ciclo de vida.'],
    ],
  },
  {
    entidad: 'Inventory',
    descripcion: 'Saldo de stock por producto',
    campos: [
      ['product_id', '16 bytes', 'uuid', 'Producto al que corresponde el saldo.'],
      ['business_id', '16 bytes', 'uuid', 'Negocio propietario del saldo.'],
      ['quantity', '4 bytes', 'integer', 'Cantidad de unidades disponibles.'],
      ['low_stock_threshold', '4 bytes', 'integer', 'Umbral a partir del cual la disponibilidad se considera baja.'],
      ['version', '8 bytes', 'bigint', 'Número de versión para el control de concurrencia.'],
    ],
  },
  {
    entidad: 'OrderRequest',
    descripcion: 'Solicitud del consumidor previa al contacto',
    campos: [
      ['id', '16 bytes', 'uuid', 'Identificador interno de la solicitud.'],
      ['business_id', '16 bytes', 'uuid', 'Negocio destinatario de la solicitud.'],
      ['consumer_id', '16 bytes', 'uuid', 'Cuenta del consumidor, cuando está autenticado.'],
      ['reference', '32 caracteres', 'varchar(32)', 'Referencia pública de la solicitud; no autoriza el acceso al detalle.'],
      ['access_token_hash', '128 caracteres', 'varchar(128)', 'Hash del token privado que habilita la consulta del detalle.'],
      ['contact_name', '120 caracteres', 'varchar(120)', 'Nombre informado por el consumidor.'],
      ['contact_phone', '16 caracteres', 'varchar(16)', 'Teléfono opcional informado por el consumidor.'],
      ['status', '64 caracteres', 'varchar(64)', 'Estado de la solicitud en su ciclo de vida.'],
      ['origin', '64 caracteres', 'varchar(64)', 'Canal por el que se originó la solicitud.'],
      ['idempotency_key', '128 caracteres', 'varchar(128)', 'Clave que evita repetir los efectos de la operación.'],
      ['created_at', '8 bytes', 'timestamptz', 'Fecha y hora de creación en UTC.'],
      ['contact_confirmed_at', '8 bytes', 'timestamptz', 'Instante en que el negocio confirmó la recepción del contacto.'],
    ],
  },
  {
    entidad: 'Sale',
    descripcion: 'Venta reportada por el negocio',
    campos: [
      ['id', '16 bytes', 'uuid', 'Identificador interno de la venta.'],
      ['business_id', '16 bytes', 'uuid', 'Negocio que registra la venta.'],
      ['order_request_id', '16 bytes', 'uuid', 'Solicitud vinculada, cuando la venta proviene de una.'],
      ['recorded_by', '16 bytes', 'uuid', 'Responsable que registró la venta.'],
      ['origin', '64 caracteres', 'varchar(64)', 'Origen de la venta: plataforma o presencial.'],
      ['status', '64 caracteres', 'varchar(64)', 'Estado de la venta: registrada o anulada.'],
      ['idempotency_key', '128 caracteres', 'varchar(128)', 'Clave que impide registrar dos veces la misma venta.'],
      ['recorded_at', '8 bytes', 'timestamptz', 'Fecha y hora del registro en UTC.'],
    ],
  },
];

export const pantallasIntro =
  'Se presentan cuatro prototipos de baja fidelidad correspondientes al núcleo del negocio, con datos ficticios. Los cuatro conforman el recorrido completo de la propuesta: el comerciante publica su producto con asistencia, el consumidor descubre el negocio en su zona, arma una solicitud y el comerciante la convierte en venta. Cada pantalla se corresponde con las historias de usuario especificadas en el apartado anterior.';

export const pantallas = [
  {
    archivo: 'P05.png',
    titulo: 'Asistencia de catálogo',
    historia: 'HU-05',
    descripcion:
      'Pantalla de carga de un producto con asistencia de inteligencia artificial generativa. El comerciante sube la fotografía y los datos mínimos, y el sistema propone nombre, descripción y categoría en campos editables. La publicación no se produce hasta que el comerciante confirma, y ante un fallo del proveedor la pantalla habilita la carga manual. Desde aquí se accede al catálogo completo del negocio.',
  },
  {
    archivo: 'P07.png',
    titulo: 'Descubrí tu zona',
    historia: 'HU-07',
    descripcion:
      'Pantalla de entrada del consumidor. Presenta los negocios de la zona ordenados por el ranking hiperlocal, con selección de ubicación manual o del dispositivo y filtros por rubro. Es el punto de partida de la navegación del consumidor y deriva al perfil de cada negocio y a la búsqueda de productos cercanos.',
  },
  {
    archivo: 'P10.png',
    titulo: 'Tu solicitud',
    historia: 'HU-10',
    descripcion:
      'Pantalla de armado de la solicitud. Muestra los productos seleccionados de un único negocio con sus cantidades y precios, y genera el resumen que se enviará por mensajería. La confirmación registra la solicitud con una referencia única sin reservar stock, y deriva a la pantalla de confirmación con esa referencia.',
  },
  {
    archivo: 'P13.png',
    titulo: 'Registrar venta',
    historia: 'HU-13',
    descripcion:
      'Pantalla del comerciante para convertir una solicitud en venta. Permite ajustar los artículos efectivamente vendidos antes de confirmar, y al registrarla descuenta el stock de cada producto en la misma operación. Cierra el recorrido iniciado en el descubrimiento y alimenta las métricas del negocio.',
  },
];

export const arquitecturaTexto = [
  'El diagrama de arquitectura representa los componentes del prototipo y la forma en que se comunican entre sí. El recorrido comienza en la capa de cliente, donde el comerciante y el consumidor acceden desde el navegador de un dispositivo móvil o de escritorio, sin necesidad de instalar una aplicación nativa.',
  'Las peticiones llegan por HTTPS a la aplicación web construida con Next.js sobre React, desplegada en Vercel, que resuelve las interfaces del catálogo, la búsqueda, el mapa y el panel del comercio. Esa aplicación no accede a la base de datos: se comunica mediante una interfaz REST con intercambio en formato JSON y autenticación por token contra el backend.',
  'El backend está desarrollado en C# con ASP.NET Core y se organiza en N capas. La capa de presentación expone los servicios y resuelve la autenticación y la comprobación de permisos. La capa de lógica de negocio concentra las reglas del sistema, entre ellas el motor de ranking orgánico que calcula el puntaje S = w₁·Rel + w₂·Prox + w₃·Disp + w₄·Eq. La capa de acceso a datos traduce los objetos del dominio a la base mediante Entity Framework Core. Cada capa invoca únicamente a la inmediata inferior, lo que permite verificar las reglas de ranking y de ventas sin depender de proveedores externos.',
  'La persistencia se resuelve en PostgreSQL con la extensión PostGIS, que almacena el catálogo, el stock, la geolocalización de los negocios y el historial de exposición necesario para el cálculo de equidad. Las fotografías de los productos se conservan en un almacenamiento de objetos compatible con S3, al que la capa de presentación accede para su carga y lectura, y del que la base de datos guarda únicamente la referencia. Ambos servicios se alojan en Supabase.',
  'Por último, la capa de lógica de negocio se comunica por HTTPS con la API de OpenAI, único servicio externo del prototipo, al que envía la fotografía y los datos mínimos del producto y del que recibe la propuesta estructurada de nombre, descripción y categoría. Esa propuesta queda sujeta a la revisión del comerciante antes de publicarse, de modo que el servicio externo no interviene en la indexación ni en la recuperación de la información, que dependen exclusivamente de la lógica propia del sistema.',
];
