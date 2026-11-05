import AdminShell from "../admin-shell";
import AdminModeration from "./moderation";

export const metadata = { title: "Moderación · Administración", robots: { index: false, follow: false } };
export default function ModerationPage() {
  return <AdminShell section="moderacion" title="Moderación"><AdminModeration /></AdminShell>;
}
