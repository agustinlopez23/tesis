"use client";

import { useEffect, useState } from "react";
import { catalogApi, catalogError } from "./catalog-api";
import { Order, orderStatus } from "./order-api";
import OrderReceipt from "./order-receipt";

type Summary = Pick<Order, "id" | "reference" | "customerName" | "createdAt" | "status" | "contactReceivedAt">;
export default function BusinessOrders({ id }: { id: string }) {
  const [open, setOpen] = useState(false);
  return <section className="catalog-panel"><div className="catalog-heading"><div><p className="eyebrow">Del catálogo a la conversación</p><h2>Solicitudes de productos</h2></div>
    <button className="button button-outline" aria-expanded={open} onClick={() => setOpen(!open)}>{open ? "Cerrar solicitudes" : "Ver solicitudes"}</button></div>
    {open && <OrderList key={id} business={id} />}
  </section>;
}

function OrderList({ business }: { business: string }) {
  const [items, setItems] = useState<Summary[]>([]);
  const [page, setPage] = useState(1);
  const [more, setMore] = useState(false);
  const [filter, setFilter] = useState({ reference: "", status: "" });
  const [draft, setDraft] = useState({ reference: "", status: "" });
  const [selected, setSelected] = useState("");
  const [attempt, setAttempt] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  useEffect(() => {
    const controller = new AbortController();
    const query = new URLSearchParams({ page: String(page), reference: filter.reference });
    if (filter.status) query.set("status", filter.status);
    void catalogApi(`/businesses/${business}/orders?${query}`, "GET", undefined, controller.signal).then(async response => {
      if (!response.ok) throw new Error(await catalogError(response));
      const data = await response.json();
      if (!controller.signal.aborted) { setItems(data.items); setMore(data.hasNextPage); }
    }).catch(error => { if (!controller.signal.aborted) setError(error instanceof Error ? error.message : "No pudimos cargar las solicitudes."); })
      .finally(() => { if (!controller.signal.aborted) setLoading(false); });
    return () => controller.abort();
  }, [business, filter, page, attempt]);
  function refresh() { setError(""); setLoading(true); setAttempt(n => n + 1); }
  return <>
    <p>Una solicitud puede existir aunque el cliente todavía no haya enviado el mensaje. Podés resolver las recibidas aunque venza tu plan.</p>
    <form className="catalog-search" onSubmit={e => { e.preventDefault(); setFilter({ reference: draft.reference.trim(), status: draft.status }); setPage(1); refresh(); }}>
      <label>Referencia de solicitud<input maxLength={40} placeholder="CE-…" value={draft.reference} onChange={e => setDraft({ ...draft, reference: e.target.value })} /></label>
      <label>Filtrar por estado<select value={draft.status} onChange={e => setDraft({ ...draft, status: e.target.value })}><option value="">Todos</option><option value="pending">Pendiente</option><option value="in_attention">En atención</option><option value="cancelled">Cancelada</option></select></label>
      <button className="button button-outline">Buscar solicitudes</button>
    </form>
    {error && <p role="alert" className="error">{error}</p>}
    <button className="text-link" disabled={loading} onClick={refresh}>Actualizar solicitudes</button>
    {loading ? <p role="status">Cargando solicitudes…</p> : !error && <>
      {!items.length && <p>No hay solicitudes para estos filtros.</p>}
      <div className="catalog-owner-list">{items.map(item => <article key={item.id}><div><strong>{item.reference} · {item.customerName}</strong><p>{orderStatus(item.status)} · {new Date(item.createdAt).toLocaleString("es-AR")}</p></div><button className="text-link" aria-label={`Abrir ${item.reference}`} onClick={() => setSelected(item.id)}>Abrir solicitud</button></article>)}</div>
      <nav className="catalog-toolbar" aria-label="Páginas de solicitudes"><button className="button button-outline" disabled={page === 1} onClick={() => { setLoading(true); setPage(page - 1); }}>Anterior</button><span>Página {page}</span><button className="button button-outline" disabled={!more} onClick={() => { setLoading(true); setPage(page + 1); }}>Siguiente</button></nav>
    </>}
    {selected && <OrderManager key={selected} business={business} id={selected} onSaved={refresh} />}
  </>;
}

function OrderManager({ business, id, onSaved }: { business: string; id: string; onSaved: () => void }) {
  const [order, setOrder] = useState<Order | null>(null);
  const [status, setStatus] = useState("pending");
  const [contact, setContact] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [pending, setPending] = useState(false);
  const [loading, setLoading] = useState(true);
  const [attempt, setAttempt] = useState(0);
  const path = `/businesses/${business}/orders/${id}`;
  useEffect(() => {
    const controller = new AbortController();
    void catalogApi(path, "GET", undefined, controller.signal).then(async response => {
      if (!response.ok) throw new Error(await catalogError(response));
      const data: Order = await response.json();
      if (!controller.signal.aborted) { setOrder(data); setStatus(data.status); setContact(!!data.contactReceivedAt); }
    }).catch(error => { if (!controller.signal.aborted) setError(error instanceof Error ? error.message : "No pudimos cargar la solicitud."); })
      .finally(() => { if (!controller.signal.aborted) setLoading(false); });
    return () => controller.abort();
  }, [path, attempt]);
  async function save() {
    if (!order) return;
    setPending(true); setError(""); setSuccess("");
    try {
      const response = await catalogApi(path, "PUT", { version: order.version, status, contactReceived: contact });
      if (!response.ok) throw new Error(await catalogError(response));
      const data: Order = await response.json(); setOrder(data); setStatus(data.status); setContact(!!data.contactReceivedAt);
      setSuccess("Estado guardado."); onSaved();
    } catch (error) { setError(error instanceof Error ? error.message : "No pudimos guardar el estado."); }
    finally { setPending(false); }
  }
  return <div className="order-manager">
    {error && <p role="alert" className="error">{error}</p>}{success && <p role="status" className="success-message">{success}</p>}
    <button className="text-link" disabled={pending || loading} onClick={() => { setError(""); setLoading(true); setAttempt(n => n + 1); }}>Actualizar esta solicitud</button>
    {loading ? <p role="status">Consultando detalle…</p> : order && <>
      <OrderReceipt order={order} />
      <form className="catalog-form" onSubmit={e => { e.preventDefault(); void save(); }}><fieldset disabled={pending || loading || order.status === "cancelled"}>
        <label>Estado de la solicitud<select value={status} onChange={e => setStatus(e.target.value)}><option value="pending" disabled={order.status !== "pending"}>Pendiente</option><option value="in_attention">En atención</option><option value="cancelled">Cancelada</option></select></label>
        <label className="order-contact-check"><input type="checkbox" checked={contact} disabled={!!order.contactReceivedAt} onChange={e => setContact(e.target.checked)} />Confirmo que recibí el contacto del cliente</label>
        <p className="field-help">Confirmar contacto deja constancia y no se puede desmarcar. Cancelar es definitivo y no modifica stock.</p>
        <button className="button button-primary" type="submit">Guardar estado</button>
      </fieldset></form>
    </>}
  </div>;
}
