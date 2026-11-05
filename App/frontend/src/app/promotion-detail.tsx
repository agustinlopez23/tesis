"use client";

import { useEffect, useState } from "react";
import Image from "next/image";
import Link from "next/link";
import { catalogApi } from "./catalog-api";
import { campaignEvent, PublicCampaign, rememberCampaign } from "./campaign-api";
import { Alert, Icon, SiteFooter, SiteHeader, Skeleton } from "./ui";

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
  return <><SiteHeader /><main id="contenido" className="container promotion-detail"><Link className="text-link profile-back" href="/explorar"><Icon name="chevron" className="rotate" />Explorar negocios</Link>
    {error ? <p className="error" role="alert">{error}</p> : !campaign ? <Skeleton variant="lines" count={4} label="Consultando promoción…" /> : <article className="promotion-card">
      <div className="sponsored-label">Patrocinado</div>
      <header className="promotion-heading"><h1>{campaign.title}</h1><p className="promotion-business"><Icon name="store" />{campaign.businessName}</p></header>
      {campaign.hasImage && <Image className="promotion-image" src={`/api/marketplace/public/campaigns/${campaign.id}/image`} alt={campaign.title} width={960} height={640} unoptimized />}
      <dl className="promotion-facts">
        <div><dt>Detalle</dt><dd>{campaign.description}</dd></div>
        <div><dt>Condiciones</dt><dd>{campaign.conditions}</dd></div>
        <div><dt>Vigencia</dt><dd><Icon name="clock" />Vigente hasta {new Date(campaign.endsAt).toLocaleString("es-AR")}.</dd></div>
      </dl>
      <div className="promotion-actions"><a className="button button-whatsapp" onClick={visit} href={`https://wa.me/${campaign.whatsApp.replace(/\D/g, "")}?text=${encodeURIComponent(`Hola, consulto por la promoción «${campaign.title}» que vi en Cerca.`)}`} target="_blank" rel="noreferrer"><Icon name="message" />Consultar por WhatsApp</a>
        <Link className="button button-outline" onClick={visit} href={`/negocios/${campaign.businessId}?campaign=${campaign.id}`}><Icon name="store" />Ver negocio</Link></div>
      <Alert tone="info">Confirmá disponibilidad y precio con el negocio. Abrir WhatsApp no confirma el envío de un mensaje ni una compra.</Alert>
    </article>}
  </main><SiteFooter /></>;
}
