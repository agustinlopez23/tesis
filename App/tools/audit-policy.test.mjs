import assert from "node:assert/strict";
import { test } from "node:test";
import { checkAudit } from "./audit-policy.mjs";

const now = new Date("2026-11-02T00:00:00Z");
const advisory = { name: "minimatch", dependency: "minimatch", severity: "moderate",
  url: "https://github.com/advisories/GHSA-g3hv-75wg-m249" };
// Shape taken from `npm audit --json --include=dev` in App/frontend (2026-11-02).
const chain = {
  minimatch: { via: [advisory], nodes: {
    "node_modules/minimatch": "3.1.5",
    "node_modules/@typescript-eslint/typescript-estree/node_modules/minimatch": "10.2.6" } },
  "@eslint/config-array": { via: ["minimatch"], nodes: { "node_modules/@eslint/config-array": "0.21.2" } },
  "@eslint/eslintrc": { via: ["minimatch"], nodes: { "node_modules/@eslint/eslintrc": "3.3.7" } },
  "@typescript-eslint/typescript-estree": { via: ["minimatch"],
    nodes: { "node_modules/@typescript-eslint/typescript-estree": "8.72.1" } },
  "eslint-plugin-import": { via: ["minimatch"], nodes: { "node_modules/eslint-plugin-import": "2.32.0" } },
  "eslint-plugin-jsx-a11y": { via: ["minimatch"], nodes: { "node_modules/eslint-plugin-jsx-a11y": "6.10.2" } },
  "eslint-plugin-react": { via: ["minimatch"], nodes: { "node_modules/eslint-plugin-react": "7.37.5" } },
  eslint: { via: ["@eslint-community/eslint-utils", "@eslint/config-array", "@eslint/eslintrc", "minimatch"],
    nodes: { "node_modules/eslint": "9.39.1" } },
  "@eslint-community/eslint-utils": { via: ["eslint"], nodes: {
    "node_modules/@eslint-community/eslint-utils": "4.10.1",
    "node_modules/@next/eslint-plugin-next/node_modules/@eslint-community/eslint-utils": "4.9.1" } },
  "@next/eslint-plugin-next": { via: ["@eslint-community/eslint-utils"],
    nodes: { "node_modules/@next/eslint-plugin-next": "16.4.3" } },
  "eslint-config-next": { via: ["@next/eslint-plugin-next", "eslint", "eslint-plugin-import",
    "eslint-plugin-jsx-a11y", "eslint-plugin-react", "typescript-eslint"],
  nodes: { "node_modules/eslint-config-next": "16.4.3" } },
  "@typescript-eslint/utils": { via: ["@eslint-community/eslint-utils", "@typescript-eslint/typescript-estree", "eslint"],
    nodes: { "node_modules/@typescript-eslint/utils": "8.72.1" } },
  "@typescript-eslint/parser": { via: ["@typescript-eslint/typescript-estree", "eslint"],
    nodes: { "node_modules/@typescript-eslint/parser": "8.72.1" } },
  "@typescript-eslint/type-utils": { via: ["@typescript-eslint/typescript-estree", "@typescript-eslint/utils", "eslint"],
    nodes: { "node_modules/@typescript-eslint/type-utils": "8.72.1" } },
  "@typescript-eslint/eslint-plugin": { via: ["@typescript-eslint/parser", "@typescript-eslint/type-utils",
    "@typescript-eslint/utils", "eslint"], nodes: { "node_modules/@typescript-eslint/eslint-plugin": "8.72.1" } },
  "typescript-eslint": { via: ["@typescript-eslint/eslint-plugin", "@typescript-eslint/parser",
    "@typescript-eslint/typescript-estree", "@typescript-eslint/utils", "eslint"],
  nodes: { "node_modules/typescript-eslint": "8.72.1" } },
};
function fixture() {
  const vulnerabilities = {}, packages = {};
  for (const [name, { via, nodes }] of Object.entries(chain)) {
    vulnerabilities[name] = { name, severity: "moderate", via: structuredClone(via), nodes: Object.keys(nodes) };
    for (const [node, version] of Object.entries(nodes)) packages[node] = { version, dev: true };
  }
  const total = Object.keys(vulnerabilities).length;
  return { report: { auditReportVersion: 2, vulnerabilities, metadata: { vulnerabilities: { total } } },
    lock: { lockfileVersion: 3, packages } };
}
const total = Object.keys(chain).length;

test("permite solo el aviso autorizado en la cadena de desarrollo", () => {
  const { report, lock } = fixture();
  assert.equal(checkAudit(report, lock, 1, now), total);
});
test("sin avisos no necesita excepción, incluso después del vencimiento", () => {
  const { report, lock } = fixture();
  report.vulnerabilities = {}; report.metadata.vulnerabilities.total = 0;
  assert.equal(checkAudit(report, lock, 0, new Date("2027-02-01")), 0);
});
test("vence al terminar el 31/12/2026 en Argentina, sin prórroga implícita", () => {
  const { report, lock } = fixture();
  assert.equal(checkAudit(report, lock, 1, new Date("2026-12-31T23:59:59.999-03:00")), total);
  assert.throws(() => checkAudit(report, lock, 1, new Date("2027-01-01T00:00:00-03:00")), /expired/);
});
test("rechaza el mismo paquete si alcanza producción", () => {
  const { report, lock } = fixture();
  lock.packages["node_modules/minimatch"].dev = false;
  assert.throws(() => checkAudit(report, lock, 1, now), /development/);
  const other = fixture(); delete other.lock.packages["node_modules/eslint"].dev;
  assert.throws(() => checkAudit(other.report, other.lock, 1, now), /development/);
});
test("rechaza un nuevo aviso aunque afecte al paquete autorizado", () => {
  const { report, lock } = fixture();
  report.vulnerabilities.minimatch.via.push({ ...advisory, url: "https://github.com/advisories/GHSA-another" });
  assert.throws(() => checkAudit(report, lock, 1, now), /advisory/);
  const other = fixture(); other.report.vulnerabilities.eslint.via.push({ ...advisory, name: "eslint", dependency: "eslint" });
  assert.throws(() => checkAudit(other.report, other.lock, 1, now), /chain/);
});
test("rechaza avisos fuera de la cadena autorizada", () => {
  const { report, lock } = fixture();
  report.vulnerabilities.next = { name: "next", severity: "moderate", via: [advisory], nodes: ["node_modules/next"] };
  lock.packages["node_modules/next"] = { version: "16.4.3", dev: true };
  report.metadata.vulnerabilities.total++;
  assert.throws(() => checkAudit(report, lock, 1, now), /Unexpected/);
});
test("rechaza una cadena incompleta o un nuevo origen transitivo", () => {
  const { report, lock } = fixture();
  delete report.vulnerabilities.minimatch; report.metadata.vulnerabilities.total--;
  assert.throws(() => checkAudit(report, lock, 1, now), /chain/);
  const other = fixture(); other.report.vulnerabilities.eslint.via.push("unknown");
  assert.throws(() => checkAudit(other.report, other.lock, 1, now), /chain/);
  const empty = fixture(); empty.report.vulnerabilities["eslint-plugin-react"].via = [];
  assert.throws(() => checkAudit(empty.report, empty.lock, 1, now), /chain/);
});
test("rechaza cambios de versión o nodos no comprobados", () => {
  const { report, lock } = fixture();
  lock.packages["node_modules/minimatch"].version = "3.1.6";
  assert.throws(() => checkAudit(report, lock, 1, now), /version/);
  const swapped = fixture();
  swapped.lock.packages["node_modules/@eslint-community/eslint-utils"].version = "4.9.1";
  assert.equal(checkAudit(swapped.report, swapped.lock, 1, now), total);
  const other = fixture(); other.report.vulnerabilities.minimatch.nodes = [];
  assert.throws(() => checkAudit(other.report, other.lock, 1, now), /nodes/);
  const extra = fixture();
  extra.report.vulnerabilities.minimatch.nodes.push("node_modules/glob/node_modules/minimatch");
  assert.throws(() => checkAudit(extra.report, extra.lock, 1, now), /nodes|version/);
  const dup = fixture(); dup.report.vulnerabilities.eslint.nodes.push("node_modules/eslint");
  assert.throws(() => checkAudit(dup.report, dup.lock, 1, now), /nodes/);
});
test("rechaza una severidad mayor que la revisada", () => {
  const { report, lock } = fixture();
  report.vulnerabilities.eslint.severity = "high";
  assert.throws(() => checkAudit(report, lock, 1, now), /Unexpected/);
  const other = fixture(); other.report.vulnerabilities.minimatch.via[0].severity = "high";
  assert.throws(() => checkAudit(other.report, other.lock, 1, now), /advisory/);
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
