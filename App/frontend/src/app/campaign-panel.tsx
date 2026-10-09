"use client";

import { useEffect, useState } from "react";
import Image from "next/image";
import { catalogApi, catalogError } from "./catalog-api";
import { Campaign, campaignState } from "./campaign-api";
import { useSaleOperation } from "./use-sale-operation";
import CampaignEditor from "./campaign-editor";
import { money } from "./order-api";
import { useBusinessRefresh } from "./business-refresh";

export default function CampaignPanel({ business, admin = false }: { business?: string; admin?: boolean }) {
  const [open, setOpen] = useState(admin);
  return <section className="catalog-panel"><div className="catalog-heading"><h2>Campañas publicitarias</h2>{!admin && <button className="button button-outline" aria-expanded={open} onClick={() => setOpen(!open)}>{open ? "Cerrar campañas" : "Ver campañas"}</button>}</div>
    {open && <CampaignList key={business ?? "admin"} business={business} admin={admin} />}</section>;
}
function CampaignList({ business, admin }: { business?: string; admin: boolean }) {
  const [items, setItems] = useState<Campaign[]>([]), [page, setPage] = useState(1), [more, setMore] = useState(false);
  const [attempt, setAttempt] = useState(0), [loading, setLoading] = useState(true), [error, setError] = useState("");
  const [editing, setEditing] = useState<Campaign | "new" | null>(null), [selected, setSelected] = useState<Campaign | null>(null), [metrics, setMetrics] = useState<Campaign | null>(null);
  const path = admin ? "/admin/campaigns" : `/businesses/${business}/campaigns`;
  function refresh() { setError(""); setLoading(true); setAttempt(n => n + 1); }
  useBusinessRefresh(business ?? "", refresh);
  function saved() { setEditing(null); setSelected(null); setMetrics(null); refresh(); }
  useEffect(() => {
    const controller = new AbortController();
    void catalogApi(`${path}?page=${page}`, "GET", undefined, controller.signal).then(async response => {
      if (!response.ok) throw new Error(response.status === 403 ? "Necesitás una cuenta administradora." : await catalogError(response));
      const data = await response.json(); if (!controller.signal.aborted) { setItems(data.items); setMore(data.hasNextPage); }
    }).catch(e => { if (!controller.signal.aborted) setError(e.message); }).finally(() => { if (!controller.signal.aborted) setLoading(false); });
    return () => controller.abort();
  }, [path, page, attempt]);
  return <><p>La difusión requiere revisión y confirmación manual de pago. No modifica el orden orgánico ni garantiza vistas o ventas. Hasta tres campañas contratadas por zona en fechas superpuestas.</p>
    <div className="catalog-toolbar">{!admin && <button className="button button-outline" onClick={() => setEditing(editing ? null : "new")}>{editing ? "Cerrar edición de campaña" : "Preparar campaña"}</button>}<button className="text-link" disabled={loading} onClick={refresh}>Actualizar campañas</button></div>
    {error && <p role="alert">{error}</p>}{editing && <CampaignEditor key={editing === "new" ? "new" : editing.id} business={business!} campaign={editing === "new" ? undefined : editing} onSaved={saved} />}
    {loading ? <p role="status">Cargando campañas…</p> : !error && <>{!items.length && <p>No hay campañas registradas.</p>}{items.map(c => <article className="campaign-record" key={c.id}>
      <h3>{c.title}</h3><p>{c.businessName} · {campaignState(c.deliveryState)} · {c.zoneId}</p><p>{new Date(c.startsAt).toLocaleString("es-AR")} — {new Date(c.endsAt).toLocaleString("es-AR")}</p>
      {c.hasImage && <Image src={`/api/marketplace/public/campaigns/${c.id}/image`} alt={c.title} width={320} height={200} unoptimized />}
      <p>{c.description}</p><p>Condiciones: {c.conditions}</p>
      {c.deliveryState === "blocked" && <p>La campaña está oculta por el estado del negocio, catálogo o stock. El período no se extiende automáticamente; la administración puede registrar su resolución.</p>}
      {admin ? <button className="text-link" aria-label={`Gestionar ${c.title}`} onClick={() => setSelected(c)}>Gestionar campaña</button> : <div className="catalog-toolbar">
        {(c.status === "draft" || c.status === "rejected") && <button className="text-link" onClick={() => setEditing(c)}>Editar {c.title}</button>}
        {c.status === "scheduled" && <button className="text-link" onClick={() => setSelected(c)}>Pausar {c.title}</button>}
        <button className="text-link" onClick={() => setMetrics(c)}>Resultados de {c.title}</button></div>}
    </article>)}</>}
    {selected && <CampaignAction key={`${selected.id}:${selected.version}`} campaign={selected} admin={admin} onSaved={saved} />}
    {metrics && <CampaignResults key={metrics.id} campaign={metrics} />}
    <nav className="catalog-toolbar" aria-label="Páginas de campañas"><button disabled={loading || page === 1} onClick={() => { setLoading(true); setPage(page - 1); }}>Anterior</button><span>Página {page}</span><button disabled={loading || !more} onClick={() => { setLoading(true); setPage(page + 1); }}>Siguiente</button></nav>
  </>;
}
function CampaignAction({ campaign, admin, onSaved }: { campaign: Campaign; admin: boolean; onSaved: () => void }) {
  const choices = !admin ? ["pause"] : campaign.status === "pending_review" ? ["approve", "reject"] : campaign.status === "awaiting_payment" ? ["confirm_payment", "reject"] : campaign.status === "scheduled" ? ["pause", "resolve"] : campaign.status === "paused" ? ["resume", "resolve"] : ["resolve"];
  const [action, setAction] = useState(choices[0]), [reason, setReason] = useState(""), [payment, setPayment] = useState("");
  const operation = useSaleOperation(`cerca-campaign-action:${campaign.id}:${admin}`, onSaved);
  return <form className="catalog-form" onSubmit={e => { e.preventDefault(); void operation.submit(() => ({ path: admin ? `/admin/campaigns/${campaign.id}/actions` : `/businesses/${campaign.businessId}/campaigns/${campaign.id}/pause`,
    body: { operationId: crypto.randomUUID(), version: campaign.version, action, reason: reason.trim(), paymentReference: action === "confirm_payment" ? payment.trim() : null } })); }}>
    <h3>{campaign.title}</h3>{operation.error && <p role="alert">{operation.error}</p>}
    <fieldset disabled={operation.locked}><label>Decisión administrativa<select value={action} onChange={e => setAction(e.target.value)}>{choices.map(value => <option key={value} value={value}>{{ approve: "Aprobar contenido", reject: "Rechazar", confirm_payment: "Registrar pago confirmado", pause: "Pausar difusión", resume: "Reanudar difusión", resolve: "Registrar resolución comercial" }[value]}</option>)}</select></label>
      <label>Motivo de la decisión<input required minLength={3} maxLength={300} value={reason} onChange={e => setReason(e.target.value)} /></label>
      {action === "confirm_payment" && <><label>Referencia del pago verificado<input required minLength={3} maxLength={120} value={payment} onChange={e => setPayment(e.target.value)} /></label><label className="order-contact-check"><input type="checkbox" required />Confirmo que verifiqué el pago por fuera de la plataforma</label><p>Registrar esta confirmación no realiza un cobro.</p></>}
    </fieldset><button className="button button-primary" disabled={operation.pending || operation.restoring}>{operation.intent ? "Reintentar decisión" : "Guardar decisión"}</button>
  </form>;
}
type Metrics = { eventDays: number; impressions: number; clicks: number; orders: number; activeSales: number; amounts: Record<string, Record<string, number>>; changes: { version: number; action: string; reason: string; createdAt: string }[] };
function CampaignResults({ campaign }: { campaign: Campaign }) {
  const [data, setData] = useState<Metrics | null>(null), [error, setError] = useState("");
  const [attempt, setAttempt] = useState(0);
  useBusinessRefresh(campaign.businessId, () => { setData(null); setError(""); setAttempt(n => n + 1); });
  useEffect(() => { const controller = new AbortController(); void catalogApi(`/businesses/${campaign.businessId}/campaigns/${campaign.id}/metrics`, "GET", undefined, controller.signal).then(async response => {
    if (!response.ok) throw new Error(await catalogError(response)); const value = await response.json(); if (!controller.signal.aborted) setData(value);
  }).catch(e => { if (!controller.signal.aborted) setError(e.message); }); return () => controller.abort(); }, [campaign, attempt]);
  return <section><h3>Resultados de {campaign.title}</h3>{error ? <p role="alert">{error}</p> : !data ? <p role="status">Consultando resultados…</p> : <>
    <p>Últimos {data.eventDays} días: {data.impressions} apariciones visibles · {data.clicks} clics.</p><p>Historial de la campaña: {data.orders} solicitudes · {data.activeSales} ventas vigentes reportadas.</p>
    {Object.entries(data.amounts).map(([origin, totals]) => <div key={origin}><strong>{origin === "requested" ? "Productos solicitados" : "Acordado en conversación"}</strong>{Object.entries(totals).map(([currency, total]) => <p key={currency}>{money(total, currency)}</p>)}</div>)}
    <p>Los clics no confirman mensajes ni pagos. Importes sin ventas anuladas; no se mezclan monedas.</p><details><summary>Últimos 100 cambios registrados</summary><ul>{data.changes.map(change => <li key={change.version}>{new Date(change.createdAt).toLocaleString("es-AR")}: {change.reason}</li>)}</ul></details>
  </>}</section>;
}
