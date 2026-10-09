import { expect, it, vi } from "vitest";
import { GET, POST } from "../src/app/api/marketplace/[...path]/route";

it("reenvía la clave solo en cabecera y descarta parámetros del seguimiento", async () => {
  vi.stubEnv("APP_ORIGIN", "http://localhost:3000"); vi.stubEnv("API_BASE_URL", "http://localhost:5080");
  const fetcher = vi.spyOn(globalThis, "fetch").mockResolvedValue(Response.json({}));
  const id = "d71cd8d6-7245-492f-92de-4caa0cf11e50", key = "a".repeat(64);
  const path = ["public", "orders", id];
  const response = await GET(new Request(`http://localhost:3000/api/marketplace/public/orders/${id}?accessKey=ignored`, { headers: { "X-Order-Key": key } }), { params: Promise.resolve({ path }) });
  expect(response.headers.get("cache-control")).toBe("no-store");
  expect(fetcher.mock.calls[0][0]).toBe(`http://localhost:5080/public/orders/${id}`);
  expect((fetcher.mock.calls[0][1]!.headers as Headers).get("X-Order-Key")).toBe(key);
});
it("limita el cuerpo de solicitud y exige origen confiable", async () => {
  vi.stubEnv("APP_ORIGIN", "http://localhost:3000"); vi.stubEnv("API_BASE_URL", "http://localhost:5080");
  const fetcher = vi.spyOn(globalThis, "fetch");
  const path = ["public", "businesses", "d71cd8d6-7245-492f-92de-4caa0cf11e50", "orders"];
  const context = { params: Promise.resolve({ path }) };
  const url = "http://localhost:3000/api/marketplace/" + path.join("/");
  expect((await POST(new Request(url, { method: "POST", body: "{}" }), context)).status).toBe(403);
  expect((await POST(new Request(url, { method: "POST", headers: { Origin: "http://localhost:3000", "X-Requested-With": "marketplace" }, body: " ".repeat(16385) }), context)).status).toBe(413);
  expect(fetcher).not.toHaveBeenCalled();
});
