"use client";

import { useEffect, useEffectEvent, useState } from "react";
import Image from "next/image";
import { catalogApi, Product, priceLabel } from "./catalog-api";
import { money } from "./order-api";
import { useOrderCart } from "./use-order-cart";
import { useToast } from "./toast";
import { Badge, categoryStyle, EmptyState, Icon, Skeleton } from "./ui";
import OrderCheckout from "./order-checkout";

const ADDED_FEEDBACK_MS = 1600;

function countLabel(units: number) { return units === 1 ? "1 producto" : `${units} productos`; }

// Without an active plan the API answers 404 for the catalog, so the profile skips the request entirely.
export default function PublicCatalog({ id, enabled = true }: { id: string; enabled?: boolean }) {
  const cart = useOrderCart(id);
  const reconcile = useEffectEvent((products: Product[]) => cart.reconcile(products));
  const { show, toast } = useToast();
  const [items, setItems] = useState<Product[]>([]);
  const [page, setPage] = useState(1);
  const [more, setMore] = useState(false);
  const [query, setQuery] = useState("");
  const [draft, setDraft] = useState("");
  const [state, setState] = useState(enabled ? "loading" : "hidden");
  const [attempt, setAttempt] = useState(0);
  const [added, setAdded] = useState("");
  useEffect(() => {
    if (!enabled) return;
    const controller = new AbortController();
    async function load() {
      try {
        const response = await catalogApi(`/public/businesses/${id}/products?page=${page}&q=${encodeURIComponent(query)}`, "GET", undefined, controller.signal);
        if (response.status === 404) { if (!controller.signal.aborted) { setItems([]); setState("hidden"); } return; }
        if (!response.ok) throw new Error();
        const data = await response.json();
        if (!Array.isArray(data.items)) throw new Error();
        if (!controller.signal.aborted) { setItems(data.items); setMore(data.hasNextPage); setState("ready"); reconcile(data.items); }
      } catch { if (!controller.signal.aborted) { setItems([]); setState("error"); } }
    }
    void load();
    return () => controller.abort();
  }, [id, enabled, page, query, attempt]);
  useEffect(() => {
    if (!added) return;
    const timer = setTimeout(() => setAdded(""), ADDED_FEEDBACK_MS);
    return () => clearTimeout(timer);
  }, [added]);
  if (state === "hidden") return <OrderCheckout cart={cart} />;
  function reload() { setState("loading"); setItems([]); setAttempt((n) => n + 1); }
  function add(product: Product) {
    // The cart refuses silently past 20 distinct products; only confirm what it will actually keep.
    const accepted = cart.selection.length < 20 || cart.selection.some((line) => line.product.id === product.id);
    cart.add(product);
    if (accepted) { setAdded(product.id); show("Agregado a tu solicitud"); }
  }
  function openCheckout() {
    const target = document.getElementById("order-checkout-title");
    target?.scrollIntoView?.({ behavior: "smooth", block: "start" });
    target?.closest<HTMLElement>(".order-checkout")?.focus();
  }
  const units = cart.selection.reduce((sum, line) => sum + line.quantity, 0);
  const totals: Record<string, number> = {};
  for (const { product, quantity } of cart.selection) totals[product.currency] = (totals[product.currency] ?? 0) + product.price * quantity;
  const showBar = cart.selection.length > 0 && !cart.order;
  return <section className="public-catalog" aria-labelledby="catalog-title">
    <div className="catalog-heading">
      <div><p className="eyebrow">Para descubrir</p><h2 id="catalog-title">Productos del negocio</h2>
        {units > 0 && !cart.order && <p className="catalog-count"><Icon name="cart" />{countLabel(units)} en tu solicitud</p>}</div>
      <button className="text-link" disabled={state === "loading"} onClick={reload}>Actualizar catálogo</button>
    </div>
    <form className="catalog-search" onSubmit={(e) => { e.preventDefault(); setState("loading"); setItems([]); setPage(1); setQuery(draft.trim()); setAttempt((n) => n + 1); }}>
      <label>Buscar en este catálogo<input maxLength={150} value={draft} onChange={(e) => setDraft(e.target.value)} placeholder="Nombre o categoría" /></label><button className="button button-outline"><Icon name="search" />Buscar productos</button>
    </form>
    {state === "loading" ? <Skeleton variant="products" label="Cargando catálogo…" /> :
      state === "error" ? <EmptyState compact role="alert" icon="alert" title="No pudimos cargar los productos.">Podés actualizar el catálogo para reintentar.</EmptyState> : <>
      <p className="muted catalog-note">Precios y disponibilidad informados por el negocio. Consultá para confirmar.</p>
      {!items.length && <EmptyState compact icon="box" title="No hay productos publicados para esta búsqueda.">Probá con otro nombre o categoría.</EmptyState>}
      <div className="catalog-grid">{items.map((p) => {
        const look = categoryStyle(p.category);
        const inCart = cart.selection.find((line) => line.product.id === p.id);
        return <article className={`product-card ${p.available ? "" : "product-card-unavailable"}`} key={p.id}>
          {p.hasPhoto ? <Image src={`/api/marketplace/public/businesses/${id}/products/${p.id}/photo?v=${p.version}`} alt={p.name} width={400} height={300} unoptimized />
            : <div className={`product-card-placeholder ${look.tone}`} aria-hidden="true"><Icon name={look.icon} /></div>}
          <div className="product-card-copy">
            <span className="eyebrow">{p.category}</span><h3>{p.name}</h3><p className="product-card-description">{p.description}</p>
            <div className="product-card-meta"><strong className="product-price">{priceLabel(p)}</strong>
              <span className={`product-availability ${p.available ? "is-available" : "is-unavailable"}`}><Icon name={p.available ? "check" : "close"} />{p.available ? "Disponible" : "No disponible"}</span>
              {p.available && p.lowStock && <span className="stock-badge"><Badge tone="warning">Disponibilidad baja</Badge></span>}</div>
          </div>
          <button type="button" className={`button product-add ${added === p.id ? "button-dark is-added" : "button-outline"}`} aria-label={`Agregar ${p.name}`}
            disabled={!p.available || cart.pending || cart.restoring || !!cart.intent || !!cart.order} onClick={() => add(p)}>
            <Icon name={added === p.id ? "check" : "plus"} />{added === p.id ? "Agregado" : inCart ? `Agregar otro (${inCart.quantity})` : "Agregar a la solicitud"}</button>
        </article>;
      })}</div>
      <nav className="catalog-toolbar" aria-label="Páginas del catálogo"><button className="button button-outline" disabled={page === 1} onClick={() => { setState("loading"); setItems([]); setPage(page - 1); }}>Anterior</button><span>Página {page}</span><button className="button button-outline" disabled={!more} onClick={() => { setState("loading"); setItems([]); setPage(page + 1); }}>Siguiente</button></nav>
    </>}
    <OrderCheckout cart={cart} />
    {showBar && <div className="order-float" role="region" aria-label="Resumen de tu solicitud">
      <div className="order-float-copy"><Icon name="cart" /><span><strong>{countLabel(units)}</strong> · Total {Object.entries(totals).map(([currency, total]) => money(total, currency)).join(" + ")}</span></div>
      <button type="button" className="button button-primary button-small" onClick={openCheckout}>Ver solicitud<Icon name="arrow" /></button>
    </div>}
    {toast}
  </section>;
}
