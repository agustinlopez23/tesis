import AdminShell from "../admin-shell";
import AdminPlans from "./plans";

export const metadata = { title: "Administrar planes | Cerca", robots: { index: false, follow: false } };
export default function PlansPage() {
  return <AdminShell section="planes" title="Planes de negocios"><AdminPlans /></AdminShell>;
}
