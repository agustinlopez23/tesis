import { SiteFooter, SiteHeader } from "../../ui";
import AdminNav from "../admin-nav";
import AdminModeration from "./moderation";

export const metadata = { title: "Moderación · Administración", robots: { index: false, follow: false } };
export default function ModerationPage() {
  return <><SiteHeader /><main id="contenido" className="container admin-plans"><p className="eyebrow">Administración</p><h1>Moderación</h1><AdminNav current="moderacion" /><AdminModeration /></main><SiteFooter /></>;
}
