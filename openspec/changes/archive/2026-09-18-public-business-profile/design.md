# Diseño

API de perfil: GET /public/businesses/{id}; devuelve 404 para ausentes,
borradores y responsables suspendidos. La guía puede agregar coverPhotoSlot
cuando hay fotos; conserva contratos existentes cuando no las hay.

Fotos en business_photos: clave (BusinessId, Slot), slot 1..5, caption hasta
160 caracteres, dimensiones y JPEG normalizado como bytea. FK con cascade.
Para este prototipo, máximo cinco imágenes por negocio y 1 MB de salida cada
una evita incorporar un servicio externo. Migrar a object storage al escalar.
Las consultas de guía/perfil leen metadatos sin cargar bytes.

PUT /businesses/{id}/photos/{slot} recibe multipart con file y caption.
GET /businesses/{id}/photos devuelve metadatos privados; GET del slot devuelve
imagen al propietario. DELETE elimina el slot. Las mutaciones requieren sesión,
propiedad y origen confiable. GET /public/businesses/{id}/photos/{slot} vuelve
a comprobar publicación y suspensión en cada acceso. Respuestas no-store y
nosniff; Next Image unoptimized evita mantener caché pública tras despublicar.

Se aceptan JPEG, PNG y WebP hasta 5 MB y 20 megapíxeles, decodificando sólo
imágenes estáticas. Se normaliza orientación, reduce a máximo 1600 px y recodifica
a JPEG sobre fondo blanco sin metadatos. Se rechazan archivos corruptos.
SkiaSharp 4.152.0 y native assets Linux de igual versión: dependencia de un
utilidad de imágenes en Marketplace.Utility, sin referencias a las capas.
Un adaptador en IoC implementa la interfaz BLL y mapea el resultado de la
utilidad. Models/DTOs/BLL no dependen de Skia ni EF; se conserva el ADR-001.

Frontend: /negocios/[id], galería con selección de foto y alternativas de texto,
tarjetas enlazadas, fotos administrables después de guardar un negocio.
Cada carga/reemplazo se guarda explícitamente. Eliminar pide confirmación
inline y permite cancelar. Fallos preservan datos y permiten reintentar.

Fuentes consultadas: https://www.nuget.org/packages/SkiaSharp/4.152.0,
https://www.nuget.org/packages/SkiaSharp.NativeAssets.Linux.NoDependencies/4.152.0,
https://learn.microsoft.com/en-us/dotnet/api/skiasharp.skcodec
