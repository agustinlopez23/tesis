import { SiteFooter, SiteHeader } from "../../ui";
import AdminNav from "../admin-nav";
import AdminPlans from "./plans";

export const metadata = { title: "Administrar planes | Cerca", robots: { index: false, follow: false } };
export default function PlansPage() {
  return <><SiteHeader /><main id="contenido" className="container admin-plans"><p className="eyebrow">Administración</p><h1>Planes de negocios</h1><AdminNav current="planes" /><AdminPlans /></main><SiteFooter /></>;
}
