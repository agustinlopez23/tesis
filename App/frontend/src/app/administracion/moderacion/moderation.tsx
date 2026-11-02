"use client";

import { FormEvent, useEffect, useState } from "react";
import Link from "next/link";
import { catalogApi, catalogError } from "../../catalog-api";

type Kind = "accounts" | "businesses" | "products";
type Row = { id: string; label: string; detail: string; isSuspended: boolean };
type ApiRow = {
  id: string; isSuspended: boolean; email?: string; displayName?: string; businessCount?: number; name?: string;
  ownerEmail?: string; category?: string; status?: string; businessName?: string;
};
const tabs: { kind: Kind; label: string; noun: string; suspend: string; done: string; restored: string }[] = [
  { kind: "accounts", label: "Cuentas", noun: "cuentas", suspend: "Suspender", done: "Cuenta suspendida", restored: "Cuenta reactivada" },
  { kind: "businesses", label: "Negocios", noun: "negocios", suspend: "Suspender", done: "Negocio suspendido", restored: "Negocio reactivado" },
  { kind: "products", label: "Productos", noun: "productos", suspend: "Ocultar", done: "Producto oculto", restored: "Producto reactivado" },
];
const statusLabel = (value?: string) => value === "published" ? "publicado" : value === "draft" ? "borrador" : "archivado";
function toRow(kind: Kind, item: ApiRow): Row {
  if (kind === "accounts") return { id: item.id, label: item.email ?? "", isSuspended: item.isSuspended,
    detail: `${item.displayName} · ${item.businessCount} ${item.businessCount === 1 ? "negocio" : "negocios"}` };
  if (kind === "businesses") return { id: item.id, label: item.name ?? "", isSuspended: item.isSuspended,
    detail: `${item.category} · ${item.ownerEmail} · ${statusLabel(item.status)}` };
  return { id: item.id, label: item.name ?? "", isSuspended: item.isSuspended, detail: `${item.businessName} · ${statusLabel(item.status)}` };
}

export default function AdminModeration() {
  const [kind, setKind] = useState<Kind>("accounts");
  const [rows, setRows] = useState<Row[]>([]);
  const [draft, setDraft] = useState("");
  const [query, setQuery] = useState("");
  const [page, setPage] = useState(1);
  const [state, setState] = useState("loading");
  const [attempt, setAttempt] = useState(0);
  const [target, setTarget] = useState<Row | null>(null);
  const [reason, setReason] = useState("");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const tab = tabs.find((item) => item.kind === kind)!;
  useEffect(() => {
    const controller = new AbortController();
    void catalogApi(`/admin/moderation/${kind}?page=${page}&q=${encodeURIComponent(query)}`, "GET", undefined, controller.signal).then(async (response) => {
      if (response.status === 401 || response.status === 403) { if (!controller.signal.aborted) setState("denied"); return; }
      if (!response.ok) throw new Error(await catalogError(response));
      const data: ApiRow[] = await response.json();
      if (!controller.signal.aborted) { setRows(data.map((item) => toRow(kind, item))); setState("ready"); }
    }).catch((failure) => { if (!controller.signal.aborted) { setError(failure instanceof Error ? failure.message : "No pudimos cargar la lista."); setState("error"); } });
    return () => controller.abort();
  }, [kind, query, page, attempt]);
  async function confirm(event: FormEvent) {
    event.preventDefault();
    if (!target) return;
    const suspend = !target.isSuspended;
    setPending(true); setError(""); setSuccess("");
    try {
      const response = await catalogApi(`/admin/moderation/${kind}/${target.id}/${suspend ? "suspend" : "reinstate"}`, "POST", { reason: reason.trim() });
      if (!response.ok) throw new Error(await catalogError(response));
      setSuccess(`${suspend ? tab.done : tab.restored}. Quedó registrado en la auditoría.`);
      setTarget(null); setReason(""); setAttempt((n) => n + 1);
    } catch (failure) { setError(failure instanceof Error ? failure.message : "No pudimos aplicar la moderación."); }
    finally { setPending(false); }
  }
  if (state === "denied") return <p role="alert">Necesitás ingresar con una cuenta administradora. <Link href="/mi-negocio">Ir a Mi negocio</Link></p>;
  return <>
    <p>Suspender una cuenta impide su ingreso y retira sus negocios de la guía. Suspender un negocio u ocultar un producto lo retira de las búsquedas públicas. Cada acción exige un motivo y queda en la auditoría.</p>
    <div role="tablist" aria-label="Elementos a moderar" className="catalog-toolbar">{tabs.map((item) => <button key={item.kind} type="button" role="tab" aria-selected={item.kind === kind}
      className={item.kind === kind ? "button button-primary" : "button button-outline"}
      onClick={() => { setKind(item.kind); setPage(1); setTarget(null); setRows([]); setState("loading"); setError(""); setSuccess(""); }}>{item.label}</button>)}</div>
    <form className="catalog-search" onSubmit={(e) => { e.preventDefault(); setState("loading"); setQuery(draft.trim()); setPage(1); setAttempt((n) => n + 1); }}>
      <label>Buscar {tab.noun}<input maxLength={150} value={draft} onChange={(e) => setDraft(e.target.value)} /></label><button className="button button-outline">Buscar</button>
    </form>
    {error && <p role="alert" className="error">{error}</p>}{success && <p role="status" className="success-message">{success}</p>}
    {state === "error" && <button className="button button-outline" onClick={() => { setState("loading"); setAttempt((n) => n + 1); }}>Reintentar carga</button>}
    {state === "loading" ? <p>Cargando…</p> : state === "ready" && <>
      <div className="catalog-owner-list">{rows.map((row) => <article key={row.id}><div><strong>{row.label}</strong><p>{row.detail}{row.isSuspended ? (kind === "products" ? " · Oculto" : " · Suspendido") : ""}</p></div>
        <button className="text-link" disabled={pending} onClick={() => { setTarget(row); setReason(""); }}>{row.isSuspended ? `Reactivar ${row.label}` : `${tab.suspend} ${row.label}`}</button></article>)}</div>
      {!rows.length && <p>No se encontraron resultados.</p>}
      <nav className="catalog-toolbar" aria-label="Páginas de resultados"><button className="button button-outline" disabled={page === 1} onClick={() => { setState("loading"); setPage(page - 1); }}>Anterior</button><span>Página {page}</span><button className="button button-outline" disabled={rows.length < 20} onClick={() => { setState("loading"); setPage(page + 1); }}>Siguiente</button></nav>
    </>}
    {target && <form className="catalog-form" onSubmit={confirm}><fieldset disabled={pending}>
      <h2>{target.isSuspended ? "Reactivar" : tab.suspend} {target.label}</h2>
      <label>Motivo<textarea required minLength={3} maxLength={500} value={reason} onChange={(e) => setReason(e.target.value)} /></label>
      <div className="catalog-toolbar"><button className="button button-primary" type="submit">{target.isSuspended ? "Confirmar reactivación" : "Confirmar suspensión"}</button><button className="text-link" type="button" onClick={() => setTarget(null)}>Cancelar</button></div>
    </fieldset></form>}
  </>;
}
