import { SiteFooter, SiteHeader } from "../../ui";
import AdminNav from "../admin-nav";
import AdminAudit from "./audit";

export const metadata = { title: "Auditoría · Administración", robots: { index: false, follow: false } };
export default function AuditPage() {
  return <><SiteHeader /><main id="contenido" className="container admin-plans"><p className="eyebrow">Administración</p><h1>Auditoría</h1><AdminNav current="auditoria" /><AdminAudit /></main><SiteFooter /></>;
}
