"use client";

import Image from "next/image";
import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";
import { categoryStyle, Icon, SiteFooter, SiteHeader } from "../ui";
import { areaParams, DiscoveryLocation, readArea } from "./discovery-location";
import { LocationFilter } from "./location-filter";
import SponsoredCampaigns from "../sponsored-campaigns";
import { BusinessCard, type Business } from "../business-card";

type Directory = { items: Business[]; hasMore: boolean; measurement?: Record<string, string> };
type Filters = { page: number; query: string; category: string; area: DiscoveryLocation | null };
const initialFilters: Filters = { page: 1, query: "", category: "", area: null };

export default function Explore() {
  const [filters, setFilters] = useState(initialFilters);
  const [ready, setReady] = useState(false);
  const [attempt, setAttempt] = useState(0);
  const [directory, setDirectory] = useState<Directory | null>(null);
  const [categories, setCategories] = useState<string[]>([]);
  const [error, setError] = useState(false);
  const [locationNotice, setLocationNotice] = useState(false);

  useEffect(() => {
    function readLocation() {
      const params = new URLSearchParams(window.location.search);
      const page = Number(params.get("page") ?? 1);
      const area = readArea(params, window.history.state);
      setLocationNotice(params.get("near") === "me" && !area);
      setFilters({ page: Number.isInteger(page) && page >= 1 && page <= 10000 ? page : 1,
        query: (params.get("q") ?? "").slice(0, 150), category: (params.get("category") ?? "").slice(0, 100), area });
      setDirectory(null);
      setError(false);
      setReady(true);
    }
    readLocation();
    window.addEventListener("popstate", readLocation);
    const controller = new AbortController();
    void fetch("/api/marketplace/business-categories", { signal: controller.signal }).then(async (response) => {
      if (response.ok) setCategories((await response.json()).map((item: { name: string }) => item.name));
    }).catch(() => undefined);
    return () => { window.removeEventListener("popstate", readLocation); controller.abort(); };
  }, []);

  useEffect(() => {
    if (!ready) return;
    let active = true;
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 10000);
    const params = new URLSearchParams({ page: String(filters.page) });
    if (filters.query) params.set("q", filters.query);
    if (filters.category) params.set("category", filters.category);
    areaParams(params, filters.area);
    void fetch("/api/marketplace/public/businesses?" + params, {
      cache: "no-store", signal: controller.signal,
    }).then(async (response) => {
      if (!response.ok) throw new Error("Directory unavailable");
      const data: Directory = await response.json();
      if (active) setDirectory(data);
    }).catch(() => { if (active) setError(true); }).finally(() => clearTimeout(timeout));
    return () => { active = false; clearTimeout(timeout); controller.abort(); };
  }, [filters, attempt, ready]);

  function navigate(next: Filters, scroll = false) {
    setDirectory(null);
    setError(false);
    setLocationNotice(false);
    setFilters(next);
    setAttempt((value) => value + 1);
    const params = new URLSearchParams();
    if (next.query) params.set("q", next.query);
    if (next.category) params.set("category", next.category);
    if (next.page > 1) params.set("page", String(next.page));
    areaParams(params, next.area, true);
    const search = params.toString();
    const destination = window.location.pathname + (search ? "?" + search : "");
    const discoveryLocation = next.area?.kind === "device" ? next.area : null;
    if (destination !== window.location.pathname + window.location.search ||
      JSON.stringify(discoveryLocation) !== JSON.stringify(window.history.state?.discoveryLocation ?? null)) {
      window.history.pushState({ ...window.history.state, discoveryLocation }, "", destination);
    }
    if (scroll) document.getElementById("negocios")?.scrollIntoView({ block: "start" });
  }

  function search(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    navigate({ ...filters, page: 1, query: String(new FormData(event.currentTarget).get("q") ?? "").trim() }, true);
  }
  const filtered = Boolean(filters.query || filters.category || filters.area);

  return <><SiteHeader /><main id="contenido" tabIndex={-1} className="container explore-main">
    <section className="discovery-hero" aria-labelledby="discovery-title">
      <div className="hero-copy"><p className="eyebrow"><span className="tiny-spark">✳</span> Tu ciudad, a la vuelta</p>
        <h1 id="discovery-title">Hay mucho por <br/>descubrir. <br/><em>Y está cerca.</em></h1>
        <Link className="text-link" href="/productos">Buscar productos entre negocios <Icon name="arrow" /></Link>
        <p className="hero-description">Ese café que todavía no conocés. El almacén de siempre.<br className="desktop-break"/> Encontrá tu próximo favorito en Córdoba.</p>
        <a className="text-link" href="#negocios">Conocé los negocios <Icon name="arrow" /></a>
        <div className="hero-footnote"><span className="mini-icon"><Icon name="store" /></span><span>Comercios locales.<br/><strong>Conexiones de verdad.</strong></span></div>
      </div>
      <div className="hero-picture"><Image src="/images/neighborhood-editorial.webp" alt="Escena ilustrativa de un café y un almacén de barrio" fill sizes="(max-width: 700px) 100vw, 50vw" preload />
        <span className="picture-tag"><span className="location-dot"/> Espíritu de barrio</span>
        <div className="picture-caption">Las mejores historias<br/>están a la vuelta.</div>
        <span className="picture-credit">Imagen ilustrativa</span>
      </div>
    </section>

    <section className="directory-section" id="negocios" aria-labelledby="directory-title">
      <div className="section-heading"><div><p className="eyebrow">La guía local</p><h2 id="directory-title">Descubrí comercios de tu ciudad.</h2></div><span className="city-caption"><Icon name="pin" /> Córdoba capital</span></div>
      <form className="search-form" role="search" onSubmit={search}>
        <Icon name="search" /><label className="sr-only" htmlFor="business-search">Buscar negocios</label>
        <input key={filters.query} id="business-search" name="q" type="search" defaultValue={filters.query} maxLength={150} placeholder="Un negocio, algo que te gusta o una dirección…" />
        <button className="button button-primary" type="submit">Buscar <Icon name="arrow" /></button>
      </form>
      <LocationFilter key={JSON.stringify(filters.area)} area={filters.area} onChange={(area) => navigate({ ...filters, page: 1, area })} />
      <SponsoredCampaigns zone={filters.area?.kind === "zone" ? filters.area.zone ?? null : null} />
      {locationNotice && <p className="location-note" role="status">Este enlace no incluye la ubicación del dispositivo. Elegí una zona o usá tu ubicación para ver distancias.</p>}
      <div className="category-filters" role="group" aria-label="Filtrar por rubro">
        <button className={"category-chip " + (!filters.category ? "selected" : "")} aria-pressed={!filters.category} onClick={() => navigate({ ...filters, page: 1, category: "" })}><Icon name="grid" />Todos los rubros</button>
        {categories.map((category) => <button key={category} className={"category-chip " + (filters.category === category ? "selected" : "")} aria-pressed={filters.category === category} onClick={() => navigate({ ...filters, page: 1, category })}><Icon name={categoryStyle(category).icon} />{category}</button>)}
      </div>
      {(filters.query || filters.category) && <div className="applied-filters"><span>Tu búsqueda:</span>
        {filters.query && <button onClick={() => navigate({ ...filters, page: 1, query: "" })} aria-label={"Quitar búsqueda " + filters.query}>“{filters.query}” <Icon name="close" /></button>}
        {filters.category && <button onClick={() => navigate({ ...filters, page: 1, category: "" })} aria-label={"Quitar filtro " + filters.category}>{filters.category} <Icon name="close" /></button>}
        <button className="clear-filters" onClick={() => navigate(initialFilters)}>Limpiar filtros</button></div>}
      <div className="results-toolbar"><span aria-live="polite">{directory ? directory.items.length + (directory.hasMore ? "+" : "") + (directory.items.length === 1 ? " comercio" : " comercios") + " en esta página" : "Buscando comercios locales"}</span><span>Orden: <strong>{filters.area ? "Más cercanos" : "Nombre, A–Z"}</strong></span></div>
      {error ? <div className="empty-state"><span className="empty-icon"><Icon name="store" /></span><h3>La guía no está disponible por un momento.</h3><p role="alert">No pudimos cargar los negocios. Revisá tu conexión y volvé a intentarlo.</p><button className="button button-dark" onClick={() => navigate(filters)}>Reintentar</button></div>
        : !directory ? <div role="status" aria-label="Cargando negocios"><span className="sr-only">Cargando negocios…</span><div className="directory-grid skeleton-grid" aria-hidden="true">{[1, 2, 3].map((item) => <div className="skeleton-card" key={item}><div/><span/><span/><span/></div>)}</div></div>
        : <section aria-label="Negocios publicados">
          {directory.items.length === 0 ? <div className="empty-state"><span className="empty-icon"><Icon name="search" /></span><h3>{filtered ? "No encontramos negocios con esa búsqueda." : filters.page === 1 ? "Todavía no hay negocios publicados." : "No hay más negocios en esta página."}</h3><p>{filters.area ? "Ampliá el radio, elegí otra zona o quitá la ubicación para ver más negocios." : filtered ? "Probá con otro nombre, una dirección o un rubro diferente." : "Cada comercio tiene una historia. La tuya puede ser la próxima."}</p>{filtered ? <button className="button button-dark" onClick={() => navigate(initialFilters)}>Ver todos los negocios</button> : <Link className="button button-dark" href="/mi-negocio">Sumar mi negocio <Icon name="arrow" /></Link>}</div>
          : <div className="directory-grid">{directory.items.map((business) => <BusinessCard key={business.id} business={business} token={directory.measurement?.[business.id]}/>)}</div>}
        </section>}
      <nav className="directory-pagination" aria-label="Páginas de negocios">
        <button className="button button-outline" disabled={filters.page === 1 || (!directory && !error)} onClick={() => navigate({ ...filters, page: filters.page - 1 }, true)}><Icon name="chevron" className="rotate" />Anterior</button>
        <span>Página <strong>{filters.page}</strong></span>
        <button className="button button-outline" disabled={!directory?.hasMore || filters.page >= 10000} onClick={() => navigate({ ...filters, page: filters.page + 1 }, true)}>Siguiente<Icon name="chevron" /></button>
      </nav>
    </section>
    <section className="merchant-banner"><span className="banner-icon"><Icon name="store" /></span><div><p className="eyebrow">Detrás de cada persiana, una historia</p><h2>Tu negocio también tiene su lugar.</h2><p>Creá tu perfil y conectá con personas de tu ciudad.</p></div><Link className="button button-dark" href="/mi-negocio">Sumar mi negocio <Icon name="arrow" /></Link></section>
  </main><SiteFooter /></>;
}
