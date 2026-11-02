import { SiteFooter, SiteHeader } from "../../ui";
import AdminNav from "../admin-nav";
import AdminCategories from "./categories";

export const metadata = { title: "Rubros · Administración", robots: { index: false, follow: false } };
export default function CategoriesPage() {
  return <><SiteHeader /><main id="contenido" className="container admin-plans"><p className="eyebrow">Administración</p><h1>Rubros</h1><AdminNav current="rubros" /><AdminCategories /></main><SiteFooter /></>;
}
