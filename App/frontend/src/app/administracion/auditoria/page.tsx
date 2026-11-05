import AdminShell from "../admin-shell";
import AdminAudit from "./audit";

export const metadata = { title: "Auditoría · Administración", robots: { index: false, follow: false } };
export default function AuditPage() {
  return <AdminShell section="auditoria" title="Auditoría"><AdminAudit /></AdminShell>;
}
