# ADR-001: Arquitectura N capas con inversión de dependencias

**Estado:** aceptada e implementada; actualización de alcance técnico 2026-09-18.

## Decisión

Organizar la aplicación en un único repositorio, con un backend monolítico ASP.NET Core dividido en API, BLL y DAL, y proyectos de apoyo IoC, DTOs, Models y Utility. Mantener Next.js como frontend y PostgreSQL/PostGIS como base de datos, conforme a la propuesta técnica.

La nomenclatura tradicional facilita la defensa académica. La separación de responsabilidades y la inversión de dependencias permiten mantener código limpio y probar las reglas de negocio sin una base de datos.

## Contexto y alcance

La propuesta describe un marketplace hiperlocal con catálogo, búsqueda geográfica, ranking equitativo y asistencia de IA. La arquitectura debe concentrar la complejidad en esas funcionalidades y su evaluación, no en operar infraestructura distribuida.

Esta decisión reemplaza la organización inicialmente sugerida en Api/Core/Infrastructure. No incorpora pagos, logística, facturación ni otras funcionalidades fuera del alcance inicial.

Fuentes: propuesta técnica DOCX y entrega PDF en [Seminario1 entrega](../Seminario1%20entrega/), particularmente las secciones de herramientas técnicas, objetivos, asistencia de IA y evaluación. Se preservan los documentos originales.

## Responsabilidades

API, BLL y DAL son capas. Los demás proyectos son apoyos, no capas adicionales.

| Proyecto | Responsabilidad | Límite principal |
| --- | --- | --- |
| `Marketplace.Api` | Controllers, contratos HTTP, autenticación HTTP y manejo de errores | Sin reglas de negocio ni consultas EF Core |
| `Marketplace.BLL` | Servicios, reglas, ranking e interfaces de persistencia e integraciones | Sin referencias a DAL, EF Core, HTTP o SDKs de proveedores |
| `Marketplace.DAL` | DbContext, configuraciones EF Core, migraciones y repositorios específicos | Sin decisiones del negocio ni integraciones con OpenAI |
| `Marketplace.IoC` | Registro de interfaces e implementaciones | Sin lógica de negocio; utiliza el contenedor nativo de .NET |
| `Marketplace.DTOs` | Contratos de entrada y salida | Sin persistencia ni dependencias de infraestructura |
| `Marketplace.Models` | Entidades y tipos del negocio | Sin dependencias de EF Core; mapeos en DAL |
| `Marketplace.Utility` | Funciones transversales pequeñas, cuando sean necesarias | No es un contenedor de reglas, servicios o integraciones |
| `Marketplace.Integrations` | Adaptador OpenAI para borradores de catálogo | Implementa contrato BLL; sin persistencia, ranking ni publicación |

`Marketplace` es un nombre provisional. Utility se incorporó para normalizar
imágenes mediante SkiaSharp: recibe bytes y devuelve bytes/dimensiones, sin
referencias a BLL, DAL ni Models. IoC adapta ese resultado al contrato de BLL.

## Dependencias de código

La flecha indica una referencia de proyecto permitida; no obliga a agregar referencias que todavía no sean necesarias.

```text
Api  -> BLL, DTOs, IoC
BLL  -> Models, DTOs
DAL  -> BLL, Models
IoC  -> BLL, DAL, Utility, Integrations
Integrations -> BLL, DTOs
Models, DTOs -> sin referencias a las capas
Utility     -> sin referencias a las capas
```

Los consumidores pueden referenciar Utility únicamente cuando necesiten una utilidad concreta. Ningún proyecto referencia Api; BLL nunca referencia DAL o IoC. Las dependencias deben permanecer acíclicas.

En ejecución, el flujo sigue siendo `API -> BLL -> DAL -> base de datos`. BLL llama a una interfaz propia y DAL aporta la implementación. Api invoca el registro de IoC al iniciar la aplicación.

Ejemplo:

- `BLL/Interfaces/IProductRepository.cs` declara las operaciones que necesita el negocio.
- `DAL/Repositories/ProductRepository.cs` implementa esas operaciones con EF Core.
- `BLL/Services/ProductService.cs` recibe la interfaz por constructor.
- `IoC/DependencyInjection.cs` conecta la interfaz con la implementación.

No devolver `IQueryable` desde DAL a BLL ni exponer entidades directamente como respuestas HTTP. Usar mapeo manual explícito entre entidades y DTOs al inicio.

## Estructura objetivo

```text
App/
├── README.md
├── compose.yaml                       # PostgreSQL/PostGIS local
├── backend/
│   ├── Marketplace.sln
│   ├── src/
│   │   ├── Marketplace.Api/
│   │   ├── Marketplace.BLL/
│   │   ├── Marketplace.DAL/
│   │   ├── Marketplace.IoC/
│   │   ├── Marketplace.DTOs/
│   │   ├── Marketplace.Models/
│   │   └── Marketplace.Utility/       # Normalización de imágenes
│   ├── tests/
│   │   ├── Marketplace.BLL.Tests/
│   │   ├── Marketplace.DAL.Tests/
│   │   └── Marketplace.Api.Tests/
│   └── tools/
│       └── Marketplace.Evaluation/    # Al implementar evaluación
└── frontend/
    ├── src/
    │   ├── app/
    │   ├── features/
    │   ├── components/
    │   └── lib/
    └── tests/
```

Agrupar por funcionalidad dentro de los proyectos cuando crezcan. No crear clases o carpetas vacías solamente para completar el diagrama.

## Patrones y pruebas

| Elección | Uso concreto | Evidencia esperada |
| --- | --- | --- |
| Service Layer | Servicios BLL para casos de uso | Controllers pequeños y reglas probadas en BLL |
| Inyección de dependencias | Dependencias recibidas por constructor | Tests BLL sin base ni proveedores reales |
| Adapter | OpenAI y almacenamiento externo | Sustitución por dobles de prueba y fallback manual |
| Strategy | Variantes de ranking para evaluación | Comparaciones reproducibles sobre los mismos datos |

Usar repositorios específicos solamente donde sean necesarios. No agregar un repositorio genérico ni un Unit of Work adicional por defecto: EF Core ya proporciona seguimiento de cambios y una unidad de trabajo mediante DbContext.

- **BLL.Tests:** reglas de publicación, autorización sobre recursos, ranking y límites por comercio, con dobles de prueba.
- **DAL.Tests:** consultas reales contra PostgreSQL/PostGIS de pruebas, incluyendo búsqueda textual y espacial; no sustituir estas pruebas por EF InMemory.
- **Api.Tests:** rutas, contratos, autenticación y códigos HTTP mediante servidor de pruebas; base aislada cuando corresponda.
- **Evaluation:** escenarios con semillas controladas y registro de exposición para medir equidad y relevancia.

La autorización HTTP no reemplaza la comprobación de propiedad de los productos en el caso de uso. La IA genera sugerencias editables, requiere confirmación de publicación y debe permitir continuar manualmente si falla; no participa del ranking.

## Integraciones externas

`Marketplace.Integrations` se incorporó el 05/10/2026 con el adaptador OpenAI de
borradores de catálogo, desactivado hasta configurar modelo/clave. Implementa una
interfaz BLL y se registra desde IoC. No se ubican clientes en DAL ni Utility.

Este proyecto adicional se incorpora cuando exista la funcionalidad, no como requisito para el primer esqueleto.

## Despliegue y mantenimiento

Los proyectos .NET son unidades de organización del código, no servicios desplegados por separado. El backend se publica como una sola aplicación, con frontend y base de datos separados.

La recomendación previa de Vercel para Next.js, Render para ASP.NET Core y Supabase para datos/almacenamiento sigue siendo una **alternativa pendiente de confirmación**, no una contratación ni una decisión de proveedor. Antes de elegir se deben revisar presupuesto, disponibilidad durante la defensa, extensiones PostGIS y pg_trgm, backups y restricciones actuales de los planes.

Mantener un repositorio, main estable, ramas cortas, cambios pequeños, CI, migraciones versionadas y configuración de ejemplo sin secretos. No iniciar Git, publicar ni contratar servicios como efecto de este ADR.

## Consecuencias y alternativas

- Más proyectos y referencias que Api/Core/Infrastructure, a cambio de una nomenclatura familiar para la evaluación académica.
- Testing independiente de la base gracias a las interfaces de BLL; la estructura por sí sola no garantiza esa independencia.
- DTOs puede volverse compartido por demasiadas funcionalidades: organizar sus contratos por caso de uso y evitar tipos universales.
- Utility puede perder cohesión: extraer solamente funciones realmente transversales, no lógica de dominio.
- Se descartan por ahora microservicios, CQRS/MediatR, event bus y Kubernetes por no justificar su costo en el alcance inicial.

## Próximo paso

Acordar la primera entrega técnica: esqueleto compilable del backend o backend más frontend. Antes de generarlo, verificar herramientas disponibles y fijar versiones compatibles. Probar las referencias y el arranque sin introducir funcionalidades de negocio todavía.
