# Integración a main: excepción de auditoría autorizada

PR: https://github.com/agustinlopez23/tesis/pull/20. Main exige Backend, Frontend
y Secrets aprobados e historial lineal; corresponde squash al integrar la rama.
Backend y Secrets aprobaron el primer intento. Frontend se detuvo en npm audit.

Se actualizan Next y eslint-config-next de 16.3.4 a 16.3.8 y las dependencias
transitivas con actualización compatible. No se usa `npm audit fix --force`,
que propone degradar ESLint de Next a 14.2.35.

Validación en worktree aislado, sin alterar node_modules de la demo:
`npm ci --ignore-scripts`, 126/126 pruebas, lint y build correctos.
`npm audit --omit=dev --audit-level=low`: cero vulnerabilidades.
La auditoría completa conserva cinco paquetes afectados por un único aviso:
eslint-config-next → @next/eslint-plugin-next → fast-glob → micromatch → braces.
`braces@3.0.3` no tiene parche publicado para
[GHSA-vfj7-8cjw-p6xm](https://github.com/advisories/GHSA-vfj7-8cjw-p6xm).
El aviso describe agotamiento de pila con patrones profundamente anidados.

El responsable autorizó expresamente la excepción propuesta para completar el PR.
Solo cubre ese aviso y las cinco versiones de desarrollo revisadas, hasta el
15/10/2026 inclusive en Argentina (vence 16/10 a las 03:00 UTC). No se renueva sola.
CI mantiene auditoría de producción sin excepciones y analiza además el informe
completo mediante `npm run audit:ci`. Rechaza cualquier otro aviso, alcance de
producción, versión/nodo/cadena distinta, informe inválido o error del comando.
Si desaparecen todos los avisos, no necesita excepción incluso tras el vencimiento.

`node --test App/tools/audit-policy.test.mjs`: rojo antes de implementar el módulo,
luego 9/9 aprobadas, incluidos el límite horario y casos de rechazo.
`npm --prefix App/frontend run audit:ci`: acepta únicamente la cadena autorizada
del informe real; no declara cero vulnerabilidades. El workflow ejecuta estas
pruebas antes de auditar. La aplicación no cambia en esta unidad.

Para retirar la excepción, actualizar la dependencia cuando exista un parche y
reemplazar los tres pasos de auditoría por `npm audit --audit-level=low`; eliminar
audit-policy.mjs, audit-policy.test.mjs y el script audit:ci. No desactivar Secrets,
Backend o Frontend ni alterar las protecciones de main.

Rollback del parche: revertir package.json, package-lock.json y versión del
proyecto; reintroduce avisos corregidos. La demo conserva su runtime
anterior y no se despliega ni se modifica la base con esta integración.
