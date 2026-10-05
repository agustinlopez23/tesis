# Diseño y límites

Cambios solo en frontend, sin paquetes nuevos ni modificaciones de API, BLL, DAL o
base de datos. Se mantiene ADR-001 y se usa el harness existente con base temporal.

Tres unidades independientes, cada una por debajo de 400 líneas:

1. `fe84d4b`: [paneles abiertos](paneles.md), aviso local por negocio tras guardar.
2. `1453aae`: [selección editable](seleccion.md), reconciliación al cargar catálogo.
3. `1979c05`: [acceso y recuperación](sesion.md), tratamiento explícito de 401.

Cada unidad documenta regresión, resultado en ejecución real y rollback. No se
requieren PRs encadenados ni cambios de dependencias para esta corrección local.
Especificaciones actualizadas en sales-inventory, product-orders y onboarding.
