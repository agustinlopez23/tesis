import { expect, it, vi } from "vitest";
import { GET, PUT } from "../src/app/api/marketplace/[...path]/route";
it("permite ventas e inventario privados con filtros acotados y límite de cuerpo", async () => {
  vi.stubEnv("APP_ORIGIN", "http://localhost:3000"); vi.stubEnv("API_BASE_URL", "http://localhost:5080");
  const fetcher = vi.spyOn(globalThis, "fetch").mockResolvedValue(Response.json({}));
  const id = "d71cd8d6-7245-492f-92de-4caa0cf11e50", path = ["businesses", id, "sales"];
  expect((await GET(new Request(`http://localhost:3000/api/marketplace/${path.join("/")}?page=2&orderId=${id}&secret=bad`), { params: Promise.resolve({ path }) })).status).toBe(200);
  expect(String(fetcher.mock.calls[0][0])).toContain("page=2"); expect(String(fetcher.mock.calls[0][0])).not.toContain("secret");
  const response = await PUT(new Request("http://localhost:3000/api/marketplace/" + [...path, id].join("/"), { method: "PUT", headers: { Origin: "http://localhost:3000", "X-Requested-With": "marketplace" }, body: " ".repeat(16385) }), { params: Promise.resolve({ path: [...path, id] }) });
  expect(response.status).toBe(413);
});
