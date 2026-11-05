import type { ReactNode } from "react";
import { PageHeader, SiteFooter, SiteHeader } from "../ui";
import AdminNav, { type AdminSection } from "./admin-nav";

// Shared frame for every /administracion/* page: side navigation plus a page header.
// It is a plain component (not a Next layout) so each page keeps its own <main id="contenido"> and title.
export default function AdminShell({ section, title, description, children }: { section: AdminSection; title: ReactNode; description?: ReactNode; children: ReactNode }) {
  return <>
    <SiteHeader />
    <main id="contenido" className="container admin-shell">
      <div className="admin-layout">
        <AdminNav current={section} />
        <div className="admin-content">
          <PageHeader eyebrow="Administración" title={title} description={description} />
          {children}
        </div>
      </div>
    </main>
    <SiteFooter />
  </>;
}
