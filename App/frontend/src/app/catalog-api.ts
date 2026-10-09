export type Product = {
  id: string; name: string; description: string; category: string; price: number;
  currency: string; available: boolean; status: string; version: number; hasPhoto: boolean;
};
export type Plan = { catalogEnabled: boolean; plan: string; periods: {
  id: string; startsAt: string; endsAt: string; reason: string; revokedAt: string | null; revocationReason: string | null;
}[] };
export async function catalogApi(path: string, method = "GET", body?: unknown, signal?: AbortSignal) {
  const multipart = body instanceof FormData;
  const timeout = AbortSignal.timeout(15000);
  return fetch("/api/marketplace" + path, { method, cache: "no-store", signal: signal ? AbortSignal.any([signal, timeout]) : timeout,
    headers: { "X-Requested-With": "marketplace", ...(multipart ? {} : { "Content-Type": "application/json" }) },
    body: body === undefined ? undefined : multipart ? body : JSON.stringify(body) });
}
export async function catalogError(response: Response) {
  if (response.status === 401) return "Tu sesión venció. Volvé a ingresar desde Mi negocio.";
  if (response.status === 413) return "La foto supera el límite de 5 MB.";
  try { return (await response.json()).message ?? "No pudimos completar la operación. Intentá nuevamente."; }
  catch { return "No pudimos completar la operación. Intentá nuevamente."; }
}
export function priceLabel(product: Product) {
  return new Intl.NumberFormat("es-AR", { style: "currency", currency: product.currency, currencyDisplay: "code" }).format(product.price);
}
