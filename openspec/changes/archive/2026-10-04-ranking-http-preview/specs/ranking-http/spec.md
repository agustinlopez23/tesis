# Requisitos HTTP

- POST `/public/ranking-snapshots`: zoneId conocido, categoryId existente,
  radiusKm exactamente 4, requestId Guid no vacío; JSON <=4096 bytes aun sin
  Content-Length. Rechaza propiedades desconocidas y filtros por query string.
- Origin configurado no vacío y X-Requested-With=marketplace obligatorios.
  403 origen no confiable; 400 datos/tipos inválidos; 413 cuerpo excesivo.
- Solo Development/Testing con PreviewEnabled=true crea listas. Resto: 404.
  No existe interruptor de activación productiva en este incremento.
- Reintento idéntico entrega lista original (200), primera creación 201,
  requestId con otro contexto 409; vínculo vencido 410. No agrega exposición.
- GET `/public/ranking-snapshots/page?cursor=...` exige únicamente un cursor.
  Alteración, esquema inválido, mezcla de filtros o tamaño incorrecto: 400.
  Vencimiento auténtico, snapshot ausente o retirada de emergencia: 410.
- Las páginas conservan huecos y posiciones. 45 elegibles entregan 20/20/5;
  repetir página no crea oportunidades. DTO no revela traza/dueños/candidatos.
- Todas las respuestas de estas rutas, incluidos errores y rechazo por tasa,
  usan no-store. Creación y lectura tienen cuotas globales separadas, sin colas.
- Política/semilla no vienen del cliente. Dueño se resuelve desde sesión real;
  inclusión de negocio propio conserva toda la oportunidad con exposición cero.
