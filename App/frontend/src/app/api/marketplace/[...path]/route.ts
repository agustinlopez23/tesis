type RouteParameters = { params: Promise<{ path: string[] }> };

const allowedRoutes: Record<string, RegExp[]> = {
  GET: [
    /^auth\/session$/,
    /^business-categories$/,
    /^businesses$/,
    /^businesses\/[0-9a-f-]{36}$/i,
  ],
  POST: [/^auth\/(register|login|logout)$/, /^businesses$/],
  PUT: [/^businesses\/[0-9a-f-]{36}$/i],
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
    const upstream = await fetch(new URL(path, baseUrl).toString(), {
      method: request.method,
      headers,
      body: request.method === "GET" ? undefined : await request.arrayBuffer(),
      cache: "no-store",
      redirect: "manual",
    });
    const responseHeaders = new Headers();
    const responseType = upstream.headers.get("content-type");
    const setCookie = upstream.headers.get("set-cookie");
    if (responseType) responseHeaders.set("Content-Type", responseType);
    if (setCookie) responseHeaders.set("Set-Cookie", setCookie);
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
