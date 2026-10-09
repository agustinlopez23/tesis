import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { fileURLToPath } from "node:url";

// Authorized by the project owner; expires after October 15 in Argentina.
const expiresAt = Date.parse("2026-10-16T00:00:00-03:00");
const advisoryUrl = "https://github.com/advisories/GHSA-vfj7-8cjw-p6xm";
const allowed = new Map([
  ["braces", { version: "3.0.3", via: null }],
  ["micromatch", { version: "4.0.8", via: "braces" }],
  ["fast-glob", { version: "3.3.1", via: "micromatch" }],
  ["@next/eslint-plugin-next", { version: "16.3.8", via: "fast-glob" }],
  ["eslint-config-next", { version: "16.3.8", via: "@next/eslint-plugin-next" }],
]);
const object = value => value !== null && typeof value === "object" && !Array.isArray(value);

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
    const rule = allowed.get(name);
    assert.ok(rule && vulnerability?.name === name && vulnerability.severity === "high", `Unexpected vulnerability: ${name}`);
    assert.ok(Array.isArray(vulnerability.nodes) && vulnerability.nodes.length === 1 &&
      vulnerability.nodes[0] === `node_modules/${name}`, `Unexpected audit nodes: ${name}`);
    const pkg = lock.packages[vulnerability.nodes[0]];
    assert.equal(pkg?.dev, true, `Exception is limited to development dependencies: ${name}`);
    assert.equal(pkg.version, rule.version, `Unreviewed package version: ${name}`);
    const via = vulnerability.via;
    if (rule.via) {
      assert.ok(Array.isArray(via) && via.length === 1 && via[0] === rule.via &&
        Object.hasOwn(report.vulnerabilities, rule.via), `Unexpected dependency chain: ${name}`);
    } else {
      assert.ok(Array.isArray(via) && via.length === 1 && via[0]?.url === advisoryUrl &&
        via[0]?.name === "braces" && via[0]?.dependency === "braces" && via[0]?.severity === "high",
      "Unexpected advisory; only GHSA-vfj7-8cjw-p6xm is authorized.");
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
    console.log(count ? `Accepted temporary GHSA-vfj7-8cjw-p6xm exception: ${count} development packages; expires after 2026-10-15 Argentina time.` : "Audit passed without exceptions.");
  } catch (error) {
    console.error(`Audit rejected: ${error.message}`);
    process.exitCode = 1;
  }
}
