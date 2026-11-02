import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { fileURLToPath } from "node:url";

// Authorized by the project owner on 2026-11-02 until 2026-12-31 (end of day, Argentina time).
// GHSA-g3hv-75wg-m249 (minimatch <11.0.1, ReDoS via globparts amplification) reaches only the
// development ESLint toolchain. No upstream fix exists: every ESLint package depends on minimatch
// ^3 or ^10, and minimatch 11 drops the default export they call. The production audit is clean.
const expiresAt = Date.parse("2027-01-01T00:00:00-03:00");
const advisoryUrl = "https://github.com/advisories/GHSA-g3hv-75wg-m249";
const root = "minimatch";
// Reviewed package -> versions installed in App/frontend/package-lock.json.
const allowed = new Map([
  ["minimatch", ["3.1.5", "10.2.6"]],
  ["@eslint-community/eslint-utils", ["4.10.1", "4.9.1"]],
  ["@eslint/config-array", ["0.21.2"]],
  ["@eslint/eslintrc", ["3.3.7"]],
  ["@next/eslint-plugin-next", ["16.4.3"]],
  ["@typescript-eslint/eslint-plugin", ["8.72.1"]],
  ["@typescript-eslint/parser", ["8.72.1"]],
  ["@typescript-eslint/type-utils", ["8.72.1"]],
  ["@typescript-eslint/typescript-estree", ["8.72.1"]],
  ["@typescript-eslint/utils", ["8.72.1"]],
  ["eslint", ["9.39.1"]],
  ["eslint-config-next", ["16.4.3"]],
  ["eslint-plugin-import", ["2.32.0"]],
  ["eslint-plugin-jsx-a11y", ["6.10.2"]],
  ["eslint-plugin-react", ["7.37.5"]],
  ["typescript-eslint", ["8.72.1"]],
]);
const object = value => value !== null && typeof value === "object" && !Array.isArray(value);
const authorizedAdvisory = item => object(item) && item.url === advisoryUrl && item.name === root &&
  item.dependency === root && item.severity === "moderate";

export function checkAudit(report, lock, status, now = new Date()) {
  assert.ok(Number.isFinite(now.getTime()), "Invalid audit date.");
  assert.ok(object(report) && !report.error && report.auditReportVersion === 2 &&
    object(report.vulnerabilities), "Invalid audit report.");
  assert.ok(lock?.lockfileVersion === 3 && object(lock.packages), "Invalid lockfile.");
  const entries = Object.entries(report.vulnerabilities);
  assert.equal(report.metadata?.vulnerabilities?.total, entries.length, "Invalid audit totals.");
  assert.equal(status, entries.length ? 1 : 0, "Invalid npm audit exit status.");
  if (!entries.length) return 0;
  assert.ok(now.getTime() < expiresAt, "Temporary audit exception expired; update dependencies.");
  for (const [name, vulnerability] of entries) {
    const versions = allowed.get(name);
    assert.ok(versions && vulnerability?.name === name && vulnerability.severity === "moderate",
      `Unexpected vulnerability: ${name}`);
    const nodes = vulnerability.nodes;
    assert.ok(Array.isArray(nodes) && nodes.length > 0 && new Set(nodes).size === nodes.length &&
      nodes.every(node => typeof node === "string" && node.endsWith(`node_modules/${name}`)),
    `Unexpected audit nodes: ${name}`);
    for (const node of nodes) {
      const pkg = lock.packages[node];
      assert.ok(object(pkg), `Unexpected audit nodes: ${name}`);
      assert.equal(pkg.dev, true, `Exception is limited to development dependencies: ${name}`);
      assert.ok(versions.includes(pkg.version), `Unreviewed package version: ${name}@${pkg.version}`);
    }
    const via = vulnerability.via;
    if (name === root) {
      assert.ok(Array.isArray(via) && via.length > 0 && via.every(authorizedAdvisory),
        "Unexpected advisory; only GHSA-g3hv-75wg-m249 is authorized.");
    } else {
      assert.ok(Array.isArray(via) && via.length > 0 && via.every(origin => typeof origin === "string" &&
        allowed.has(origin) && Object.hasOwn(report.vulnerabilities, origin)), `Unexpected dependency chain: ${name}`);
    }
  }
  return entries.length;
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try {
    assert.ok(process.env.npm_execpath, "Run through npm run audit:ci.");
    const result = spawnSync(process.execPath, [process.env.npm_execpath, "audit", "--json", "--include=dev"], {
      encoding: "utf8", windowsHide: true, timeout: 120000, maxBuffer: 10 * 1024 * 1024,
    });
    assert.ok(!result.error && !result.signal, "npm audit failed to execute.");
    const count = checkAudit(JSON.parse(result.stdout), JSON.parse(readFileSync("package-lock.json", "utf8")), result.status);
    console.log(count ? `Accepted temporary GHSA-g3hv-75wg-m249 exception: ${count} development packages; expires after 2026-12-31 Argentina time.` : "Audit passed without exceptions.");
  } catch (error) {
    console.error(`Audit rejected: ${error.message}`);
    process.exitCode = 1;
  }
}
