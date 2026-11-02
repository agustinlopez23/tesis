"use client";

import { FormEvent, useEffect, useState } from "react";
import Link from "next/link";
import { catalogApi, catalogError } from "../../catalog-api";

type Category = { id: number; name: string; slug: string; isActive: boolean; businessCount: number };

export default function AdminCategories() {
  const [categories, setCategories] = useState<Category[]>([]);
  const [state, setState] = useState("loading");
  const [attempt, setAttempt] = useState(0);
  const [name, setName] = useState("");
  const [editing, setEditing] = useState<{ id: number; name: string } | null>(null);
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
  if (state === "denied") return <p role="alert">Necesitás ingresar con una cuenta administradora. <Link href="/mi-negocio">Ir a Mi negocio</Link></p>;
  return <>
    <p>Los rubros deshabilitados no se pueden elegir para negocios nuevos. Los negocios que ya los usan siguen visibles.</p>
    {error && <p role="alert" className="error">{error}</p>}{success && <p role="status" className="success-message">{success}</p>}
    <form className="catalog-search" onSubmit={create}><fieldset disabled={pending || state !== "ready"}>
      <label>Nombre del nuevo rubro<input required minLength={2} maxLength={80} value={name} onChange={(e) => setName(e.target.value)} /></label>
      <button className="button button-primary" type="submit">Crear rubro</button>
    </fieldset></form>
    {state === "error" && <button className="button button-outline" onClick={() => { setState("loading"); setAttempt((n) => n + 1); }}>Reintentar carga</button>}
    {state === "loading" && <p role="status">Cargando rubros…</p>}
    <div className="catalog-owner-list">{categories.map((category) => <article key={category.id}>
      {editing?.id === category.id ? <form className="catalog-search" onSubmit={(e) => void rename(e, category)}><fieldset disabled={pending}>
        <label>Nuevo nombre de {category.name}<input required minLength={2} maxLength={80} value={editing.name} onChange={(e) => setEditing({ id: category.id, name: e.target.value })} /></label>
        <button className="button button-outline" type="submit">Guardar nombre</button><button className="text-link" type="button" onClick={() => setEditing(null)}>Cancelar</button>
      </fieldset></form> : <div><strong>{category.name}</strong><p>{category.isActive ? "Habilitado" : "Deshabilitado"} · {category.businessCount} {category.businessCount === 1 ? "negocio" : "negocios"}</p></div>}
      <div className="catalog-toolbar">
        <button className="text-link" disabled={pending} aria-label={`Renombrar ${category.name}`} onClick={() => setEditing({ id: category.id, name: category.name })}>Renombrar</button>
        <button className="text-link" disabled={pending} aria-label={`${category.isActive ? "Deshabilitar" : "Habilitar"} ${category.name}`}
          onClick={() => void save(`/admin/categories/${category.id}`, "PUT", { name: category.name, isActive: !category.isActive }, category.isActive ? "Rubro deshabilitado." : "Rubro habilitado.")}>
          {category.isActive ? "Deshabilitar" : "Habilitar"}</button>
      </div>
    </article>)}</div>
  </>;
}
