"use client";

import Image from "next/image";
import Link from "next/link";
import { FormEvent, useEffect, useRef, useState } from "react";
import { BusinessCard } from "../business-card";
import { zones } from "../explorar/discovery-location";
import { Icon, SiteFooter, SiteHeader } from "../ui";
import { useRankingPreview } from "./use-ranking-preview";
import "./revista.css";

type Category = { id: number; name: string };
const messages: Record<number, [string, string]> = {
  410: ["Esta lista ya no está disponible.", "Actualizá los resultados para empezar una nueva lista."],
  404: ["La revista está pausada.", "Podés seguir explorando en la guía de negocios."],
  429: ["Hay muchas consultas en este momento.", "Esperá un momento y volvé a intentarlo."],
  503: ["No pudimos preparar esta lista.", "Podés reintentar o explorar la guía de negocios."],
  400: ["Necesitamos actualizar esta lista.", "Volvé a consultar para descubrir los negocios de la zona."],
  409: ["Necesitamos actualizar esta lista.", "Volvé a consultar para descubrir los negocios de la zona."],
  403: ["No pudimos abrir esta consulta.", "Podés seguir explorando en la guía de negocios."],
  0: ["No pudimos cargar los negocios.", "Revisá tu conexión y volvé a intentarlo."],
};

export default function RankingPreview() {
  const ranking = useRankingPreview();
  const [draft, setDraft] = useState({ zoneId: "centro", categoryId: 0 });
  const [categories, setCategories] = useState<Category[] | null>(null);
  const [categoryError, setCategoryError] = useState(false);
  const [attempt, setAttempt] = useState(0);
  const resultsHeading = useRef<HTMLHeadingElement>(null);
  const selection = ranking.navigation?.selection ?? draft;
  useEffect(() => {
    const controller = new AbortController();
    let active = true;
    void fetch("/api/marketplace/business-categories", {
      cache: "no-store", signal: AbortSignal.any([controller.signal, AbortSignal.timeout(10000)]),
    }).then(async response => {
      if (!response.ok) throw new Error("Categories unavailable");
      const data: Category[] = await response.json();
      if (active) { setCategories(data); setCategoryError(false); }
    }).catch(() => { if (active) setCategoryError(true); });
    return () => { active = false; controller.abort(); };
  }, [attempt]);

  useEffect(() => {
    if (ranking.data || ranking.error !== null) {
      resultsHeading.current?.focus({ preventScroll: true });
      resultsHeading.current?.scrollIntoView?.({ block: "start" });
    }
  }, [ranking.data, ranking.error]);

  function discover(event: FormEvent) {
    event.preventDefault();
    if (categories?.some(category => category.id === selection.categoryId)) void ranking.start(selection);
  }
  function change(next: typeof draft) { setDraft(next); ranking.reset(); }
  const error = ranking.error === null ? null : (messages[ranking.error] ?? messages[0]);
  const refresh = ranking.error !== null && [400, 409, 410].includes(ranking.error);
  const retry = ranking.error !== null && ![400, 403, 404, 409, 410].includes(ranking.error);
  const category = categories?.find(item => item.id === selection.categoryId)?.name;
  const zone = zones.find(item => item.id === selection.zoneId)!;

  return <><SiteHeader active="preview" /><main id="contenido" tabIndex={-1} className="container magazine-main">
    <Link className="text-link" href="/explorar"><Icon name="chevron" className="rotate" />Volver a la guía</Link>
    <section className="magazine-hero" aria-labelledby="magazine-title">
      <div className="magazine-copy"><span className="magazine-badge"><Icon name="eye" />Vista previa · Revista local</span>
        <h1 id="magazine-title">Tu próxima parada<br/><em>está cerca.</em></h1>
        <p>Un café, un regalo, lo de todos los días. Elegí una zona y un rubro para descubrir los comercios de Córdoba.</p>
        <span className="magazine-note"><Icon name="pin" />Comercios a hasta 4 km del punto elegido</span>
      </div>
      <div className="magazine-picture"><Image src="/images/neighborhood-editorial.webp" alt="Escena ilustrativa de comercios de barrio" fill sizes="(max-width: 700px) 100vw, 40vw" preload />
        <span className="picture-tag">Hecho de historias locales</span><span className="picture-credit">Imagen ilustrativa</span></div>
    </section>

    <section className="magazine-discovery" aria-labelledby="magazine-filters-title">
      <div className="magazine-form-heading"><h2 id="magazine-filters-title">¿Por dónde empezamos?</h2><span>Una zona. Un rubro. Algo por descubrir.</span></div>
      <form className="magazine-form" onSubmit={discover} aria-label="Elegir zona y rubro">
        <label htmlFor="magazine-zone">Zona<select id="magazine-zone" value={selection.zoneId} onChange={event => change({ ...selection, zoneId: event.target.value })}>
          {zones.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}
        </select></label>
        <label htmlFor="magazine-category">Rubro<select id="magazine-category" value={selection.categoryId || ""} required disabled={!categories} onChange={event => change({ ...selection, categoryId: Number(event.target.value) })}>
          <option value="" disabled>{categories ? "Elegí un rubro" : "Cargando rubros…"}</option>
          {categories?.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}
        </select></label>
        <button className="button button-primary" disabled={ranking.busy || !selection.categoryId || !categories} type="submit">Descubrir negocios<Icon name="arrow" /></button>
      </form>
      {categoryError && <div className="magazine-category-error"><p role="alert">No pudimos cargar los rubros.</p><button className="text-link" onClick={() => { setCategoryError(false); setAttempt(value => value + 1); }}>Reintentar rubros</button></div>}
      <p className="magazine-form-note">Esta vista está en prueba. La guía habitual sigue disponible para buscar por nombre o usar tu ubicación.</p>
    </section>

    <section className="magazine-results" aria-labelledby="magazine-results-title" aria-busy={ranking.busy}>
      <div className="section-heading"><div><p className="eyebrow">La revista de tu zona</p><h2 id="magazine-results-title" ref={resultsHeading} tabIndex={-1}>{ranking.navigation ? `${category ?? "Comercios"} en ${zone.name.split(" · ")[0]}` : "Encontrá un nuevo favorito."}</h2></div>
        {ranking.data && <button className="button button-outline" onClick={() => void ranking.start(selection)}>Actualizar resultados</button>}
      </div>
      {ranking.busy ? <div role="status"><p>Buscando comercios locales…</p><div className="directory-grid skeleton-grid" aria-hidden="true">{[1, 2, 3].map(item => <div className="skeleton-card" key={item}><div/><span/><span/><span/></div>)}</div></div>
        : error ? <div className="empty-state"><span className="empty-icon"><Icon name="store" /></span><h3>{error[0]}</h3><p role="alert">{error[1]}</p><div className="magazine-actions">
          {refresh && <button className="button button-dark" onClick={() => void ranking.start(selection)}>Actualizar resultados</button>}
          {retry && <button className="button button-dark" onClick={() => void ranking.retry()}>Reintentar</button>}
          <Link className="button button-outline" href="/explorar">Ir a la guía de negocios</Link></div></div>
        : ranking.data ? <>
          <div className="results-toolbar"><span role="status">{ranking.data.items.length} comercios en esta página</span><span>Orden: <strong>Revista · vista previa</strong></span></div>
          {ranking.data.items.length ? <div className="directory-grid">{ranking.data.items.map(business => <BusinessCard key={business.id} business={business} token={ranking.data?.measurement[business.id]} />)}</div>
            : <div className="empty-state"><span className="empty-icon"><Icon name="search" /></span><h3>{ranking.data.hasMore ? "No hay negocios disponibles en esta página." : "No encontramos negocios disponibles."}</h3><p>{ranking.data.hasMore ? "Podés seguir a la próxima página o actualizar los resultados." : "Probá con otro rubro o elegí una zona diferente."}</p></div>}
          <nav className="directory-pagination" aria-label="Páginas de la revista">
            <button className="button button-outline" disabled={ranking.navigation!.page === 1} onClick={() => void ranking.go(ranking.navigation!.page - 1)}><Icon name="chevron" className="rotate" />Anterior</button>
            <span>Página <strong>{ranking.navigation!.page}</strong></span>
            <button className="button button-outline" disabled={!ranking.data.hasMore} onClick={() => void ranking.go(ranking.navigation!.page + 1)}>Siguiente<Icon name="chevron" /></button>
          </nav><p className="magazine-expiry">El orden se mantiene hasta las {new Date(ranking.data.expiresAt).toLocaleTimeString("es-AR", { hour: "2-digit", minute: "2-digit" })}. Los negocios que dejan de estar disponibles pueden dejar espacios en la lista.</p>
        </> : <div className="magazine-invitation"><span className="mini-icon"><Icon name="coffee" /></span><div><h3>Hay historias a la vuelta.</h3><p>Elegí una zona y un rubro, y tocá «Descubrir negocios» para empezar.</p></div></div>}
    </section>
  </main><SiteFooter /></>;
}
