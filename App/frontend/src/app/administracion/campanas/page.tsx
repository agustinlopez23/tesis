import AdminNav from "../admin-nav";
import { SiteFooter, SiteHeader } from "../../ui";
import CampaignPanel from "../../campaign-panel";

export const metadata = { title: "Campañas · Administración", robots: { index: false, follow: false } };
export default function CampaignAdministration() {
  return <><SiteHeader /><main id="contenido" className="container admin-plans"><p className="eyebrow">Administración</p><h1>Revisión y difusión</h1><AdminNav current="campanas" /><CampaignPanel admin /></main><SiteFooter /></>;
}
