"use client";

import Image from "next/image";
import Link from "next/link";
import { useEffect, useState } from "react";
import { categoryStyle, Icon, SiteFooter, SiteHeader } from "./ui";
import { profileMeasurementToken, trackActivity, useProfileVisit } from "./activity";

export type Photo = { slot: number; caption: string; width: number; height: number };
type Profile = { id: string; name: string; description: string; address: string;
  category: string; whatsApp: string; photos: Photo[]; measurementToken?: string };

export default function BusinessProfile({ id }: { id: string }) {
  const [profile, setProfile] = useState<Profile | null>(null);
  const [status, setStatus] = useState<"loading" | "ready" | "missing" | "error">("loading");
  const [attempt, setAttempt] = useState(0);
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
  return <><SiteHeader /><main id="contenido" tabIndex={-1} className="container profile-main">
    <Link className="text-link profile-back" href="/explorar"><Icon name="chevron" className="rotate" />Volver a la guía</Link>
    {status === "loading" ? <div className="profile-loading" role="status">Cargando el negocio…</div> :
      status === "missing" ? <div className="empty-state"><Icon name="store" /><h1>Este negocio no está disponible.</h1><p>Puede que todavía no esté publicado o que haya dejado de aparecer en la guía.</p><Link className="button button-dark" href="/explorar">Descubrir otros negocios</Link></div> :
      status === "error" ? <div className="empty-state"><h1>No pudimos abrir este negocio.</h1><p role="alert">No pudimos cargar el perfil. Revisá tu conexión y volvé a intentarlo.</p><button className="button button-dark" onClick={() => { setStatus("loading"); setAttempt((value) => value + 1); }}>Reintentar</button></div> :
      profile && <>
        <header className="profile-heading"><div><span className={"category-label " + style.tone}>{profile.category}</span><h1>{profile.name}</h1><p><Icon name="pin" />{profile.address || "Córdoba · Consultá la dirección al negocio"}</p></div><span className="profile-local"><Icon name="store" />Comercio local</span></header>
        <div className="profile-layout">
          <div className="profile-story">
            {profile.photos.length ? <PhotoGallery key={profile.id} business={profile} /> :
              <div className={"profile-no-photos " + style.tone}><Icon name={style.icon} /><span className="profile-phrase">{style.phrase}</span><p>Este negocio todavía no compartió fotos.</p></div>}
            <section className="profile-about"><p className="eyebrow">Detrás del negocio</p><h2>Conocé su propuesta.</h2><p>{profile.description || "Para conocer su propuesta, contactá directamente al negocio."}</p></section>
          </div>
          <aside className="profile-contact"><span className="mini-icon"><Icon name="message" /></span><h2>Una buena conexión <br/>empieza con un hola.</h2><p>Consultá disponibilidad, precios o lo que quieras saber directamente con el negocio.</p>
            <a className="button button-dark full-width" onClick={() => void trackActivity(id, profileMeasurementToken(id, profile.measurementToken), "contact", "profile")} href={"https://wa.me/" + profile.whatsApp.replace(/^\+/, "")} target="_blank" rel="noopener noreferrer">Contactar por WhatsApp<Icon name="arrow" /></a>
            <div className="profile-address"><Icon name="pin" /><div><strong>Dónde encontrarlo</strong><p>{profile.address || "Consultá la dirección por WhatsApp."}</p>{profile.address && <a className="text-link" href={"https://www.google.com/maps/search/?api=1&query=" + encodeURIComponent(profile.address + ", Córdoba, Argentina")} target="_blank" rel="noopener noreferrer">Buscar dirección en Maps <Icon name="arrow" /></a>}</div></div>
          </aside>
        </div>
      </>}
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
