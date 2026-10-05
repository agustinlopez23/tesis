"use client";

import { useEffect, useState } from "react";
import { catalogApi, catalogError } from "./catalog-api";
import { useSaleOperation } from "./use-sale-operation";
import { businessChanged, useBusinessRefresh } from "./business-refresh";

type Item = { productId: string; name: string; quantity: number | null; version: number };
type Movement = { operationId: string; productId: string; productName: string; delta: number; balance: number; reason: string; createdAt: string };
export default function BusinessInventory({ business }: { business: string }) {
  const [open, setOpen] = useState(false);
  return <section className="catalog-panel"><div className="catalog-heading"><h2>Inventario</h2><button className="button button-outline" aria-expanded={open} onClick={() => setOpen(!open)}>{open ? "Cerrar inventario" : "Ver inventario"}</button></div>{open && <Inventory key={business} business={business} />}</section>;
}
function Inventory({ business }: { business: string }) {
  const [items, setItems] = useState<Item[]>([]), [selected, setSelected] = useState<Item | null>(null);
  const [delta, setDelta] = useState(""), [reason, setReason] = useState("");
  const [history, setHistory] = useState<Movement[]>([]), [page, setPage] = useState(1), [more, setMore] = useState(false);
  const [attempt, setAttempt] = useState(0), [error, setError] = useState(""), [success, setSuccess] = useState("");
  const [loading, setLoading] = useState(true);
  const operation = useSaleOperation(`cerca-stock:${business}`, () => { setSuccess("Ajuste guardado."); setSelected(null); businessChanged(business); });
  function refresh() { setLoading(true); setError(""); setAttempt(n => n + 1); }
  useBusinessRefresh(business, refresh);
  useEffect(() => {
    const controller = new AbortController();
    void Promise.all([catalogApi(`/businesses/${business}/inventory`, "GET", undefined, controller.signal), catalogApi(`/businesses/${business}/inventory/movements?page=${page}`, "GET", undefined, controller.signal)])
      .then(async ([stock, movements]) => {
        if (!stock.ok || !movements.ok) throw new Error(await catalogError(!stock.ok ? stock : movements));
        const [rows, entries] = await Promise.all([stock.json(), movements.json()]);
        if (!controller.signal.aborted) { setItems(rows); setHistory(entries.items); setMore(entries.hasNextPage); }
      }).catch(e => { if (!controller.signal.aborted) setError(e.message); }).finally(() => { if (!controller.signal.aborted) setLoading(false); });
    return () => controller.abort();
  }, [business, page, attempt]);
  return <>
    <p>Cargá el saldo inicial real. Luego sumá ingresos o restá bajas indicando el motivo. Una solicitud no reserva unidades.</p>
    {error && <p role="alert">{error}</p>}{operation.error && <p role="alert">{operation.error}</p>}{success && <p role="status">{success}</p>}
    <button className="text-link" disabled={loading || operation.pending} onClick={refresh}>Actualizar inventario</button>
    {loading ? <p role="status">Cargando inventario…</p> : !error && <ul className="inventory-list">{items.map(item => <li key={item.productId}><span><strong>{item.name}</strong> · {item.quantity === null ? <span>Sin cargar</span> : `${item.quantity} unidades`}</span>
      <button className="text-link" disabled={operation.locked} onClick={() => { setSelected(item); setDelta(""); setReason(""); setSuccess(""); }}>Ajustar {item.name}</button></li>)}</ul>}
    {(selected || operation.intent) && <form className="catalog-form" onSubmit={e => { e.preventDefault(); void operation.submit(() => ({ path: `/businesses/${business}/inventory/adjustments/${crypto.randomUUID()}`, body: { productId: selected!.productId, delta: Number(delta), version: selected!.version, reason: reason.trim() } })); }}>
      <fieldset disabled={operation.locked}><legend>{selected?.name ?? "Ajuste pendiente"}</legend><label>Unidades a sumar o restar<input required type="number" min={-1000000} max={1000000} value={delta} onChange={e => setDelta(e.target.value)} /></label>
        <label>Motivo del ajuste<input required minLength={3} maxLength={300} value={reason} onChange={e => setReason(e.target.value)} /></label></fieldset>
      <button className="button button-primary" disabled={operation.pending || operation.restoring}>{operation.intent ? "Reintentar ajuste" : "Guardar ajuste"}</button></form>}
    <h3>Historial de movimientos</h3><ul className="inventory-list">{history.map(m => <li key={`${m.operationId}:${m.productId}`}><div><strong>{m.productName}: {m.delta > 0 ? "+" : ""}{m.delta}</strong><p>Saldo: {m.balance} · {m.reason} · {new Date(m.createdAt).toLocaleString("es-AR")}</p></div></li>)}</ul>
    <nav className="catalog-toolbar" aria-label="Páginas del inventario"><button disabled={loading || page === 1} onClick={() => { setLoading(true); setPage(page - 1); }}>Anterior</button><span>Página {page}</span><button disabled={loading || !more} onClick={() => { setLoading(true); setPage(page + 1); }}>Siguiente</button></nav>
  </>;
}
