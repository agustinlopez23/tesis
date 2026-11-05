import AdminShell from "../admin-shell";
import AdminCategories from "./categories";

export const metadata = { title: "Rubros · Administración", robots: { index: false, follow: false } };
export default function CategoriesPage() {
  return <AdminShell section="rubros" title="Rubros"><AdminCategories /></AdminShell>;
}
