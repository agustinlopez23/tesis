import OwnerOnboarding from "../owner-onboarding";
import { SiteFooter, SiteHeader } from "../ui";

export default function OwnerPage() {
  return <><SiteHeader active="owner" /><main id="contenido" tabIndex={-1} className="container owner-main"><OwnerOnboarding /></main><SiteFooter /></>;
}
