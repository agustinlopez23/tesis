type RouteParameters = { params: Promise<{ path: string[] }> };

const allowedRoutes: Record<string, RegExp[]> = {
  GET: [
    /^public\/businesses$/,
    /^public\/businesses\/[0-9a-f-]{36}(\/photos\/[1-5])?$/i,
    /^auth\/session$/,
    /^business-categories$/,
    /^businesses$/,
    /^businesses\/[0-9a-f-]{36}$/i,
    /^businesses\/[0-9a-f-]{36}\/photos(\/[1-5])?$/i,
  ],
  POST: [/^auth\/(register|login|logout)$/, /^businesses$/],
  PUT: [/^businesses\/[0-9a-f-]{36}$/i, /^businesses\/[0-9a-f-]{36}\/photos\/[1-5]$/i],
  DELETE: [/^businesses\/[0-9a-f-]{36}\/photos\/[1-5]$/i],
};

async function forward(request: Request, context: RouteParameters): Promise<Response> {
  const { path: segments } = await context.params;
  const path = segments.join("/");
  if (!allowedRoutes[request.method]?.some((route) => route.test(path))) {
    return new Response(null, { status: 404 });
  }

  const appOrigin = process.env.APP_ORIGIN;
  if (!appOrigin) return Response.json({ message: "Configuración incompleta." }, { status: 503 });
  if (request.method !== "GET" && (
    request.headers.get("origin") !== appOrigin ||
    request.headers.get("x-requested-with") !== "marketplace"
  )) return new Response(null, { status: 403 });

  const baseUrl = parseApiBase(process.env.API_BASE_URL);
  if (!baseUrl) return Response.json({ message: "API no configurada." }, { status: 503 });
  const headers = new Headers({ Origin: appOrigin, "X-Requested-With": "marketplace" });
  const contentType = request.headers.get("content-type");
  const cookie = request.headers.get("cookie");
  if (contentType) headers.set("Content-Type", contentType);
  if (cookie) headers.set("Cookie", cookie);

  try {
    const maxBody = 5 * 1024 * 1024 + 65536;
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
    if (path === "public/businesses") upstreamUrl.search = new URL(request.url).search;
    const upstream = await fetch(upstreamUrl.toString(), {
      method: request.method,
      headers,
      body: request.method === "GET" ? undefined : body,
      cache: "no-store",
      redirect: "manual",
      signal: AbortSignal.timeout(30000),
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
