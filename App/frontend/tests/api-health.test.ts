import { describe, expect, it, vi } from "vitest";
import { checkApiHealth } from "../src/lib/check-api-health";

const endpoint = "http://localhost:5080/prefix/health/live";
const response = (body = '{"status":"ok"}', status = 200, type = "application/json") =>
  new Response(body, { status, headers: { "Content-Type": type } });

describe("checkApiHealth", () => {
  it("consulta la URL configurada sin caché y limpia el timer en éxito", async () => {
    vi.useFakeTimers();
    const fetcher = vi.fn<typeof fetch>().mockResolvedValue(response());
    expect(await checkApiHealth("http://localhost:5080/prefix///", fetcher)).toBe(true);
    expect(fetcher).toHaveBeenCalledWith(endpoint, {
      cache: "no-store", signal: expect.any(AbortSignal),
    });
    expect(vi.getTimerCount()).toBe(0);
  });

  it.each([undefined, "", "/relative", "invalid", "ftp://example.com", "http://"])(
    "rechaza URL inválida %s sin hacer requests", async (url) => {
      const fetcher = vi.fn<typeof fetch>();
      expect(await checkApiHealth(url, fetcher)).toBe(false);
      expect(fetcher).not.toHaveBeenCalled();
    },
  );

  it.each([
    ["ok", 500, "application/json"],
    ['{"status":"ok"}', 201, "application/json"],
    ['{"status":"ok"}', 200, "text/plain"],
    ["invalid json", 200, "application/json"],
    ['{"status":"down"}', 200, "application/json"],
    ['{"status":"ok","extra":true}', 200, "application/json"],
    ["null", 200, "application/json"],
    ["[]", 200, "application/json"],
  ])("rechaza contrato inválido %s / %s / %s", async (body, status, type) => {
    const fetcher = vi.fn<typeof fetch>().mockResolvedValue(response(body, status, type));
    expect(await checkApiHealth("http://localhost:5080", fetcher)).toBe(false);
  });

  it("acepta JSON con charset y URL HTTPS", async () => {
    const fetcher = vi.fn<typeof fetch>().mockResolvedValue(response(undefined, 200, "application/json; charset=utf-8"));
    expect(await checkApiHealth("https://example.com", fetcher)).toBe(true);
  });

  it("controla error de red y limpia el timer", async () => {
    vi.useFakeTimers();
    const fetcher = vi.fn<typeof fetch>().mockRejectedValue(new TypeError("Network error"));
    expect(await checkApiHealth("http://localhost:5080", fetcher)).toBe(false);
    expect(vi.getTimerCount()).toBe(0);
  });

  it("aborta a los 3000 ms y limpia el timer", async () => {
    vi.useFakeTimers();
    let signal: AbortSignal | undefined;
    const fetcher = vi.fn<typeof fetch>().mockImplementation((_url, init) => {
      signal = init?.signal as AbortSignal;
      return new Promise((_resolve, reject) => {
        signal!.addEventListener("abort", () => reject(new DOMException("Aborted", "AbortError")));
      });
    });
    const result = checkApiHealth("http://localhost:5080", fetcher);
    await vi.advanceTimersByTimeAsync(2999);
    expect(signal?.aborted).toBe(false);
    await vi.advanceTimersByTimeAsync(1);
    expect(signal?.aborted).toBe(true);
    expect(await result).toBe(false);
    expect(vi.getTimerCount()).toBe(0);
  });
});
