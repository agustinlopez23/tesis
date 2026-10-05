export type Order = {
  id: string; businessId: string; reference: string; customerName: string; businessName: string; whatsApp: string;
  createdAt: string; accessExpiresAt: string; status: string; version: number; contactReceivedAt: string | null; canContact: boolean;
  lines: { productId: string; name: string; quantity: number; unitPrice: number; currency: string }[];
  totals: Record<string, number>;
};
export type OrderIntent = { requestId: string; accessKey: string; customerName: string;
  lines: { productId: string; quantity: number; expectedVersion: number }[] };
export const orderStatus = (value: string) => ({ pending: "Pendiente", in_attention: "En atención", cancelled: "Cancelada" }[value] ?? value);
export const money = (value: number, currency: string) => new Intl.NumberFormat("es-AR", { style: "currency", currency, currencyDisplay: "code" }).format(value);
export function orderMessage(order: Order) {
  const text = (value: string) => value.replace(/\s+/g, " ").trim();
  return [`Solicitud ${order.reference} · ${text(order.businessName)}`, `Soy ${text(order.customerName)}. Quisiera consultar por:`,
    ...order.lines.map(line => `${line.quantity} × ${text(line.name)} — ${money(line.unitPrice, line.currency)} c/u`),
    ...Object.entries(order.totals).map(([currency, total]) => `Total de referencia: ${money(total, currency)}`),
    "A confirmar disponibilidad y precio final con el negocio."].join("\n");
}
export function newOrderKey() {
  return Array.from(crypto.getRandomValues(new Uint8Array(32)), b => b.toString(16).padStart(2, "0")).join("");
}
export function privateOrderPath(id: string, key: string) { return `/pedido/${id}#clave=${key}`; }
export function readOrder(id: string, key: string, signal?: AbortSignal) {
  const timeout = AbortSignal.timeout(15000);
  return fetch(`/api/marketplace/public/orders/${id}`, { cache: "no-store", headers: { "X-Order-Key": key },
    signal: signal ? AbortSignal.any([signal, timeout]) : timeout });
}
