# Repetir el flujo comercial en tres motores de navegador

Dejar una verificación ejecutable de acceso, solicitud con respuesta perdida,
venta, stock, anulación, métricas y permisos en Chromium, Firefox y WebKit.
Usar el harness comercial existente con base temporal y puertos 3200/5280.
La demo en 3000/5080 conserva datos y procesos. No se contratan servicios.

Playwright 1.63.0 ya usado localmente, ahora fijado en herramientas con lockfile;
instalar sus navegadores aislados sin reemplazar Chrome/Edge del usuario.
Comprobar escritorio y anchos móviles, sin afirmar prueba en dispositivos físicos.
Fallar ante regresiones y guardar resumen/capturas sin claves privadas ni cookies.
Cerrar navegadores, servicios temporales y base tanto ante éxito como ante fallo.

Fuera de alcance: carga, auditoría de accesibilidad completa, IA real, hosting y
evaluación científica. Las pruebas definen el comportamiento; corregir el producto
solo si un fallo reproducido lo requiere, con regresión antes de implementar.
