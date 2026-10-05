# Asistencia opcional de catálogo

El titular con plan vigente MUST poder solicitar un borrador a partir de hechos
escritos explícitamente, con consentimiento previo para enviarlos al proveedor.
La propuesta contiene solamente nombre, descripción y categoría. Generar MUST NOT
guardar productos ni modificar precio, stock, publicación, exposición o ranking.
El usuario revisa/edita y confirma el uso del borrador; guardar el producto sigue
siendo una acción separada sometida a las reglas normales del catálogo.

Escenario: al generar, el formulario conserva su contenido hasta elegir Usar este
borrador. Descartar o cerrar deja disponible la carga manual.

Propiedad y plan MUST validarse antes de llamar al proveedor; entrada 3–1500
caracteres y consentimiento verdadero. Sesión, CSRF y no-store; JSON máximo
8 KiB. Se revalida plan al devolver el borrador. Salida limitada a nombre 160,
descripción 2000 y categoría 80; errores, rechazo o salida inválida permiten
continuar manualmente sin sustituir contenido.

El adaptador MUST permanecer desactivado por defecto y requerir habilitación,
modelo y clave de servidor. La interfaz de proveedor reside en BLL y su adaptador
en Integrations, registrado por IoC. Endpoint OpenAI HTTPS fijo, sin redirecciones,
sin herramientas ni contexto extraído de cuenta/pedidos, `store:false`, esquema
estricto. No se registran hechos/respuestas en logs propios. Esto no declara
retención cero del proveedor; su configuración y condiciones son externas.

Límites del prototipo por instancia: seis POST/minuto, dos generaciones simultáneas
sin cola, 12 segundos y 128 KiB de respuesta, 1000 tokens de salida solicitados.
No hay reintentos automáticos. La interfaz informa envío a OpenAI y permite escribir
a mano cuando el adaptador está desconfigurado o falla.

Escenario: proveedor deshabilitado no realiza solicitudes externas. Respuesta
incompleta, rechazo, JSON malformado o campos incompatibles devuelven fallback.
La implementación local con dobles de transporte no acredita acceso a un modelo
real ni calidad factual de sus borradores.
