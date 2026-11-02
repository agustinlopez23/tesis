import { beforeEach, expect, it, vi } from "vitest";
import { GET, POST } from "../src/app/api/marketplace/[...path]/route";

const path = ["public", "ranking-snapshots"];
const context = (page = false) => ({ params: Promise.resolve({ path: page ? [...path, "page"] : path }) });
const origin = "http://localhost:3000";
const url = origin + "/api/marketplace/" + path.join("/");
const post = (body = "{}", headers = {}) => new Request(url, { method: "POST", body,
  headers: { Origin: origin, "X-Requested-With": "marketplace", "Content-Type": "application/json", ...headers } });
beforeEach(() => {
  vi.stubEnv("NODE_ENV", "development");
  vi.stubEnv("RANKING_PREVIEW_ENABLED", "true");
  vi.stubEnv("APP_ORIGIN", origin);
  vi.stubEnv("API_BASE_URL", "http://localhost:5080");
});

it("reenvía identidad, sesión, cuerpo y filtros sin eliminar query inválida", async () => {
  const fetcher = vi.spyOn(globalThis, "fetch").mockImplementation(async () => Response.json({ cursor: "current" }, { status: 201 }));
  const body = JSON.stringify({ requestId: "same-request", zoneId: "centro", categoryId: 1, radiusKm: 4 });
  const response = await POST(post(body, { Cookie: "marketplace_session=owner" }), context());
  expect(response.status).toBe(201);
  expect(response.headers.get("cache-control")).toBe("no-store");
  const options = fetcher.mock.calls[0][1]!;
  expect(new TextDecoder().decode(options.body as Uint8Array)).toBe(body);
  expect((options.headers as Headers).get("cookie")).toBe("marketplace_session=owner");
  await GET(new Request(url + "/page?cursor=a%2Bb&categoryId=2"), context(true));
  expect(fetcher).toHaveBeenLastCalledWith("http://localhost:5080/public/ranking-snapshots/page?cursor=a%2Bb&categoryId=2", expect.anything());
  await POST(new Request(url + "?extra=1", { method: "POST", headers: post().headers, body: "{}" }), context());
  expect(fetcher).toHaveBeenLastCalledWith("http://localhost:5080/public/ranking-snapshots?extra=1", expect.anything());
});

it.each([200, 400, 409, 410, 429, 503])("conserva estado %s y no-store", async (status) => {
  vi.spyOn(globalThis, "fetch").mockResolvedValue(Response.json({ message: "Prueba" }, { status }));
  const response = await GET(new Request(url + "/page?cursor=token"), context(true));
  expect(response.status).toBe(status);
  expect(response.headers.get("cache-control")).toBe("no-store");
});

it.each(["production", "development"])("cierra creación por defecto en %s y mantiene GET solo local", async (environment) => {
  vi.stubEnv("NODE_ENV", environment);
  vi.stubEnv("RANKING_PREVIEW_ENABLED", environment === "production" ? "true" : "false");
  const fetcher = vi.spyOn(globalThis, "fetch").mockResolvedValue(Response.json({}));
  const response = await POST(post(), context());
  expect(response.status).toBe(404);
  expect(response.headers.get("cache-control")).toBe("no-store");
  expect(fetcher).not.toHaveBeenCalled();
  expect((await GET(new Request(url + "/page?cursor=token"), context(true))).status).toBe(environment === "production" ? 404 : 200);
});

it("rechaza origen ajeno y cuerpos de más de 4096 bytes aun sin longitud declarada", async () => {
  const fetcher = vi.spyOn(globalThis, "fetch");
  for (const [request, status] of [[post("{}", { Origin: "https://other.test" }), 403],
    [post("x".repeat(4097)), 413], [post("{}", { "Content-Length": "4097" }), 413]] as const) {
    const response = await POST(request, context());
    expect(response.status).toBe(status);
    expect(response.headers.get("cache-control")).toBe("no-store");
  }
  expect(fetcher).not.toHaveBeenCalled();
});

it("propaga cancelación del visitante y falla sin cachear ni revelar errores internos", async () => {
  const controller = new AbortController();
  const fetcher = vi.spyOn(globalThis, "fetch").mockRejectedValue(new Error("private connection"));
  const response = await GET(new Request(url + "/page?cursor=token", { signal: controller.signal }), context(true));
  expect(response.status).toBe(502);
  expect(response.headers.get("cache-control")).toBe("no-store");
  expect(await response.text()).not.toContain("private connection");
  controller.abort();
  expect(fetcher.mock.calls[0][1]?.signal?.aborted).toBe(true);
});

it("abre creación y páginas en producción con RANKING_ENABLED", async () => {
  vi.stubEnv("NODE_ENV", "production");
  vi.stubEnv("RANKING_PREVIEW_ENABLED", "false");
  vi.stubEnv("RANKING_ENABLED", "true");
  const fetcher = vi.spyOn(globalThis, "fetch").mockImplementation(async () => Response.json({ cursor: "current" }, { status: 201 }));
  expect((await POST(post(), context())).status).toBe(201);
  expect((await GET(new Request(url + "/page?cursor=token"), context(true))).status).toBe(201);
  expect(fetcher).toHaveBeenCalledTimes(2);
});
