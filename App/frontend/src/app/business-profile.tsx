"use client";
import PublicCatalog from "./public-catalog";

import Image from "next/image";
import Link from "next/link";
import { useEffect, useState } from "react";
import { Badge, categoryStyle, EmptyState, Icon, SiteFooter, SiteHeader } from "./ui";
import { useToast } from "./toast";
import { profileMeasurementToken, trackActivity, useProfileVisit } from "./activity";
import { SponsoredCard } from "./sponsored-campaigns";
import type { PublicCampaign } from "./campaign-api";
import { whatsAppLink } from "./whatsapp";

export type Photo = { slot: number; caption: string; width: number; height: number };
type Profile = { id: string; name: string; description: string; address: string;
  category: string; whatsApp: string; photos: Photo[]; measurementToken?: string; promotions?: PublicCampaign[]; hasPublicCatalog?: boolean };

export default function BusinessProfile({ id }: { id: string }) {
  const [profile, setProfile] = useState<Profile | null>(null);
  const [status, setStatus] = useState<"loading" | "ready" | "missing" | "error">("loading");
  const [attempt, setAttempt] = useState(0);
  const { show, toast } = useToast();
  useProfileVisit(id, status === "ready" ? profile?.measurementToken : undefined);
  useEffect(() => {
    let active = true;
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 10000);
    void fetch("/api/marketplace/public/businesses/" + id, { cache: "no-store", signal: controller.signal })
      .then(async (response) => {
        if (response.status === 404) { if (active) setStatus("missing"); return; }
        if (!response.ok) throw new Error("Unavailable");
        const data: Profile = await response.json();
        if (active) { setProfile(data); setStatus("ready"); }
      }).catch(() => { if (active) setStatus("error"); }).finally(() => clearTimeout(timeout));
    return () => { active = false; controller.abort(); clearTimeout(timeout); };
  }, [id, attempt]);
  const style = categoryStyle(profile?.category ?? "");
  // Native share sheet when the browser offers one (mobile); otherwise copy the link and confirm with a toast.
  async function share() {
    const url = window.location.href;
    if (typeof navigator.share === "function") {
      try { await navigator.share({ title: profile?.name, url }); } catch { /* The visitor closed the share sheet. */ }
      return;
    }
    try { await navigator.clipboard.writeText(url); show("Enlace copiado"); }
    catch { show("No pudimos copiar el enlace. Copialo desde la barra de direcciones."); }
  }
  function contact() { if (profile) void trackActivity(id, profileMeasurementToken(id, profile.measurementToken), "contact", "profile"); }
  const mapsHref = profile?.address ? "https://www.google.com/maps/search/?api=1&query=" + encodeURIComponent(profile.address + ", Córdoba, Argentina") : null;
  return <><SiteHeader /><main id="contenido" tabIndex={-1} className="container profile-main">
    <Link className="text-link profile-back" href="/explorar"><Icon name="chevron" className="rotate" />Volver a la guía</Link>
    {status === "loading" ? <div className="profile-loading" role="status">Cargando el negocio…</div> :
      status === "missing" ? <EmptyState icon="store" headingLevel={2} title="Este negocio no está disponible." actions={<Link className="button button-dark" href="/explorar">Descubrir otros negocios</Link>}>Puede que todavía no esté publicado o que haya dejado de aparecer en la guía.</EmptyState> :
      status === "error" ? <div className="empty-state"><span className="empty-icon"><Icon name="alert" /></span><h1>No pudimos abrir este negocio.</h1><p role="alert">No pudimos cargar el perfil. Revisá tu conexión y volvé a intentarlo.</p><button className="button button-dark" onClick={() => { setStatus("loading"); setAttempt((value) => value + 1); }}>Reintentar</button></div> :
      profile && <>
        <header className="profile-heading">
          <div className="profile-identity">
            <div className="profile-badges"><Badge plain><Icon name={style.icon} />{profile.category}</Badge><span className="profile-local"><Icon name="store" />Comercio local</span></div>
            <h1>{profile.name}</h1>
            <p className="profile-address-line"><Icon name="pin" />{profile.address || "Córdoba · Consultá la dirección al negocio"}</p>
          </div>
          <div className="profile-actions">
            <a className="button button-whatsapp" onClick={contact} href={whatsAppLink(profile.whatsApp, profile.name)} target="_blank" rel="noopener noreferrer"><Icon name="message" />Contactar por WhatsApp</a>
            {mapsHref && <a className="button button-outline" href={mapsHref} target="_blank" rel="noopener noreferrer"><Icon name="map" />Cómo llegar</a>}
            <button type="button" className="button button-outline" onClick={() => void share()}><Icon name="share" />Compartir</button>
          </div>
        </header>
        <div className="profile-layout">
          <div className="profile-story">
            {profile.photos.length ? <PhotoGallery key={profile.id} business={profile} /> :
              <div className={"profile-no-photos " + style.tone}><Icon name={style.icon} /><span className="profile-phrase">{style.phrase}</span><p>Este negocio todavía no compartió fotos.</p></div>}
            <section className="profile-about"><p className="eyebrow">Detrás del negocio</p><h2>Conocé su propuesta.</h2><p>{profile.description || "Para conocer su propuesta, contactá directamente al negocio."}</p></section>
            <PublicCatalog key={profile.id} id={profile.id} enabled={profile.hasPublicCatalog !== false} />
            {!!profile.promotions?.length && <section className="profile-promotions" aria-labelledby="profile-promotions-title">
              <p className="eyebrow">Para aprovechar</p><h2 id="profile-promotions-title">Promociones vigentes</h2>
              <div className="catalog-grid">{profile.promotions.filter((item) => typeof item.token === "string").map((item) => <SponsoredCard key={item.id} campaign={item} />)}</div>
            </section>}
          </div>
          <aside className="profile-contact">
            <p className="eyebrow">Hablá con el negocio</p>
            <h2>Consultá disponibilidad, precios o lo que quieras saber.</h2>
            <a className="button button-whatsapp full-width" onClick={contact} href={whatsAppLink(profile.whatsApp, profile.name)} target="_blank" rel="noopener noreferrer"><Icon name="message" />Escribir por WhatsApp</a>
            <div className="profile-address"><Icon name="pin" /><div><strong>Dónde encontrarlo</strong><p>{profile.address || "Consultá la dirección por WhatsApp."}</p>{mapsHref && <a className="text-link" href={mapsHref} target="_blank" rel="noopener noreferrer">Abrir en Google Maps <Icon name="arrow" /></a>}</div></div>
          </aside>
        </div>
      </>}
    {toast}
  </main><SiteFooter /></>;
}

function PhotoGallery({ business }: { business: Profile }) {
  const [slot, setSlot] = useState(business.photos[0].slot);
  const [failed, setFailed] = useState(false);
  const [retry, setRetry] = useState(0);
  const photo = business.photos.find((item) => item.slot === slot) ?? business.photos[0];
  const index = business.photos.indexOf(photo);
  function select(next: number) { setSlot(business.photos[next].slot); setFailed(false); }
  return <section className="profile-gallery" aria-label="Fotos del negocio">
    <div className="profile-photo">
      {failed ? <div className="photo-unavailable"><p>No pudimos cargar esta foto.</p><button className="button button-outline" onClick={() => { setFailed(false); setRetry((value) => value + 1); }}>Reintentar foto</button></div> :
        <Image key={slot + ":" + retry} src={"/api/marketplace/public/businesses/" + business.id + "/photos/" + photo.slot + (retry ? "?v=" + retry : "")}
          alt={photo.caption || "Foto " + (index + 1) + " de " + business.name} fill unoptimized preload onError={() => setFailed(true)} />}
      <span className="photo-count">{index + 1} / {business.photos.length}</span>
    </div>
    <div className="gallery-caption"><p aria-live="polite">{photo.caption || business.name}</p>{business.photos.length > 1 && <div className="gallery-arrows">
      <button className="icon-button" aria-label="Foto anterior" onClick={() => select((index + business.photos.length - 1) % business.photos.length)}><Icon name="chevron" className="rotate" /></button>
      <button className="icon-button" aria-label="Foto siguiente" onClick={() => select((index + 1) % business.photos.length)}><Icon name="chevron" /></button>
    </div>}</div>
    {business.photos.length > 1 && <div className="gallery-thumbnails">{business.photos.map((item, number) => <button key={item.slot} className={photo.slot === item.slot ? "selected" : ""}
      aria-label={"Ver foto " + (number + 1) + ": " + (item.caption || business.name)} aria-pressed={photo.slot === item.slot} onClick={() => select(number)}>
      <Image src={"/api/marketplace/public/businesses/" + business.id + "/photos/" + item.slot} width={120} height={90} alt="" unoptimized /><span>{String(number + 1).padStart(2, "0")}</span>
    </button>)}</div>}
  </section>;
}
