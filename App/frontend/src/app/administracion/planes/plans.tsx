"use client";

import { FormEvent, useEffect, useState } from "react";
import Link from "next/link";
import { catalogApi, catalogError, Plan } from "../../catalog-api";

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
  if (state === "denied") return <p role="alert">Necesitás ingresar con una cuenta administradora. <Link href="/mi-negocio">Ir a Mi negocio</Link></p>;
  return <>
    <p>Activá el catálogo con una vigencia definida. Cada activación y revocación conserva su motivo.</p>
    <form className="catalog-search" onSubmit={(e) => { e.preventDefault(); setState("loading"); setQuery(draft.trim()); setPage(1); setAttempt((n) => n + 1); }}>
      <label>Buscar negocio o correo<input maxLength={150} value={draft} onChange={(e) => setDraft(e.target.value)} /></label><button className="button button-outline">Buscar negocios</button>
    </form>
    {state === "error" && <><p role="alert">{error}</p><button className="button button-outline" onClick={() => { setState("loading"); setAttempt((n) => n + 1); }}>Reintentar carga</button></>}
    {state === "loading" ? <p role="status">Cargando negocios…</p> : state === "ready" && <>
      <div className="catalog-owner-list">{businesses.map((b) => <article key={b.id}><div><strong>{b.name}</strong><p>{b.ownerEmail}</p></div><button className="text-link" onClick={() => setSelected(b)}>Gestionar plan de {b.name}</button></article>)}</div>
      {!businesses.length && <p>No se encontraron negocios.</p>}
      <nav className="catalog-toolbar" aria-label="Páginas de negocios"><button className="button button-outline" disabled={page === 1} onClick={() => { setState("loading"); setPage(page - 1); }}>Anterior</button><span>Página {page}</span><button className="button button-outline" disabled={businesses.length < 20} onClick={() => { setState("loading"); setPage(page + 1); }}>Siguiente</button></nav>
    </>}
    {selected && <PlanEditor key={selected.id} business={selected} />}
  </>;
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
  return <section className="catalog-panel">
    <div className="catalog-heading"><h2>{business.name}</h2><button className="text-link" disabled={pending} onClick={() => { setError(""); setAttempt((n) => n + 1); }}>Actualizar plan</button></div>
    {error && <p role="alert" className="error">{error}</p>}{success && <p role="status" className="success-message">{success}</p>}
    <p>{plan ? `Plan actual: ${plan.plan}` : "Consultando vigencias…"}</p>
    <form className="catalog-form" onSubmit={grant}><h3>Habilitar Impulso</h3><fieldset disabled={pending || !plan}>
      <p className="field-help">Las fechas usan la hora local de tu dispositivo. El acceso termina exactamente en la fecha y hora de fin.</p>
      <div className="catalog-fields"><label>Inicio de vigencia<input type="datetime-local" required value={period.startsAt} onChange={(e) => { setRequestId(null); setPeriod({ ...period, startsAt: e.target.value }); }} /></label><label>Fin de vigencia<input type="datetime-local" required value={period.endsAt} onChange={(e) => { setRequestId(null); setPeriod({ ...period, endsAt: e.target.value }); }} /></label></div>
      <label>Motivo de activación<textarea required minLength={3} maxLength={500} value={period.reason} onChange={(e) => { setRequestId(null); setPeriod({ ...period, reason: e.target.value }); }} /></label>
      <button className="button button-primary" type="submit">Registrar vigencia</button>
    </fieldset></form>
    <h3>Historial de vigencias</h3>
    {plan?.periods.length === 0 && <p>Sin activaciones registradas.</p>}
    <div className="catalog-owner-list">{plan?.periods.map((p) => <article key={p.id}><div><strong>{date(p.startsAt)} — {date(p.endsAt)}</strong><p>{p.reason}</p>{p.revokedAt && <p>Revocada el {date(p.revokedAt)}: {p.revocationReason}</p>}</div>
      {!p.revokedAt && <button className="text-link" disabled={pending} onClick={() => { setRevoking(p.id); setReason(""); }}>Revocar vigencia</button>}
    </article>)}</div>
    {revoking && <form className="catalog-form" onSubmit={revoke}><fieldset disabled={pending}><p>Revocar retira el catálogo público si esta vigencia está activa. Los productos permanecen guardados.</p><label>Motivo de revocación<textarea required minLength={3} maxLength={500} value={reason} onChange={(e) => setReason(e.target.value)} /></label><div className="catalog-toolbar"><button className="button button-outline" type="submit">Confirmar revocación</button><button className="text-link" type="button" onClick={() => setRevoking("")}>Cancelar</button></div></fieldset></form>}
  </section>;
}
