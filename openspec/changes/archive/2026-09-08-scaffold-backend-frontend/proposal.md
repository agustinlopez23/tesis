# Scaffold compilable de backend y frontend

Este cambio establece la primera base técnica ejecutable del marketplace: un backend ASP.NET Core organizado según la arquitectura en capas aceptada y un frontend Next.js con TypeScript, ambos compilables y unidos por una señal mínima de salud/conectividad. La entrega permite validar temprano la arquitectura y el entorno local sin incorporar funcionalidades del negocio ni secretos.

## Intención

Hoy `App/` todavía no contiene una aplicación sobre la cual desarrollar y verificar los casos de uso futuros. Sin un baseline compilable, cada funcionalidad tendría que resolver a la vez estructura, dependencias, configuración y comunicación entre procesos, aumentando el riesgo y el costo de revisión.

La propuesta crea un punto de partida reproducible para quienes implementan y evalúan la tesis. Al finalizar el cambio deberá ser posible instalar dependencias, compilar, ejecutar y comprobar una señal mínima entre frontend y API mediante configuración local documentada.

## Alcance

### Incluido

- Scaffold de una solución backend ASP.NET Core con los proyectos mínimos definidos por ADR-001:
  - `Marketplace.Api`
  - `Marketplace.BLL`
  - `Marketplace.DAL`
  - `Marketplace.IoC`
  - `Marketplace.DTOs`
  - `Marketplace.Models`
- Referencias de proyecto mínimas, justificadas y acíclicas; en particular, `Marketplace.BLL` no dependerá de `Marketplace.DAL`.
- Scaffold de frontend Next.js con TypeScript.
- Una señal de salud del backend y una comprobación mínima, observable desde el frontend, de que la API configurada es alcanzable.
- Comportamiento explícito y comprensible cuando la API no esté disponible, sin ocultar el fallo como éxito.
- Configuración de desarrollo local y archivos de ejemplo sin credenciales ni secretos reales.
- Pruebas mínimas de los comportamientos acordados, desarrolladas con strict TDD, más comandos reproducibles de compilación y ejecución.
- Documentación breve del camino local esperado y de los prerrequisitos.

### Fuera de alcance

- Catálogo, productos, stock, comercios, usuarios, autenticación o autorización.
- Geobúsqueda, ranking, exposición equitativa, pagos, logística, facturación o reglas del marketplace.
- Asistencia de IA, integraciones con proveedores o `Marketplace.Integrations`.
- Persistencia de datos de negocio, repositorios de dominio, migraciones funcionales o datos semilla.
- Contratación o configuración de despliegue en Vercel, Render, Supabase u otro proveedor.
- Microservicios, CQRS/MediatR, event bus, Kubernetes u otra infraestructura no requerida por este slice.
- `Marketplace.Utility`, porque todavía no existe una utilidad concreta que justifique crearlo.
- Inicialización de Git o publicación remota.
- Cualquier implementación dentro de `App/` durante esta fase de propuesta; el scaffold se implementará solo después de aprobar propuesta, specs, diseño y tareas.

## Resultado observable

El camino feliz esperado para el slice es:

1. Una persona configura el entorno local usando valores de ejemplo no secretos.
2. El backend y el frontend restauran dependencias y compilan con versiones compatibles fijadas en el diseño.
3. El backend inicia y publica una señal mínima de salud.
4. El frontend inicia, consulta la URL de API configurada y muestra un estado verificable de conectividad.
5. Si la API no responde, el frontend presenta un estado de indisponibilidad controlado y útil para diagnóstico local.

La ruta, el contrato exacto de la señal, el momento de la consulta y si existe una comprobación separada de PostgreSQL/PostGIS se definirán en specs y diseño. No se usará una respuesta de salud para introducir lógica de negocio.

## Áreas afectadas

| Área | Impacto previsto |
| --- | --- |
| `App/backend/` | Nueva solución y proyectos de la arquitectura en capas, junto con pruebas mínimas. |
| `App/frontend/` | Nueva aplicación Next.js TypeScript y prueba de la experiencia de conectividad. |
| Configuración local | URL pública de la API y valores locales documentados mediante archivos de ejemplo; ningún secreto versionado. |
| PostgreSQL/PostGIS | Tecnología de persistencia confirmada para el proyecto; el grado de participación en la señal de readiness queda por decidir en diseño. |
| Documentación | Instrucciones de instalación, arranque, verificación y límites del scaffold. |
| CI y despliegue | No se contratan servicios; los comandos acordados quedarán aptos para automatización posterior. |

## Dependencias y decisiones por confirmar

- Revalidar y fijar una versión objetivo de .NET compatible con el SDK disponible; el SDK local 9.0.311 no determina por sí solo el target framework.
- Revalidar Next.js y su compatibilidad con Node antes del scaffold. En esta sesión se observó Next.js 16.3.4 con requisito Node `>=20.9.0`, y el entorno local dispone de Node 24.20.0 y npm 11.19.0; estos datos todavía no constituyen una decisión de versionado.
- Precisar en diseño los paquetes y scripts mínimos. No se agregará una dependencia sin responsabilidad inmediata.
- Definir si PostgreSQL/PostGIS participa en este primer indicador de readiness o queda preparado para un slice posterior; si se prueba DAL, se utilizará una instancia real aislada, nunca EF Core InMemory.
- Mantener cada unidad de implementación dentro del presupuesto de revisión de 400 líneas cambiadas; si el scaffold completo lo supera, dividirlo en PRs encadenados que permanezcan compilables.

## Riesgos y mitigaciones

| Riesgo | Mitigación propuesta |
| --- | --- |
| Elegir versiones incompatibles o efímeras | Revalidar compatibilidad y fijar versiones en diseño antes de generar proyectos. |
| Crear capas vacías solo para satisfacer el diagrama | Crear únicamente los proyectos obligatorios y agregar referencias o tipos cuando tengan un uso verificable; omitir `Utility` e integraciones. |
| Acoplar frontend a detalles internos del backend | Definir un contrato HTTP mínimo y estable para salud, sin exponer modelos de dominio. |
| Confundir liveness de la API con disponibilidad total de dependencias | Especificar por separado qué verifica cada señal y representar fallos sin falsos positivos. |
| Filtrar secretos mediante configuración local | Versionar solo plantillas y valores seguros; usar variables de entorno o archivos locales ignorados para valores sensibles futuros. |
| Introducir complejidad de negocio durante el scaffold | Revisar el cambio contra la lista de fuera de alcance y rechazar entidades, servicios o endpoints de marketplace. |
| Superar el presupuesto de revisión | Implementar en slices encadenados: baseline backend, señal verificable y frontend, preservando un estado compilable en cada corte. |
| Ausencia actual de repositorio Git | No inicializarlo implícitamente; dejar cualquier estrategia de ramas o PRs como planificación hasta que exista repositorio. |

## Rollback

Como el cambio solo introduce un scaffold y configuración local, el rollback consiste en revertir los archivos creados por las unidades de implementación afectadas. No habrá datos de negocio, migraciones funcionales ni recursos remotos que restaurar. Cada PR encadenado deberá dejar el workspace compilable para permitir revertir el siguiente corte de forma independiente.

Si una versión o dependencia resulta incompatible antes de integrar el slice, se vuelve al último baseline compilable y se actualiza la decisión de diseño; no se sostendrán adaptaciones ad hoc que contradigan ADR-001.

## Criterios de éxito

- [ ] La solución backend restaura y compila con el comando y las versiones documentadas.
- [ ] Las referencias entre proyectos cumplen ADR-001, son acíclicas y `Marketplace.BLL` no referencia `Marketplace.DAL`.
- [ ] El frontend restaura dependencias, ejecuta sus pruebas y produce un build de producción con comandos documentados.
- [ ] La API expone una señal de salud mínima con contrato verificable automáticamente.
- [ ] El frontend representa tanto la conectividad exitosa con la API como su indisponibilidad esperada.
- [ ] La configuración local puede prepararse desde ejemplos versionados sin incluir secretos.
- [ ] Las pruebas acordadas evidencian ciclos strict TDD y pasan en los runners definidos por el diseño.
- [ ] No se incluyen funcionalidades, entidades ni reglas del marketplace.
- [ ] No se crean `Marketplace.Utility` ni proyectos de integraciones sin una necesidad concreta.
- [ ] La implementación se divide si es necesario para respetar el presupuesto de 400 líneas cambiadas por revisión.

## Propuesta de ronda de preguntas

El modo de ejecución automático no permite esperar respuestas antes de persistir esta propuesta. Estas preguntas buscan mejorar el PRD al descubrir reglas, impacto, casos límite y tradeoffs; pueden responderse, omitirse, corregirse o ampliarse con una segunda ronda antes de aprobar specs y diseño.

1. **Usuario y urgencia:** ¿el principal consumidor del scaffold será quien desarrolla la tesis, quien la evalúa, o ambos con el mismo camino de arranque?
2. **Semántica de salud:** ¿la primera señal debe demostrar solo que la API vive, o también que PostgreSQL/PostGIS está disponible mediante una señal de readiness separada?
3. **Experiencia ante fallos:** cuando la API no esté disponible, ¿alcanza con un estado diagnóstico para desarrollo o la primera pantalla debe ofrecer además una acción de reintento?
4. **Dependencia local:** ¿PostgreSQL/PostGIS debe levantarse desde el primer slice aunque todavía no haya persistencia funcional, o conviene posponerlo hasta el primer comportamiento DAL?
5. **Límite de entrega:** si el presupuesto de 400 líneas obliga a dividir, ¿se prioriza primero el backend saludable y luego la verificación desde frontend, manteniendo ambos cortes compilables?

### Supuestos usados hasta recibir correcciones

- Los destinatarios son tanto la persona desarrolladora como la evaluación académica, por lo que el arranque debe ser reproducible y fácil de demostrar.
- La señal mínima obligatoria prueba liveness de la API y conectividad frontend–API; la readiness de base de datos se decidirá explícitamente sin hacerla pasar por confirmada.
- El frontend mostrará un estado controlado de éxito o indisponibilidad; las decisiones de interacción fina quedan para specs/diseño.
- El scaffold no necesita datos, migraciones ni servicios de negocio.
- Si el límite de revisión lo exige, se aceptan PRs encadenados y autocontenidos en vez de un único cambio grande.

## Próximo paso

Revisar las preguntas y supuestos; luego redactar specs observables para compilación, salud, error de conectividad y configuración, seguidas por un diseño que fije versiones, referencias, scripts, límites de PostgreSQL/PostGIS y cortes de implementación.
