# Diseño y entrega

Monolito existente: DTOs/Models, contratos y validación BLL, EF/PostgreSQL en DAL,
registro IoC y endpoints API. Sin nuevas dependencias ni cambios del ranking.
Plan como períodos auditados; capacidad derivada en el momento de acceso.
Productos con versión explícita e ID estable; fotos en tabla separada para no
cargar bytes en listados. Precio decimal, sin stock implícito.

El bloqueo de negocio serializa activaciones, revocaciones, cambios y fotos.
Toda escritura vuelve a comprobar la vigencia después de tomar el bloqueo.
La API revalida roles desde sesión; el registro sigue creando solo responsables.
La cuenta inicial administradora se asigna mediante operación controlada del entorno.

Next usa el proxy existente con lista de rutas permitidas y CSRF. El catálogo
público se monta en perfil; el editor privado carga bajo demanda. El administrador
tiene pantalla con búsqueda, períodos e historial. Estados de carga, error y
vencimiento explícitos; cancelación de lecturas antiguas y plazo de 15 segundos.

Límites del incremento: 200 productos por negocio incluido archivo; 20 por página
pública; foto de entrada 5 MiB/20 MP, JPEG almacenado hasta 1 MiB; precio ARS/USD.
La búsqueda local distingue tildes, ignora mayúsculas. No es ranking global.

## Unidades de revisión

Una pasada de separación por comportamiento: almacenamiento con rollback protegido;
API de catálogo/planes; catálogo público/proxy; panel de gestión; verificación.
No se abre PR ni se hace push. Para futuras PRs usar esta cadena sobre la base
local: almacenamiento → API → público → panel → evidencia.

La migración/designer/snapshot generados se conservan completos aunque excedan
400 líneas. La API conserva junta la transacción de plan/producto/foto y su suite
HTTP para que el permiso no quede incompleto entre commits. Recomendar
`size:exception` para esas dos unidades al abrir PR, con conteos finales en el
reporte; no comprimir código ni fabricar cortes por tipo de archivo.

Rollback: UI/panel/proxy pueden retirarse juntos con sus rutas. API se retira
quitando registro y endpoints. Mantener almacenamiento aditivo si existen datos;
Down rechaza productos, historial o asignaciones administrativas no vacíos.
