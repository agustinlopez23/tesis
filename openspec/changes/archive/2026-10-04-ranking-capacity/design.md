# Diseño

Límites iniciales locales: 1.000 candidatos, 10.000 saldos históricos por contexto,
4 POST y 16 GET simultáneos por proceso. Son decisiones operativas, no capacidad
productiva certificada. Configuración validada; máximos permitidos 10.000 candidatos,
100.000 saldos y 64 solicitudes simultáneas por ruta.

DAL obtiene como máximo límite+1 filas: si existe la fila adicional rechaza todo.
Nunca entrega el prefijo al motor. Mantiene consulta única y rollback transaccional.
El exceso devuelve 503 sin datos internos; reintentar el mismo requestId es seguro.
Los límites de concurrencia complementan las cuotas por minuto; rechazan con 429
sin cola y liberan el permiso al finalizar, fallar o cancelar. GET tiene su propio cupo.

Verificación TDD con bases temporales y carga HTTP integrada. No se miden garantías
distribuidas ni se usa la demo. Rollback: revertir controles/registro/configuración
y pruebas de este incremento; no hay cambio de esquema ni de política.
