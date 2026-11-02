import { afterEach, describe, expect, it, vi } from "vitest";
import { POST } from "../src/app/api/marketplace/[...path]/route";

const context = { params: Promise.resolve({ path: ["auth", "login"] }) };
const login = (headers: Record<string, string>) => new Request("https://cerca.example.test/api/marketplace/auth/login", {
  method: "POST",
  headers: { Origin: "https://cerca.example.test", "X-Requested-With": "marketplace", "Content-Type": "application/json", ...headers },
  body: "{}",
});

async function forwardedHeaders(headers: Record<string, string>) {
  const fetcher = vi.spyOn(globalThis, "fetch").mockResolvedValue(new Response(null, { status: 401 }));
  await POST(login(headers), context);
  return fetcher.mock.calls.at(-1)![1]?.headers as Headers;
}

describe("dirección del cliente en el proxy", () => {
  afterEach(() => { vi.unstubAllEnvs(); vi.restoreAllMocks(); });

  it("reenvía la IP real del visitante con el secreto compartido", async () => {
    vi.stubEnv("APP_ORIGIN", "https://cerca.example.test");
    vi.stubEnv("API_BASE_URL", "https://api.example.test");
    vi.stubEnv("API_PROXY_SECRET", "s".repeat(40));
    const headers = await forwardedHeaders({ "X-Real-IP": "203.0.113.9", "X-Forwarded-For": "198.51.100.1, 10.0.0.1" });
    expect(headers.get("X-Marketplace-Client-IP")).toBe("203.0.113.9");
    expect(headers.get("X-Marketplace-Proxy-Secret")).toBe("s".repeat(40));
  });

  it("usa el primer x-forwarded-for y descarta valores que no son IP", async () => {
    vi.stubEnv("APP_ORIGIN", "https://cerca.example.test");
    vi.stubEnv("API_BASE_URL", "https://api.example.test");
    vi.stubEnv("API_PROXY_SECRET", "s".repeat(40));
    expect((await forwardedHeaders({ "X-Forwarded-For": "2001:db8::7, 10.0.0.1" })).get("X-Marketplace-Client-IP")).toBe("2001:db8::7");
    const invalid = await forwardedHeaders({ "X-Real-IP": "evil<script>" });
    expect(invalid.get("X-Marketplace-Client-IP")).toBeNull();
  });

  it("no envía la IP ni el secreto cuando el secreto no está configurado", async () => {
    vi.stubEnv("APP_ORIGIN", "https://cerca.example.test");
    vi.stubEnv("API_BASE_URL", "https://api.example.test");
    vi.stubEnv("API_PROXY_SECRET", "");
    const headers = await forwardedHeaders({ "X-Real-IP": "203.0.113.9" });
    expect(headers.get("X-Marketplace-Client-IP")).toBeNull();
    expect(headers.get("X-Marketplace-Proxy-Secret")).toBeNull();
  });
});
