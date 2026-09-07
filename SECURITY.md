# Seguridad del repositorio

## Prevención local

No versionar secretos aunque el repositorio sea privado. Usar variables de entorno,
`.env.local` o el almacén de secretos del entorno. Los ejemplos versionados deben
contener únicamente valores de desarrollo sin credenciales.

`.gitignore` excluye credenciales, claves, SDK local, dependencias, resultados,
memoria de agentes y documentos académicos originales. Un archivo ya seguido por
Git no queda protegido al agregarlo a `.gitignore`: revisar siempre el índice.

Antes del primer commit en cada clon:

```powershell
git config --local core.hooksPath .githooks
git config --local push.default simple
git config --local fetch.prune true
git diff --cached --stat
git diff --cached
```

Instalar Gitleaks desde sus releases oficiales y verificar el checksum publicado.
Este workspace usa la versión 8.30.1 en `.tools/security/gitleaks/`. Los hooks
bloquean commit/push si falta el scanner o detecta un secreto. El pre-commit revisa
el índice y el pre-push el historial completo. No desactivarlos para sortear un
hallazgo. Las reglas por defecto de Gitleaks están habilitadas, sin allowlists.

Un hook local puede omitirse y no se instala automáticamente al clonar. La revisión
de CI complementa estos controles; ningún scanner garantiza detectar todo secreto.

## GitHub

Mantener el repositorio privado y conceder acceso solo a colaboradores necesarios.
Usar passkey o segundo factor en la cuenta y guardar los códigos de recuperación
fuera del repositorio. No compartir tokens por chats, issues ni archivos.

Los workflows deben usar permisos mínimos de lectura, actions fijadas por SHA,
checkout sin credenciales persistentes y eventos `pull_request` (sin ejecutar
código no confiable con `pull_request_target`). No hay despliegues ni secretos CI.
Dependabot revisa npm, NuGet y GitHub Actions semanalmente; no hay auto-merge.

Activar alertas y actualizaciones de seguridad de Dependabot. La protección de
ramas y el escaneo/protección de secretos de GitHub dependen del plan y la
visibilidad; registrar qué opciones se pudieron activar, sin asumir cobertura.
Para `main`, impedir force-push/borrado y requerir checks cuando el plan lo permita.
En trabajo individual no exigir una aprobación propia imposible de emitir.

## Dependencias

```powershell
# App/frontend
npm ci
npm audit --audit-level=moderate

# App/backend, con SDK fijado disponible
dotnet restore Marketplace.sln -p:NuGetAudit=true -p:NuGetAuditMode=all -m:1
dotnet list Marketplace.sln package --vulnerable --include-transitive --no-restore
```

Revisar advisories y cambios de dependencias antes de integrarlos, ejecutar tests,
lint y builds. No aplicar `npm audit fix --force` sin evaluar el cambio.

## Reportar y responder

Contactar al propietario por un canal privado. No incluir un secreto real en un
issue o pull request. Ante una filtración: revocar/rotar primero, revisar accesos,
retirar el secreto y coordinar la limpieza del historial y clones afectados.
Borrar el archivo del último commit no invalida la credencial ni borra el historial.
