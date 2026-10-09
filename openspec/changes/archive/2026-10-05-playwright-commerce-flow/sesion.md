# Errores de acceso que permiten recuperarse

Playwright reprodujo dos mensajes genéricos: credenciales incorrectas al ingresar
y sesión vencida al ajustar stock. Las regresiones fallaron antes de distinguir
401 en ingreso y en `useSaleOperation`; después pasaron.

Ingreso indica revisar correo/contraseña. Una escritura autenticada pide volver
a ingresar y conserva el intento para reintentar sin duplicarlo. El hook también
lo aplica a ventas, anulaciones y decisiones de campañas que ya lo utilizaban.

Las seis pruebas de onboarding y cinco de ventas pasaron dentro de la ejecución
conjunta de 22 indicada en `paneles.md`. Playwright real: borrar cookie antes de
ajustar → 401 y mensaje útil → recargar → ingresar → reintentar mismo cuerpo →
stock 3 a 4 una sola vez. Build final: ingreso incorrecto muestra nueva ayuda;
ingreso correcto y salida funcionan; la recarga conserva el cierre de sesión.

Rollback: revertir las ramas 401 de `owner-onboarding.tsx` y
`use-sale-operation.ts`, sus dos regresiones y requisitos correspondientes.
No cambia autenticación, cookies, contratos HTTP ni persistencia del servidor.
