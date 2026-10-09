# Borradores opcionales del catálogo

El titular podrá describir hechos del producto y solicitar nombre, descripción y
categoría sugeridos; revisar/editar y aplicar al formulario será explícito, y el
guardado seguirá siendo manual. Precio, stock, publicación y ranking quedan fuera
de la salida. Fallas o proveedor desconfigurado permiten seguir cargando a mano.

Adaptador OpenAI en Marketplace.Integrations, como anticipa ADR-001, con modelo y
clave configurados solo en servidor; deshabilitado por defecto. No se eligió ni
activó una cuenta/proveedor real: esta implementación queda preparada y validada
con transporte simulado, sin llamadas pagas. Endpoint fijo Responses HTTPS,
Structured Outputs, store:false, respuesta acotada y tiempo máximo. Solo se envían
los hechos escritos con consentimiento, sin extraer cuenta, pedidos o métricas.

Propiedad/plan, CSRF, consentimiento, límite de entrada/salida, cuota global y
concurrencia acotados. TDD HTTP/adaptador/UI, fallback y revisión antes de aplicar;
sin migración ni modificación del ranking. Configuración real/evaluación de calidad
externa quedan pendientes hasta contar con proveedor y modelo accesibles.

Fuentes oficiales consultadas 05/10/2026 con OpenAI Docs:
[Structured Outputs](https://developers.openai.com/api/docs/guides/structured-outputs?api-mode=responses),
[Responses y almacenamiento](https://developers.openai.com/api/docs/guides/migrate-to-responses?api-mode=responses.html).
