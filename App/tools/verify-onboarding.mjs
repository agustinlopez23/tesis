import { spawn } from "node:child_process";
import { once } from "node:events";
import { readFile, writeFile } from "node:fs/promises";
import { randomBytes } from "node:crypto";
import { resolve } from "node:path";

const root = process.cwd();
const appRoot = resolve(root, "App");
const composeArgs = ["compose", "--env-file", resolve(appRoot, ".env.local"), "-f", resolve(appRoot, "compose.yaml")];
const databaseName = `onboarding_smoke_${Date.now()}_${randomBytes(4).toString("hex")}`;
const children = [];
const logs = { api: "", next: "" };
let databaseCreated = false;

function start(name, command, args, cwd, extra = {}) {
  const child = spawn(command, args, { cwd, env: { ...process.env, ...extra }, windowsHide: true });
  children.push(child);
  child.stdout.on("data", (chunk) => { logs[name] += chunk; });
  child.stderr.on("data", (chunk) => { logs[name] += chunk; });
  return child;
}

async function run(command, args, cwd = root, extra = {}) {
  const child = spawn(command, args, { cwd, env: { ...process.env, ...extra }, windowsHide: true });
  let output = "";
  child.stdout.on("data", (chunk) => { output += chunk; });
  child.stderr.on("data", (chunk) => { output += chunk; });
  const [code] = await once(child, "exit");
  if (code !== 0) throw new Error(`${command} terminó con ${code}: ${output}`);
  return output;
}

async function stop(child) {
  if (child.exitCode === null && child.signalCode === null) {
    const exited = once(child, "exit");
    child.kill();
    await exited;
  }
}

async function waitFor(url) {
  for (let attempt = 0; attempt < 80; attempt++) {
    try {
      const response = await fetch(url, { cache: "no-store", signal: AbortSignal.timeout(1_500) });
      if (response.ok) return;
    } catch {}
    await new Promise((resolvePromise) => setTimeout(resolvePromise, 250));
  }
  throw new Error(`No inició ${url}: ${JSON.stringify(logs)}`);
}

function expectStatus(response, expected, step) {
  if (response.status !== expected) throw new Error(`${step}: HTTP ${response.status}, se esperaba ${expected}`);
}

async function proxy(path, method = "GET", body, cookie) {
  const headers = { Origin: "http://localhost:3000", "X-Requested-With": "marketplace" };
  if (body !== undefined) headers["Content-Type"] = "application/json";
  if (cookie) headers.Cookie = cookie;
  return fetch(`http://localhost:3000/api/marketplace${path}`, {
    method, headers, body: body === undefined ? undefined : JSON.stringify(body), cache: "no-store",
  });
}

try {
  const envText = await readFile(resolve(appRoot, ".env.local"), "utf8");
  const password = envText.split(/\r?\n/).find((line) => line.startsWith("POSTGRES_PASSWORD="))?.slice(18);
  if (!password) throw new Error("Falta POSTGRES_PASSWORD en App/.env.local");

  await run("docker", [...composeArgs, "exec", "-T", "database", "psql", "-U", "marketplace", "-d", "postgres", "-v", "ON_ERROR_STOP=1", "-c", `CREATE DATABASE "${databaseName}"`]);
  databaseCreated = true;
  const connection = `Host=127.0.0.1;Port=55432;Database=${databaseName};Username=marketplace;Password=${password}`;
  const dotnet = resolve(root, ".tools/dotnet/dotnet.exe");
  const apiDll = resolve(appRoot, "backend/src/Marketplace.Api/bin/Release/net10.0/Marketplace.Api.dll");
  const apiEnvironment = { DOTNET_ROOT: resolve(root, ".tools/dotnet"), ConnectionStrings__Marketplace: connection };
  await run(dotnet, [apiDll, "--migrate"], resolve(appRoot, "backend/src/Marketplace.Api"), apiEnvironment);

  start("api", dotnet, [apiDll], resolve(appRoot, "backend/src/Marketplace.Api"), {
    ...apiEnvironment, ASPNETCORE_URLS: "http://localhost:5080", ASPNETCORE_ENVIRONMENT: "Development",
  });
  await waitFor("http://localhost:5080/health/live");
  start("next", process.execPath, ["node_modules/next/dist/bin/next", "start", "-p", "3000"], resolve(appRoot, "frontend"), {
    NEXT_PUBLIC_API_URL: "http://localhost:5080", API_BASE_URL: "http://localhost:5080",
    APP_ORIGIN: "http://localhost:3000", NEXT_TELEMETRY_DISABLED: "1",
  });
  await waitFor("http://localhost:3000");

  const email = `smoke-${randomBytes(6).toString("hex")}@example.test`;
  expectStatus(await proxy("/auth/register", "POST", { email, displayName: "Prueba integrada", password: "Local-smoke-password-123" }), 201, "registro");
  const login = await proxy("/auth/login", "POST", { email, password: "Local-smoke-password-123" });
  expectStatus(login, 200, "login");
  const cookie = login.headers.get("set-cookie")?.split(";", 1)[0];
  if (!cookie) throw new Error("Login sin cookie de sesión");
  expectStatus(await proxy("/auth/session", "GET", undefined, cookie), 200, "sesión");
  const categoriesResponse = await proxy("/business-categories", "GET", undefined, cookie);
  expectStatus(categoriesResponse, 200, "categorías");
  const categoryId = (await categoriesResponse.json())[0].id;
  const create = await proxy("/businesses", "POST", {
    name: "Negocio de prueba", description: "Se elimina al finalizar", address: "Córdoba", categoryId,
    whatsapp: "+5493515551234", latitude: -31.4167, longitude: -64.1833, status: "draft",
  }, cookie);
  expectStatus(create, 201, "alta de negocio");
  const created = await create.json();
  const update = await proxy(`/businesses/${created.id}`, "PUT", { ...created, name: "Negocio actualizado" }, cookie);
  expectStatus(update, 200, "edición de negocio");
  const read = await proxy(`/businesses/${created.id}`, "GET", undefined, cookie);
  expectStatus(read, 200, "consulta de negocio");
  if ((await read.json()).name !== "Negocio actualizado") throw new Error("La edición no quedó persistida");
  expectStatus(await proxy("/auth/logout", "POST", undefined, cookie), 204, "logout");
  expectStatus(await proxy("/auth/session", "GET", undefined, cookie), 401, "revocación");

  const result = { utc: new Date().toISOString(), registration: 201, login: 200, businessCreate: 201,
    businessUpdate: 200, logout: 204, revokedSession: 401, temporaryDatabase: true };
  await writeFile(resolve(root, ".tools/onboarding-smoke-result.json"), JSON.stringify(result, null, 2));
  console.log(JSON.stringify(result));
} finally {
  for (const child of children.reverse()) await stop(child);
  if (databaseCreated) {
    await run("docker", [...composeArgs, "exec", "-T", "database", "psql", "-U", "marketplace", "-d", "postgres", "-v", "ON_ERROR_STOP=1", "-c", `DROP DATABASE IF EXISTS "${databaseName}" WITH (FORCE)`]);
  }
}
