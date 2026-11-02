import { spawn } from 'node:child_process';
import { createServer } from 'node:http';
import { once } from 'node:events';
import { resolve } from 'node:path';
import { writeFile } from 'node:fs/promises';

const root = process.cwd();
const children = [];
const logs = { api: '', next: '' };
let bRequests = 0;
const sentinel = createServer((req, res) => {
  bRequests++;
  res.writeHead(200, { 'content-type': 'application/json' });
  res.end('{"status":"ok"}');
});
function start(name, command, args, cwd, extra = {}) {
  const child = spawn(command, args, { cwd, env: { ...process.env, ...extra }, windowsHide: true });
  children.push(child);
  child.stdout.on('data', chunk => { logs[name] += chunk; });
  child.stderr.on('data', chunk => { logs[name] += chunk; });
  child.on('error', error => { logs[name] += error.message; });
  return child;
}
async function waitFor(url) {
  for (let i = 0; i < 80; i++) {
    try { const response = await fetch(url, { signal: AbortSignal.timeout(1500) }); if (response.ok) return response; } catch {}
    await new Promise(resolve => setTimeout(resolve, 250));
  }
  throw new Error(`No iniciÃ³ ${url}: ${JSON.stringify(logs)}`);
}
async function stop(child) {
  if (child.exitCode === null && child.signalCode === null) {
    const exited = once(child, 'exit');
    child.kill();
    await exited;
  }
}
try {
  await new Promise((resolve, reject) => { sentinel.once('error', reject); sentinel.listen(5081, 'localhost', resolve); });
  const api = start('api', resolve(root, '.tools/dotnet/dotnet.exe'),
    [resolve(root, 'App/backend/src/Marketplace.Api/bin/Release/net10.0/Marketplace.Api.dll')],
    resolve(root, 'App/backend/src/Marketplace.Api'),
    { ASPNETCORE_URLS: 'http://localhost:5080', ASPNETCORE_ENVIRONMENT: 'Development', DOTNET_ROOT: resolve(root, '.tools/dotnet'), 'Logging__LogLevel__Microsoft.AspNetCore': 'Information' });
  const health = await waitFor('http://localhost:5080/health/live');
  if (await health.text() !== '{"status":"ok"}') throw new Error('Contrato API incorrecto');
  start('next', process.execPath, ['node_modules/next/dist/bin/next', 'start', '-p', '3000'],
    resolve(root, 'App/frontend'), { NEXT_PUBLIC_API_URL: 'http://localhost:5081', NEXT_TELEMETRY_DISABLED: '1' });
  const available = await (await waitFor('http://localhost:3000/estado')).text();
  if (!available.includes('API disponible')) throw new Error('No muestra disponibilidad');
  if (bRequests !== 0) throw new Error(`El artefacto consultÃ³ B ${bRequests} veces`);
  await stop(api);
  const unavailable = await (await fetch('http://localhost:3000/estado')).text();
  if (!unavailable.includes('API no disponible')) throw new Error('No muestra indisponibilidad');
  const result = { utc: new Date().toISOString(), health: 200, available: true, unavailable: true,
    buildUrl: 'http://localhost:5080', startUrl: 'http://localhost:5081', bRequests, logs };
  await writeFile(resolve(root, '.tools/smoke-result.json'), JSON.stringify(result, null, 2));
  console.log(JSON.stringify({ ...result, logs: undefined }));
} finally {
  for (const child of children) await stop(child);
  sentinel.closeAllConnections();
  await new Promise(resolve => sentinel.close(resolve));
}
