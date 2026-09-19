import { describe, expect, it, vi } from "vitest";
import { GET, POST, PUT, DELETE } from "../src/app/api/marketplace/[...path]/route";

const context = (path: string[]) => ({ params: Promise.resolve({ path }) });

describe("proxy del marketplace", () => {
  it("limita cargas sin Content-Length y rechaza eliminaciones de otro origen", async () => {
    vi.stubEnv("APP_ORIGIN", "http://localhost:3000");
    vi.stubEnv("API_BASE_URL", "http://localhost:5080");
    const fetcher = vi.spyOn(globalThis, "fetch");
    const id = "d71cd8d6-7245-492f-92de-4caa0cf11e50";
    const path = ["businesses", id, "photos", "1"];
    const url = "http://localhost:3000/api/marketplace/" + path.join("/");
    const large = new Request(url, { method: "PUT", headers: { Origin: "http://localhost:3000",
      "X-Requested-With": "marketplace" }, body: new Uint8Array(5 * 1024 * 1024 + 65537) });
    expect((await PUT(large, context(path))).status).toBe(413);
    expect((await DELETE(new Request(url, { method: "DELETE", headers: {
      Origin: "https://other.example", "X-Requested-With": "marketplace" } }), context(path))).status).toBe(403);
    expect(fetcher).not.toHaveBeenCalled();
  });
  it("reenvía perfiles e imágenes sin cachear y conserva el multipart de fotos", async () => {
    vi.stubEnv("APP_ORIGIN", "http://localhost:3000");
    vi.stubEnv("API_BASE_URL", "http://localhost:5080");
    const id = "d71cd8d6-7245-492f-92de-4caa0cf11e50";
    const fetcher = vi.spyOn(globalThis, "fetch").mockResolvedValue(new Response(new Uint8Array([255, 216]), {
      headers: { "Content-Type": "image/jpeg", "X-Content-Type-Options": "nosniff" },
    }));
    const response = await GET(new Request("http://localhost:3000/api/marketplace/public/businesses/" + id + "/photos/1"),
      context(["public", "businesses", id, "photos", "1"]));
    expect(response.status).toBe(200);
    expect(response.headers.get("content-type")).toBe("image/jpeg");
    expect(response.headers.get("x-content-type-options")).toBe("nosniff");
    const form = new FormData();
    form.append("file", new Blob(["pixels"], { type: "image/png" }), "photo.png");
    const request = new Request("http://localhost:3000/api/marketplace/businesses/" + id + "/photos/1", {
      method: "PUT", headers: { Origin: "http://localhost:3000", "X-Requested-With": "marketplace" }, body: form,
    });
    expect((await PUT(request, context(["businesses", id, "photos", "1"]))).status).toBe(200);
    expect((fetcher.mock.calls.at(-1)![1]?.headers as Headers).get("Content-Type")).toContain("multipart/form-data; boundary=");
  });
  it("permite explorar sin sesión y conserva la página sin permitir mutaciones públicas", async () => {
    vi.stubEnv("APP_ORIGIN", "http://localhost:3000");
    vi.stubEnv("API_BASE_URL", "http://localhost:5080");
    const fetcher = vi.spyOn(globalThis, "fetch").mockResolvedValue(Response.json({ items: [], hasMore: false }));
    const url = "http://localhost:3000/api/marketplace/public/businesses?page=2";
    const response = await GET(new Request(url), context(["public", "businesses"]));
    expect(response.status).toBe(200);
    expect(response.headers.get("cache-control")).toBe("no-store");
    expect(fetcher).toHaveBeenCalledWith("http://localhost:5080/public/businesses?page=2", expect.objectContaining({ method: "GET" }));
    expect((fetcher.mock.calls[0][1]?.headers as Headers).has("Cookie")).toBe(false);
    expect((await POST(new Request(url, { method: "POST" }), context(["public", "businesses"]))).status).toBe(404);
  });

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
