"use client";

import Image from "next/image";
import Link from "next/link";
import { useEffect, useState } from "react";
import { catalogApi } from "./catalog-api";
import { money } from "./order-api";
import { areaParams, DiscoveryLocation, distanceLabel, readArea } from "./explorar/discovery-location";
import { LocationFilter } from "./explorar/location-filter";
import SponsoredCampaigns from "./sponsored-campaigns";
import { SiteFooter, SiteHeader } from "./ui";

type Product = { id: string; businessId: string; businessName: string; businessCategory: string; name: string; description: string; category: string;
  price: number; currency: string; version: number; hasPhoto: boolean; distanceKm: number | null };
type Filters = { page: number; query: string; category: string; area: DiscoveryLocation | null };
type Results = { items: Product[]; page: number; hasNextPage: boolean; order: string };
export default function ProductDiscovery() {
  const [filters, setFilters] = useState<Filters>({ page: 1, query: "", category: "", area: null });
  const [draft, setDraft] = useState(""), [ready, setReady] = useState(false), [attempt, setAttempt] = useState(0);
  const [data, setData] = useState<Results | null>(null), [error, setError] = useState(false), [notice, setNotice] = useState(false);
  const [categories, setCategories] = useState<string[]>([]);
  useEffect(() => {
    function restore() {
      const params = new URLSearchParams(window.location.search), page = Number(params.get("page") ?? 1);
      const area = readArea(params, window.history.state), query = (params.get("q") ?? "").slice(0, 150);
      setFilters({ page: Number.isInteger(page) && page >= 1 && page <= 10000 ? page : 1, query, category: (params.get("category") ?? "").slice(0, 100), area });
      setDraft(query); setNotice(params.get("near") === "me" && !area); setData(null); setError(false); setReady(true);
    }
    restore(); window.addEventListener("popstate", restore);
    const controller = new AbortController();
    void catalogApi("/business-categories", "GET", undefined, controller.signal).then(async response => {
      if (response.ok) { const items = await response.json(); if (!controller.signal.aborted) setCategories(items.map((item: { name: string }) => item.name)); }
    }).catch(() => undefined);
    return () => { controller.abort(); window.removeEventListener("popstate", restore); };
  }, []);
  useEffect(() => {
    if (!ready) return;
    const controller = new AbortController();
    const query = new URLSearchParams({ page: String(filters.page), q: filters.query, category: filters.category });
    areaParams(query, filters.area);
    void catalogApi(`/public/products?${query}`, "GET", undefined, controller.signal).then(async response => {
      if (!response.ok) throw new Error();
      const result = await response.json(); if (!controller.signal.aborted) setData(result);
    }).catch(() => { if (!controller.signal.aborted) { setData(null); setError(true); } });
    return () => controller.abort();
  }, [filters, ready, attempt]);
  function navigate(next: Filters) {
    setData(null); setError(false); setNotice(false); setFilters(next); setAttempt(n => n + 1);
    const params = new URLSearchParams();
    if (next.query) params.set("q", next.query); if (next.category) params.set("category", next.category); if (next.page > 1) params.set("page", String(next.page));
    areaParams(params, next.area, true);
    const destination = "/productos" + (params.size ? "?" + params : "");
    const discoveryLocation = next.area?.kind === "device" ? next.area : null;
    if (destination !== window.location.pathname + window.location.search || JSON.stringify(discoveryLocation) !== JSON.stringify(window.history.state?.discoveryLocation ?? null))
      window.history.pushState({ ...window.history.state, discoveryLocation }, "", destination);
  }
  return <><SiteHeader active="products" /><main id="contenido" tabIndex={-1} className="container explore-main product-discovery">
    <p className="eyebrow">Lo que buscás, cerca</p><h1>Productos de tu ciudad</h1>
    <p>Explorá la oferta de los negocios. Entrá a su catálogo para revisar y armar una solicitud.</p><Link className="text-link" href="/explorar">Explorar negocios</Link>
    <form className="catalog-search" onSubmit={e => { e.preventDefault(); navigate({ ...filters, page: 1, query: draft.trim() }); }}>
      <label>Buscar productos<input maxLength={150} value={draft} onChange={e => setDraft(e.target.value)} placeholder="Producto, categoría o negocio" /></label><button className="button button-primary">Buscar</button>
      <label>Rubro del negocio<select value={filters.category} onChange={e => navigate({ ...filters, category: e.target.value, page: 1 })}><option value="">Todos los rubros</option>{categories.map(category => <option key={category}>{category}</option>)}</select></label>
    </form>
    <LocationFilter area={filters.area} onChange={area => navigate({ ...filters, area, page: 1 })} />
    <SponsoredCampaigns zone={filters.area?.kind === "zone" ? filters.area.zone ?? null : null} />
    {notice && <p role="status">Volvé a elegir tu ubicación para buscar cerca de vos; las coordenadas no se comparten en el enlace.</p>}
    <div className="catalog-toolbar"><p>{filters.area ? "Ordenados por distancia en línea recta." : "Ordenados alfabéticamente."} Precios informados por cada negocio.</p><button className="text-link" onClick={() => navigate(filters)}>Actualizar productos</button></div>
    {error ? <p role="alert">No pudimos consultar los productos. Actualizá para reintentar.</p> : !data ? <p role="status">Buscando productos…</p> : <>
      {!data.items.length && <p>No encontramos productos disponibles con esos filtros.</p>}
      <div className="catalog-grid">{data.items.map(product => <article className="product-card" key={product.id}>
        {product.hasPhoto && <Image src={`/api/marketplace/public/businesses/${product.businessId}/products/${product.id}/photo?v=${product.version}`} alt={product.name} width={400} height={300} unoptimized />}
        <div className="product-card-copy"><span className="eyebrow">{product.category}</span><h2>{product.name}</h2><p>{product.description}</p><strong>{money(product.price, product.currency)}</strong><p>{product.businessName} · {product.businessCategory}</p>{product.distanceKm !== null && <p>{distanceLabel(product.distanceKm)}</p>}
          <Link className="text-link" href={`/negocios/${product.businessId}#catalog-title`} aria-label={`Ver catálogo de ${product.businessName}`}>Ver catálogo y solicitar</Link></div>
      </article>)}</div>
      <nav className="catalog-toolbar" aria-label="Páginas de productos"><button className="button button-outline" disabled={filters.page === 1} onClick={() => navigate({ ...filters, page: filters.page - 1 })}>Anterior</button><span>Página {data.page}</span><button className="button button-outline" disabled={!data.hasNextPage} onClick={() => navigate({ ...filters, page: filters.page + 1 })}>Siguiente</button></nav>
    </>}
  </main><SiteFooter /></>;
}
