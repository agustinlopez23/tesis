"use client";

import { useEffect, useState } from "react";
import { catalogApi, catalogError } from "./catalog-api";
import { money } from "./order-api";
import SaleEditor from "./sale-editor";
import { useSaleOperation } from "./use-sale-operation";

type Sale = { id: string; orderId: string | null; orderReference: string | null; createdAt: string; voidedAt: string | null; voidReason: string | null;
  lines: { productId: string; name: string; quantity: number; unitPrice: number; currency: string; origin: string; returnedQuantity: number }[];
  totals: Record<string, Record<string, number>> };
const originLabel = (origin: string) => ({ requested: "Solicitado", conversation: "Acordado en conversación", direct: "Presencial" }[origin] ?? origin);
export default function BusinessSales({ business }: { business: string }) {
  const [open, setOpen] = useState(false);
  return <section className="catalog-panel"><div className="catalog-heading"><h2>Ventas reportadas</h2><button className="button button-outline" aria-expanded={open} onClick={() => setOpen(!open)}>{open ? "Cerrar ventas" : "Ver ventas"}</button></div>{open && <SalesList key={business} business={business} />}</section>;
}
function SalesList({ business }: { business: string }) {
  const [items, setItems] = useState<Sale[]>([]), [page, setPage] = useState(1), [more, setMore] = useState(false);
  const [attempt, setAttempt] = useState(0), [loading, setLoading] = useState(true), [error, setError] = useState("");
  const [creating, setCreating] = useState(false), [selected, setSelected] = useState<Sale | null>(null), [notice, setNotice] = useState("");
  function refresh() { setError(""); setLoading(true); setAttempt(n => n + 1); }
  useEffect(() => {
    const controller = new AbortController();
    void catalogApi(`/businesses/${business}/sales?page=${page}`, "GET", undefined, controller.signal).then(async response => {
      if (!response.ok) throw new Error(await catalogError(response));
      const data = await response.json(); if (!controller.signal.aborted) { setItems(data.items); setMore(data.hasNextPage); }
    }).catch(e => { if (!controller.signal.aborted) setError(e.message); }).finally(() => { if (!controller.signal.aborted) setLoading(false); });
    return () => controller.abort();
  }, [business, page, attempt]);
  return <>
    <p>Ventas declaradas por el negocio. Para convertir una solicitud, abrila en Solicitudes de productos. Las ventas presenciales no se atribuyen a solicitudes.</p>
    <div className="catalog-toolbar"><button className="button button-outline" onClick={() => setCreating(!creating)}>{creating ? "Cerrar venta presencial" : "Nueva venta presencial"}</button><button className="text-link" disabled={loading} onClick={refresh}>Actualizar ventas</button></div>
    {error && <p role="alert">{error}</p>}{notice && <p role="status">{notice}</p>}
    {creating && <SaleEditor business={business} onSaved={() => { setCreating(false); setNotice("Venta registrada."); refresh(); }} />}
    {loading ? <p role="status">Cargando ventas…</p> : !error && <>{!items.length && <p>Todavía no hay ventas registradas.</p>}{items.map(sale => <article className="sale-record" key={sale.id}>
      <h3>{sale.orderReference ?? "Venta presencial"}</h3><p>{new Date(sale.createdAt).toLocaleString("es-AR")} · {sale.voidedAt ? "Anulada" : "Registrada"}</p>
      <ul>{sale.lines.map(line => <li key={line.productId}>{line.quantity} × {line.name} · {money(line.unitPrice, line.currency)} c/u · {originLabel(line.origin)}{sale.voidedAt ? ` · Devueltas: ${line.returnedQuantity}` : ""}</li>)}</ul>
      {Object.entries(sale.totals).map(([origin, totals]) => <div key={origin}><strong>{originLabel(origin)}</strong>{Object.entries(totals).map(([currency, total]) => <p key={currency}>Importe {sale.voidedAt ? "original anulado" : "registrado"}: {money(total, currency)}</p>)}</div>)}
      {sale.voidedAt ? <p>Motivo: {sale.voidReason}</p> : <button className="text-link" aria-label={`Anular venta ${sale.orderReference ?? sale.id}`} onClick={() => setSelected(sale)}>Anular venta</button>}
    </article>)}</>}
    {selected && <VoidSale key={selected.id} business={business} sale={selected} onSaved={() => { setSelected(null); setNotice("Anulación guardada."); refresh(); }} />}
    <nav className="catalog-toolbar" aria-label="Páginas de ventas"><button disabled={loading || page === 1} onClick={() => { setLoading(true); setPage(page - 1); }}>Anterior</button><span>Página {page}</span><button disabled={loading || !more} onClick={() => { setLoading(true); setPage(page + 1); }}>Siguiente</button></nav>
  </>;
}
function VoidSale({ business, sale, onSaved }: { business: string; sale: Sale; onSaved: () => void }) {
  const [reason, setReason] = useState(""), [returns, setReturns] = useState<Record<string, number>>({});
  const operation = useSaleOperation(`cerca-void:${business}:${sale.id}`, onSaved);
  return <form className="catalog-form" onSubmit={e => { e.preventDefault(); void operation.submit(() => ({ path: `/businesses/${business}/sales/${sale.id}/void`, body: {
    operationId: crypto.randomUUID(), reason: reason.trim(), returns: Object.entries(returns).filter(([, quantity]) => quantity > 0).map(([productId, quantity]) => ({ productId, quantity })) } })); }}>
    <h3>Anular venta {sale.orderReference}</h3><p>Esta operación es definitiva. Indicá solo unidades efectivamente devueltas: cero conserva el stock actual. La solicitud vinculada quedará cancelada.</p>
    {operation.error && <p role="alert">{operation.error}</p>}
    <fieldset disabled={operation.locked}><label>Motivo de anulación<input required minLength={3} maxLength={300} value={reason} onChange={e => setReason(e.target.value)} /></label>
      {sale.lines.map(line => <label key={line.productId}>Unidades devueltas de {line.name}<input type="number" required min={0} max={line.quantity} value={returns[line.productId] ?? 0} onChange={e => setReturns({ ...returns, [line.productId]: Number(e.target.value) })} /></label>)}</fieldset>
    <button className="button button-primary" disabled={operation.pending || operation.restoring}>{operation.intent ? "Reintentar anulación" : "Confirmar anulación"}</button>
  </form>;
}
