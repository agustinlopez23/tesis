# Frontend Connectivity Specification

## Purpose

Proveer un frontend Next.js con TypeScript que pueda compilarse y que comunique de forma visible si la API configurada está disponible, sin implementar capacidades del marketplace.

## Requirements

### Requirement: Frontend TypeScript verificable

El sistema MUST proporcionar un frontend Next.js con TypeScript. Con las dependencias restauradas y la versión compatible de Node documentada, los comandos documentados `npm test` y `npm run build` MUST finalizar correctamente.

#### Scenario: Pruebas y build de producción

- GIVEN un entorno con la versión de Node compatible documentada
- WHEN se instalan las dependencias del frontend y se ejecutan `npm test` y `npm run build`
- THEN ambos comandos finalizan correctamente

### Requirement: Estado visible de API disponible

El frontend MUST consultar `GET /health/live` en la URL base de API configurada al cargar su experiencia inicial. Si recibe el contrato de liveness `200 OK` con `{"status":"ok"}`, MUST mostrar un estado visible con el texto `API disponible`.

#### Scenario: Conectividad exitosa

- GIVEN una URL base de API válida y una API que responde el contrato de liveness
- WHEN se carga el frontend
- THEN el frontend consulta `{URL_BASE_API}/health/live`
- AND muestra el texto `API disponible`

### Requirement: Estado controlado de API no disponible

Si la consulta de liveness falla por error de red, tiempo de espera, respuesta no exitosa o respuesta que no cumple el contrato, el frontend MUST mostrar un estado visible con el texto `API no disponible`. En ese caso MUST NOT mostrar `API disponible` como resultado de la misma comprobación.

#### Scenario: API inaccesible

- GIVEN una URL base de API configurada que no es alcanzable
- WHEN se carga el frontend
- THEN muestra el texto `API no disponible`
- AND no muestra el estado de disponibilidad como éxito

#### Scenario: Respuesta inválida de la API

- GIVEN una API accesible que no devuelve el contrato de liveness acordado
- WHEN se carga el frontend
- THEN muestra el texto `API no disponible`
- AND no presenta la respuesta inválida como conectividad exitosa

### Requirement: Exclusión de funcionalidades de negocio

El frontend MUST limitar este slice a la comprobación de conectividad y MUST NOT presentar flujos ni datos de catálogo, stock, comercios, usuarios, autenticación, autorización, geobúsqueda, ranking, IA, pagos, logística o facturación.

#### Scenario: Revisión de alcance de la pantalla inicial

- GIVEN el frontend generado
- WHEN se revisa la experiencia inicial disponible
- THEN solo se observa la comprobación de conectividad acordada
- AND no se observa una funcionalidad de negocio del marketplace
