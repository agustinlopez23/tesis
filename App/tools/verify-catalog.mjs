import { spawn } from "node:child_process";
import { once } from "node:events";
import { mkdir, readFile, writeFile, access } from "node:fs/promises";
import { randomBytes, randomUUID } from "node:crypto";
import { resolve } from "node:path";
import assert from "node:assert/strict";

const root = resolve(import.meta.dirname, "../..");
const artifacts = resolve(root, ".tools/catalog");
const database = `catalog_smoke_${Date.now()}_${randomBytes(3).toString("hex")}`;
const compose = ["compose", "--env-file", resolve(root, "App/.env.local"), "-f", resolve(root, "App/compose.yaml")];
const children = [], logs = {};
const webOrigin = "http://localhost:3200", apiOrigin = "http://localhost:5280";
let created = false;
const wait = ms => new Promise(done => setTimeout(done, ms));
await mkdir(artifacts, { recursive: true });
function start(name, command, args, cwd = root, extra = {}) {
  const child = spawn(command, args, { cwd, env: { ...process.env, ...extra }, windowsHide: true });
  children.push(child); logs[name] = "";
  child.stdout.on("data", chunk => logs[name] += chunk);
  child.stderr.on("data", chunk => logs[name] += chunk);
  return child;
}
async function run(command, args, cwd = root, extra = {}) {
  const child = start("command", command, args, cwd, extra);
  const [code] = await once(child, "exit");
  if (code !== 0) throw new Error("Command failed; see local catalog harness logs.");
  return logs.command;
}
async function stop(child) {
  if (child.exitCode !== null || child.signalCode !== null) return;
  const exited = once(child, "exit");
  if (process.platform === "win32") await run("taskkill", ["/pid", String(child.pid), "/T", "/F"]);
  else child.kill("SIGTERM");
  await exited;
}
async function ready(url, child) {
  for (let i = 0; i < 100; i++) {
    if (child.exitCode !== null) throw new Error("Service exited; see local catalog harness logs.");
    try { if ((await fetch(url, { signal: AbortSignal.timeout(1500) })).ok) return; } catch {}
    await wait(250);
  }
  throw new Error("Service startup timed out.");
}
const sql = statement => run("docker", [...compose, "exec", "-T", "database", "psql", "-U", "marketplace", "-d", created ? database : "postgres", "-v", "ON_ERROR_STOP=1", "-tAc", statement]);
async function request(path, method = "GET", body, cookie) {
  return fetch(webOrigin + "/api/marketplace" + path, { method, headers: { Origin: webOrigin,
    "X-Requested-With": "marketplace", "Content-Type": "application/json", ...(cookie ? { Cookie: cookie } : {}) },
    body: body === undefined ? undefined : JSON.stringify(body), signal: AbortSignal.timeout(30000) });
}
try {
  for (const port of [3200, 5280]) {
    let occupied = false;
    try { await fetch(`http://localhost:${port}`, { signal: AbortSignal.timeout(500) }); occupied = true; } catch {}
    assert.equal(occupied, false, `Port ${port} already occupied; leave its service intact.`);
  }
  const env = await readFile(resolve(root, "App/.env.local"), "utf8");
  const password = env.split(/\r?\n/).find(line => line.startsWith("POSTGRES_PASSWORD="))?.slice(18);
  assert.ok(password);
  await sql(`CREATE DATABASE "${database}"`); created = true;
  const dotnet = resolve(root, ".tools/dotnet/dotnet.exe"), apiDir = resolve(root, "App/backend/src/Marketplace.Api");
  const dll = resolve(apiDir, "bin/Release/net10.0/Marketplace.Api.dll");
  const apiEnv = { DOTNET_ROOT: resolve(root, ".tools/dotnet"),
    ConnectionStrings__Marketplace: `Host=127.0.0.1;Port=55432;Database=${database};Username=marketplace;Password=${password}`,
    ASPNETCORE_ENVIRONMENT: "Testing", ASPNETCORE_URLS: apiOrigin, FrontendOrigin: webOrigin, Auth__PermitLimit: "1000" };
  await run(dotnet, [dll, "--migrate"], apiDir, apiEnv);
  const api = start("api", dotnet, [dll], apiDir, apiEnv);
  await ready(apiOrigin + "/health/live", api);
  const web = start("next", process.execPath, ["node_modules/next/dist/bin/next", "start", "-p", "3200"], resolve(root, "App/frontend"), {
    API_BASE_URL: apiOrigin, APP_ORIGIN: webOrigin, NEXT_PUBLIC_API_URL: apiOrigin, NEXT_TELEMETRY_DISABLED: "1" });
  await ready(webOrigin, web);
  async function account(admin) {
    const email = `catalog-${randomBytes(6).toString("hex")}@example.test`, password = `Test-${randomBytes(16).toString("hex")}`;
    assert.equal((await request("/auth/register", "POST", { email, password, displayName: admin ? "Administración local" : "Responsable local" })).status, 201);
    if (admin) await sql(`INSERT INTO user_account_roles (user_id, role_id) SELECT id, 2 FROM user_accounts WHERE email = '${email}'`);
    const login = await request("/auth/login", "POST", { email, password });
    assert.equal(login.status, 200);
    return { email, password, cookie: login.headers.get("set-cookie").split(";", 1)[0] };
  }
  const owner = await account(false), admin = await account(true);
  const createdBusiness = await request("/businesses", "POST", { name: "Café del Patio", description: "Café de especialidad y sabores del barrio.",
    address: "Av. Colón 120 · Córdoba", categoryId: 2, whatsApp: "+5493515550101", latitude: -31.416281, longitude: -64.184022, status: "published" }, owner.cookie);
  assert.equal(createdBusiness.status, 201);
  const business = (await createdBusiness.json()).id, product = randomUUID(), subscription = randomUUID();
  const productPath = `/businesses/${business}/products/${product}`;
  const value = { name: "Café de especialidad", description: "Granos seleccionados, paquete de 250 g.", category: "Café", price: 12500,
    currency: "ARS", available: true, status: "published", version: 0 };
  assert.equal((await request(productPath, "PUT", value, owner.cookie)).status, 403);
  const period = { startsAt: new Date(Date.now() - 60000).toISOString(), endsAt: new Date(Date.now() + 86400000).toISOString(), reason: "Verificación aislada del catálogo" };
  assert.equal((await request(`/admin/businesses/${business}/subscriptions/${subscription}`, "PUT", period, owner.cookie)).status, 403);
  assert.equal((await request(`/admin/businesses/${business}/subscriptions/${subscription}`, "PUT", period, admin.cookie)).status, 200);
  assert.equal((await request(productPath, "PUT", value, owner.cookie)).status, 200);
  assert.equal((await request(productPath, "PUT", value, owner.cookie)).status, 409);
  const visible = await request(`/public/businesses/${business}/products?q=Caf%C3%A9&page=1`);
  assert.equal(visible.headers.get("cache-control"), "no-store");
  assert.equal((await visible.json()).items.length, 1);
  assert.equal((await request(productPath, "PUT", { ...value, version: 1, status: "archived" }, owner.cookie)).status, 200);
  assert.equal((await (await request(`/public/businesses/${business}/products`)).json()).items.length, 0);
  assert.equal((await request(productPath, "PUT", { ...value, version: 2 }, owner.cookie)).status, 200);
  assert.equal((await request(`/admin/businesses/${business}/subscriptions/${subscription}/revoke`, "POST", { reason: "Prueba de baja" }, admin.cookie)).status, 200);
  assert.equal((await request(`/public/businesses/${business}/products`)).status, 404);
  assert.equal((await (await request(`/businesses/${business}/products`, "GET", undefined, owner.cookie)).json()).length, 1);
  assert.equal((await request(`/admin/businesses/${business}/subscriptions/${randomUUID()}`, "PUT", period, admin.cookie)).status, 200);
  const stopFile = resolve(artifacts, `stop-${database}`);
  await writeFile(resolve(artifacts, "session.json"), JSON.stringify({ webOrigin, business, product, owner, admin, stopFile }, null, 2));
  await writeFile(resolve(artifacts, "result.json"), JSON.stringify({ passed: true, database, checks: ["proxy", "plan", "authorization", "catalog", "archive", "revocation", "preservation"], at: new Date().toISOString() }, null, 2));
  console.log("Catalog integration passed; isolated browser session ready at " + webOrigin);
  if (process.argv.includes("--serve")) for (let i = 0; i < 1200; i++) {
    try { await access(stopFile); break; } catch {}
    await wait(1000);
  }
} finally {
  for (const child of [...children].reverse()) await stop(child);
  if (created) { created = false; await sql(`DROP DATABASE IF EXISTS "${database}" WITH (FORCE)`); }
  await writeFile(resolve(artifacts, "logs.json"), JSON.stringify(logs, null, 2));
}
