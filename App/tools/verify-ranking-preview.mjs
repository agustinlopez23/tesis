import { spawn } from "node:child_process";
import { once } from "node:events";
import { mkdir, readFile, writeFile, access } from "node:fs/promises";
import { randomBytes } from "node:crypto";
import { resolve } from "node:path";
import assert from "node:assert/strict";

// Uses only a generated database and dedicated ports. --serve keeps them alive for browser review.
const root = resolve(import.meta.dirname, "../..");
const artifacts = resolve(root, ".tools/ranking-preview");
const database = `ranking_preview_${Date.now()}_${randomBytes(3).toString("hex")}`;
const compose = ["compose", "--env-file", resolve(root, "App/.env.local"), "-f", resolve(root, "App/compose.yaml")];
const children = [];
const logs = {};
let created = false;
const wait = ms => new Promise(done => setTimeout(done, ms));
await mkdir(artifacts, { recursive: true });

function start(name, command, args, cwd = root, extra = {}) {
  const child = spawn(command, args, { cwd, env: { ...process.env, ...extra }, windowsHide: true });
  children.push(child);
  logs[name] = "";
  child.stdout.on("data", data => logs[name] += data);
  child.stderr.on("data", data => logs[name] += data);
  return child;
}
async function run(command, args, cwd = root, extra = {}) {
  const child = start("command", command, args, cwd, extra);
  const [code] = await once(child, "exit");
  if (code !== 0) throw new Error(`Command failed (${code}); see local harness logs.`);
  return logs.command;
}
async function stop(child) {
  if (child.exitCode === null && child.signalCode === null) {
    const exited = once(child, "exit");
    if (process.platform === "win32") await run("taskkill", ["/pid", String(child.pid), "/T", "/F"]);
    else child.kill("SIGTERM");
    await exited;
  }
}
async function ready(url, child) {
  for (let attempt = 0; attempt < 120; attempt++) {
    if (child.exitCode !== null) throw new Error("Service exited; see local harness logs.");
    try { if ((await fetch(url, { signal: AbortSignal.timeout(2000) })).ok) return; } catch { /* startup */ }
    await wait(250);
  }
  throw new Error(`Service did not start: ${url}`);
}
const sql = statement => run("docker", [...compose, "exec", "-T", "database", "psql", "-U", "marketplace",
  "-d", created ? database : "postgres", "-v", "ON_ERROR_STOP=1", "-tAc", statement]);
const webOrigin = "http://localhost:3100";
const apiOrigin = "http://localhost:5180";
async function request(base, path, body, cookie) {
  return fetch(base + path, { method: body ? "POST" : "GET", headers: {
    Origin: webOrigin, "X-Requested-With": "marketplace", "Content-Type": "application/json", ...(cookie ? { Cookie: cookie } : {}),
  }, body: body ? JSON.stringify(body) : undefined, signal: AbortSignal.timeout(30000) });
}

try {
  for (const port of [3100, 3101, 5180]) {
    let occupied = false;
    try { await fetch(`http://localhost:${port}`, { signal: AbortSignal.timeout(500) }); occupied = true; } catch { /* unused */ }
    assert.equal(occupied, false, `Port ${port} already occupied; leave its service intact.`);
  }
  const env = await readFile(resolve(root, "App/.env.local"), "utf8");
  const password = env.split(/\r?\n/).find(line => line.startsWith("POSTGRES_PASSWORD="))?.slice(18);
  assert.ok(password, "Missing local database configuration");
  await sql(`CREATE DATABASE "${database}"`);
  created = true;
  const dotnet = resolve(root, ".tools/dotnet/dotnet.exe");
  const apiDir = resolve(root, "App/backend/src/Marketplace.Api");
  const dll = resolve(apiDir, "bin/Release/net10.0/Marketplace.Api.dll");
  const apiEnv = { DOTNET_ROOT: resolve(root, ".tools/dotnet"),
    ConnectionStrings__Marketplace: `Host=127.0.0.1;Port=55432;Database=${database};Username=marketplace;Password=${password}`,
    ASPNETCORE_ENVIRONMENT: "Development", ASPNETCORE_URLS: apiOrigin, FrontendOrigin: webOrigin,
    Ranking__PreviewEnabled: "true", Auth__PermitLimit: "1000" };
  await run(dotnet, [dll, "--migrate"], apiDir, apiEnv);
  const api = start("api", dotnet, [dll], apiDir, apiEnv);
  await ready(apiOrigin + "/health/live", api);
  const email = `preview-${randomBytes(6).toString("hex")}@example.test`;
  const ownerPassword = `Local-${randomBytes(16).toString("hex")}`;
  assert.equal((await request(apiOrigin, "/auth/register", { email, displayName: "Prueba local", password: ownerPassword })).status, 201);
  const login = await request(apiOrigin, "/auth/login", { email, password: ownerPassword });
  const cookie = login.headers.get("set-cookie")?.split(";", 1)[0];
  assert.ok(cookie);
  const categories = await (await request(apiOrigin, "/business-categories")).json();
  const categoryId = categories[0].id;
  const names = ["Almacén del Patio", "Despensa La Esquina", "Mercado Las Flores", "Sabores de Córdoba", "La Canasta del Barrio"];
  for (let index = 0; index < 45; index++) {
    const response = await request(apiOrigin, "/businesses", {
      name: index < names.length ? names[index] : `Almacén de barrio ${String(index + 1).padStart(2, "0")}`,
      description: "Productos regionales, conservas y sabores para todos los días. Un comercio local, a la vuelta de tu casa.",
      address: `Av. Colón ${120 + index * 10} · Córdoba`, categoryId, whatsApp: "+5493515550101",
      latitude: -31.416281, longitude: -64.184022, status: "published",
    }, cookie);
    assert.equal(response.status, 201);
  }
  const frontend = resolve(root, "App/frontend");
  const nextEnv = { API_BASE_URL: apiOrigin, APP_ORIGIN: webOrigin, NEXT_PUBLIC_API_URL: apiOrigin,
    RANKING_PREVIEW_ENABLED: "true", NEXT_TELEMETRY_DISABLED: "1" };
  const production = start("production", process.execPath, ["node_modules/next/dist/bin/next", "start", "-p", "3101"], frontend, nextEnv);
  await ready("http://localhost:3101", production);
  const closed = await fetch("http://localhost:3101/revista");
  assert.equal(closed.status, 404);
  for (const [path, body] of [["/public/ranking-snapshots", { requestId: crypto.randomUUID() }], ["/public/ranking-snapshots/page?cursor=invalid", undefined]]) {
    const response = await request("http://localhost:3101/api/marketplace", path, body);
    assert.equal(response.status, 404);
    assert.equal(response.headers.get("cache-control"), "no-store");
  }
  await stop(production);
  const web = start("next", process.execPath, ["node_modules/next/dist/bin/next", "dev", "-p", "3100"], frontend, nextEnv);
  await ready(webOrigin, web);
  assert.equal((await fetch(webOrigin + "/revista")).status, 200);
  const body = { requestId: crypto.randomUUID(), zoneId: "centro", categoryId, radiusKm: 4 };
  const proxy = webOrigin + "/api/marketplace";
  const response = await request(proxy, "/public/ranking-snapshots", body);
  assert.equal(response.status, 201);
  assert.equal(response.headers.get("cache-control"), "no-store");
  const first = await response.json();
  const visitId = crypto.randomUUID();
  const token = first.measurement[first.items[0].id];
  for (const [kind, surface] of [["impression", "directory"], ["profile_view", "profile"], ["contact", "profile"]]) {
    for (let retry = 0; retry < 2; retry++)
      assert.equal((await request(proxy, "/public/events", { token, visitId, kind, surface })).status, 204);
  }
  assert.equal(Number((await sql("SELECT count(*) FROM business_activity WHERE length(ranking_version) = 64")).trim()), 3);
  const read = async cursor => {
    const result = await request(proxy, "/public/ranking-snapshots/page?cursor=" + encodeURIComponent(cursor));
    assert.equal(result.status, 200);
    return result.json();
  };
  const second = await read(first.nextCursor);
  const third = await read(second.nextCursor);
  assert.deepEqual([first, second, third].map(page => page.items.length), [20, 20, 5]);
  assert.equal(new Set([first, second, third].flatMap(page => page.items.map(item => item.id))).size, 45);
  assert.deepEqual((await read(first.cursor)).items, first.items);
  assert.equal((await request(proxy, "/public/ranking-snapshots", body)).status, 200);
  assert.equal(Number((await sql("SELECT count(*) FROM ranking_opportunities")).trim()), 1);
  const result = { database, url: webOrigin + "/revista", categoryId, pages: [20, 20, 5],
    productionClosed: true, opportunitiesAfterReplay: 1, measuredEvents: 3, stopFile: resolve(artifacts, database + ".stop") };
  await writeFile(resolve(artifacts, "runtime.json"), JSON.stringify(result, null, 2));
  console.log(JSON.stringify(result));
  if (process.argv.includes("--serve")) {
    const until = Date.now() + 20 * 60 * 1000;
    while (Date.now() < until) {
      try { await access(result.stopFile); break; } catch { /* waiting for browser review */ }
      await wait(500);
    }
  }
} finally {
  for (const child of [...children].reverse()) await stop(child);
  if (created) {
    created = false;
    await sql(`DROP DATABASE IF EXISTS "${database}" WITH (FORCE)`);
  }
  await writeFile(resolve(artifacts, "logs.json"), JSON.stringify(logs, null, 2).replace(/cursor=[^\s"&\\]+/g, "cursor=[redacted]"));
}
