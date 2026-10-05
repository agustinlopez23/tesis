import Link from "next/link";
import { SiteFooter, SiteHeader } from "../../ui";
import CampaignPanel from "../../campaign-panel";

export const metadata = { title: "Campañas · Administración", robots: { index: false, follow: false } };
export default function CampaignAdministration() {
  return <><SiteHeader /><main id="contenido" className="container admin-plans"><p className="eyebrow">Administración</p><h1>Revisión y difusión</h1><Link className="text-link" href="/administracion/planes">Administrar planes</Link><CampaignPanel admin /></main><SiteFooter /></>;
}
