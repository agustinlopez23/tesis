# Revista local de previsualización

- `/revista` y su enlace exigen habilitación server-side
  RANKING_PREVIEW_ENABLED=true y NODE_ENV development/test. Página noindex.
  Producción rechaza página/POST/GET aun con flag true.
- Proxy Next solo admite POST ranking-snapshots y GET ranking-snapshots/page;
  origen confiable, cuerpo <=4096 bytes, sesión/query sin alteración,
  cancelación propagada y no-store en todo estado, incluidos errores propios.
  Deshabilitar creación permite GET local para listas ya abiertas.
- Montar la vista no crea oportunidades. Zona predefinida y rubro existente
  obligatorios; radio fijo 4 km. Confirmar o actualizar inicia otro requestId.
  Reintentar conserva identidad y filtros; navegar usa GET de página actual/siguiente.
- Historial almacena contexto/cursor, nunca tarjetas ni tokens en la URL.
  Recarga/atrás/retorno desde perfil revalida elegibilidad sin nueva oportunidad.
  Cambiar filtros retira resultados e invalida solicitudes anteriores.
- Orden estable y huecos conservados; una página vacía con hasMore permite continuar.
  Vencimiento local/410 retira tarjetas y exige actualización explícita.
  429/503/red ofrecen reintento; 404 informa pausa y guía. No reemplazar ni
  reordenar la lista silenciosamente ante fallos.
- Tarjetas comparten perfil/WhatsApp y medición observada de la guía con tokens
  del ranking, sin confundirla con el ledger modelado ni exponer hashes/IDs internos.
- Estados de carga/error y foco en resultados accesibles; 1440/390/320 px sin
  desborde horizontal. Vista identificada como previsualización, sin afirmar
  validación experimental de la política ni activación productiva.
