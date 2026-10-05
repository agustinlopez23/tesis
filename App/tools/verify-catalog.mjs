import { spawn } from "node:child_process";
import { once } from "node:events";
import { mkdir, readFile, writeFile, access } from "node:fs/promises";
import { randomBytes, randomUUID } from "node:crypto";
import { resolve } from "node:path";
import assert from "node:assert/strict";

const root = resolve(import.meta.dirname, "../..");
const salesEnabled = process.argv.includes("--sales");
const ordersEnabled = process.argv.includes("--orders") || salesEnabled;
const artifacts = resolve(root, salesEnabled ? ".tools/sales" : ordersEnabled ? ".tools/orders" : ".tools/catalog");
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
async function request(path, method = "GET", body, cookie, headers = {}) {
  return fetch(webOrigin + "/api/marketplace" + path, { method, headers: { Origin: webOrigin,
    "X-Requested-With": "marketplace", "Content-Type": "application/json", ...(cookie ? { Cookie: cookie } : {}), ...headers },
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
  let orderInfo;
  if (ordersEnabled) {
    const requestId = randomUUID(), accessKey = randomBytes(32).toString("hex");
    const body = { requestId, accessKey, customerName: "Cliente de prueba", lines: [{ productId: product, quantity: 2, expectedVersion: 3 }] };
    const path = `/public/businesses/${business}/orders`;
    const created = await request(path, "POST", body);
    assert.equal(created.status, 201);
    const saved = await created.json();
    assert.equal(saved.totals.ARS, 25000);
    assert.equal((await request(path, "POST", body)).status, 200);
    assert.equal((await request(`/public/orders/${requestId}`)).status, 404);
    const read = () => request(`/public/orders/${requestId}`, "GET", undefined, undefined, { "X-Order-Key": accessKey });
    assert.equal((await read()).status, 200);
    assert.equal((await request(productPath, "PUT", { ...value, version: 3, price: 13000 }, owner.cookie)).status, 200);
    assert.equal((await (await read()).json()).totals.ARS, 25000);
    const filtered = await request(`/businesses/${business}/orders?reference=${saved.reference}&status=pending`, "GET", undefined, owner.cookie);
    assert.equal((await filtered.json()).items.length, 1);
    await sql(`UPDATE business_subscriptions SET revoked_at=now(), revoked_by=granted_by, revocation_reason='Order test' WHERE business_id='${business}' AND revoked_at IS NULL`);
    assert.equal((await request(path, "POST", body)).status, 200);
    assert.equal((await request(path, "POST", { ...body, requestId: randomUUID() })).status, 403);
    assert.equal((await request(`/businesses/${business}/orders/${requestId}`, "PUT", { version: 1, status: "in_attention", contactReceived: true }, owner.cookie)).status, 200);
    assert.equal((await (await read()).json()).status, "in_attention");
    assert.equal((await request(`/businesses/${business}/orders/${requestId}`, "PUT", { version: 2, status: "cancelled", contactReceived: true }, owner.cookie)).status, 200);
    assert.equal((await (await read()).json()).canContact, false);
    assert.equal(Number((await sql(`SELECT count(*) FROM customer_orders WHERE business_id='${business}'`)).trim()), 1);
    assert.equal((await request(`/admin/businesses/${business}/subscriptions/${randomUUID()}`, "PUT", period, admin.cookie)).status, 200);
    orderInfo = { id: requestId, key: accessKey, reference: saved.reference };
  }
  let salesInfo;
  if (salesEnabled) {
    const inventory = `/businesses/${business}/inventory`;
    const adjustment = `${inventory}/adjustments/${randomUUID()}`;
    const opening = { productId: product, delta: 5, version: 0, reason: "Saldo inicial verificado" };
    assert.equal((await request(adjustment, "PUT", opening, owner.cookie)).status, 200);
    assert.equal((await request(adjustment, "PUT", opening, owner.cookie)).status, 200);
    const orderId = randomUUID(), key = randomBytes(32).toString("hex");
    const source = await request(`/public/businesses/${business}/orders`, "POST", { requestId: orderId, accessKey: key, customerName: "Venta integrada", lines: [{ productId: product, quantity: 2, expectedVersion: 4 }] });
    assert.equal(source.status, 201);
    const sourceOrder = await source.json();
    const saleId = randomUUID(), path = `/businesses/${business}/sales/${saleId}`;
    const body = { orderId, orderVersion: 1, lines: [{ productId: product, quantity: 2, unitPrice: 12000, currency: "ARS", origin: "requested" }] };
    assert.equal((await request(path, "PUT", body, owner.cookie)).status, 201);
    assert.equal((await request(path, "PUT", body, owner.cookie)).status, 200);
    assert.equal((await (await request(inventory, "GET", undefined, owner.cookie)).json())[0].quantity, 3);
    assert.equal((await (await request(`/public/orders/${orderId}`, "GET", undefined, undefined, { "X-Order-Key": key })).json()).status, "converted");
    const commercePath = `/businesses/${business}/commerce?days=7`;
    const commerce = await request(commercePath, "GET", undefined, owner.cookie);
    assert.equal(commerce.headers.get("cache-control"), "no-store");
    const metrics = await commerce.json();
    assert.equal(metrics.orders, 2); assert.equal(metrics.converted, 1); assert.equal(metrics.conversionRate, 0.5);
    assert.equal(metrics.amounts.requested.ARS, 24000);
    const oversized = { lines: [{ productId: product, quantity: 4, unitPrice: 12000, currency: "ARS", origin: "direct" }] };
    assert.equal((await request(`/businesses/${business}/sales/${randomUUID()}`, "PUT", oversized, owner.cookie)).status, 409);
    const voidBody = { operationId: randomUUID(), reason: "Una unidad fue devuelta", returns: [{ productId: product, quantity: 1 }] };
    assert.equal((await request(path + "/void", "PUT", voidBody, owner.cookie)).status, 200);
    assert.equal((await request(path + "/void", "PUT", voidBody, owner.cookie)).status, 200);
    assert.equal((await (await request(inventory, "GET", undefined, owner.cookie)).json())[0].quantity, 4);
    assert.equal((await (await request(`/public/orders/${orderId}`, "GET", undefined, undefined, { "X-Order-Key": key })).json()).status, "cancelled");
    const afterVoid = await (await request(commercePath, "GET", undefined, owner.cookie)).json();
    assert.equal(afterVoid.activeSales, 0); assert.equal(afterVoid.voidedSales, 1);
    assert.equal(afterVoid.converted, 0); assert.deepEqual(afterVoid.amounts, {});
    assert.equal(Number((await sql(`SELECT count(*) FROM sales WHERE business_id='${business}'`)).trim()), 1);
    assert.equal(Number((await sql(`SELECT count(*) FROM inventory_movements WHERE business_id='${business}'`)).trim()), 3);
    salesInfo = { saleId, orderId, key, reference: sourceOrder.reference };
    const found = await request("/public/products?q=cafe&lat=-31.416281&lon=-64.184022&radiusKm=1");
    assert.equal(found.status, 200); assert.equal(found.headers.get("cache-control"), "no-store");
    const discovery = await found.json(); assert.equal(discovery.items[0].id, product); assert.equal(discovery.order, "distance");
    assert.equal(discovery.items[0].distanceKm, 0);
    assert.equal((await (await request("/public/products?q=cafe&lat=0&lon=0&radiusKm=1")).json()).items.length, 0);
  }
  const stopFile = resolve(artifacts, `stop-${database}`);
  await writeFile(resolve(artifacts, "session.json"), JSON.stringify({ webOrigin, business, product, owner, admin, stopFile, orderInfo, salesInfo }, null, 2));
  await writeFile(resolve(artifacts, "result.json"), JSON.stringify({ passed: true, database, checks: ["proxy", "plan", "authorization", "catalog", "archive", "revocation", "preservation", ...(process.argv.includes("--orders") ? ["order-snapshot", "order-replay", "private-key", "owner-attention", "contact-received", "cancellation", "order-after-plan-expiry"] : [])], at: new Date().toISOString() }, null, 2));
  if (salesEnabled) await writeFile(resolve(artifacts, "sales-result.json"), JSON.stringify({ passed: true, checks: ["opening-replay", "linked-sale", "sale-replay", "atomic-stock", "overselling-rejected", "explicit-return", "void-replay", "private-order-state"], at: new Date().toISOString() }, null, 2));
  console.log((salesEnabled ? "Catalog, orders and sales" : ordersEnabled ? "Catalog and orders" : "Catalog") + " integration passed; isolated browser session ready at " + webOrigin);
  if (process.argv.includes("--serve")) for (let i = 0; i < 1200; i++) {
    try { await access(stopFile); break; } catch {}
    await wait(1000);
  }
} finally {
  for (const child of [...children].reverse()) await stop(child);
  if (created) { created = false; await sql(`DROP DATABASE IF EXISTS "${database}" WITH (FORCE)`); }
  await writeFile(resolve(artifacts, "logs.json"), JSON.stringify(logs, null, 2));
}
