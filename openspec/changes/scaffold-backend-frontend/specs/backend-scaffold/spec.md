# Backend Scaffold Specification

## Purpose

Proveer una solución backend mínima, compilable y verificable que respete la arquitectura en capas de ADR-001 sin incorporar comportamiento de negocio.

## Requirements

### Requirement: Solución backend en capas y compilable

El sistema MUST incluir una solución backend con los proyectos `Marketplace.Api`, `Marketplace.BLL`, `Marketplace.DAL`, `Marketplace.IoC`, `Marketplace.DTOs` y `Marketplace.Models`. La solución MUST restaurar dependencias y compilar satisfactoriamente mediante los comandos documentados `dotnet restore` y `dotnet build`.

Las referencias productivas MUST ser acíclicas y ser un subconjunto de las permitidas por ADR-001: API a BLL, DTOs e IoC; BLL a Models y DTOs; DAL a BLL y Models; e IoC a BLL y DAL. `Marketplace.BLL` MUST NOT referenciar `Marketplace.DAL` ni `Marketplace.IoC`; `Marketplace.Models` y `Marketplace.DTOs` MUST NOT referenciar capas; y ningún proyecto productivo MUST referenciar `Marketplace.Api`. El proyecto `Marketplace.Api.Tests` MAY referenciar `Marketplace.Api` para verificar su contrato HTTP.

#### Scenario: Restauración y compilación del backend

- GIVEN un entorno con el SDK .NET compatible documentado
- WHEN se ejecutan los comandos documentados `dotnet restore` y `dotnet build` sobre la solución backend
- THEN ambos comandos finalizan correctamente
- AND la solución contiene los seis proyectos requeridos

#### Scenario: Validación de referencias en capas

- GIVEN la solución backend generada
- WHEN se inspeccionan sus referencias de proyecto
- THEN no existe un ciclo de referencias
- AND cada referencia pertenece al conjunto permitido
- AND `Marketplace.BLL` no referencia `Marketplace.DAL` ni `Marketplace.IoC`

### Requirement: Liveness HTTP de la API

`Marketplace.Api` MUST exponer `GET /health/live` como señal de liveness. Ante una solicitud válida, el endpoint MUST responder `200 OK` con `Content-Type: application/json` y el cuerpo JSON `{"status":"ok"}`.

La respuesta de liveness MUST indicar únicamente que el proceso HTTP de la API está disponible y MUST NOT requerir ni afirmar disponibilidad de PostgreSQL, PostGIS u otra dependencia externa.

#### Scenario: API disponible

- GIVEN la API está iniciada
- WHEN un cliente realiza `GET /health/live`
- THEN recibe `200 OK`
- AND recibe el cuerpo JSON `{"status":"ok"}`

#### Scenario: Base de datos no configurada

- GIVEN la API está iniciada sin una conexión disponible a PostgreSQL o PostGIS
- WHEN un cliente realiza `GET /health/live`
- THEN recibe `200 OK` con el contrato de liveness
- AND la respuesta no declara readiness de base de datos

### Requirement: Verificación automatizada y límite del slice

El backend MUST incluir pruebas automatizadas que verifiquen el contrato de `GET /health/live`, y dichas pruebas MUST finalizar correctamente con el comando documentado `dotnet test`.

El scaffold MUST NOT exponer endpoints ni incorporar entidades, persistencia, migraciones, reglas o casos de uso de catálogo, stock, comercios, usuarios, autenticación, autorización, geobúsqueda, ranking, IA, pagos, logística o facturación. El scaffold MUST NOT crear `Marketplace.Utility` ni proyectos de integraciones.

#### Scenario: Ejecución de pruebas del contrato

- GIVEN las dependencias del backend están restauradas
- WHEN se ejecuta `dotnet test` según la documentación
- THEN las pruebas finalizan correctamente
- AND incluyen una comprobación automatizada del contrato de liveness

#### Scenario: Revisión de alcance técnico

- GIVEN los artefactos del scaffold backend
- WHEN se revisan sus proyectos públicos y capacidades expuestas
- THEN solo se encuentra la señal de liveness acordada
- AND no se encuentra una capacidad de negocio ni los proyectos excluidos
