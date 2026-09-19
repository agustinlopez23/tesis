# Verificación — 2026-09-18

Perfil público con fotos implementado, probado y disponible en la demo local.

## Evidencia de pruebas

- Rojo API: dos pruebas HTTP fallaron por ausencia de rutas de fotos
  (404 en lugar de 401/200). Las 27 pruebas anteriores seguían pasando.
- Verde API: lectura pública/privada, propiedad, publicación, suspensión,
  reemplazo atómico, eliminación, validación y máximo de cinco posiciones.
- Rojo frontend: seis pruebas de perfil/panel fallaron por ausencia de
  controles y contenido. Una prueba del proxy falló con 404 en las rutas nuevas.
- Verde frontend: perfil con/sin fotos, selección de galería, 404 y error
  recuperable, carga multipart, reemplazo fallido, confirmación/cancelación
  de eliminación y recuperación de lista.
- Ampliaciones posteriores: concurrencia del mismo slot, JPEG/PNG/WebP,
  reducción a 1600 px, las ocho orientaciones EXIF, eliminación de metadatos,
  límite del proxy sin Content-Length y rechazo de DELETE desde otro origen.
  Estas ampliaciones no se presentan como pruebas rojas previas.
- Un PNG de prueba con datos inválidos fue reemplazado por un PNG generado
  con el encoder; la implementación mantuvo el rechazo del archivo corrupto.
- La prueba de guardado del panel se actualizó al flujo nuevo: mantener
  seleccionado el negocio para cargar fotos, en lugar de abrir un alta vacía.

Resultado final: **41 pruebas backend, 48 frontend, lint sin advertencias,
build Next y compilación .NET aprobados**. PostgreSQL/PostGIS real aislado.
Restore con NuGetAuditMode=all y warnaserror completado.
Dependencias nuevas fijadas en SkiaSharp 4.152.0 y assets Linux de igual versión.
Las pruebas locales corrieron en Windows; no se afirma ejecución remota de CI.

## MCP Playwright

Con frontend, API y PostgreSQL locales:

- Editar Café de la Cañada, abrir Administrar fotos, cargar WebP y descripción.
- Cargar una segunda imagen, abrir perfil desde el panel, seleccionar miniatura
  y navegar con Foto anterior. Imágenes decodificadas por el navegador.
- Archivo con extensión PNG pero contenido inválido: error, conserva original.
- Simular 503 al reemplazar: conserva formulario/foto; reintento guarda.
- Eliminar segunda foto: cancelar conserva dos; confirmar deja una.
- Despublicar desde el panel: perfil e imagen públicos responden 404 sin cookies.
  Republicar restaura el acceso.
- Portada visible en guía; acceder al perfil y volver atrás conserva zona/radio.
- Perfil ausente: mensaje específico. 503 del perfil: reintento recupera.
- Fallo de imagen: reintento recupera el archivo.
- Imágenes públicas con image/jpeg, no-store y nosniff.
- Perfil y panel revisados a 320, 390, 768 y 1440 px. Sin desborde de documento.
- Capturas locales: .playwright-mcp/perfil-escritorio.png, perfil-movil.png
  y fotos-panel-movil.png.

La imagen de la demo reutiliza la ilustración editorial existente y lleva el
texto “Imagen ilustrativa · negocio de demostración”. No es evidencia de un
comercio real. La segunda copia se eliminó tras probar la galería.
Los errores 400/404/503 en consola corresponden a casos provocados.

## Límites y reversión

Hasta cinco JPEG normalizados de 1 MB por negocio guardados en PostgreSQL.
No hay almacenamiento de objetos, catálogo ni ranking equitativo.
Las rutas web usan carga de perfil del lado cliente; el 404 de disponibilidad
lo entrega la API y la interfaz muestra el estado correspondiente.
La lectura pública posterior a despublicar queda bloqueada; una imagen que
alguien ya descargó no se puede retirar de su dispositivo.

Rollback de aplicación mediante revert. La migración Down elimina la tabla
business_photos: respaldarla antes si se requiere conservar imágenes.
No se hizo push ni despliegue público.
