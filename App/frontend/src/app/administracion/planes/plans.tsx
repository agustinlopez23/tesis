"use client";

import { FormEvent, useEffect, useState } from "react";
import Link from "next/link";
import { catalogApi, catalogError, Plan } from "../../catalog-api";
import { Alert, Badge, EmptyState, Icon, Panel, Skeleton, type Tone } from "../../ui";

type Business = { id: string; name: string; ownerEmail: string };
export default function AdminPlans() {
  const [businesses, setBusinesses] = useState<Business[]>([]);
  const [selected, setSelected] = useState<Business | null>(null);
  const [query, setQuery] = useState("");
  const [draft, setDraft] = useState("");
  const [page, setPage] = useState(1);
  const [error, setError] = useState("");
  const [state, setState] = useState("loading");
  const [attempt, setAttempt] = useState(0);
  useEffect(() => {
    const controller = new AbortController();
    async function load() {
      try {
        const response = await catalogApi(`/admin/businesses?page=${page}&q=${encodeURIComponent(query)}`, "GET", undefined, controller.signal);
        if (response.status === 401 || response.status === 403) { if (!controller.signal.aborted) setState("denied"); return; }
        if (!response.ok) throw new Error(await catalogError(response));
        const data = await response.json();
        if (!controller.signal.aborted) { setBusinesses(data); setState("ready"); }
      } catch (error) { if (!controller.signal.aborted) { setError(error instanceof Error ? error.message : "No pudimos cargar los negocios."); setState("error"); } }
    }
    void load();
    return () => controller.abort();
  }, [query, page, attempt]);
  if (state === "denied") return <Alert tone="danger" role="alert">Necesitás ingresar con una cuenta administradora. <Link href="/mi-negocio">Ir a Mi negocio</Link></Alert>;
  return <>
    <p className="admin-intro">Activá el catálogo con una vigencia definida. Cada activación y revocación conserva su motivo.</p>
    <form role="search" className="search-form admin-search" onSubmit={(e) => { e.preventDefault(); setState("loading"); setQuery(draft.trim()); setPage(1); setAttempt((n) => n + 1); }}>
      <Icon name="search" />
      <label htmlFor="plans-search" className="sr-only">Buscar negocio o correo</label>
      <input id="plans-search" maxLength={150} placeholder="Buscar negocio o correo" value={draft} onChange={(e) => setDraft(e.target.value)} />
      <button className="button button-outline">Buscar negocios</button>
    </form>
    {state === "error" && <><Alert tone="danger" role="alert">{error}</Alert><div className="action-row"><button className="button button-outline" onClick={() => { setState("loading"); setAttempt((n) => n + 1); }}>Reintentar carga</button></div></>}
    {state === "loading" ? <Skeleton variant="rows" count={4} label="Cargando negocios…" /> : state === "ready" && <>
      {businesses.length > 0 && <div className="resource-list">{businesses.map((b) => <article key={b.id} className="resource-row">
        <div className="resource-main"><strong>{b.name}</strong><p>{b.ownerEmail}</p></div>
        <div className="resource-actions"><button className="button button-outline button-small" aria-current={selected?.id === b.id ? "true" : undefined} onClick={() => setSelected(b)}>Gestionar plan de {b.name}</button></div>
      </article>)}</div>}
      {!businesses.length && <EmptyState compact icon="search" title="No se encontraron negocios." />}
      <nav className="admin-pager" aria-label="Páginas de negocios"><button className="button button-outline button-small" disabled={page === 1} onClick={() => { setState("loading"); setPage(page - 1); }}>Anterior</button><span>Página {page}</span><button className="button button-outline button-small" disabled={businesses.length < 20} onClick={() => { setState("loading"); setPage(page + 1); }}>Siguiente</button></nav>
    </>}
    {selected && <PlanEditor key={selected.id} business={selected} />}
  </>;
}

function periodStatus(period: Plan["periods"][number]): { tone: Tone; label: string } {
  if (period.revokedAt) return { tone: "neutral", label: "Revocada" };
  const now = Date.now();
  if (new Date(period.endsAt).getTime() < now) return { tone: "neutral", label: "Finalizada" };
  if (new Date(period.startsAt).getTime() > now) return { tone: "info", label: "Programada" };
  return { tone: "success", label: "Vigente" };
}

function PlanEditor({ business }: { business: Business }) {
  const [plan, setPlan] = useState<Plan | null>(null);
  const [period, setPeriod] = useState({ startsAt: "", endsAt: "", reason: "" });
  const [requestId, setRequestId] = useState<string | null>(null);
  const [revoking, setRevoking] = useState("");
  const [reason, setReason] = useState("");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [attempt, setAttempt] = useState(0);
  useEffect(() => {
    const controller = new AbortController();
    void catalogApi(`/admin/businesses/${business.id}/plan`, "GET", undefined, controller.signal).then(async (response) => {
      if (!response.ok) throw new Error(await catalogError(response));
      const data = await response.json();
      if (!controller.signal.aborted) setPlan(data);
    }).catch((error) => { if (!controller.signal.aborted) setError(error instanceof Error ? error.message : "No pudimos cargar el plan."); });
    return () => controller.abort();
  }, [business.id, attempt]);
  async function grant(event: FormEvent) {
    event.preventDefault(); setPending(true); setError(""); setSuccess("");
    const id = requestId ?? crypto.randomUUID();
    setRequestId(id);
    try {
      const response = await catalogApi(`/admin/businesses/${business.id}/subscriptions/${id}`, "PUT", {
        startsAt: new Date(period.startsAt).toISOString(), endsAt: new Date(period.endsAt).toISOString(), reason: period.reason });
      if (!response.ok) throw new Error(await catalogError(response));
      setRequestId(null); setPeriod({ startsAt: "", endsAt: "", reason: "" }); setSuccess("Vigencia registrada."); setAttempt((n) => n + 1);
    } catch (error) { setError(error instanceof Error ? error.message : "No pudimos registrar la vigencia."); }
    finally { setPending(false); }
  }
  async function revoke(event: FormEvent) {
    event.preventDefault(); setPending(true); setError(""); setSuccess("");
    try {
      const response = await catalogApi(`/admin/businesses/${business.id}/subscriptions/${revoking}/revoke`, "POST", { reason });
      if (!response.ok) throw new Error(await catalogError(response));
      setRevoking(""); setReason(""); setSuccess("Vigencia revocada. Los productos se conservan."); setAttempt((n) => n + 1);
    } catch (error) { setError(error instanceof Error ? error.message : "No pudimos revocar la vigencia."); }
    finally { setPending(false); }
  }
  const date = (value: string) => new Date(value).toLocaleString("es-AR");
  return <Panel className="admin-form admin-plan-editor" eyebrow="Plan del negocio" title={business.name} description={business.ownerEmail}
    actions={<button className="button button-outline button-small" disabled={pending} onClick={() => { setError(""); setAttempt((n) => n + 1); }}>Actualizar plan</button>}>
    {error && <Alert tone="danger" role="alert">{error}</Alert>}{success && <Alert tone="success" role="status">{success}</Alert>}
    <p className="admin-plan-current">{plan ? `Plan actual: ${plan.plan}` : "Consultando vigencias…"}</p>
    <form className="catalog-form" onSubmit={grant}><h3>Habilitar Impulso</h3><fieldset disabled={pending || !plan}>
      <p className="field-help">Las fechas usan la hora local de tu dispositivo. El acceso termina exactamente en la fecha y hora de fin.</p>
      <div className="catalog-fields"><label>Inicio de vigencia<input type="datetime-local" required value={period.startsAt} onChange={(e) => { setRequestId(null); setPeriod({ ...period, startsAt: e.target.value }); }} /></label><label>Fin de vigencia<input type="datetime-local" required value={period.endsAt} onChange={(e) => { setRequestId(null); setPeriod({ ...period, endsAt: e.target.value }); }} /></label></div>
      <label>Motivo de activación<textarea required minLength={3} maxLength={500} value={period.reason} onChange={(e) => { setRequestId(null); setPeriod({ ...period, reason: e.target.value }); }} /></label>
      <div className="action-row"><button className="button button-primary" type="submit">Registrar vigencia</button></div>
    </fieldset></form>
    <h3 className="admin-section-title">Historial de vigencias</h3>
    {plan?.periods.length === 0 && <EmptyState compact icon="clock" title="Sin activaciones registradas." />}
    {plan && plan.periods.length > 0 && <div className="resource-list">{plan.periods.map((p) => {
      const status = periodStatus(p);
      return <article key={p.id} className="resource-row">
        <div className="resource-main"><strong>{date(p.startsAt)} — {date(p.endsAt)}</strong><p>{p.reason}</p>{p.revokedAt && <p>Revocada el {date(p.revokedAt)}: {p.revocationReason}</p>}</div>
        <Badge tone={status.tone}>{status.label}</Badge>
        <div className="resource-actions">{!p.revokedAt && <button className="text-link danger" disabled={pending} onClick={() => { setRevoking(p.id); setReason(""); }}>Revocar vigencia</button>}</div>
        {revoking === p.id && <div className="resource-wide"><Alert tone="warning" title="Revocar esta vigencia">
          <p>Revocar retira el catálogo público si esta vigencia está activa. Los productos permanecen guardados.</p>
          <form className="catalog-form" onSubmit={revoke}><fieldset disabled={pending}>
            <label>Motivo de revocación<textarea required minLength={3} maxLength={500} value={reason} onChange={(e) => setReason(e.target.value)} /></label>
            <div className="action-row"><button className="text-link" type="button" onClick={() => setRevoking("")}>Cancelar</button><button className="button button-outline" type="submit">Confirmar revocación</button></div>
          </fieldset></form>
        </Alert></div>}
      </article>;
    })}</div>}
  </Panel>;
}
