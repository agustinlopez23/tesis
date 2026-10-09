# Campañas patrocinadas con activación manual

El negocio prepara una campaña de producto o promoción, el administrador revisa
contenido y registra confirmación manual de pago, y la difusión aparece separada
y marcada Patrocinado. Impresiones/clics y solicitudes/ventas vinculadas pertenecen
a la campaña, sin alterar exposición ni puntaje orgánico.

Política local del prototipo: zonas Centro, Nueva Córdoba y Alberdi; hasta tres
campañas contratadas superpuestas por zona, rotación del orden por intervalo;
vigencia máxima 30 días. No tarifas ni procesamiento de cobros. Promociones pueden
pertenecer a negocios gratuitos y reutilizan una imagen de su galería; campañas
de producto requieren catálogo habilitado. Publicación revalida negocio, titular,
fechas y contenido; producto con saldo conocido cero no se anuncia.

Al interrumpirse elegibilidad se oculta automáticamente y se conserva contrato/
historia. El período no se extiende ni se genera compensación automática; su
resolución comercial queda registrada por administración mediante motivo. Pausa
del titular o administrador inmediata. Datos revisados no se modifican bajo una
aprobación anterior; editar un rechazo/borrador exige nueva revisión.

Medición con contexto protegido, TTL, deduplicación y exclusión del titular.
Atribución explícita a solicitud solo desde contexto de campaña válido; replay
de solicitud no pierde identidad al vencer contexto. Ventas reportadas conservan
la relación a través de la solicitud. Métricas sin sumar monedas/orígenes ni
contabilizar ventas anuladas como vigentes. No se prometen vistas ni ventas.

Validación: strict TDD sobre PostgreSQL temporal, límites/autorización/transiciones,
vigencia/cupo/concurrencia, eventos y atribución, interfaz y recorrido integrado.
No nuevas dependencias, sin publicación ni modificaciones de la demo.
