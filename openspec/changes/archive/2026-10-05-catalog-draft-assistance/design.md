# Diseño

Sin persistencia nueva. BLL valida propietario/plan/entrada/salida mediante
ICatalogRepository e ICatalogDraftProvider. Integrations contiene el cliente
Responses de endpoint fijo; ninguna referencia de BLL a HTTP/proveedor. IoC crea
cliente de vida larga con renovación de conexiones y lo dispone al cerrar.

El panel se consulta solo al abrir la ayuda en un producto. Hechos escritos a
propósito, consentimiento, borrador editable y botón de aplicación independiente
del guardado. No se incluyen precios/estado en la respuesta. Cerrar aborta espera;
no se promete cancelar facturación del proveedor de una petición ya enviada.

Modelo sin valor por defecto: se seleccionará cuando exista proveedor habilitado.
Pruebas deshabilitan configuración heredada para impedir llamadas externas; una
factory de pruebas sustituye la interfaz explícitamente para el camino exitoso.

Unidades: servicio/HTTP/adaptador y pruebas; interfaz/proxy y pruebas; evidencia y
configuración. No nuevas dependencias NuGet/npm. Propuesta compatible con ADR-001.
