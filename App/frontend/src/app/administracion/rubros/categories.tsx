"use client";

import { FormEvent, useEffect, useState } from "react";
import Link from "next/link";
import { catalogApi, catalogError } from "../../catalog-api";
import { Alert, Badge, EmptyState, Panel, Skeleton } from "../../ui";

type Category = { id: number; name: string; slug: string; isActive: boolean; businessCount: number };

export default function AdminCategories() {
  const [categories, setCategories] = useState<Category[]>([]);
  const [state, setState] = useState("loading");
  const [attempt, setAttempt] = useState(0);
  const [name, setName] = useState("");
  const [editing, setEditing] = useState<{ id: number; name: string } | null>(null);
  const [deleting, setDeleting] = useState<number | null>(null);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  useEffect(() => {
    const controller = new AbortController();
    void catalogApi("/admin/categories", "GET", undefined, controller.signal).then(async (response) => {
      if (response.status === 401 || response.status === 403) { if (!controller.signal.aborted) setState("denied"); return; }
      if (!response.ok) throw new Error(await catalogError(response));
      const data = await response.json();
      if (!controller.signal.aborted) { setCategories(data); setState("ready"); }
    }).catch((failure) => { if (!controller.signal.aborted) { setError(failure instanceof Error ? failure.message : "No pudimos cargar los rubros."); setState("error"); } });
    return () => controller.abort();
  }, [attempt]);
  async function save(path: string, method: string, body: unknown, message: string) {
    setPending(true); setError(""); setSuccess("");
    try {
      const response = await catalogApi(path, method, body);
      if (!response.ok) throw new Error(await catalogError(response));
      setSuccess(message); setAttempt((n) => n + 1);
      return true;
    } catch (failure) { setError(failure instanceof Error ? failure.message : "No pudimos guardar el rubro."); return false; }
    finally { setPending(false); }
  }
  async function create(event: FormEvent) {
    event.preventDefault();
    if (await save("/admin/categories", "POST", { name: name.trim() }, "Rubro creado.")) setName("");
  }
  async function rename(event: FormEvent, category: Category) {
    event.preventDefault();
    if (editing && await save(`/admin/categories/${category.id}`, "PUT", { name: editing.name.trim(), isActive: category.isActive }, "Rubro renombrado.")) setEditing(null);
  }
  if (state === "denied") return <Alert tone="danger" role="alert">Necesitás ingresar con una cuenta administradora. <Link href="/mi-negocio">Ir a Mi negocio</Link></Alert>;
  return <>
    <p className="admin-intro">Los rubros deshabilitados no se pueden elegir para negocios nuevos. Los negocios que ya los usan siguen visibles.</p>
    {error && <Alert tone="danger" role="alert">{error}</Alert>}{success && <Alert tone="success" role="status">{success}</Alert>}
    <Panel className="admin-form" title="Nuevo rubro" description="Aparece de inmediato en el formulario de los negocios.">
      <form className="admin-inline-form" onSubmit={create}><fieldset disabled={pending || state !== "ready"}>
        <label>Nombre del nuevo rubro<input required minLength={2} maxLength={80} value={name} onChange={(e) => setName(e.target.value)} /></label>
        <button className="button button-primary" type="submit">Crear rubro</button>
      </fieldset></form>
    </Panel>
    <h2 className="admin-section-title">Rubros existentes</h2>
    {state === "error" && <div className="action-row"><button className="button button-outline" onClick={() => { setState("loading"); setAttempt((n) => n + 1); }}>Reintentar carga</button></div>}
    {state === "loading" && <Skeleton variant="rows" count={4} label="Cargando rubros…" />}
    {state === "ready" && !categories.length && <EmptyState compact icon="tag" title="Todavía no hay rubros." />}
    {categories.length > 0 && <div className="resource-list">{categories.map((category) => <article key={category.id} className="resource-row">
      {editing?.id === category.id ? <form className="admin-inline-form resource-wide" onSubmit={(e) => void rename(e, category)}><fieldset disabled={pending}>
        <label>Nuevo nombre de {category.name}<input required minLength={2} maxLength={80} value={editing.name} onChange={(e) => setEditing({ id: category.id, name: e.target.value })} /></label>
        <button className="button button-outline" type="submit">Guardar nombre</button><button className="text-link" type="button" onClick={() => setEditing(null)}>Cancelar</button>
      </fieldset></form> : <>
        <div className="resource-main"><strong>{category.name}</strong><p>{category.businessCount} {category.businessCount === 1 ? "negocio" : "negocios"}</p></div>
        <Badge tone={category.isActive ? "success" : "neutral"}>{category.isActive ? "Habilitado" : "Deshabilitado"}</Badge>
        <div className="resource-actions">
          <button className="text-link" disabled={pending} aria-label={`Renombrar ${category.name}`} onClick={() => setEditing({ id: category.id, name: category.name })}>Renombrar</button>
          <button className="text-link" disabled={pending} aria-label={`${category.isActive ? "Deshabilitar" : "Habilitar"} ${category.name}`}
            onClick={() => void save(`/admin/categories/${category.id}`, "PUT", { name: category.name, isActive: !category.isActive }, category.isActive ? "Rubro deshabilitado." : "Rubro habilitado.")}>
            {category.isActive ? "Deshabilitar" : "Habilitar"}</button>
          <button className="text-link danger" disabled={pending} aria-label={`Eliminar ${category.name}`} onClick={() => setDeleting(category.id)}>Eliminar</button>
        </div>
      </>}
      {deleting === category.id && <div className="resource-wide"><Alert tone="warning" title={`¿Eliminar el rubro ${category.name}?`}>
        <p>Si algún negocio lo usa, no se podrá eliminar; en ese caso deshabilitalo.</p>
        <div className="action-row">
          <button className="button button-outline button-small" disabled={pending} aria-label={`Confirmar eliminación de ${category.name}`}
            onClick={() => { setDeleting(null); void save(`/admin/categories/${category.id}`, "DELETE", undefined, "Rubro eliminado."); }}>Confirmar eliminación</button>
          <button className="text-link" disabled={pending} onClick={() => setDeleting(null)}>Cancelar</button>
        </div>
      </Alert></div>}
    </article>)}</div>}
  </>;
}
