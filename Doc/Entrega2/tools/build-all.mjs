import { spawnSync } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
const here = path.dirname(fileURLToPath(import.meta.url));
for (const file of ['build-model.mjs', 'render-figures.mjs', 'build-bpmn.mjs', 'build-report.mjs']) {
  const result = spawnSync(process.execPath, [path.join(here, file)], { stdio: 'inherit', windowsHide: true });
  if (result.error) throw result.error;
  if (result.status !== 0) process.exit(result.status || 1);
}
