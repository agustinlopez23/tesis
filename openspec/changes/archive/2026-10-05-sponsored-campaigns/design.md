# Diseño y unidades de revisión

Tablas campañas, decisiones y eventos; FK nullable de solicitud a campaña. Imagen
JPEG procesada copiada desde catálogo/galería. Reutiliza bloqueo de negocio para
escrituras y elegibilidad, más advisory lock PostgreSQL por zona para reservas.
Estado comercial almacenado; activo/finalizado/bloqueado derivados al consultar.
DataProtection protege campaña/contexto/vencimiento. Navegador conserva token
solo tras abrir anuncio y solo lo adjunta al confirmar desde ese enlace.

Endpoints privados titular y administrador separados. UI de propietario se carga
al abrir el panel; administración dedicada. Anuncios de descubrimiento son una
región aparte. Reintentos de escritura guardan URL y cuerpo antes de enviar.

Una pasada de separación: esquema/migración/pruebas de rollback; backend con
contratos/transacciones/HTTP y pruebas; gestión frontend con pruebas; superficie
pública/proxy/atribución con pruebas; integración y documentación. El esquema
generado y el backend transaccional exceden 400 líneas: `size:exception` sugerida
si se abre PR, evitando dividir un cambio coherente solo para reducir el número.
No se abrió PR ni se publicó en un entorno remoto.
