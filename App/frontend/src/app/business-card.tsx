"use client";

import Image from "next/image";
import Link from "next/link";
import { useState } from "react";
import { categoryStyle, Icon } from "./ui";
import { distanceLabel } from "./explorar/discovery-location";
import { rememberDiscovery, trackActivity, useImpression } from "./activity";

export type Business = { id: string; name: string; description: string; address: string; category: string; whatsApp: string; distanceKm?: number; coverPhotoSlot?: number };

export function BusinessCard({ business, token }: { business: Business; token?: string }) {
  const impression = useImpression(business.id, token);
  const style = categoryStyle(business.category);
  const [expanded, setExpanded] = useState(false);
  const [coverFailed, setCoverFailed] = useState(false);
  const profileUrl = "/negocios/" + business.id;
  return <article className="business-card" ref={impression}>
    <Link href={profileUrl} onClick={() => rememberDiscovery(business.id, token)} className={"business-cover " + style.tone} aria-label={"Ver perfil de " + business.name} tabIndex={-1}>
      {business.coverPhotoSlot && !coverFailed ? <Image src={"/api/marketplace/public/businesses/" + business.id + "/photos/" + business.coverPhotoSlot} alt="" fill unoptimized onError={() => setCoverFailed(true)} /> :
        <><span className="cover-phrase">{style.phrase}</span><Icon name={style.icon} /><span className="cover-monogram">{business.name.charAt(0)}</span></>}
    </Link>
    <div className="business-card-body"><span className={"category-label " + style.tone}>{business.category}</span><h3>{business.name}</h3>
      <p className="business-meta"><Icon name="pin" /><span className="business-address">{business.address || "Córdoba · Consultá la dirección"}</span>
        {business.distanceKm !== undefined && <span className="business-distance">{distanceLabel(business.distanceKm)}</span>}</p>
      <p className={"business-description " + (!expanded && business.description.length > 110 ? "collapsed" : "")}>{business.description || "Conocé su propuesta y contactá directamente con el negocio."}</p>
      {business.description.length > 110 && <button className="description-toggle" aria-expanded={expanded} onClick={() => setExpanded(!expanded)}>{expanded ? "Ver menos" : "Leer descripción completa"}</button>}
      <div className="card-actions">
        <Link className="button button-outline button-small" href={profileUrl} onClick={() => rememberDiscovery(business.id, token)}>Conocer el negocio <Icon name="arrow" /></Link>
        <a className="button button-whatsapp button-small" onClick={() => void trackActivity(business.id, token, "contact", "directory")} href={"https://wa.me/" + business.whatsApp.replace(/^\+/, "")} target="_blank" rel="noopener noreferrer" aria-label={"Contactar a " + business.name + " por WhatsApp"}><Icon name="message" />WhatsApp</a>
      </div>
    </div>
  </article>;
}
