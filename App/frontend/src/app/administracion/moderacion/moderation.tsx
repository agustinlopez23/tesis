"use client";

import { FormEvent, useEffect, useState } from "react";
import Link from "next/link";
import { catalogApi, catalogError } from "../../catalog-api";
import { Alert, Badge, EmptyState, Icon, Panel, SectionNav, Skeleton } from "../../ui";

type Kind = "accounts" | "businesses" | "products";
type StatusFilter = "all" | "active" | "suspended";
type Row = { id: string; label: string; detail: string; isSuspended: boolean };
type ApiRow = {
  id: string; isSuspended: boolean; email?: string; displayName?: string; businessCount?: number; name?: string;
  ownerEmail?: string; category?: string; status?: string; businessName?: string;
};
const tabs: { kind: Kind; label: string; noun: string; suspend: string; done: string; restored: string; active: string; suspended: string }[] = [
  { kind: "accounts", label: "Cuentas", noun: "cuentas", suspend: "Suspender", done: "Cuenta suspendida", restored: "Cuenta reactivada", active: "Activas", suspended: "Suspendidas" },
  { kind: "businesses", label: "Negocios", noun: "negocios", suspend: "Suspender", done: "Negocio suspendido", restored: "Negocio reactivado", active: "Activos", suspended: "Suspendidos" },
  { kind: "products", label: "Productos", noun: "productos", suspend: "Ocultar", done: "Producto oculto", restored: "Producto reactivado", active: "Visibles", suspended: "Ocultos" },
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
  const [filter, setFilter] = useState<StatusFilter>("all");
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
  if (state === "denied") return <Alert tone="danger" role="alert">Necesitás ingresar con una cuenta administradora. <Link href="/mi-negocio">Ir a Mi negocio</Link></Alert>;
  // Status tabs filter the current page on the client; the API keeps serving the full page.
  const suspendedCount = rows.filter((row) => row.isSuspended).length;
  const visible = rows.filter((row) => filter === "all" || (filter === "suspended") === row.isSuspended);
  const statusItems = [
    { id: "all", label: "Todas", count: rows.length },
    { id: "active", label: tab.active, count: rows.length - suspendedCount },
    { id: "suspended", label: tab.suspended, count: suspendedCount },
  ];
  return <>
    <p className="admin-intro">Suspender una cuenta impide su ingreso y retira sus negocios de la guía. Suspender un negocio u ocultar un producto lo retira de las búsquedas públicas. Cada acción exige un motivo y queda en la auditoría.</p>
    <div role="tablist" aria-label="Elementos a moderar" className="action-row admin-kind-tabs">{tabs.map((item) => <button key={item.kind} type="button" role="tab" aria-selected={item.kind === kind}
      className={item.kind === kind ? "button button-small button-dark" : "button button-small button-outline"}
      onClick={() => { setKind(item.kind); setPage(1); setFilter("all"); setTarget(null); setRows([]); setState("loading"); setError(""); setSuccess(""); }}>{item.label}</button>)}</div>
    <form role="search" className="search-form admin-search" onSubmit={(e) => { e.preventDefault(); setState("loading"); setQuery(draft.trim()); setPage(1); setAttempt((n) => n + 1); }}>
      <Icon name="search" />
      <label htmlFor="moderation-search" className="sr-only">Buscar {tab.noun}</label>
      <input id="moderation-search" maxLength={150} placeholder={`Buscar ${tab.noun}`} value={draft} onChange={(e) => setDraft(e.target.value)} />
      <button className="button button-outline">Buscar</button>
    </form>
    {error && <Alert tone="danger" role="alert">{error}</Alert>}{success && <Alert tone="success" role="status">{success}</Alert>}
    {state === "error" && <div className="action-row"><button className="button button-outline" onClick={() => { setState("loading"); setAttempt((n) => n + 1); }}>Reintentar carga</button></div>}
    {state === "loading" ? <Skeleton variant="rows" count={4} label="Cargando…" /> : state === "ready" && <>
      <div className="admin-filter"><SectionNav items={statusItems} current={filter} onSelect={(id) => setFilter(id as StatusFilter)} label="Filtrar por estado" /></div>
      {visible.length > 0 && <div className="resource-list">{visible.map((row) => <article key={row.id} className="resource-row">
        <div className="resource-main"><strong>{row.label}</strong><p>{row.detail}</p></div>
        <Badge tone={row.isSuspended ? "danger" : "success"}>{row.isSuspended ? (kind === "products" ? "Oculto" : "Suspendido") : (kind === "products" ? "Visible" : "Activo")}</Badge>
        <div className="resource-actions"><button className="text-link" disabled={pending} onClick={() => { setTarget(row); setReason(""); }}>{row.isSuspended ? `Reactivar ${row.label}` : `${tab.suspend} ${row.label}`}</button></div>
      </article>)}</div>}
      {!rows.length && <EmptyState compact icon="search" title="No se encontraron resultados." />}
      {rows.length > 0 && !visible.length && <EmptyState compact icon="list" title="No hay elementos con ese estado en esta página." />}
      <nav className="admin-pager" aria-label="Páginas de resultados"><button className="button button-outline button-small" disabled={page === 1} onClick={() => { setState("loading"); setPage(page - 1); }}>Anterior</button><span>Página {page}</span><button className="button button-outline button-small" disabled={rows.length < 20} onClick={() => { setState("loading"); setPage(page + 1); }}>Siguiente</button></nav>
    </>}
    {target && <Panel className="admin-form" title={`${target.isSuspended ? "Reactivar" : tab.suspend} ${target.label}`} description="El motivo queda registrado en la auditoría.">
      <form className="catalog-form" onSubmit={confirm}><fieldset disabled={pending}>
        <label>Motivo<textarea required minLength={3} maxLength={500} value={reason} onChange={(e) => setReason(e.target.value)} /></label>
        <div className="action-row"><button className="text-link" type="button" onClick={() => setTarget(null)}>Cancelar</button><button className="button button-primary" type="submit">{target.isSuspended ? "Confirmar reactivación" : "Confirmar suspensión"}</button></div>
      </fieldset></form>
    </Panel>}
  </>;
}
