import { describe, expect, it, vi } from "vitest";
import { GET, POST } from "../src/app/api/marketplace/[...path]/route";

const context = (path: string[]) => ({ params: Promise.resolve({ path }) });

describe("proxy del marketplace", () => {
  it("rechaza mutaciones de otro origen antes de contactar la API", async () => {
    vi.stubEnv("APP_ORIGIN", "http://localhost:3000");
    vi.stubEnv("API_BASE_URL", "http://localhost:5080");
    const fetcher = vi.spyOn(globalThis, "fetch");
    const response = await POST(new Request("http://localhost:3000/api/marketplace/auth/login", {
      method: "POST", headers: { Origin: "https://attacker.example", "X-Requested-With": "marketplace" },
      body: "{}",
    }), context(["auth", "login"]));
    expect(response.status).toBe(403);
    expect(fetcher).not.toHaveBeenCalled();
  });

  it("solo reenvía rutas permitidas y propaga la cookie privada", async () => {
    vi.stubEnv("APP_ORIGIN", "http://localhost:3000");
    vi.stubEnv("API_BASE_URL", "http://localhost:5080/base/");
    const fetcher = vi.spyOn(globalThis, "fetch").mockResolvedValue(new Response('{"id":"1"}', {
      status: 200,
      headers: { "Content-Type": "application/json", "Set-Cookie": "marketplace_session=secret; HttpOnly; Path=/" },
    }));
    const request = new Request("http://localhost:3000/api/marketplace/auth/login", {
      method: "POST",
      headers: { Origin: "http://localhost:3000", "X-Requested-With": "marketplace", Cookie: "old=value" },
      body: '{"email":"owner@example.test"}',
    });
    const response = await POST(request, context(["auth", "login"]));
    expect(fetcher).toHaveBeenCalledWith("http://localhost:5080/base/auth/login", expect.objectContaining({
      method: "POST",
      cache: "no-store",
    }));
    const forwardedHeaders = fetcher.mock.calls[0][1]?.headers as Headers;
    expect(forwardedHeaders.get("Origin")).toBe("http://localhost:3000");
    expect(forwardedHeaders.get("Cookie")).toBe("old=value");
    expect(response.headers.get("set-cookie")).toContain("HttpOnly");

    const blocked = await GET(new Request("http://localhost:3000/api/marketplace/admin"), context(["admin"]));
    expect(blocked.status).toBe(404);
    expect(fetcher).toHaveBeenCalledTimes(1);
  });
});
