import Link from "next/link";
import type { ReactNode } from "react";

export type IconName = "pin" | "arrow" | "search" | "store" | "coffee" | "bag" | "shirt" | "grid" | "check" | "close" | "message" | "chevron" | "logout" | "plus" | "eye"
  | "home" | "tag" | "cart" | "share" | "map" | "box" | "chart" | "megaphone" | "image" | "users" | "shield" | "list" | "alert" | "info" | "clock" | "user" | "camera";
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
  home: <path d="M3 11 12 3l9 8M5 10v11h5v-6h4v6h5V10"/>,
  tag: <><path d="M3 12V3h9l9 9-9 9Z"/><circle cx="8" cy="8" r="1.5"/></>,
  cart: <><path d="M3 4h2l2.5 12h11L21 7H7"/><circle cx="9" cy="20" r="1.5"/><circle cx="17" cy="20" r="1.5"/></>,
  share: <><circle cx="18" cy="5" r="2.5"/><circle cx="6" cy="12" r="2.5"/><circle cx="18" cy="19" r="2.5"/><path d="m8.2 10.8 7.6-4.6M8.2 13.2l7.6 4.6"/></>,
  map: <path d="m3 6 6-3 6 3 6-3v15l-6 3-6-3-6 3ZM9 3v15M15 6v15"/>,
  box: <path d="m3 7 9-4 9 4-9 4Zm0 0v10l9 4 9-4V7M12 11v10"/>,
  chart: <path d="M4 20V10M10 20V4M16 20v-7M22 20H2"/>,
  megaphone: <path d="M3 10v4h3l8 5V5L6 10Zm15-1a4 4 0 0 1 0 6"/>,
  image: <><rect x="3" y="4" width="18" height="16" rx="2"/><circle cx="9" cy="10" r="1.5"/><path d="m21 17-6-6-7 7"/></>,
  users: <><circle cx="9" cy="8" r="3.5"/><path d="M2 20c0-4 3-6 7-6s7 2 7 6M16 4.5a3.5 3.5 0 0 1 0 7M22 20c0-3-1.7-5-4.5-5.7"/></>,
  shield: <path d="M12 2 4 5v6c0 5 3.5 9 8 11 4.5-2 8-6 8-11V5Zm-3 10 2 2 4-4"/>,
  list: <path d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01"/>,
  alert: <path d="M12 3 2 21h20ZM12 10v5M12 18v.5"/>,
  info: <><circle cx="12" cy="12" r="9"/><path d="M12 11v5M12 8v.5"/></>,
  clock: <><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></>,
  user: <><circle cx="12" cy="8" r="4"/><path d="M4 21c0-4 3.5-7 8-7s8 3 8 7"/></>,
  camera: <><path d="M3 8h4l2-3h6l2 3h4v12H3Z"/><circle cx="12" cy="13" r="3.5"/></>,
};

export function Icon({ name, className = "" }: { name: IconName; className?: string }) {
  return <svg className={`icon ${className}`} width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.65" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">{paths[name]}</svg>;
}

export function SiteHeader({ active = "explore" }: { active?: "explore" | "owner" | "preview" | "products" }) {
  return <header className="site-header"><div className="container nav-inner">
    <Link className="brand" href="/" aria-label="Cerca, inicio"><span className="brand-symbol"><Icon name="store" /></span>cerca<span className="brand-dot">.</span></Link>
    <span className="location-label"><Icon name="pin" /> Córdoba, Argentina</span>
    <nav aria-label="Navegación principal">
      <Link className={`nav-link ${active === "explore" ? "active" : ""}`} href="/explorar" aria-current={active === "explore" ? "page" : undefined}><Icon name="home" />Explorar negocios</Link>
      <Link className={`nav-link ${active === "products" ? "active" : ""}`} href="/productos" aria-current={active === "products" ? "page" : undefined}><Icon name="search" />Productos</Link>
      <Link className="button button-small button-dark" href="/mi-negocio" aria-current={active === "owner" ? "page" : undefined}><Icon name="user" />Mi negocio <Icon name="arrow" className="arrow-icon" /></Link>
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

// Shared primitives. They only add structure and tone; copy and accessible names stay in each screen.
export type Tone = "success" | "warning" | "danger" | "info" | "neutral";
const toneIcon: Record<Tone, IconName> = { success: "check", warning: "alert", danger: "alert", info: "info", neutral: "info" };

export function Alert({ tone = "info", title, children, onClose, role, className = "" }: {
  tone?: Tone; title?: string; children?: ReactNode; onClose?: () => void; role?: "alert" | "status"; className?: string;
}) {
  return <div role={role ?? (tone === "danger" ? "alert" : "status")} className={`alert alert-${tone} ${className}`}>
    <Icon name={toneIcon[tone]} /><div>{title && <strong>{title}</strong>}{children}</div>
    {onClose && <button type="button" className="icon-button" aria-label="Cerrar aviso" onClick={onClose}><Icon name="close" /></button>}
  </div>;
}

export function Badge({ tone = "neutral", children, plain = false }: { tone?: Tone; children: ReactNode; plain?: boolean }) {
  return <span className={`badge badge-${tone} ${plain ? "badge-plain" : ""}`}>{children}</span>;
}

export function EmptyState({ icon = "store", title, children, actions, compact = false, headingLevel = 3, role }: {
  icon?: IconName; title: string; children?: ReactNode; actions?: ReactNode; compact?: boolean; headingLevel?: 2 | 3; role?: "status" | "alert";
}) {
  const Heading = headingLevel === 2 ? "h2" : "h3";
  return <div className={`empty-state ${compact ? "compact" : ""}`} role={role}>
    <span className="empty-icon"><Icon name={icon} /></span><Heading>{title}</Heading>
    {children && <p>{children}</p>}{actions && <div className="empty-actions">{actions}</div>}
  </div>;
}

export function Panel({ id, eyebrow, title, description, actions, children, className = "", labelledBy }: {
  id?: string; eyebrow?: string; title?: ReactNode; description?: ReactNode; actions?: ReactNode; children?: ReactNode; className?: string; labelledBy?: string;
}) {
  return <section id={id} className={`panel ${className}`} aria-labelledby={labelledBy}>
    {(title || actions) && <div className="panel-heading"><div>{eyebrow && <p className="eyebrow">{eyebrow}</p>}{title && <h2 id={labelledBy}>{title}</h2>}{description && <p>{description}</p>}</div>{actions && <div className="panel-actions">{actions}</div>}</div>}
    {children}
  </section>;
}

export function PageHeader({ eyebrow, title, description, actions }: { eyebrow?: string; title: ReactNode; description?: ReactNode; actions?: ReactNode }) {
  return <header className="page-header"><div>{eyebrow && <p className="eyebrow">{eyebrow}</p>}<h1>{title}</h1>{description && <p>{description}</p>}</div>{actions && <div className="panel-actions">{actions}</div>}</header>;
}

export type SectionItem = { id: string; label: string; icon?: IconName; href?: string; count?: number };
export function SectionNav({ items, current, onSelect, label }: { items: SectionItem[]; current: string; onSelect?: (id: string) => void; label: string }) {
  return <nav className="section-nav" aria-label={label}>
    {items.map((item) => {
      const content = <>{item.icon && <Icon name={item.icon} />}{item.label}{item.count !== undefined && item.count > 0 && <span className="count">{item.count}</span>}</>;
      return item.href
        ? <Link key={item.id} href={item.href} aria-current={item.id === current ? "page" : undefined}>{content}</Link>
        : <button key={item.id} type="button" aria-current={item.id === current ? "true" : undefined} onClick={() => onSelect?.(item.id)}>{content}</button>;
    })}
  </nav>;
}

export function Skeleton({ variant = "lines", count = 3, label = "Cargando…" }: { variant?: "lines" | "rows" | "products" | "cards"; count?: number; label?: string }) {
  const items = Array.from({ length: count }, (_, index) => index);
  return <div role="status" aria-label={label}><span className="sr-only">{label}</span>
    {variant === "lines" && <div className="skeleton-list" aria-hidden="true">{items.map((i) => <span key={i} className={`skeleton-line ${i % 2 ? "short" : ""}`} />)}</div>}
    {variant === "rows" && <div aria-hidden="true">{items.map((i) => <div key={i} className="skeleton-row"><span className="skeleton-block" /><div><span className="skeleton-line" /><span className="skeleton-line short" /></div></div>)}</div>}
    {variant === "products" && <div className="catalog-grid" aria-hidden="true">{items.map((i) => <div key={i} className="product-skeleton"><span className="skeleton-block" /><div><span className="skeleton-line short" /><span className="skeleton-line" /><span className="skeleton-line short" /></div></div>)}</div>}
    {variant === "cards" && <div className="directory-grid skeleton-grid" aria-hidden="true">{items.map((i) => <div className="skeleton-card" key={i}><div/><span/><span/><span/></div>)}</div>}
  </div>;
}

export function KpiTile({ icon, label, value, hint }: { icon?: IconName; label: string; value: ReactNode; hint?: ReactNode }) {
  return <div className="kpi"><span>{icon && <Icon name={icon} />}{label}</span><strong>{value}</strong>{hint && <small>{hint}</small>}</div>;
}
