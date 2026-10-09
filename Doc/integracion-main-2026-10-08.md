# Integración a main: bloqueo de auditoría

PR: https://github.com/agustinlopez23/tesis/pull/20. Main exige Backend, Frontend
y Secrets aprobados e historial lineal; corresponde squash al integrar la rama.
Backend y Secrets aprobaron el primer intento. Frontend se detuvo en npm audit.

Se actualizan Next y eslint-config-next de 16.3.4 a 16.3.8 y las dependencias
transitivas con actualización compatible. No se usa `npm audit fix --force`,
que propone degradar ESLint de Next a 14.2.35. Las reglas CI no se modifican.

Validación en worktree aislado, sin alterar node_modules de la demo:
`npm ci --ignore-scripts`, 126/126 pruebas, lint y build correctos.
`npm audit --omit=dev --audit-level=low`: cero vulnerabilidades.
La auditoría completa conserva cinco paquetes afectados por un único aviso:
eslint-config-next → @next/eslint-plugin-next → fast-glob → micromatch → braces.
`braces@3.0.3` no tiene parche publicado para
[GHSA-vfj7-8cjw-p6xm](https://github.com/advisories/GHSA-vfj7-8cjw-p6xm).
El aviso describe agotamiento de pila con patrones profundamente anidados.

La fusión sigue bloqueada por la política vigente. Para continuar se necesita
un parche compatible o una excepción de riesgo expresamente autorizada. Propuesta
de excepción aún NO aplicada: solo ese aviso en dependencias de desarrollo,
caducidad 15/10/2026, manteniendo auditoría completa y rechazo de cualquier otro
aviso, dependencia afectada en producción o resultado de auditoría inválido.

Rollback del parche: revertir package.json, package-lock.json y versión en
openspec/config.yaml; reintroduce avisos corregidos. La demo conserva su runtime
anterior y no se despliega ni se modifica la base con esta integración.
