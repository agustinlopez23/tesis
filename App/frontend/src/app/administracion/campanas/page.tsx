import AdminShell from "../admin-shell";
import CampaignPanel from "../../campaign-panel";

export const metadata = { title: "Campañas · Administración", robots: { index: false, follow: false } };
export default function CampaignAdministration() {
  return <AdminShell section="campanas" title="Revisión y difusión"><CampaignPanel admin /></AdminShell>;
}
