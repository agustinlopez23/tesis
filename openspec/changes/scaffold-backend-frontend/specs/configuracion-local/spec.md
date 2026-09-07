# Configuración Local Specification

## Purpose

Permitir preparar y verificar el scaffold en desarrollo local con instrucciones y valores de ejemplo seguros, sin versionar secretos ni depender de un proveedor de hosting.

## Requirements

### Requirement: Ejemplos de configuración local sin secretos

El sistema MUST versionar ejemplos de configuración local para backend y frontend, junto con instrucciones para utilizarlos. Los ejemplos MUST contener únicamente valores seguros de desarrollo y MUST NOT contener credenciales, tokens, claves privadas, cadenas de conexión con contraseña ni otros secretos reales.

#### Scenario: Preparación desde ejemplos

- GIVEN una copia nueva del repositorio
- WHEN una persona sigue las instrucciones y crea su configuración local a partir de los ejemplos versionados
- THEN puede preparar el backend y el frontend para el camino de verificación local
- AND no necesita obtener un secreto versionado

#### Scenario: Inspección de secretos

- GIVEN los archivos de ejemplo versionados
- WHEN se inspeccionan sus valores
- THEN no contienen una credencial, token, clave privada ni contraseña real

### Requirement: URL de API configurable por el frontend

La configuración local del frontend MUST definir una URL base pública de API mediante la variable `NEXT_PUBLIC_API_URL`. La documentación MUST indicar cómo establecerla y MUST usarla como la fuente de la URL que el frontend consulta para la liveness.

#### Scenario: Cambio de URL de API sin modificar código

- GIVEN una configuración local con `NEXT_PUBLIC_API_URL` apuntando a una API disponible
- WHEN se cambia `NEXT_PUBLIC_API_URL` para apuntar a otra URL base de API y se reconstruye y reinicia el frontend en producción (o se reinicia el servidor de desarrollo)
- THEN el frontend consulta `/health/live` en la nueva URL base
- AND no requiere modificar código fuente para aplicar ese cambio

### Requirement: Dependencias de datos diferidas

Este slice MUST NOT exigir que PostgreSQL o PostGIS estén instalados, iniciados o disponibles para restaurar, compilar, probar la liveness o mostrar la conectividad frontend--API. Una señal de readiness de base de datos queda fuera de alcance y MAY especificarse en un cambio posterior separado.

#### Scenario: Verificación sin servicios de datos

- GIVEN PostgreSQL y PostGIS no están disponibles localmente
- WHEN se ejecuta el camino documentado de restauración, pruebas, build y liveness del scaffold
- THEN las verificaciones definidas para este slice pueden completarse
- AND ninguna de ellas se interpreta como readiness de base de datos

### Requirement: Sin decisión de hosting en el scaffold

La configuración del slice MUST ser apta para ejecución local y MUST NOT requerir contratación, configuración ni selección de Vercel, Render, Supabase u otro proveedor de hosting.

#### Scenario: Ejecución local independiente de proveedor

- GIVEN una persona sigue la documentación del scaffold
- WHEN prepara y ejecuta el entorno local
- THEN puede verificar el backend y frontend sin una cuenta ni recurso de un proveedor de hosting
