export async function checkApiHealth(
  baseUrl: string | undefined,
  fetcher: typeof fetch = fetch,
  timeoutMs = 3000,
): Promise<boolean> {
  let url: URL;
  try {
    url = new URL(baseUrl ?? "");
    if (!["http:", "https:"].includes(url.protocol)) return false;
  } catch {
    return false;
  }
  url.pathname = `${url.pathname.replace(/\/+$/, "")}/health/live`;
  url.search = "";
  url.hash = "";
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeoutMs);
  try {
    const response = await fetcher(url.toString(), {
      cache: "no-store", signal: controller.signal,
    });
    const mediaType = response.headers.get("content-type")?.split(";")[0].trim().toLowerCase();
    if (response.status !== 200 || mediaType !== "application/json") return false;
    const body: unknown = await response.json();
    return body !== null && typeof body === "object" && !Array.isArray(body)
      && Object.keys(body).length === 1 && "status" in body && body.status === "ok";
  } catch {
    return false;
  } finally {
    clearTimeout(timer);
  }
}
