"use client";

import { useEffect, useState } from "react";
import Image from "next/image";
import Link from "next/link";
import { catalogApi } from "./catalog-api";
import { campaignEvent, PublicCampaign, rememberCampaign } from "./campaign-api";
import { SiteFooter, SiteHeader } from "./ui";

export default function PromotionDetail({ id }: { id: string }) {
  const [campaign, setCampaign] = useState<PublicCampaign | null>(null), [error, setError] = useState("");
  useEffect(() => {
    const controller = new AbortController();
    void catalogApi(`/public/campaigns/${id}`, "GET", undefined, controller.signal).then(async response => {
      if (!response.ok) throw new Error(response.status === 404 ? "Esta promoción ya no está disponible." : "No pudimos consultar la promoción. Volvé a intentarlo.");
      const data = await response.json(); if (!controller.signal.aborted) setCampaign(data);
    }).catch(e => { if (!controller.signal.aborted) setError(e.message); });
    return () => controller.abort();
  }, [id]);
  function visit() { if (campaign) { rememberCampaign(campaign); void campaignEvent(campaign.token, "click"); } }
  return <><SiteHeader /><main id="contenido" className="container promotion-detail"><Link className="text-link" href="/explorar">Explorar negocios</Link>
    {error ? <p role="alert">{error}</p> : !campaign ? <p role="status">Consultando promoción…</p> : <article>
      <p className="sponsored-label">Patrocinado</p><h1>{campaign.title}</h1><p>{campaign.businessName}</p>
      {campaign.hasImage && <Image className="promotion-image" src={`/api/marketplace/public/campaigns/${campaign.id}/image`} alt={campaign.title} width={960} height={640} unoptimized />}
      <p>{campaign.description}</p><h2>Condiciones</h2><p>{campaign.conditions}</p><p>Vigente hasta {new Date(campaign.endsAt).toLocaleString("es-AR")}.</p>
      <div className="catalog-toolbar"><a className="button button-primary" onClick={visit} href={`https://wa.me/${campaign.whatsApp.replace(/\D/g, "")}?text=${encodeURIComponent(`Hola, consulto por la promoción «${campaign.title}» que vi en Cerca.`)}`} target="_blank" rel="noreferrer">Consultar por WhatsApp</a>
        <Link className="text-link" onClick={visit} href={`/negocios/${campaign.businessId}?campaign=${campaign.id}`}>Ver negocio</Link></div>
      <p className="field-help">Confirmá disponibilidad y precio con el negocio. Abrir WhatsApp no confirma el envío de un mensaje ni una compra.</p>
    </article>}
  </main><SiteFooter /></>;
}
