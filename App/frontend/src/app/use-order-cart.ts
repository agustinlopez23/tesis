"use client";

import { useEffect, useState } from "react";
import { catalogApi, catalogError, Product } from "./catalog-api";
import { newOrderKey, Order, OrderIntent, readOrder } from "./order-api";
import { campaignAttribution } from "./campaign-api";

export type Selection = { product: Product; quantity: number };
export function useOrderCart(businessId: string) {
  const [selection, setSelection] = useState<Selection[]>([]);
  const [name, setName] = useState("");
  const [intent, setIntent] = useState<OrderIntent | null>(null);
  const [order, setOrder] = useState<Order | null>(null);
  const [pending, setPending] = useState(false);
  const [restoring, setRestoring] = useState(true);
  const [error, setError] = useState("");
  const storageKey = `cerca-order-intent:${businessId}`;
  useEffect(() => {
    const controller = new AbortController();
    async function restore() {
      try {
        const value = sessionStorage.getItem(storageKey);
        if (!value) return;
        const saved: OrderIntent & { accessExpiresAt?: string } = JSON.parse(value);
        if (!/^[0-9a-f]{64}$/.test(saved.accessKey) || !/^[0-9a-f-]{36}$/i.test(saved.requestId) || !Array.isArray(saved.lines)) throw new Error();
        if (saved.accessExpiresAt && Date.parse(saved.accessExpiresAt) <= Date.now()) {
          sessionStorage.removeItem(storageKey); return;
        }
        setIntent(saved); setName(saved.customerName);
        const response = await readOrder(saved.requestId, saved.accessKey, controller.signal);
        if (controller.signal.aborted) return;
        if (response.ok) {
          const recovered: Order = await response.json();
          setOrder(recovered);
          sessionStorage.setItem(storageKey, JSON.stringify({ ...saved, accessExpiresAt: recovered.accessExpiresAt }));
        }
        else setError("La solicitud está pendiente de recuperar. Reintentá con el mismo intento.");
      } catch { if (!controller.signal.aborted) setError("No pudimos recuperar la solicitud. Reintentá si ya habías confirmado."); }
      finally { if (!controller.signal.aborted) setRestoring(false); }
    }
    void restore();
    return () => controller.abort();
  }, [storageKey]);
  function add(product: Product) {
    if (pending || intent || order || restoring || !product.available) return;
    const current = selection.find(x => x.product.id === product.id);
    if (!current && selection.length >= 20) { setError("Podés seleccionar hasta 20 productos distintos."); return; }
    setError("");
    setSelection(items => current ? items.map(x => x.product.id === product.id ? { product, quantity: Math.min(99, x.quantity + 1) } : x)
      : [...items, { product, quantity: 1 }]);
  }
  function quantity(id: string, quantity: number) {
    if (pending || intent) return;
    setSelection(items => quantity === 0 ? items.filter(x => x.product.id !== id)
      : items.map(x => x.product.id === id ? { ...x, quantity: Math.max(1, Math.min(99, Math.trunc(quantity) || 1)) } : x));
  }
  async function submit() {
    if (pending || restoring || order) return;
    if (!intent && (!selection.length || !name.trim())) { setError("Indicá tu nombre y seleccioná productos."); return; }
    setPending(true); setError("");
    const attempt = intent ?? { requestId: crypto.randomUUID(), accessKey: newOrderKey(), customerName: name.trim(),
      lines: selection.map(x => ({ productId: x.product.id, quantity: x.quantity, expectedVersion: x.product.version })), campaignToken: campaignAttribution(businessId) };
    try {
      // Persist before POST: a lost response or reload must keep the same identity/key.
      sessionStorage.setItem(storageKey, JSON.stringify(attempt)); setIntent(attempt);
      const response = await catalogApi(`/public/businesses/${businessId}/orders`, "POST", attempt);
      if (!response.ok) {
        if ([400, 403, 409].includes(response.status)) {
          sessionStorage.removeItem(storageKey); setIntent(null);
          setError(await catalogError(response)); return;
        }
        throw new Error(await catalogError(response));
      }
      const confirmed: Order = await response.json();
      setOrder(confirmed); setSelection([]);
      sessionStorage.setItem(storageKey, JSON.stringify({ ...attempt, accessExpiresAt: confirmed.accessExpiresAt }));
    } catch { setError("No pudimos confirmar la respuesta. Reintentá para recuperar la misma solicitud."); }
    finally { setPending(false); }
  }
  function reset() {
    if (!order) return;
    try { sessionStorage.removeItem(storageKey); } catch { setError("No pudimos preparar una nueva solicitud."); return; }
    setOrder(null); setIntent(null); setSelection([]); setError("");
  }
  return { selection, name, setName, intent, order, pending, restoring, error, add, quantity, submit, reset };
}
