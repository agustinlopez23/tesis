import Link from "next/link";

const sections = [
  { key: "planes", href: "/administracion/planes", label: "Planes" },
  { key: "campanas", href: "/administracion/campanas", label: "Campañas" },
  { key: "rubros", href: "/administracion/rubros", label: "Rubros" },
  { key: "moderacion", href: "/administracion/moderacion", label: "Moderación" },
  { key: "auditoria", href: "/administracion/auditoria", label: "Auditoría" },
] as const;

export type AdminSection = (typeof sections)[number]["key"];

export default function AdminNav({ current }: { current: AdminSection }) {
  return <nav className="admin-nav" aria-label="Administración">
    {sections.map((section) => <Link key={section.key} className="text-link" href={section.href}
      aria-current={section.key === current ? "page" : undefined}>{section.label}</Link>)}
  </nav>;
}
