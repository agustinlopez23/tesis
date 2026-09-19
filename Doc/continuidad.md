# Punto de continuidad — 19/09/2026

El usuario pidió guardar acá y continuar en otra sesión. No iniciar otro incremento al guardar este punto.

## Estado terminado

Cerca, marketplace hiperlocal de Córdoba. Implementados y verificados: identidad y panel del responsable, guía pública con búsqueda/rubro, ubicación/radio/distancia, perfil y galería con hasta cinco fotos, y actividad privada por negocio.

El último incremento investigó patrones de Google Business Profile, Airbnb, Etsy, GA4 e IAB y los aplicó al panel **Mi negocio → Ver actividad**:

- Apariciones visibles (50 % de tarjeta, 1 segundo continuo, pestaña activa), visitas al perfil y clics de WhatsApp.
- Sesión aleatoria de pestaña de 30 minutos; deduplicación también atómica en PostgreSQL; visitas del propio dueño autenticado excluidas.
- Comprobantes protegidos de contexto: negocio, posición, origen y versión del orden. No guardar coordenadas del visitante ni búsquedas textuales en eventos.
- Períodos 7/30 días, hora de Córdoba, gráfica interactiva, tabla y estados vacíos/error/reintento.
- Retención de eventos de 30 días; purga al arrancar y cada hora. Sin proveedores analíticos externos.

Validación: **46 pruebas backend + 56 frontend = 102**, lint y builds aprobados. Playwright verificó visitante separado, recarga sin duplicado, perfil/contacto, error 503/reintento y anchos 320/390/768/1440 sin desbordes. El café de demostración registró 1 aparición, 1 visita y 1 clic de prueba: no representan clientes reales.

## Próximo paso acordado

**Ranking equitativo y evaluación reproducible.** Primero leer PRD y definir baseline, relevancia independiente, candidatos/contexto, descuento por posición, ventana y métricas de equidad. Mantener IA fuera del ranking. No confundir esta instrumentación con un experimento ya completado ni afirmar mejoras comerciales.

Después quedan catálogo, capacidades por plan, solicitudes/pedidos, ventas/stock y publicidad. Trabajar un incremento completo por vez, con investigación primaria cuando corresponda, pruebas y revisión visual mediante MCP Playwright. El usuario prioriza una UI/UX profesional y avanzar sin pedir confirmaciones rutinarias.

## Dónde retomar

- [Estado general](../openspec/project.md).
- [PRD](PRD.md), secciones de exposición, ranking y evaluación.
- [Investigación de actividad](analytics-research-2026-09-19.md).
- [Contrato de actividad](../openspec/specs/business-activity/spec.md).
- [Verificación y límites](../openspec/changes/archive/2026-09-19-business-activity/verify-report.md).
- [Arquitectura](architecture/ADR-001-layered-architecture.md).
- [Ejecución local](../App/README.md).

Rama `main`. Últimos commits del incremento: `a836349`, `e8f0729`, `3b6f76e`, `2aacb38`, `f352189`, `75d76eb`. Guardados localmente; sin push ni despliegue. Árbol limpio antes de agregar esta nota.

## Entorno y precauciones prácticas

Windows PowerShell; workspace `C:\Users\AgustinPc\Documents\Proyectos\Tesis`. Next.js 16.3.4, Node 24.20.0, .NET SDK 10.0.400, PostgreSQL/PostGIS local. Antes de tocar Next, leer `App/frontend/AGENTS.md` y la guía pertinente incluida en `node_modules/next/dist/docs`.

Demo: frontend `http://localhost:3000`, API `http://localhost:5080`, PostgreSQL puerto 55432. El navegador se dejó en Mi negocio, actividad del Café de la Cañada. Comprobar disponibilidad al volver: los procesos pueden no sobrevivir a la sesión. No guardar credenciales en documentación.

Helpers locales ignorados: `.tools/profile-check.ps1` prueba backend y detiene temporalmente la API; `.tools/refresh-ui-api.ps1` compila, migra y reinicia API; `.tools/ui-demo-processes.json` guarda PID. Revisar contenido antes de ejecutar. `.tools/version-current.ps1` contiene los commits del incremento ya ejecutados: NO volver a correrlo sin actualizar sus rutas.

Capturas ignoradas: `.playwright-mcp/actividad-escritorio.png` y `actividad-movil.png`. Al capturar, desplazar a inicio y quitar foco para evitar artefactos de enlaces fijos. Usar bases temporales PostgreSQL en pruebas, nunca EF InMemory. No iniciar subagentes salvo pedido explícito. No repetir suites completas sin cambios o dudas nuevas.
