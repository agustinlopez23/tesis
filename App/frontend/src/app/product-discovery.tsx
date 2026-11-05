"use client";

import Image from "next/image";
import Link from "next/link";
import { useEffect, useState } from "react";
import { catalogApi } from "./catalog-api";
import { money } from "./order-api";
import { areaParams, DiscoveryLocation, distanceLabel, radii, readArea } from "./explorar/discovery-location";
import { LocationFilter, radiusLabel } from "./explorar/location-filter";
import SponsoredCampaigns from "./sponsored-campaigns";
import { categoryStyle, EmptyState, Icon, PageHeader, SiteFooter, SiteHeader, Skeleton } from "./ui";

type Product = { id: string; businessId: string; businessName: string; businessCategory: string; name: string; description: string; category: string;
  price: number; currency: string; version: number; hasPhoto: boolean; distanceKm: number | null; lowStock?: boolean };
type Filters = { page: number; query: string; category: string; area: DiscoveryLocation | null };
type Results = { items: Product[]; page: number; hasNextPage: boolean; order: string; policy?: string | null };
const initialFilters: Filters = { page: 1, query: "", category: "", area: null };
export default function ProductDiscovery() {
  const [filters, setFilters] = useState<Filters>(initialFilters);
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
    if (filters.area?.kind === "zone" && filters.area.zone) query.set("zone", filters.area.zone);
    void catalogApi(`/public/products?${query}`, "GET", undefined, controller.signal).then(async response => {
      if (!response.ok) throw new Error();
      const result = await response.json(); if (!controller.signal.aborted) setData(result);
    }).catch(() => { if (!controller.signal.aborted) { setData(null); setError(true); } });
    return () => controller.abort();
  }, [filters, ready, attempt]);
  function navigate(next: Filters) {
    setData(null); setError(false); setNotice(false); setFilters(next); setDraft(next.query); setAttempt(n => n + 1);
    const params = new URLSearchParams();
    if (next.query) params.set("q", next.query); if (next.category) params.set("category", next.category); if (next.page > 1) params.set("page", String(next.page));
    areaParams(params, next.area, true);
    const destination = "/productos" + (params.size ? "?" + params : "");
    const discoveryLocation = next.area?.kind === "device" ? next.area : null;
    if (destination !== window.location.pathname + window.location.search || JSON.stringify(discoveryLocation) !== JSON.stringify(window.history.state?.discoveryLocation ?? null))
      window.history.pushState({ ...window.history.state, discoveryLocation }, "", destination);
  }
  const orderLabel = data?.order === "ranking" ? `Ordenado por ranking hiperlocal con equidad (${data.policy}).` : filters.area ? "Ordenados por distancia en línea recta." : "Ordenados alfabéticamente.";
  const count = data ? data.items.length + (data.hasNextPage ? "+" : "") + (data.items.length === 1 ? " producto" : " productos") + " en esta página" : "Buscando productos";
  return <><SiteHeader active="products" /><main id="contenido" tabIndex={-1} className="container explore-main product-discovery">
    <PageHeader eyebrow="Lo que buscás, cerca" title="Productos de tu ciudad" description="Explorá la oferta de los negocios. Entrá a su catálogo para revisar y armar una solicitud."
      actions={<Link className="text-link" href="/explorar">Explorar negocios <Icon name="arrow" /></Link>} />
    <div className="discovery-filters">
      <form className="search-form" role="search" onSubmit={e => { e.preventDefault(); navigate({ ...filters, page: 1, query: draft.trim() }); }}>
        <Icon name="search" /><label className="sr-only" htmlFor="product-search">Buscar productos</label>
        <input id="product-search" type="search" maxLength={150} value={draft} onChange={e => setDraft(e.target.value)} placeholder="Producto, categoría o negocio…" />
        <button className="button button-primary" type="submit">Buscar <Icon name="arrow" /></button>
      </form>
      <div className="chip-row" role="group" aria-label="Filtrar por rubro">
        <label className={"chip chip-select " + (filters.category ? "selected" : "")}><Icon name="grid" /><span className="sr-only">Rubro del negocio</span>
          <select value={filters.category} onChange={e => navigate({ ...filters, category: e.target.value, page: 1 })}><option value="">Todos los rubros</option>{categories.map(category => <option key={category}>{category}</option>)}</select>
          <Icon name="chevron" className="chip-caret" />
        </label>
      </div>
      <LocationFilter area={filters.area} onChange={area => navigate({ ...filters, area, page: 1 })} />
    </div>
    <SponsoredCampaigns zone={filters.area?.kind === "zone" ? filters.area.zone ?? null : null} />
    {notice && <p className="results-note" role="status"><Icon name="info" />Volvé a elegir tu ubicación para buscar cerca de vos; las coordenadas no se comparten en el enlace.</p>}
    {(filters.query || filters.category) && <div className="applied-filters"><span>Tu búsqueda:</span>
      {filters.query && <button onClick={() => navigate({ ...filters, page: 1, query: "" })} aria-label={"Quitar búsqueda " + filters.query}>“{filters.query}” <Icon name="close" /></button>}
      {filters.category && <button onClick={() => navigate({ ...filters, page: 1, category: "" })} aria-label={"Quitar filtro " + filters.category}>{filters.category} <Icon name="close" /></button>}
      <button className="clear-filters" onClick={() => navigate(initialFilters)}>Limpiar filtros</button></div>}
    <div className="results-toolbar"><span aria-live="polite">{count}</span>
      <p className="results-order"><strong>{orderLabel}</strong> Precios informados por cada negocio. <button className="text-link" onClick={() => navigate(filters)}>Actualizar productos</button></p></div>
    {error ? <EmptyState icon="box" role="alert" title="No pudimos consultar los productos." actions={<button className="button button-dark" onClick={() => navigate(filters)}>Reintentar</button>}>Revisá tu conexión y actualizá para reintentar.</EmptyState>
      : !data ? <Skeleton variant="products" label="Buscando productos…" /> : <>
      {!data.items.length && (filters.area ? <WiderSearch area={filters.area} onChange={area => navigate({ ...filters, area, page: 1 })} />
        : <EmptyState icon="search" title="No encontramos productos disponibles con esos filtros." actions={(filters.query || filters.category) && <button className="button button-dark" onClick={() => navigate(initialFilters)}>Ver todos los productos</button>}>Probá con otro nombre, una categoría o un rubro diferente.</EmptyState>)}
      <div className="catalog-grid">{data.items.map(product => <ProductCard key={product.id} product={product} />)}</div>
      <nav className="directory-pagination" aria-label="Páginas de productos">
        <button className="button button-outline" disabled={filters.page === 1} onClick={() => navigate({ ...filters, page: filters.page - 1 })}><Icon name="chevron" className="rotate" />Anterior</button>
        <span>Página <strong>{data.page}</strong></span>
        <button className="button button-outline" disabled={!data.hasNextPage} onClick={() => navigate({ ...filters, page: filters.page + 1 })}>Siguiente<Icon name="chevron" /></button>
      </nav>
    </>}
  </main><SiteFooter /></>;
}

function ProductCard({ product }: { product: Product }) {
  const style = categoryStyle(product.businessCategory);
  return <article className="product-card">
    {product.hasPhoto ? <Image src={`/api/marketplace/public/businesses/${product.businessId}/products/${product.id}/photo?v=${product.version}`} alt={product.name} width={400} height={300} unoptimized />
      : <div className={"product-placeholder " + style.tone} aria-hidden="true"><Icon name={style.icon} /></div>}
    <div className="product-card-copy"><span className="eyebrow">{product.category}</span><h2>{product.name}</h2><p className="product-description">{product.description}</p>
      <strong className="product-price">{money(product.price, product.currency)}</strong>
      <p className="product-business"><Icon name="store" />{product.businessName} · {product.businessCategory}</p>
      {product.distanceKm !== null && <p className="product-distance"><Icon name="pin" />{distanceLabel(product.distanceKm)}</p>}
      {product.lowStock && <span className="stock-badge">Disponibilidad baja</span>}
      <Link className="button button-outline button-small" href={`/negocios/${product.businessId}#catalog-title`} aria-label={`Ver catálogo de ${product.businessName}`}>Ver catálogo y solicitar <Icon name="arrow" /></Link></div>
  </article>;
}

function WiderSearch({ area, onChange }: { area: DiscoveryLocation; onChange: (area: DiscoveryLocation | null) => void }) {
  const wider = radii.find(radius => radius > area.radiusKm);
  return <EmptyState icon="pin" role="status" title={`No encontramos productos disponibles en esta zona dentro de ${radiusLabel(area.radiusKm)}.`}
    actions={<>{wider !== undefined && <button className="button button-dark" onClick={() => onChange({ ...area, radiusKm: wider })}>Ampliar radio a {radiusLabel(wider)}</button>}
      <button className="button button-outline" onClick={() => onChange(null)}>Buscar en toda la ciudad</button></>}>Ampliá el radio o buscá en toda la ciudad.</EmptyState>;
}
