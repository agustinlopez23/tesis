# Registro del responsable y gestión de su negocio

El responsable podrá crear su cuenta, ingresar, cerrar sesión y guardar o editar negocios propios con rubro, ubicación y WhatsApp. Los datos se conservarán en PostgreSQL/PostGIS y otro responsable no podrá consultarlos ni modificarlos desde el panel privado.

## Alcance autorizado

Primera entrega de implementación acordada en conversación: registro/sesión, alta y edición del negocio y persistencia real. Trazabilidad: RF-01, RF-02, CU-01 y CU-02 de `Doc/Entrega2/`; RNF de propiedad, integridad, privacidad y fallos controlados. La documentación académica anterior permanece como borrador del producto completo.

Incluye formulario de registro/ingreso, panel de negocios propios, categorías iniciales de demostración, coordenadas geográficas, migración explícita, entorno Docker local y pruebas HTTP/DAL reales. No incluye recuperación de contraseña, verificación de correo, administración, catálogo, planes comerciales, publicaciones, revista pública, ranking ni publicidad; se implementarán en cortes posteriores.

## Criterio de cierre

Un responsable puede registrar, iniciar sesión, crear y editar su negocio; cerrar sesión invalida el token en el servidor. Otro responsable obtiene 404 al pedir o modificar el negocio. Los datos sobreviven al reinicio de la API. La ubicación es geography(Point,4326). Los formularios muestran errores sin perder datos introducidos no sensibles. Salud del proceso sigue disponible sin exigir conexión a la base.

## Unidades de revisión

Separar infraestructura/migración, autenticación, negocios y frontend en cortes revisables de hasta 400 líneas de código manual; documentación, pruebas y archivos generados se identifican por separado sin ocultarlos. No crear commits, publicar ni contratar recursos como parte del cambio. Evidencia TDD en `tdd-evidence.md`.

## Dependencias y rollback

SDK/backend y frontend existentes; motor Docker local; imagen PostGIS; paquetes EF/Npgsql compatibles. La base de desarrollo usa volumen propio y las pruebas crean una base temporal con nombre aleatorio. No se borra ni reinicializa una base existente. Rollback de esquema mediante migración sobre una copia o entorno vacío; nunca eliminar datos del usuario para revertir código.
