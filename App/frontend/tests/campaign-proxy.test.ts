import { expect, it, vi } from "vitest";
import { GET, POST, PUT } from "../src/app/api/marketplace/[...path]/route";

it("campañas usan rutas explícitas, parámetros acotados y máximo 16 KiB", async () => {
  vi.stubEnv("APP_ORIGIN", "http://localhost:3000"); vi.stubEnv("API_BASE_URL", "http://localhost:5080");
  const fetcher = vi.spyOn(globalThis, "fetch").mockImplementation(async () => Response.json({}));
  const id = "d71cd8d6-7245-492f-92de-4caa0cf11e50";
  const context = (path: string) => ({ params: Promise.resolve({ path: path.split("/") }) });
  const headers = { Origin: "http://localhost:3000", "X-Requested-With": "marketplace" };
  const result = await GET(new Request("http://localhost:3000/api/marketplace/public/campaigns?zone=centro&token=private"), context("public/campaigns"));
  expect(result.status).toBe(200); expect(result.headers.get("cache-control")).toBe("no-store");
  expect(fetcher.mock.calls[0][0]).toBe("http://localhost:5080/public/campaigns?zone=centro");
  const path = `admin/campaigns/${id}/actions`;
  expect((await PUT(new Request(`http://localhost:3000/api/marketplace/${path}`, { method: "PUT", headers, body: "x".repeat(16385) }), context(path))).status).toBe(413);
  expect((await POST(new Request("http://localhost:3000/api/marketplace/public/campaign-events", { method: "POST", headers, body: "{}" }), context("public/campaign-events"))).status).toBe(200);
  expect(fetcher).toHaveBeenCalledTimes(2);
});
