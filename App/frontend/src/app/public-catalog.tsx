"use client";

import { useEffect, useState } from "react";
import Image from "next/image";
import { catalogApi, Product, priceLabel } from "./catalog-api";

export default function PublicCatalog({ id }: { id: string }) {
  const [items, setItems] = useState<Product[]>([]);
  const [page, setPage] = useState(1);
  const [more, setMore] = useState(false);
  const [query, setQuery] = useState("");
  const [draft, setDraft] = useState("");
  const [state, setState] = useState("loading");
  const [attempt, setAttempt] = useState(0);
  useEffect(() => {
    const controller = new AbortController();
    async function load() {
      try {
        const response = await catalogApi(`/public/businesses/${id}/products?page=${page}&q=${encodeURIComponent(query)}`, "GET", undefined, controller.signal);
        if (response.status === 404) { if (!controller.signal.aborted) { setItems([]); setState("hidden"); } return; }
        if (!response.ok) throw new Error();
        const data = await response.json();
        if (!Array.isArray(data.items)) throw new Error();
        if (!controller.signal.aborted) { setItems(data.items); setMore(data.hasNextPage); setState("ready"); }
      } catch { if (!controller.signal.aborted) { setItems([]); setState("error"); } }
    }
    void load();
    return () => controller.abort();
  }, [id, page, query, attempt]);
  if (state === "hidden") return null;
  function reload() { setState("loading"); setItems([]); setAttempt((n) => n + 1); }
  return <section className="public-catalog" aria-labelledby="catalog-title">
    <div className="catalog-heading"><div><p className="eyebrow">Para descubrir</p><h2 id="catalog-title">Productos del negocio</h2></div><button className="text-link" disabled={state === "loading"} onClick={reload}>Actualizar catálogo</button></div>
    <form className="catalog-search" onSubmit={(e) => { e.preventDefault(); setState("loading"); setItems([]); setPage(1); setQuery(draft.trim()); setAttempt((n) => n + 1); }}>
      <label>Buscar en este catálogo<input maxLength={150} value={draft} onChange={(e) => setDraft(e.target.value)} placeholder="Nombre o categoría" /></label><button className="button button-outline">Buscar productos</button>
    </form>
    {state === "loading" ? <p role="status">Cargando catálogo…</p> : state === "error" ? <p role="alert">No pudimos cargar los productos. Podés actualizar el catálogo para reintentar.</p> : <>
      <p className="muted">Precios y disponibilidad informados por el negocio. Consultá para confirmar.</p>
      {!items.length && <p>No hay productos publicados para esta búsqueda.</p>}
      <div className="catalog-grid">{items.map((p) => <article className="product-card" key={p.id}>
        {p.hasPhoto && <Image src={`/api/marketplace/public/businesses/${id}/products/${p.id}/photo?v=${p.version}`} alt={p.name} width={400} height={300} unoptimized />}
        <div className="product-card-copy"><span className="eyebrow">{p.category}</span><h3>{p.name}</h3><p>{p.description}</p><strong>{priceLabel(p)}</strong><span className="product-availability">{p.available ? "Disponible" : "No disponible"}</span></div>
      </article>)}</div>
      <nav className="catalog-toolbar" aria-label="Páginas del catálogo"><button className="button button-outline" disabled={page === 1} onClick={() => { setState("loading"); setItems([]); setPage(page - 1); }}>Anterior</button><span>Página {page}</span><button className="button button-outline" disabled={!more} onClick={() => { setState("loading"); setItems([]); setPage(page + 1); }}>Siguiente</button></nav>
    </>}
  </section>;
}
