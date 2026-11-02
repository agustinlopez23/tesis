type RouteParameters = { params: Promise<{ path: string[] }> };

const allowedRoutes: Record<string, RegExp[]> = {
  GET: [
    /^businesses\/[0-9a-f-]{36}\/catalog-assistance$/i,
    /^public\/campaigns(\/[0-9a-f-]{36}(\/image)?)?$/i,
    /^businesses\/[0-9a-f-]{36}\/campaigns(\/[0-9a-f-]{36}\/metrics)?$/i,
    /^admin\/campaigns$/,
    /^public\/products$/,
    /^businesses\/[0-9a-f-]{36}\/commerce$/i,
    /^businesses\/[0-9a-f-]{36}\/(inventory(\/movements)?|sales(\/[0-9a-f-]{36})?)$/i,
    /^public\/orders\/[0-9a-f-]{36}$/i,
    /^businesses\/[0-9a-f-]{36}\/orders(\/[0-9a-f-]{36})?$/i,
    /^(public\/)?businesses\/[0-9a-f-]{36}\/products(\/[0-9a-f-]{36}\/photo)?$/i,
    /^businesses\/[0-9a-f-]{36}\/plan$/i,
    /^admin\/businesses(\/[0-9a-f-]{36}\/plan)?$/i,
    /^public\/businesses$/,
    /^public\/ranking-snapshots\/page$/,
    /^public\/businesses\/[0-9a-f-]{36}(\/photos\/[1-5])?$/i,
    /^auth\/session$/,
    /^business-categories$/,
    /^businesses$/,
    /^businesses\/[0-9a-f-]{36}$/i,
    /^businesses\/[0-9a-f-]{36}\/photos(\/[1-5])?$/i,
    /^businesses\/[0-9a-f-]{36}\/activity$/i,
  ],
  POST: [/^public\/businesses\/[0-9a-f-]{36}\/orders$/i, /^auth\/(register|login|logout)$/, /^businesses$/, /^public\/events$/, /^public\/ranking-snapshots$/, /^admin\/businesses\/[0-9a-f-]{36}\/subscriptions\/[0-9a-f-]{36}\/revoke$/i],
  PUT: [/^businesses\/[0-9a-f-]{36}\/(inventory\/adjustments\/[0-9a-f-]{36}|sales\/[0-9a-f-]{36}(\/void)?)$/i, /^businesses\/[0-9a-f-]{36}\/orders\/[0-9a-f-]{36}$/i, /^businesses\/[0-9a-f-]{36}$/i, /^businesses\/[0-9a-f-]{36}\/photos\/[1-5]$/i, /^businesses\/[0-9a-f-]{36}\/products\/[0-9a-f-]{36}(\/photo)?$/i, /^admin\/businesses\/[0-9a-f-]{36}\/subscriptions\/[0-9a-f-]{36}$/i],
  DELETE: [/^businesses\/[0-9a-f-]{36}\/photos\/[1-5]$/i, /^businesses\/[0-9a-f-]{36}\/products\/[0-9a-f-]{36}\/photo$/i],
};

allowedRoutes.POST.push(/^public\/campaign-events$/);
allowedRoutes.POST.push(/^businesses\/[0-9a-f-]{36}\/catalog-assistance$/i);
allowedRoutes.PUT.push(/^businesses\/[0-9a-f-]{36}\/campaigns\/[0-9a-f-]{36}(\/pause)?$/i, /^admin\/campaigns\/[0-9a-f-]{36}\/actions$/i);
allowedRoutes.GET.push(/^admin\/(categories|audit|moderation\/(accounts|businesses|products))$/);
allowedRoutes.POST.push(/^admin\/categories$/, /^admin\/moderation\/(accounts|businesses|products)\/[0-9a-f-]{36}\/(suspend|reinstate)$/i);
allowedRoutes.PUT.push(/^admin\/categories\/[0-9]{1,5}$/);

async function forward(request: Request, context: RouteParameters): Promise<Response> {
  const response = await forwardRequest(request, context);
  response.headers.set("Cache-Control", "no-store");
  return response;
}

async function forwardRequest(request: Request, context: RouteParameters): Promise<Response> {
  const { path: segments } = await context.params;
  const path = segments.join("/");
  if (!allowedRoutes[request.method]?.some((route) => route.test(path))) {
    return new Response(null, { status: 404 });
  }
  const ranking = path === "public/ranking-snapshots" || path === "public/ranking-snapshots/page";
  if (ranking && !rankingEnabled() && (!isRankingPreviewEnvironment() || request.method === "POST"))
    return new Response(null, { status: 404 });

  const appOrigin = process.env.APP_ORIGIN;
  if (!appOrigin) return Response.json({ message: "Configuración incompleta." }, { status: 503 });
  if (request.method !== "GET" && (
    request.headers.get("origin") !== appOrigin ||
    request.headers.get("x-requested-with") !== "marketplace"
  )) return new Response(null, { status: 403 });

  const baseUrl = parseApiBase(process.env.API_BASE_URL);
  if (!baseUrl) return Response.json({ message: "API no configurada." }, { status: 503 });
  const headers = new Headers({
    Origin: appOrigin, "X-Requested-With": "marketplace",
    ...clientAddressHeaders(request.headers, process.env.API_PROXY_SECRET),
  });
  const contentType = request.headers.get("content-type");
  const cookie = request.headers.get("cookie");
  if (contentType) headers.set("Content-Type", contentType);
  if (cookie) headers.set("Cookie", cookie);
  if (/^public\/orders\//.test(path)) {
    const key = request.headers.get("x-order-key");
    if (key && /^[0-9a-f]{64}$/.test(key)) headers.set("X-Order-Key", key);
  }

  try {
    const maxBody = path.endsWith("/catalog-assistance") || path.startsWith("admin/categories") || path.startsWith("admin/moderation/") ? 8192 : /\/(orders|sales|inventory|campaigns|campaign-events)(\/|$)/.test(path) ? 16384 : path === "public/events" || ranking ? 4096 : 5 * 1024 * 1024 + 65536;
    if (Number(request.headers.get("content-length")) > maxBody) return new Response(null, { status: 413 });
    // Bound streamed/chunked uploads too, before buffering them for the API.
    const chunks: Uint8Array[] = [];
    let size = 0;
    if (request.method !== "GET" && request.body) {
      const reader = request.body.getReader();
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        size += value.byteLength;
        if (size > maxBody) { await reader.cancel(); return new Response(null, { status: 413 }); }
        chunks.push(value);
      }
    }
    const body = new Uint8Array(size);
    let offset = 0;
    for (const chunk of chunks) { body.set(chunk, offset); offset += chunk.byteLength; }
    const upstreamUrl = new URL(path, baseUrl);
    if (path.endsWith("/campaigns")) {
      const key = path === "public/campaigns" ? "zone" : "page";
      const value = new URL(request.url).searchParams.get(key);
      if (value !== null) upstreamUrl.searchParams.set(key, value);
    }
    if (path === "public/products") {
      const query = new URL(request.url).searchParams;
      for (const key of ["page", "q", "category", "lat", "lon", "radiusKm"]) if (query.has(key)) upstreamUrl.searchParams.set(key, query.get(key)!);
    }
    if (path.endsWith("/commerce")) {
      const days = new URL(request.url).searchParams.get("days");
      if (days !== null) upstreamUrl.searchParams.set("days", days);
    }
    if (path.endsWith("/sales") || path.endsWith("/inventory/movements")) {
      const query = new URL(request.url).searchParams;
      for (const key of ["page", path.endsWith("/sales") ? "orderId" : "productId"]) if (query.has(key)) upstreamUrl.searchParams.set(key, query.get(key)!);
    }
    if (ranking || path === "public/businesses" || path.endsWith("/activity") || path.includes("/products") || path === "admin/businesses" || path === "admin/audit" || path.startsWith("admin/moderation/")) upstreamUrl.search = new URL(request.url).search;
    if (path.startsWith("businesses/") && path.endsWith("/orders")) {
      const query = new URL(request.url).searchParams;
      for (const key of ["page", "reference", "status"]) if (query.has(key)) upstreamUrl.searchParams.set(key, query.get(key)!);
    }
    const upstream = await fetch(upstreamUrl.toString(), {
      method: request.method,
      headers,
      body: request.method === "GET" ? undefined : body,
      cache: "no-store",
      redirect: "manual",
      signal: AbortSignal.any([request.signal, AbortSignal.timeout(30000)]),
    });
    const responseHeaders = new Headers({ "Cache-Control": "no-store" });
    const responseType = upstream.headers.get("content-type");
    const setCookie = upstream.headers.get("set-cookie");
    if (responseType) responseHeaders.set("Content-Type", responseType);
    if (setCookie) responseHeaders.set("Set-Cookie", setCookie);
    responseHeaders.set("X-Content-Type-Options", "nosniff");
    return new Response(upstream.body, { status: upstream.status, headers: responseHeaders });
  } catch {
    return Response.json({ message: "La API no está disponible." }, { status: 502 });
  }
}

function parseApiBase(value: string | undefined): URL | null {
  try {
    const url = new URL(value ?? "");
    if (!['http:', 'https:'].includes(url.protocol) || url.username || url.password) return null;
    if (!url.pathname.endsWith('/')) url.pathname += '/';
    return url;
  } catch {
    return null;
  }
}

export const GET = forward;
export const POST = forward;
export const PUT = forward;
export const DELETE = forward;
import { clientAddressHeaders } from "@/lib/client-address";
import { isRankingPreviewEnvironment, rankingEnabled } from "@/lib/ranking-preview";
