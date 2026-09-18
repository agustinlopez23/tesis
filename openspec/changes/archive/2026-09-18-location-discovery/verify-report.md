# Verificación — 2026-09-18

Resultado: búsqueda por zona/dispositivo, radio y distancia implementada y
verificada. Sin cuenta, con fallback manual y guía general.

## TDD y regresión

- Rojo API real: 11 fallos de comportamiento; el radio devolvía 20 resultados
  en lugar de 2 y parámetros inválidos respondían 200 en lugar de 400.
- Verde API: 11/11 sobre PostgreSQL/PostGIS aislado. Distancias geográficas,
  filtros antes de paginar, borradores/suspendidos excluidos, texto/rubro,
  desempate estable e índice GiST comprobado en esquema migrado.
- Rojo frontend: 4 pruebas fallaron por ausencia de zona y botón de ubicación.
- Verde frontend: esas 4 y otras 2 de enlace sin estado y recuperación desde
  un radio vacío. Esta ampliación se incorporó después de la implementación.
- Regresión final: backend 27/27, frontend 40/40, ESLint sin advertencias
  y compilación Next de producción aprobados.
- Migración AddBusinessLocationIndex generada por EF y aplicada en base local.
  Compilación backend sin errores ni advertencias.

Los fallos iniciales de bloqueo de DLL, EPERM y EventLog fueron problemas del
entorno, no evidencia de TDD. Se detuvo la API local y se ejecutaron las pruebas
con los permisos necesarios. Un error de tipado en una opción del test se
corrigió antes del build final.

## MCP Playwright con API y base reales

- Centro, 2 km: Café de la Cañada, Almacén del Patio y Taller Jacarandá,
  ordenados a aproximadamente 790 m, 1,5 km y 1,6 km.
- Centro, 1 km: solamente Café. Centro, 500 m: vacío con alternativa para ampliar.
- Alberdi, 1 km: solamente Almacén; combinar Gastronomía produce vacío y
  quitar el rubro recupera el Almacén.
- Radio y zona sobreviven recarga y navegación atrás.
- Permiso denegado mediante Chromium: mensaje visible, zona manual disponible.
- Permiso concedido y coordenada simulada del navegador: muestra distancias;
  recarga y atrás recuperan el estado. La URL no contiene coordenadas precisas.
- Abrir la URL de dispositivo en otra pestaña explica la ausencia de ubicación.
- Respuesta 503 simulada: reintentar conserva zona/radio y recupera resultados.
- Sin desborde horizontal de documento a 320, 390, 768 y 1440 px.
- Capturas locales: .playwright-mcp/ubicacion-escritorio.png y ubicacion-movil.png.

Los tres comercios son datos ficticios de la demo, con coordenadas diferenciadas
para el recorrido. No representan negocios verificados. La consola registró
el 503 provocado deliberadamente durante la recuperación.

## Límites

Distancia en línea recta, sin itinerarios ni tiempos de viaje. Tres puntos
manuales iniciales. Sin mapa, geocodificador, fotos ni ranking de equidad todavía.
El historial del navegador conserva su propio estado; no se promete borrado
automático al cerrar pestaña. No se desplegó públicamente ni se hizo push.
