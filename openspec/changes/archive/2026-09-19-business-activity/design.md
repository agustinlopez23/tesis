# Decisiones

- Referencias y límites: `Doc/analytics-research-2026-09-19.md`.
- Impresión: al menos 50 % de la tarjeta en viewport durante 1 segundo continuo, pestaña visible. IntersectionObserver + Page Visibility; al ocultar o desmontar se cancela el tiempo. No acredita atención, ni certificación publicitaria.
- Visita: perfil cargado y pestaña visible. Contacto: activación del enlace de WhatsApp; navegación inmediata, envío de evento no bloqueante.
- Sesión: UUID aleatorio por pestaña en sessionStorage, caducidad fija de 30 minutos. Sin acceso al almacenamiento, no medir. No se interpreta como persona única. Una fila por sesión/negocio/tipo/superficie, aun con recarga o nuevas búsquedas; conserva primera posición observada. No sumar contactos de superficies como personas distintas.
- El servidor emite comprobantes cifrados/autenticados de 30 minutos con negocio, posición global (1-based), origen y versión del orden. El cliente no elige posiciones ni algoritmos. Guía/búsqueda/perfil desconocido son orígenes separados; navegación explícita desde una tarjeta conserva comprobante en la pestaña durante 5 minutos. No inferir origen externo.
- Insert atómico condicionado a negocio publicado, dueño no suspendido y actor distinto del dueño. ON CONFLICT evita duplicados concurrentes. Un token válido no prueba conducta humana: protección antifraude avanzada pendiente.
- Eventos mínimos: UUID efímero, negocio, tipo, superficie, origen, posición, versión, fecha del servidor. Sin coordenadas del visitante, texto buscado, IP, user-agent, teléfono ni email en la tabla. Orgánico solamente.
- Retención: excluir inmediatamente eventos con más de 30 días; purga física al arrancar y cada hora. Si la API está detenida, purga al volver. No se almacenan agregados históricos. Logs operativos y backups requieren configuración de despliegue separada; no registrar cuerpos/tokens.
- Reportes privados con 7/30 días calendario, hoy parcial incluido, zona Córdoba; ceros completos por día, sin tendencias inventadas ni tasas sin cohorte. Ventana actualizada al consultar; estados de carga/error/reintento.
- Rate limit global acotado para ingesta del prototipo, cuerpo máximo 4 KiB. Sin dependencia de cabeceras IP reenviadas por el proxy. La medición puede perder eventos por bloqueo, cierre, caducidad, límite o desconexión; no reintentar indefinidamente.
- Capas: BLL define contrato y agregación, DAL persiste, API firma/valida comprobantes y ejecuta purga mediante BLL; IoC registra. Sin nuevas dependencias externas.
- Rollback: desactivar integración cliente/rutas; migración reversible elimina exclusivamente eventos. Los perfiles y fotos no dependen de medición.
