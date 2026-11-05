"use client";

import { useEffect, useRef, useState } from "react";
import Image from "next/image";
import Link from "next/link";
import { catalogApi } from "./catalog-api";
import { campaignEvent, PublicCampaign, rememberCampaign } from "./campaign-api";
import { Icon } from "./ui";

export default function SponsoredCampaigns({ zone }: { zone: string | null }) {
  if (!zone) return null;
  return <SponsoredList key={zone} zone={zone} />;
}
function SponsoredList({ zone }: { zone: string }) {
  const [items, setItems] = useState<PublicCampaign[]>([]);
  useEffect(() => {
    const controller = new AbortController();
    void catalogApi(`/public/campaigns?zone=${encodeURIComponent(zone)}`, "GET", undefined, controller.signal).then(async response => {
      if (response.ok) { const data = await response.json(); if (!controller.signal.aborted) setItems(Array.isArray(data.items) ? data.items.filter((c: PublicCampaign) => typeof c.token === "string" && typeof c.title === "string") : []); }
    }).catch(() => undefined);
    return () => controller.abort();
  }, [zone]);
  if (!items.length) return null;
  return <aside className="sponsored-section" aria-label="Anuncios patrocinados"><p className="eyebrow"><Icon name="megaphone" />Publicidad de negocios en la zona elegida</p><div className="catalog-grid">{items.map(item => <SponsoredCard key={item.id} campaign={item} />)}</div></aside>;
}
export function SponsoredCard({ campaign }: { campaign: PublicCampaign }) {
  const element = useRef<HTMLElement>(null);
  useEffect(() => {
    if (!element.current || typeof IntersectionObserver === "undefined") return;
    let visible = false, sent = false; let timer: ReturnType<typeof setTimeout> | undefined;
    function update() {
      if (!visible || document.visibilityState !== "visible") { clearTimeout(timer); timer = undefined; return; }
      if (sent || timer) return;
      timer = setTimeout(() => { if (visible && document.visibilityState === "visible") { sent = true; void campaignEvent(campaign.token, "impression"); } }, 1000);
    }
    const observer = new IntersectionObserver(entries => { const entry = entries.at(-1); visible = !!entry?.isIntersecting && entry.intersectionRatio >= .5; update(); }, { threshold: [0, .5] });
    observer.observe(element.current); document.addEventListener("visibilitychange", update);
    return () => { observer.disconnect(); clearTimeout(timer); document.removeEventListener("visibilitychange", update); };
  }, [campaign.token]);
  const href = campaign.productId ? `/negocios/${campaign.businessId}?campaign=${campaign.id}#catalog-title` : `/promociones/${campaign.id}`;
  // The sponsored label sits top-left on every sponsored card so it is recognised at a glance.
  return <article ref={element} className="product-card sponsored-card"><span className="sponsored-label badge badge-warning">Patrocinado</span>
    {campaign.hasImage ? <Image src={`/api/marketplace/public/campaigns/${campaign.id}/image`} alt={campaign.title} width={400} height={300} unoptimized />
      : <div className="product-placeholder sand" aria-hidden="true"><Icon name="megaphone" /></div>}
    <div className="product-card-copy"><h3>{campaign.title}</h3><p className="product-description">{campaign.description}</p>
      <p className="product-business"><Icon name="store" />{campaign.businessName}</p><p className="field-help">{campaign.conditions}</p>
      <Link className="button button-outline button-small" href={href} aria-label={`Ver ${campaign.title}`} onClick={() => { rememberCampaign(campaign); void campaignEvent(campaign.token, "click"); }}>Ver propuesta <Icon name="arrow" /></Link></div>
  </article>;
}
