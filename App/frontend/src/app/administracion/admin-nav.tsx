import { SectionNav, type SectionItem } from "../ui";

const sections = [
  { key: "planes", href: "/administracion/planes", label: "Planes", icon: "shield" },
  { key: "campanas", href: "/administracion/campanas", label: "Campañas", icon: "megaphone" },
  { key: "rubros", href: "/administracion/rubros", label: "Rubros", icon: "tag" },
  { key: "moderacion", href: "/administracion/moderacion", label: "Moderación", icon: "users" },
  { key: "auditoria", href: "/administracion/auditoria", label: "Auditoría", icon: "list" },
] as const;

export type AdminSection = (typeof sections)[number]["key"];
const items: SectionItem[] = sections.map((section) => ({ id: section.key, label: section.label, href: section.href, icon: section.icon }));

// Vertical on desktop, horizontal scroll row on narrow screens (see admin.css).
export default function AdminNav({ current }: { current: AdminSection }) {
  return <div className="admin-sidebar"><SectionNav items={items} current={current} label="Administración" /></div>;
}
