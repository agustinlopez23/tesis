"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { catalogApi, catalogError } from "../../catalog-api";
import { Alert, Badge, EmptyState, Skeleton, type Tone } from "../../ui";

type Action = { id: number; actorEmail: string; targetType: string; targetId: string; targetLabel: string; action: string; reason: string | null; createdAt: string };
const targets: Record<string, string> = { account: "Cuenta", business: "Negocio", product: "Producto", category: "Rubro" };
const actions: Record<string, string> = { suspend: "Suspensión", reinstate: "Reactivación", create: "Alta", update: "Modificación" };
const actionTone: Record<string, Tone> = { suspend: "danger", reinstate: "success", create: "info", update: "neutral" };

export default function AdminAudit() {
  const [items, setItems] = useState<Action[]>([]);
  const [page, setPage] = useState(1);
  const [hasNext, setHasNext] = useState(false);
  const [state, setState] = useState("loading");
  const [error, setError] = useState("");
  const [attempt, setAttempt] = useState(0);
  useEffect(() => {
    const controller = new AbortController();
    void catalogApi(`/admin/audit?page=${page}`, "GET", undefined, controller.signal).then(async (response) => {
      if (response.status === 401 || response.status === 403) { if (!controller.signal.aborted) setState("denied"); return; }
      if (!response.ok) throw new Error(await catalogError(response));
      const data = await response.json();
      if (!controller.signal.aborted) { setItems(data.items); setHasNext(data.hasNextPage); setState("ready"); }
    }).catch((failure) => { if (!controller.signal.aborted) { setError(failure instanceof Error ? failure.message : "No pudimos cargar la auditoría."); setState("error"); } });
    return () => controller.abort();
  }, [page, attempt]);
  if (state === "denied") return <Alert tone="danger" role="alert">Necesitás ingresar con una cuenta administradora. <Link href="/mi-negocio">Ir a Mi negocio</Link></Alert>;
  if (state === "error") return <><Alert tone="danger" role="alert">{error}</Alert><div className="action-row"><button className="button button-outline" onClick={() => { setState("loading"); setAttempt((n) => n + 1); }}>Reintentar carga</button></div></>;
  if (state === "loading") return <Skeleton variant="rows" count={5} label="Cargando auditoría…" />;
  return <>
    <p className="admin-intro">Registro de las acciones sensibles de administración, de la más reciente a la más antigua.</p>
    {!items.length ? <EmptyState compact icon="list" title="Todavía no hay acciones registradas." /> : <div className="audit-list"><div role="region" aria-label="Tabla de auditoría" tabIndex={0}><table>
      <thead><tr><th scope="col">Fecha</th><th scope="col">Acción</th><th scope="col">Tipo</th><th scope="col">Destino</th><th scope="col">Administrador</th><th scope="col">Motivo</th></tr></thead>
      <tbody>{items.map((item) => <tr key={item.id}>
        <td><time dateTime={item.createdAt}>{new Date(item.createdAt).toLocaleString("es-AR")}</time></td>
        <td><Badge tone={actionTone[item.action] ?? "neutral"}>{actions[item.action] ?? item.action}</Badge></td>
        <td>{targets[item.targetType] ?? item.targetType}</td>
        <td><strong>{item.targetLabel}</strong></td><td>{item.actorEmail}</td><td className="audit-reason">{item.reason ?? "—"}</td></tr>)}</tbody>
    </table></div></div>}
    <nav className="admin-pager" aria-label="Páginas de auditoría"><button className="button button-outline button-small" disabled={page === 1} onClick={() => { setState("loading"); setPage(page - 1); }}>Anterior</button><span>Página {page}</span><button className="button button-outline button-small" disabled={!hasNext} onClick={() => { setState("loading"); setPage(page + 1); }}>Siguiente</button></nav>
  </>;
}
