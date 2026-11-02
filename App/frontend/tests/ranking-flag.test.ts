import { afterEach, expect, it, vi } from "vitest";
import { rankingEnabled } from "../src/lib/ranking-preview";

afterEach(() => vi.unstubAllEnvs());

it.each([
  ["production", { RANKING_ENABLED: "true" }, true],
  ["production", { RANKING_PREVIEW_ENABLED: "true" }, false],
  ["production", {}, false],
  ["development", { RANKING_PREVIEW_ENABLED: "true" }, true],
  ["development", { RANKING_ENABLED: "false" }, false],
] as const)("en %s con %o el ranking queda habilitado: %s", (environment, env, expected) => {
  vi.stubEnv("NODE_ENV", environment);
  vi.stubEnv("RANKING_ENABLED", "");
  vi.stubEnv("RANKING_PREVIEW_ENABLED", "");
  for (const [key, value] of Object.entries(env)) vi.stubEnv(key, value);
  expect(rankingEnabled()).toBe(expected);
});
