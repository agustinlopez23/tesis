# Perfil público y fotos

- Un visitante abre un negocio publicado desde la guía sin iniciar sesión.
- Ve nombre, rubro, descripción completa, dirección, WhatsApp y fotos disponibles.
- Ausente, borrador o suspendido responde 404 en perfil e imágenes públicas.
- El propietario puede consultar fotos privadas, subir/reemplazar slots 1..5
  y eliminarlos. Un tercero recibe 404; sin sesión, 401; origen inválido, 403.
- Carga multipart: JPEG/PNG/WebP estático, hasta 5 MB y 20 MP, caption <=160.
  Archivo inválido o slot fuera de rango responde 400; tamaño excesivo, 413.
- Una foto inválida no reemplaza la anterior. Reemplazar el mismo slot no crea
  duplicados. Cinco slots limitan el número incluso con cargas simultáneas.
- Imágenes se decodifican, orientan y recodifican sin metadatos a JPEG,
  máximo 1600 px y 1 MB de salida. Las lecturas son no-store y nosniff.
- La galería y panel tienen estados de carga, vacío, error y confirmación.
- Eliminar requiere confirmación inline; cancelar conserva la foto.
- Se mantiene navegación de guía y funcionamiento desde enlace directo.
- No se muestran reseñas, horarios ni imágenes ficticias como datos del negocio.
