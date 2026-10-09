import assert from "node:assert/strict";
import { test } from "node:test";
import { checkAudit } from "./audit-policy.mjs";

const now = new Date("2026-10-09T00:00:00Z");
const advisory = { name: "braces", dependency: "braces", severity: "high",
  url: "https://github.com/advisories/GHSA-vfj7-8cjw-p6xm" };
function fixture() {
  const dependencies = { braces: [advisory], micromatch: ["braces"], "fast-glob": ["micromatch"],
    "@next/eslint-plugin-next": ["fast-glob"], "eslint-config-next": ["@next/eslint-plugin-next"] };
  const versions = ["3.0.3", "4.0.8", "3.3.1", "16.3.8", "16.3.8"];
  const vulnerabilities = {}, packages = {};
  Object.entries(dependencies).forEach(([name, via], index) => {
    const node = `node_modules/${name}`;
    vulnerabilities[name] = { name, severity: "high", via: structuredClone(via), nodes: [node] };
    packages[node] = { version: versions[index], dev: true };
  });
  return { report: { auditReportVersion: 2, vulnerabilities, metadata: { vulnerabilities: { total: 5 } } },
    lock: { lockfileVersion: 3, packages } };
}

test("permite solo el aviso autorizado en la cadena de desarrollo", () => {
  const { report, lock } = fixture();
  assert.equal(checkAudit(report, lock, 1, now), 5);
});
test("sin avisos no necesita excepción, incluso después del vencimiento", () => {
  const { report, lock } = fixture();
  report.vulnerabilities = {}; report.metadata.vulnerabilities.total = 0;
  assert.equal(checkAudit(report, lock, 0, new Date("2026-10-20")), 0);
});
test("vence al terminar el 15/10 en Argentina, sin prórroga implícita", () => {
  const { report, lock } = fixture();
  assert.equal(checkAudit(report, lock, 1, new Date("2026-10-15T23:59:59.999-03:00")), 5);
  assert.throws(() => checkAudit(report, lock, 1, new Date("2026-10-16T00:00:00-03:00")), /expired/);
});
test("rechaza el mismo paquete si alcanza producción", () => {
  const { report, lock } = fixture();
  lock.packages["node_modules/braces"].dev = false;
  assert.throws(() => checkAudit(report, lock, 1, now), /development/);
});
test("rechaza un nuevo aviso aunque afecte al paquete autorizado", () => {
  const { report, lock } = fixture();
  report.vulnerabilities.braces.via.push({ ...advisory, url: "https://github.com/advisories/GHSA-another" });
  assert.throws(() => checkAudit(report, lock, 1, now), /advisory/);
});
test("rechaza avisos fuera de la cadena autorizada", () => {
  const { report, lock } = fixture();
  report.vulnerabilities.next = { name: "next", via: [advisory], nodes: ["node_modules/next"] };
  report.metadata.vulnerabilities.total++;
  assert.throws(() => checkAudit(report, lock, 1, now), /Unexpected/);
});
test("rechaza una cadena incompleta o un nuevo origen transitivo", () => {
  const { report, lock } = fixture();
  delete report.vulnerabilities.braces; report.metadata.vulnerabilities.total--;
  assert.throws(() => checkAudit(report, lock, 1, now), /chain/);
  const other = fixture(); other.report.vulnerabilities.micromatch.via.push("unknown");
  assert.throws(() => checkAudit(other.report, other.lock, 1, now), /chain/);
});
test("rechaza cambios de versión o nodos no comprobados", () => {
  const { report, lock } = fixture();
  lock.packages["node_modules/braces"].version = "3.0.4";
  assert.throws(() => checkAudit(report, lock, 1, now), /version/);
  const other = fixture(); other.report.vulnerabilities.braces.nodes = [];
  assert.throws(() => checkAudit(other.report, other.lock, 1, now), /nodes/);
});
test("falla ante errores del servicio, informe incompleto o salida inconsistente", () => {
  const { report, lock } = fixture();
  for (const invalid of [null, {}, { ...report, error: { code: "EAUDIT" } },
    { ...report, metadata: { vulnerabilities: { total: 0 } } }]) {
    assert.throws(() => checkAudit(invalid, lock, 1, now), /Invalid/);
  }
  for (const status of [0, 2, null]) assert.throws(() => checkAudit(report, lock, status, now), /Invalid/);
  assert.throws(() => checkAudit(report, lock, 1, new Date("invalid")), /Invalid/);
});
