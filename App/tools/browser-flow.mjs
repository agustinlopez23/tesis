import assert from "node:assert/strict";
import { writeFile } from "node:fs/promises";
import { join } from "node:path";
import { chromium, firefox, webkit } from "playwright";

const engines = { chromium, firefox, webkit };
export async function verifyBrowserFlow(session, artifacts, names = Object.keys(engines)) {
  assert.equal(session.webOrigin, "http://localhost:3200", "Only the isolated harness is allowed.");
  const results = [];
  try {
    for (const name of names) {
      console.log(`Browser flow: ${name}`);
      const result = { browser: name, passed: false, checks: [] };
      results.push(result);
      const browser = await engines[name].launch();
      try {
        result.version = browser.version();
        await exercise(browser, session, artifacts, result);
        result.passed = true;
        console.log(`Browser flow passed: ${name} (${result.checks.length} checks)`);
      } finally { await browser.close(); }
    }
  } finally {
    await writeFile(join(artifacts, "browser-result.json"), JSON.stringify({ at: new Date().toISOString(), results }, null, 2));
  }
}

async function exercise(browser, s, artifacts, result) {
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 }, locale: "es-AR", timezoneId: "America/Argentina/Buenos_Aires" });
  const page = await context.newPage();
  const errors = [];
  page.on("pageerror", error => errors.push(error.message));
  page.setDefaultTimeout(15000);
  async function login(account) {
    await page.goto(s.webOrigin + "/mi-negocio");
    await page.getByRole("button", { name: "Ya tengo cuenta" }).click();
    await page.getByLabel("Correo", { exact: true }).fill(account.email);
    await page.getByLabel("Contraseña", { exact: true }).fill(account.password);
    await page.getByRole("button", { name: "Ingresar", exact: true }).click();
    await page.getByRole("button", { name: "Cerrar sesión", exact: true }).waitFor();
  }
  async function logout() {
    await page.getByRole("button", { name: "Cerrar sesión", exact: true }).click();
    await page.getByRole("button", { name: "Crear cuenta", exact: true }).waitFor();
  }
  try {
    await page.goto(s.webOrigin + "/mi-negocio");
    await page.getByRole("button", { name: "Ya tengo cuenta" }).click();
    await page.getByLabel("Correo", { exact: true }).fill(s.owner.email);
    await page.getByLabel("Contraseña", { exact: true }).fill("Incorrecta-de-prueba-123");
    await page.getByRole("button", { name: "Ingresar", exact: true }).click();
    await page.getByText("Revisá el correo y la contraseña e intentá nuevamente.").waitFor();
    result.checks.push("invalid-login-message");

    await page.goto(`${s.webOrigin}/negocios/${s.business}`);
    await page.getByRole("button", { name: "Agregar Café de especialidad" }).click();
    await page.getByLabel("Cantidad de Café de especialidad").fill("2");
    await page.getByLabel("Tu nombre o alias").fill(`Cliente ${result.browser}`);
    let saved, sent;
    await page.route("**/public/businesses/*/orders", async route => {
      sent = route.request().postData();
      const response = await route.fetch();
      assert.equal(response.status(), 201);
      saved = await response.json();
      await route.abort("failed");
    }, { times: 1 });
    await page.getByRole("button", { name: "Confirmar solicitud", exact: true }).click();
    await page.locator(".order-checkout [role=alert]").waitFor();
    await page.getByRole("button", { name: "Actualizar catálogo" }).click();
    const recovery = page.waitForResponse(r => r.request().method() === "POST" && r.url().endsWith("/orders"));
    await page.getByRole("button", { name: "Reintentar solicitud", exact: true }).click();
    const response = await recovery;
    assert.equal(response.status(), 200);
    assert.ok(response.request().postData() === sent, "Retry changed the saved payload.");
    assert.equal((await response.json()).id, saved.id);
    const tracking = await page.getByRole("link", { name: "Consultar mi solicitud" }).getAttribute("href");
    const whatsapp = await page.getByRole("link", { name: "Abrir WhatsApp", exact: true }).getAttribute("href");
    assert.ok(!whatsapp.includes(JSON.parse(sent).accessKey));
    result.checks.push("lost-response-same-order", "private-key-not-in-whatsapp");

    await login(s.owner);
    await page.getByRole("button", { name: "Editar", exact: true }).click();
    await page.getByRole("button", { name: "Ver inventario", exact: true }).click();
    const inventory = page.locator(".inventory-list").first();
    await inventory.getByText("4 unidades", { exact: false }).waitFor();
    await page.getByRole("button", { name: "Ver resultados comerciales" }).click();
    await page.getByRole("button", { name: "Ver solicitudes", exact: true }).click();
    await page.getByRole("button", { name: `Abrir ${saved.reference}`, exact: true }).click();
    await page.getByLabel("Estado de la solicitud").selectOption("in_attention");
    await page.getByRole("checkbox", { name: "Confirmo que recibí el contacto del cliente" }).check();
    await page.getByRole("button", { name: "Guardar estado", exact: true }).click();
    await page.getByText("Estado guardado.", { exact: true }).waitFor();
    await page.getByRole("region", { name: "Resumen de solicitud" }).getByText("En atención", { exact: true }).waitFor();
    await page.getByRole("button", { name: "Registrar venta de esta solicitud" }).click();
    await page.getByLabel("Precio final de Café de especialidad").fill("12000");
    await page.getByRole("button", { name: "Registrar venta", exact: true }).click();
    const receipt = page.getByRole("region", { name: "Resumen de solicitud" });
    await receipt.getByText("Convertida en venta", { exact: true }).waitFor();
    await inventory.getByText("2 unidades", { exact: false }).waitFor();
    await page.getByText(/^1 vigentes · \d+ anuladas$/).waitFor();
    result.checks.push("owner-attention-contact", "sale-stock-metrics-refresh");

    await page.getByRole("button", { name: "Ver ventas", exact: true }).click();
    await page.getByRole("button", { name: `Anular venta ${saved.reference}`, exact: true }).click();
    await page.getByLabel("Motivo de anulación").fill("Anulación de prueba con devolución completa");
    await page.getByLabel("Unidades devueltas de Café de especialidad").fill("2");
    await page.getByRole("button", { name: "Confirmar anulación", exact: true }).click();
    await receipt.getByText("Cancelada", { exact: true }).waitFor();
    await inventory.getByText("4 unidades", { exact: false }).waitFor();
    await page.getByText(/^0 vigentes · \d+ anuladas$/).waitFor();
    result.checks.push("void-stock-order-metrics-refresh");
    for (const width of [390, 320]) {
      await page.setViewportSize({ width, height: 844 });
      assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true);
    }
    await receipt.screenshot({ path: join(artifacts, `${result.browser}-order-320.png`) });
    result.checks.push("owner-mobile-no-overflow");
    await logout();
    await page.goto(s.webOrigin + tracking);
    await page.getByText("Cancelada", { exact: true }).waitFor();
    result.checks.push("customer-tracking-after-void");

    await login(s.owner);
    await page.goto(s.webOrigin + "/administracion/planes");
    await page.getByText("Necesitás ingresar con una cuenta administradora.", { exact: false }).waitFor();
    await page.goto(s.webOrigin + "/mi-negocio");
    await logout();
    await login(s.admin);
    await page.goto(s.webOrigin + "/administracion/planes");
    await page.getByRole("button", { name: "Gestionar plan de Café del Patio", exact: true }).waitFor();
    result.checks.push("owner-denied-admin-allowed");
    assert.deepEqual(errors, []);
    result.checks.push("no-javascript-errors");
  } catch (error) {
    await page.screenshot({ path: join(artifacts, `${result.browser}-failure.png`), fullPage: true }).catch(() => {});
    throw error;
  } finally { await context.close(); }
}
