import assert from "node:assert/strict";
import { writeFile } from "node:fs/promises";
import { join } from "node:path";
import { chromium } from "playwright";
import AxeBuilder from "@axe-core/playwright";

export async function verifyAccessibility(s, artifacts) {
  assert.equal(s.webOrigin, "http://localhost:3200", "Only the isolated harness is allowed.");
  const browser = await chromium.launch();
  const report = { at: new Date().toISOString(), browser: browser.version(), passed: false, keyboard: [], scans: [] };
  const context = await browser.newContext({ locale: "es-AR" });
  const page = await context.newPage();
  page.setDefaultTimeout(15000);
  async function scan(name) {
    for (const width of [1440, 320]) {
      await page.setViewportSize({ width, height: 900 });
      const result = await new AxeBuilder({ page }).analyze();
      report.axe = result.testEngine.version;
      const summarize = items => items.map(item => ({ rule: item.id, impact: item.impact,
        targets: item.nodes.map(node => node.target), help: item.helpUrl }));
      const overflow = await page.evaluate(() => document.documentElement.scrollWidth > innerWidth);
      report.scans.push({ name, width, violations: summarize(result.violations), incomplete: summarize(result.incomplete), overflow });
      console.log(`Accessibility: ${name} ${width}px: ${result.violations.length} violations, overflow=${overflow}`);
      await page.screenshot({ path: join(artifacts, `accessibility-${name}-${width}.png`), fullPage: true });
    }
  }
  async function authenticate(account) {
    await context.clearCookies();
    const separator = account.cookie.indexOf("=");
    await context.addCookies([{ name: account.cookie.slice(0, separator), value: account.cookie.slice(separator + 1), url: s.webOrigin }]);
  }
  try {
    await page.goto(s.webOrigin + "/explorar");
    await page.getByRole("heading", { name: "Café del Patio", exact: true }).waitFor();
    await page.keyboard.press("Tab");
    assert.equal(await page.locator(":focus").textContent(), "Saltar al contenido");
    await page.keyboard.press("Enter");
    await page.waitForFunction(() => document.activeElement?.id === "contenido");
    report.keyboard.push("skip-link-focuses-main");
    await scan("explore");
    const navigation = page.getByRole("navigation", { name: "Navegación principal" });
    for (const width of [320, 390, 768, 1440]) {
      await page.setViewportSize({ width, height: 900 });
      assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
      for (const link of await navigation.getByRole("link").all()) {
        const box = await link.boundingBox();
        assert.ok(box && box.x >= 0 && box.x + box.width <= width, "Navigation must fit the viewport.");
        if (width <= 390) assert.ok(box.height >= 44, "Mobile navigation needs a 44px touch target.");
      }
    }
    await page.setViewportSize({ width: 320, height: 900 });
    await navigation.getByRole("link", { name: "Productos", exact: true }).focus();
    await page.keyboard.press("Enter");
    await page.getByRole("heading", { name: "Café de especialidad", exact: true }).waitFor();
    assert.equal(await navigation.getByRole("link", { name: "Productos", exact: true }).getAttribute("aria-current"), "page");
    report.keyboard.push("products-link-opens-and-announces-current-section");
    await scan("products");
    await page.goto(`${s.webOrigin}/negocios/${s.business}`);
    await page.getByRole("button", { name: "Agregar Café de especialidad" }).waitFor();
    await scan("profile");
    await page.getByRole("button", { name: "Agregar Café de especialidad" }).click();
    await page.getByLabel("Tu nombre o alias").waitFor();
    await scan("checkout");
    await page.goto(s.webOrigin + "/mi-negocio");
    await page.getByRole("button", { name: "Crear cuenta", exact: true }).waitFor();
    await scan("register");
    await page.getByRole("button", { name: "Ya tengo cuenta" }).click();
    await page.getByRole("button", { name: "Ingresar", exact: true }).waitFor();
    await scan("login");
    await authenticate(s.owner);
    await page.goto(s.webOrigin + "/mi-negocio");
    await page.getByRole("button", { name: "Editar", exact: true }).click();
    const sections = page.getByRole("navigation", { name: "Secciones de tu negocio" });
    await sections.getByRole("button", { name: "Catálogo", exact: true }).click();
    await page.getByRole("button", { name: "Editar Café de especialidad", exact: true }).click();
    await scan("product-editor");
    await sections.getByRole("button", { name: "Ventas", exact: true }).click();
    await page.getByRole("heading", { name: "Ventas registradas en el período" }).waitFor();
    await scan("metrics");
    await authenticate(s.admin);
    await page.goto(s.webOrigin + "/administracion/planes");
    await page.getByRole("button", { name: "Gestionar plan de Café del Patio", exact: true }).click();
    await page.getByRole("button", { name: "Actualizar plan" }).waitFor();
    await page.getByText("Plan actual: Impulso", { exact: true }).waitFor();
    await scan("admin-plan");
    const failures = report.scans.filter(scan => scan.violations.length || scan.overflow);
    assert.equal(failures.length, 0, "Accessibility violations detected; see accessibility-result.json.");
    report.passed = true;
  } finally {
    await writeFile(join(artifacts, "accessibility-result.json"), JSON.stringify(report, null, 2));
    await browser.close();
  }
}
