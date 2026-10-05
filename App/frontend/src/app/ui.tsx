import Link from "next/link";
import type { ReactNode } from "react";

export type IconName = "pin" | "arrow" | "search" | "store" | "coffee" | "bag" | "shirt" | "grid" | "check" | "close" | "message" | "chevron" | "logout" | "plus" | "eye";
const paths: Record<IconName, ReactNode> = {
  pin: <><path d="M20 10c0 6-8 11-8 11S4 16 4 10a8 8 0 1 1 16 0Z"/><circle cx="12" cy="10" r="2.5"/></>,
  arrow: <path d="M4 12h16M14 6l6 6-6 6"/>,
  search: <><circle cx="10.5" cy="10.5" r="6.5"/><path d="m16 16 5 5"/></>,
  store: <path d="M4 10v11h16V10M3 10l2-7h14l2 7M3 10c0 4 6 4 6 0 0 4 6 4 6 0 0 4 6 4 6 0M9 21v-7h6v7"/>,
  coffee: <path d="M4 8h13v7a5 5 0 0 1-5 5H9a5 5 0 0 1-5-5ZM17 9h2a3 3 0 0 1 0 6h-2M7 2v2M12 2v2M2 23h19"/>,
  bag: <path d="M4 7h16l-1 14H5ZM8 8V6a4 4 0 0 1 8 0v2"/>,
  shirt: <path d="m8 3-6 4 3 5 3-2v11h8V10l3 2 3-5-6-4c0 4-8 4-8 0Z"/>,
  grid: <><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/></>,
  check: <path d="m5 12 4 4L19 6"/>,
  close: <path d="m6 6 12 12M6 18 18 6"/>,
  message: <><path d="M21 11.5a9 9 0 0 1-13.4 7.9L3 21l1.6-4.6A9 9 0 1 1 21 11.5Z"/><path d="M8 8c0 4 4 8 8 8l1-3-3-1-1 1-2-2 1-1-1-3Z"/></>,
  chevron: <path d="m9 5 7 7-7 7"/>,
  logout: <path d="M9 3H4v18h5M9 12h12m-5-5 5 5-5 5"/>,
  plus: <path d="M12 4v16M4 12h16"/>,
  eye: <><path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7S2 12 2 12Z"/><circle cx="12" cy="12" r="3"/></>,
};

export function Icon({ name, className = "" }: { name: IconName; className?: string }) {
  return <svg className={`icon ${className}`} width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.65" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">{paths[name]}</svg>;
}

export function SiteHeader({ active = "explore" }: { active?: "explore" | "owner" | "preview" }) {
  return <header className="site-header"><div className="container nav-inner">
    <Link className="brand" href="/" aria-label="Cerca, inicio"><span className="brand-symbol"><Icon name="store" /></span>cerca<span className="brand-dot">.</span></Link>
    <span className="location-label"><Icon name="pin" /> Córdoba, Argentina</span>
    <nav aria-label="Navegación principal">
      <Link className={`nav-link ${active === "explore" ? "active" : ""}`} href="/explorar" aria-current={active === "explore" ? "page" : undefined}>Explorar negocios</Link>
      <Link className="button button-small button-dark" href="/mi-negocio" aria-current={active === "owner" ? "page" : undefined}>Mi negocio <Icon name="arrow" /></Link>
    </nav>
  </div></header>;
}

export function SiteFooter() {
  return <footer className="site-footer container"><div><Link className="brand footer-brand" href="/">cerca<span className="brand-dot">.</span></Link><p>Lo mejor de tu ciudad empieza en tu barrio.</p></div><span>Hecho para el comercio local.<br/><span className="muted">Córdoba, Argentina</span></span></footer>;
}

export function categoryStyle(category: string): { icon: IconName; tone: string; phrase: string } {
  if (category === "Gastronomía") return { icon: "coffee", tone: "clay", phrase: "Sabores de acá" };
  if (category === "Indumentaria") return { icon: "shirt", tone: "lavender", phrase: "Con identidad local" };
  if (category === "Almacenes") return { icon: "bag", tone: "olive", phrase: "Lo de todos los días" };
  return { icon: "store", tone: "sand", phrase: "Comercio de tu ciudad" };
}
