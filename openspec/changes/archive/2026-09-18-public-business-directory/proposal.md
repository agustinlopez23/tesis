# Guía pública de negocios

El consumidor podrá explorar negocios publicados y abrir su contacto de WhatsApp
sin crear una cuenta. Continúa el onboarding verificado del 8 de septiembre.

Incluye tarjetas con nombre, rubro, descripción, dirección y contacto, paginación
de 20 negocios y estados de carga, vacío y error recuperable. Solo son visibles
negocios publicados de responsables no suspendidos; se excluyen datos de cuenta.

Quedan fuera catálogo, planes, pedidos, geobúsqueda, ranking, exposición e IA.
Depende del onboarding y de PostgreSQL/PostGIS existente, sin nuevos paquetes.
Éxito: publicar/despublicar afecta la guía, el visitante puede contactar y las
pruebas HTTP, interfaz y recorrido integrado verifican el comportamiento.

El alcance final incluye búsqueda textual y rubro, filtros persistidos en URL,
separación entre guía, panel y diagnóstico, y el rediseño documentado en
`Doc/ux-research-2026-09-18.md`. Se mantiene fuera el descubrimiento espacial.
