"use client";

import { FormEvent, useEffect, useState } from "react";
import Image from "next/image";
import { catalogApi, catalogError, Plan, Product, priceLabel } from "./catalog-api";
import CatalogAssistance from "./catalog-assistance";
import PlanCapabilities from "./plan-capabilities";
import { businessChanged, useBusinessRefresh } from "./business-refresh";

export default function BusinessCatalog({ id }: { id: string }) {
  const [open, setOpen] = useState(false);
  return <section className="catalog-panel" aria-label="Catálogo del negocio">
    <div className="catalog-heading"><div><p className="eyebrow">Tu oferta</p><h2>Catálogo de productos</h2></div>
      <button type="button" className="button button-outline" aria-expanded={open} onClick={() => setOpen(!open)}>{open ? "Cerrar catálogo" : "Administrar catálogo"}</button></div>
    {open && <CatalogEditor key={id} id={id} />}
  </section>;
}

function CatalogEditor({ id }: { id: string }) {
  const [plan, setPlan] = useState<Plan | null>(null);
  const [products, setProducts] = useState<Product[]>([]);
  const [editing, setEditing] = useState<Product | null>(null);
  const [price, setPrice] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [pending, setPending] = useState(false);
  const [loading, setLoading] = useState(true);
  const [attempt, setAttempt] = useState(0);
  const stored = products.find((p) => p.id === editing?.id);
  const dirty = editing !== null && (!stored || JSON.stringify(editing) !== JSON.stringify(stored) || Number(price) !== stored.price);
  useEffect(() => {
    const controller = new AbortController();
    async function load() {
      try {
        const [p, list] = await Promise.all([catalogApi(`/businesses/${id}/plan`, "GET", undefined, controller.signal), catalogApi(`/businesses/${id}/products`, "GET", undefined, controller.signal)]);
        if (!p.ok || !list.ok) throw new Error(await catalogError(!p.ok ? p : list));
        const [nextPlan, nextProducts] = await Promise.all([p.json(), list.json()]);
        if (!controller.signal.aborted) { setPlan(nextPlan); setProducts(nextProducts); }
      } catch (error) { if (!controller.signal.aborted) setError(error instanceof Error ? error.message : "No pudimos cargar el catálogo."); }
      finally { if (!controller.signal.aborted) setLoading(false); }
    }
    void load();
    return () => controller.abort();
  }, [id, attempt]);
  function refresh() { setError(""); setLoading(true); setAttempt((n) => n + 1); }
  useBusinessRefresh(id, refresh);
  // Photo the owner attached to the AI draft, kept client side until they choose to upload it.
  const [assistPhoto, setAssistPhoto] = useState<File | null>(null);
  function edit(product?: Product) {
    setAssistPhoto(null);
    setEditing(product ?? { id: crypto.randomUUID(), name: "", description: "", category: "", price: 0, currency: "ARS", available: true, status: "draft", version: 0, hasPhoto: false });
    setPrice(product ? String(product.price) : ""); setError(""); setSuccess("");
  }
  function saved(product: Product) {
    setProducts((items) => [...items.filter((p) => p.id !== product.id), product].sort((a, b) => a.name.localeCompare(b.name)));
    setEditing(product); setPrice(String(product.price));
    businessChanged(id);
  }
  async function handleResponse(response: Response) {
    if (!response.ok) {
      if (response.status === 403) setPlan((p) => p ? { ...p, catalogEnabled: false, plan: "Presencia" } : p);
      throw new Error(await catalogError(response));
    }
    saved(await response.json());
  }
  async function save(event: FormEvent) {
    event.preventDefault();
    if (!editing) return;
    setPending(true); setError(""); setSuccess("");
    try {
      await handleResponse(await catalogApi(`/businesses/${id}/products/${editing.id}`, "PUT", { ...editing, price: Number(price) }));
      setSuccess("Producto guardado.");
    } catch (error) { setError(error instanceof Error ? error.message : "No pudimos guardar. Tu borrador sigue acá."); }
    finally { setPending(false); }
  }
  async function photo(file: File | null) {
    if (!editing) return;
    if (file && file.size > 5 * 1024 * 1024) { setError("La foto supera el límite de 5 MB."); return; }
    setPending(true); setError(""); setSuccess("");
    try {
      const form = new FormData();
      if (file) { form.append("file", file); form.append("version", String(editing.version)); }
      await handleResponse(await catalogApi(`/businesses/${id}/products/${editing.id}/photo${file ? "" : "?version=" + editing.version}`, file ? "PUT" : "DELETE", file ? form : undefined));
      if (file === assistPhoto) setAssistPhoto(null);
      setSuccess(file ? "Foto guardada." : "Foto retirada.");
    } catch (error) { setError(error instanceof Error ? error.message : "No pudimos guardar la foto."); }
    finally { setPending(false); }
  }
  return <div className="catalog-content">
    {error && <p className="error" role="alert">{error}</p>}
    {success && <p className="success-message" role="status">{success}</p>}
    <div className="catalog-toolbar"><p>{plan ? `Plan ${plan.plan}` : "Consultando plan…"}</p><button type="button" className="text-link" disabled={pending || loading} onClick={refresh}>Actualizar catálogo</button></div>
    {plan && <PlanCapabilities current={plan.plan} />}
    {loading ? <p role="status">Cargando productos…</p> : <>
      {plan && !plan.catalogEnabled && <p className="catalog-notice">El catálogo requiere un plan Impulso vigente. Tus productos se conservan y podés consultarlos. La administración gestiona la activación.</p>}
      <button type="button" className="button button-primary" disabled={!plan?.catalogEnabled || pending} onClick={() => edit()}>Nuevo producto</button>
      {products.length === 0 && <p className="muted">Todavía no cargaste productos.</p>}
      <div className="catalog-owner-list">{products.map((p) => <article key={p.id}>
        <div><strong>{p.name}</strong><p>{priceLabel(p)} · {p.status === "published" ? "Publicado" : p.status === "archived" ? "Archivado" : "Borrador"} · {p.available ? "Disponible" : "No disponible"}</p></div>
        <button type="button" className="text-link" disabled={!plan?.catalogEnabled || pending} aria-label={`Editar ${p.name}`} onClick={() => edit(p)}>Editar</button>
      </article>)}</div>
    </>}
    {editing && <form className="catalog-form" onSubmit={save}>
      <h3>{editing.version ? "Editar producto" : "Nuevo producto"}</h3>
      <fieldset disabled={pending || !plan?.catalogEnabled || loading}>
        <CatalogAssistance key={editing.id} business={id} onApply={(draft, file) => { setEditing({ ...editing, ...draft }); setAssistPhoto(file); }} />
        {assistPhoto && editing.version === 0 && <p className="field-help">Guardá el producto para usar la foto del borrador.</p>}
        <label>Nombre del producto<input required maxLength={160} value={editing.name} onChange={(e) => setEditing({ ...editing, name: e.target.value })} /></label>
        <label>Descripción del producto<textarea maxLength={2000} value={editing.description} onChange={(e) => setEditing({ ...editing, description: e.target.value })} /></label>
        <label>Categoría del producto<input required maxLength={80} value={editing.category} onChange={(e) => setEditing({ ...editing, category: e.target.value })} /></label>
        <div className="catalog-fields"><label>Precio<input required type="number" min="0" max="9999999999.99" step="0.01" value={price} onChange={(e) => setPrice(e.target.value)} /></label>
          <label>Moneda<select value={editing.currency} onChange={(e) => setEditing({ ...editing, currency: e.target.value })}><option value="ARS">Pesos argentinos (ARS)</option><option value="USD">Dólares (USD)</option></select></label></div>
        <div className="catalog-fields"><label>Disponibilidad<select value={String(editing.available)} onChange={(e) => setEditing({ ...editing, available: e.target.value === "true" })}><option value="true">Disponible</option><option value="false">No disponible</option></select></label>
          <label>Estado del producto<select value={editing.status} onChange={(e) => setEditing({ ...editing, status: e.target.value })}><option value="draft">Borrador</option><option value="published">Publicado</option><option value="archived">Archivado</option></select></label></div>
        <p className="field-help">Solo los productos publicados aparecen en el perfil de un negocio publicado. La disponibilidad se confirma con el negocio.</p>
        <div className="catalog-toolbar"><button type="submit" className="button button-primary">{pending ? "Guardando…" : "Guardar producto"}</button><button type="button" className="text-link" onClick={() => setEditing(null)}>Cerrar edición</button></div>
      </fieldset>
      {editing.version > 0 && <div className="catalog-photo-editor">
        {editing.hasPhoto && <Image alt={editing.name} src={`/api/marketplace/businesses/${id}/products/${editing.id}/photo?v=${editing.version}`} width={240} height={180} unoptimized />}
        <label>Foto del producto<input key={editing.id + ":" + editing.version} type="file" accept="image/jpeg,image/png,image/webp" disabled={pending || !plan?.catalogEnabled || dirty || loading} onChange={(e) => { const file = e.target.files?.[0]; if (file) void photo(file); }} /></label>
        {dirty && <p className="field-help">Guardá los cambios del producto antes de modificar su foto.</p>}
        <p className="field-help">JPEG, PNG o WebP de hasta 5 MB.</p>
        {assistPhoto && <button type="button" className="button button-outline" disabled={pending || !plan?.catalogEnabled || dirty || loading} onClick={() => void photo(assistPhoto)}>Usar la foto del borrador</button>}
        {editing.hasPhoto && <button type="button" className="text-link" disabled={pending || !plan?.catalogEnabled || dirty || loading} onClick={() => void photo(null)}>Quitar foto</button>}
      </div>}
    </form>}
  </div>;
}
