"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { Badge, Icon, type IconName, KpiTile, Panel, SectionNav } from "./ui";
import BusinessPhotos from "./business-photos";
import BusinessCatalog from "./business-catalog";
import BusinessOrders from "./business-orders";
import BusinessInventory from "./business-inventory";
import BusinessSales from "./business-sales";
import CommerceMetrics from "./commerce-metrics";
import CampaignPanel from "./campaign-panel";
import { BusinessInsights } from "./business-insights";

type Owner = { id: string; email: string; displayName: string; roles?: string[] };
type Category = { id: number; name: string; slug: string; isActive?: boolean };
type Business = {
  id: string; name: string; description: string; address: string; categoryId: number;
  whatsApp: string; latitude: number; longitude: number; status: string;
};

// Disabled categories stay selectable only for businesses that already use them.
const firstActive = (items: Category[]) => String(items.find((item) => item.isActive !== false)?.id ?? "");

const emptyBusiness = {
  id: "", name: "", description: "", address: "", categoryId: "",
  whatsApp: "", latitude: "", longitude: "", status: "draft",
};
const toForm = (item: Business) => ({ ...item, categoryId: String(item.categoryId), latitude: String(item.latitude), longitude: String(item.longitude) });

// Dashboard sections (Shopify App Home / Square pattern): one area at a time, "Resumen" by default.
type Section = "resumen" | "negocio" | "catalogo" | "fotos" | "solicitudes" | "inventario" | "ventas" | "campanas";
const sections: { id: Section; label: string; icon: IconName }[] = [
  { id: "resumen", label: "Resumen", icon: "home" }, { id: "negocio", label: "Negocio", icon: "store" }, { id: "catalogo", label: "Catálogo", icon: "tag" },
  { id: "fotos", label: "Fotos", icon: "image" }, { id: "solicitudes", label: "Solicitudes", icon: "cart" }, { id: "inventario", label: "Inventario", icon: "box" },
  { id: "ventas", label: "Ventas", icon: "chart" }, { id: "campanas", label: "Campañas", icon: "megaphone" },
];

// Photo and product counts are reported by their panels once loaded; until then the step stays pending.
type Counts = { photos?: number; products?: number };
function setupSteps(item: Business, counts: Counts): { id: string; label: string; hint: string; done: boolean; action: string; section: Section }[] {
  return [
    { id: "profile", label: "Completá el perfil", hint: "Nombre, dirección y WhatsApp para que te contacten.", done: Boolean(item.name && item.address && item.whatsApp), action: "Completar perfil", section: "negocio" },
    { id: "location", label: "Cargá la ubicación", hint: "Así aparecés en las búsquedas cercanas.", done: Boolean(item.latitude && item.longitude), action: "Cargar ubicación", section: "negocio" },
    { id: "publish", label: "Publicá el negocio", hint: "Un borrador es privado. Publicado, cualquiera lo encuentra en la guía.", done: item.status === "published", action: "Publicar negocio", section: "negocio" },
    { id: "photos", label: "Subí fotos", hint: "Hasta 5 fotos de tu local y tu propuesta.", done: (counts.photos ?? 0) > 0, action: "Subir fotos", section: "fotos" },
    { id: "catalog", label: "Cargá productos", hint: "Tu catálogo permite recibir solicitudes por WhatsApp.", done: (counts.products ?? 0) > 0, action: "Cargar productos", section: "catalogo" },
  ];
}

export default function OwnerOnboarding() {
  const [mode, setMode] = useState<"register" | "login">("register");
  const [owner, setOwner] = useState<Owner | null>(null);
  const [identity, setIdentity] = useState({ displayName: "", email: "", password: "" });
  const [businesses, setBusinesses] = useState<Business[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [business, setBusiness] = useState(emptyBusiness);
  const [error, setError] = useState("");
  const [pending, setPending] = useState(false);
  const [success, setSuccess] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [locating, setLocating] = useState(false);
  const [locationMessage, setLocationMessage] = useState("");
  const [coordinatesOpen, setCoordinatesOpen] = useState(false);
  const [section, setSection] = useState<Section>("resumen");
  const [counts, setCounts] = useState<Record<string, Counts>>({});
  const reportCount = useCallback((id: string, key: keyof Counts, value: number) => {
    setCounts((current) => current[id]?.[key] === value ? current : { ...current, [id]: { ...current[id], [key]: value } });
  }, []);

  useEffect(() => {
    void api("/auth/session").then(async (response) => {
      if (response.ok) await enterPanel(await response.json());
    }).catch(() => undefined);
  }, []);

  async function enterPanel(currentOwner: Owner) {
    setOwner(currentOwner);
    const [businessResponse, categoryResponse] = await Promise.all([
      api("/businesses"), api("/business-categories"),
    ]);
    if (businessResponse.status === 401) {
      setOwner(null);
      setError("Tu sesión venció. Volvé a ingresar.");
      return;
    }
    if (!businessResponse.ok || !categoryResponse.ok) {
      setError("No pudimos cargar tus negocios.");
      return;
    }
    const loaded: Business[] = await businessResponse.json();
    setBusinesses(loaded);
    const loadedCategories: Category[] = await categoryResponse.json();
    setCategories(loadedCategories);
    // Owners with businesses land on the summary of the first one; new owners land on the empty form.
    if (loaded.length) { setBusiness(toForm(loaded[0])); setSection("resumen"); }
    else setBusiness((current) => ({ ...current, categoryId: current.categoryId || firstActive(loadedCategories) }));
  }

  function selectBusiness(item: Business) {
    setBusiness(toForm(item));
    setSuccess(""); setError(""); setLocationMessage("");
  }

  function startNewBusiness() {
    setBusiness({ ...emptyBusiness, categoryId: firstActive(categories) });
    setError(""); setLocationMessage("");
    setSection("negocio");
  }

  async function submitIdentity(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPending(true);
    setError("");
    try {
      if (mode === "register") {
        const registration = await api("/auth/register", "POST", identity);
        if (!registration.ok) return setError(await responseMessage(registration));
        setMode("login");
      }
      const login = await api("/auth/login", "POST", { email: identity.email, password: identity.password });
      if (login.status === 401) return setError("Revisá el correo y la contraseña e intentá nuevamente.");
      if (!login.ok) return setError(await responseMessage(login));
      setIdentity((current) => ({ ...current, password: "" }));
      await enterPanel(await login.json());
    } catch {
      setError("No pudimos conectar con el servidor.");
    } finally {
      setPending(false);
    }
  }

  async function saveBusiness(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPending(true);
    setError("");
    setSuccess("");
    const payload = {
      ...business,
      categoryId: Number(business.categoryId),
      latitude: Number(business.latitude),
      longitude: Number(business.longitude),
    };
    try {
      const response = await api(business.id ? `/businesses/${business.id}` : "/businesses", business.id ? "PUT" : "POST", payload);
      if (response.status === 401) {
        setOwner(null);
        return setError("Tu sesión venció. Volvé a ingresar.");
      }
      if (!response.ok) return setError(await responseMessage(response));
      const saved: Business = await response.json();
      setBusinesses((current) => business.id
        ? current.map((item) => item.id === saved.id ? saved : item)
        : [...current, saved]);
      setBusiness(toForm(saved));
      if (!business.id) setSection("negocio");
      setSuccess(saved.status === "published" ? "Negocio publicado. Ya se puede encontrar en la guía." : "Borrador guardado. Solo vos podés verlo hasta que lo publiques.");
    } catch {
      setError("No pudimos guardar el negocio.");
    } finally {
      setPending(false);
    }
  }

  async function logout() {
    setPending(true);
    setError("");
    try {
      const response = await api("/auth/logout", "POST");
      if (!response.ok) throw new Error("Logout failed");
      setOwner(null);
      setBusinesses([]);
      setBusiness(emptyBusiness);
      setSection("resumen");
      setCounts({});
      setSuccess("");
      setIdentity({ displayName: "", email: "", password: "" });
      setMode("register");
    } catch {
      setError("No pudimos cerrar la sesión. Intentá nuevamente.");
    } finally { setPending(false); }
  }

  function locateBusiness() {
    if (!navigator.geolocation) {
      setLocationMessage("Tu navegador no permite obtener la ubicación. Ingresá las coordenadas manualmente.");
      setCoordinatesOpen(true);
      return;
    }
    setLocating(true);
    setLocationMessage("");
    navigator.geolocation.getCurrentPosition((position) => {
      setBusiness((current) => ({ ...current, latitude: position.coords.latitude.toFixed(6), longitude: position.coords.longitude.toFixed(6) }));
      setLocationMessage("Ubicación cargada. Confirmá que corresponde a tu local.");
      setCoordinatesOpen(true);
      setLocating(false);
    }, () => {
      setLocationMessage("No pudimos obtener tu ubicación. Podés ingresar las coordenadas manualmente.");
      setCoordinatesOpen(true);
      setLocating(false);
    }, { timeout: 10000 });
  }

  if (!owner) return (
    <div className="access-layout">
      <section className="access-story"><p className="eyebrow">Para quienes hacen el barrio</p><h1>Tu próxima conexión<br/>empieza <em>acá.</em></h1>
        <p>Tu negocio tiene algo especial.<br/>Hagamos que más personas lo descubran.</p>
        <ol className="access-steps"><li><span>01</span><div><strong>Creá tu cuenta</strong><p>Un espacio para administrar tus negocios.</p></div></li><li><span>02</span><div><strong>Contá qué hacés</strong><p>Sumá tu propuesta, dirección y WhatsApp.</p></div></li><li><span>03</span><div><strong>Abrí la puerta a tu ciudad</strong><p>Publicá tu perfil en la guía local.</p></div></li></ol>
        <Link className="text-link" href="/explorar">Primero quiero explorar <Icon name="arrow" /></Link>
      </section>
      <section className="auth-card" aria-labelledby="access-title">
        <span className="auth-icon"><Icon name="store" /></span>
        <h2 id="access-title">{mode === "register" ? "Dale lugar a tu negocio." : "Qué bueno verte de nuevo."}</h2>
        <p className="muted">{mode === "register" ? "Creá tu cuenta para empezar." : "Ingresá para administrar tus negocios."}</p>
        <form onSubmit={submitIdentity}>
          {mode === "register" && <label>Nombre<input required autoComplete="name" placeholder="Cómo te llamás" maxLength={120} value={identity.displayName} onChange={(event) => setIdentity({ ...identity, displayName: event.target.value })} /></label>}
          <label>Correo<input required type="email" autoComplete="email" placeholder="vos@ejemplo.com" maxLength={254} value={identity.email} onChange={(event) => setIdentity({ ...identity, email: event.target.value })} /></label>
          <div><label htmlFor="owner-password">Contraseña</label><div className="password-field"><input id="owner-password" required type={showPassword ? "text" : "password"} autoComplete={mode === "register" ? "new-password" : "current-password"} minLength={12} maxLength={128} aria-describedby="password-help" value={identity.password} onChange={(event) => setIdentity({ ...identity, password: event.target.value })} /><button type="button" className="icon-button" aria-label={showPassword ? "Ocultar contraseña" : "Mostrar contraseña"} aria-pressed={showPassword} onClick={() => setShowPassword(!showPassword)}><Icon name="eye" /></button></div><p className="field-help" id="password-help">Usá al menos 12 caracteres.</p></div>
          {error && <p role="alert" className="error">{error}</p>}
          <button className="button button-primary full-width" disabled={pending} type="submit">{pending ? "Enviando…" : mode === "register" ? "Crear cuenta" : "Ingresar"}<Icon name="arrow" /></button>
        </form>
        <div className="auth-switch"><span>{mode === "register" ? "¿Ya sos parte?" : "¿Todavía no tenés cuenta?"}</span><button className="text-link" type="button" disabled={pending} onClick={() => { setMode(mode === "register" ? "login" : "register"); setError(""); }}>{mode === "register" ? "Ya tengo cuenta" : "Quiero registrarme"}</button></div>
        <p className="auth-note"><Icon name="check" /> Elegís cuándo publicar tu negocio.</p>
      </section>
    </div>
  );

  const published = businesses.filter((item) => item.status === "published").length;
  const saved = businesses.find((item) => item.id === business.id);
  // Without a selected business only the form makes sense; choosing another section selects the first business.
  const activeSection: Section = saved ? section : "negocio";
  function goToSection(id: Section) {
    if (!saved && id !== "negocio" && businesses.length) selectBusiness(businesses[0]);
    setSection(id);
  }
  const steps = saved ? setupSteps(saved, counts[saved.id] ?? {}) : [];
  const completed = steps.filter((step) => step.done).length;
  const isAdmin = owner.roles?.includes("administrator");

  const businessForm = <form id="business-form" className="business-form" onSubmit={saveBusiness}>
          <div className="form-heading"><div><p className="eyebrow">Perfil del comercio</p><div className="form-title"><h2>{business.id ? "Editar negocio" : "Nuevo negocio"}</h2>{saved && <Badge tone={saved.status === "published" ? "success" : "warning"}>{saved.status === "published" ? "Publicado" : "Borrador"}</Badge>}</div></div><span className="form-note">* Datos obligatorios</span></div>
          <fieldset disabled={pending}><legend><span>01</span> Contanos sobre tu negocio</legend>
            <label htmlFor="business-name">Nombre del negocio</label><input id="business-name" required aria-required="true" placeholder="El nombre que conoce tu barrio" maxLength={160} value={business.name} onChange={(e) => setBusiness({ ...business, name: e.target.value })} />
            <label htmlFor="business-category">Rubro</label><select id="business-category" required value={business.categoryId} onChange={(e) => setBusiness({ ...business, categoryId: e.target.value })}>{categories.filter((item) => item.isActive !== false || String(item.id) === business.categoryId).map((item) => <option key={item.id} value={item.id}>{item.isActive === false ? `${item.name} (deshabilitado)` : item.name}</option>)}</select>
            <label htmlFor="business-description">Descripción <span className="optional">Opcional</span></label><textarea id="business-description" maxLength={2000} placeholder="¿Qué ofrecés? ¿Qué hace especial a tu negocio?" value={business.description} onChange={(e) => setBusiness({ ...business, description: e.target.value })} /><p className="field-help">Una descripción clara ayuda a que te elijan.</p>
          </fieldset>
          <fieldset disabled={pending}><legend><span>02</span> Dónde encontrarte</legend>
            <label htmlFor="business-address">Dirección</label><input id="business-address" maxLength={250} placeholder="Calle, número y barrio" value={business.address} onChange={(e) => setBusiness({ ...business, address: e.target.value })} />
            <label htmlFor="business-whatsapp">WhatsApp</label><input id="business-whatsapp" required type="tel" autoComplete="tel" pattern="\+[0-9]{8,15}" title="Incluí + y el código de país, sin espacios. Ejemplo: +5493515551234" placeholder="+5493515551234" aria-describedby="whatsapp-help" value={business.whatsApp} onChange={(e) => setBusiness({ ...business, whatsApp: e.target.value })} /><p className="field-help" id="whatsapp-help">Con código de país y sin espacios. Acá vas a recibir las consultas.</p>
            <div className="location-assistance"><div><strong>Ubicación de tu local</strong><p>Si estás en tu negocio, podés usar tu ubicación actual.</p></div><button type="button" className="button button-outline" onClick={locateBusiness} disabled={locating}><Icon name="pin" />{locating ? "Ubicando…" : "Usar mi ubicación"}</button></div>
            {locationMessage && <p role="status" className="field-help">{locationMessage}</p>}
            <details open={coordinatesOpen} onToggle={(event) => setCoordinatesOpen(event.currentTarget.open)} className="coordinate-details"><summary>Ingresar coordenadas manualmente {business.latitude && business.longitude && <Icon name="check" />}</summary>
              <p className="field-help">Podés copiarlas desde un mapa. Usá las coordenadas del local.</p>
              <div className="coordinates" onInvalidCapture={(event) => { const details = event.currentTarget.closest("details"); if (details) details.open = true; setCoordinatesOpen(true); }}><label>Latitud<input required type="number" min="-90" max="90" step="any" placeholder="-31.4167" value={business.latitude} onChange={(e) => setBusiness({ ...business, latitude: e.target.value })} /></label><label>Longitud<input required type="number" min="-180" max="180" step="any" placeholder="-64.1833" value={business.longitude} onChange={(e) => setBusiness({ ...business, longitude: e.target.value })} /></label></div>
            </details>
          </fieldset>
          <fieldset disabled={pending} className="publication-section"><legend><span>03</span> Elegí quién puede verlo</legend><label htmlFor="business-status">Estado</label><select id="business-status" value={business.status} onChange={(e) => setBusiness({ ...business, status: e.target.value })}><option value="draft">Borrador</option><option value="published">Publicado</option></select><p className="field-help">{business.status === "published" ? "Al guardar, tu negocio aparecerá en la guía pública con su WhatsApp." : "Solo vos vas a poder verlo. Podés publicarlo cuando esté listo."}</p></fieldset>
          <div className="form-actions">{business.id && <button className="button button-outline" disabled={pending} type="button" onClick={startNewBusiness}>Cancelar edición</button>}<button className="button button-primary" disabled={pending || locating} type="submit">{pending ? "Guardando…" : "Guardar negocio"}<Icon name="check" /></button></div>
        </form>;

  return (
    <section className="dashboard" aria-labelledby="dashboard-title">
      <header className="panel-header"><div><p className="eyebrow">Tu espacio en Cerca</p><h1 id="dashboard-title">Hola, {owner.displayName}<span className="brand-dot">.</span></h1><p className="muted">Un buen negocio merece que lo encuentren.</p></div>
        <button className="button button-outline" type="button" disabled={pending} onClick={logout}><Icon name="logout" />Cerrar sesión</button></header>
      {/* Administrators reach their tools from any section, with or without businesses of their own. */}
      {isAdmin && <nav className="admin-nav admin-shortcuts" aria-label="Accesos de administración"><span className="eyebrow"><Icon name="shield" />Administración</span><Link className="text-link" href="/administracion/planes">Administrar planes</Link><Link className="text-link" href="/administracion/moderacion">Moderar contenido</Link><Link className="text-link" href="/administracion/rubros">Gestionar rubros</Link><Link className="text-link" href="/administracion/auditoria">Ver auditoría</Link></nav>}
      {error && <p role="alert" className="error">{error}</p>}
      {success && <div role="status" className="success-message"><Icon name="check" />{success}<button type="button" className="icon-button" aria-label="Cerrar confirmación" onClick={() => setSuccess("")}><Icon name="close" /></button></div>}
      <div className="owner-layout">
        <aside className="business-switcher" aria-label="Tus negocios"><div className="sidebar-heading"><h2>Tus negocios</h2><span>{businesses.length}</span></div>
          {businesses.length === 0 ? <div className="sidebar-empty"><Icon name="store" /><p>Todavía no cargaste negocios.</p><span>Completá el perfil de tu primer negocio para empezar.</span></div> : <ul className="business-list">{businesses.map((item) => (
            <li key={item.id}><article className={"business-item " + (business.id === item.id ? "editing" : "")}>
              <Badge tone={item.status === "published" ? "success" : "warning"}>{item.status === "published" ? "Publicado" : "Borrador"}</Badge>
              <button className="business-select" type="button" aria-pressed={business.id === item.id} disabled={pending} onClick={() => selectBusiness(item)}>{item.name}</button>
              <span className="muted">{item.address || "Sin dirección"}</span>
              <button className="text-link" type="button" disabled={pending} onClick={() => {
                selectBusiness(item); setSection("negocio");
                document.getElementById("business-form")?.scrollIntoView({ block: "start" });
              }}>Editar <Icon name="arrow" /></button>
            </article></li>
          ))}</ul>}
          {business.id && <button className="button button-outline full-width" disabled={pending} type="button" onClick={startNewBusiness}><Icon name="plus" />Nuevo negocio</button>}
          <div className="sidebar-tip"><Icon name="eye" /><strong>Vos elegís cuándo aparecer.</strong><p>Un borrador es privado. Cuando lo publiques, cualquier persona podrá encontrarlo en la guía.</p></div>
        </aside>
        <div className="owner-main">
          {businesses.length > 0 && <SectionNav label="Secciones de tu negocio" items={sections} current={activeSection} onSelect={(id) => goToSection(id as Section)} />}
          {activeSection === "resumen" && saved && <div className="owner-section">
            <Panel className="setup-guide" eyebrow="Primeros pasos" title="Guía de inicio" description={`${completed} de ${steps.length} pasos completados`} labelledBy="setup-guide-title">
              <div className="setup-bar" role="progressbar" aria-label="Progreso de la guía de inicio" aria-valuemin={0} aria-valuemax={steps.length} aria-valuenow={completed}><span style={{ width: `${completed / steps.length * 100}%` }} /></div>
              <ol className="setup-steps">{steps.map((step) => <li key={step.id} className={step.done ? "done" : ""}>
                <span className="step-check" aria-hidden="true">{step.done && <Icon name="check" />}</span>
                <div><strong>{step.label}</strong><p>{step.hint}</p></div>
                {!step.done && <button type="button" className="button button-small button-outline" onClick={() => setSection(step.section)}>{step.action}<Icon name="arrow" /></button>}
              </li>)}</ol>
            </Panel>
            <div className="kpi-grid"><KpiTile icon="store" label="Negocios" value={businesses.length} /><KpiTile icon="eye" label="Publicados" value={published} hint="Visibles en la guía" /><KpiTile icon="clock" label="Borradores" value={businesses.length - published} hint="Solo vos los ves" /></div>
            {saved.status === "published"
              ? <div className="callout"><Icon name="eye" /><div><strong>{saved.name} ya está en la guía.</strong><p>Mirá tu perfil tal como lo ven tus clientes.</p></div><Link className="button button-small button-outline" href={"/negocios/" + saved.id}>Ver perfil público<Icon name="arrow" /></Link></div>
              : <div className="callout"><Icon name="map" /><div><strong>Mirá cómo se ven los negocios publicados.</strong><p>Tu borrador sigue siendo privado hasta que lo publiques.</p></div><Link className="text-link" href="/explorar">Ver guía pública <Icon name="arrow" /></Link></div>}
            <BusinessInsights businesses={businesses} />
          </div>}
          {activeSection === "negocio" && <div className="owner-section">
            {businesses.length === 0 && <div className="welcome-intro"><span className="empty-icon"><Icon name="store" /></span><div><h2>Empecemos por tu primer negocio.</h2><p>Contanos qué hacés y dónde estás. Podés guardarlo como borrador y publicarlo cuando esté listo.</p></div></div>}
            {businessForm}
            {saved?.status === "published" && <p className="form-footnote"><Link className="text-link" href={"/negocios/" + saved.id}>Ver perfil público <Icon name="arrow" /></Link></p>}
            {!business.id && <p className="photo-save-hint"><Icon name="eye" />Guardá el negocio para agregar sus fotos. Podés empezar con un borrador.</p>}
          </div>}
          {saved && activeSection === "catalogo" && <BusinessCatalog key={"catalog-" + saved.id} id={saved.id} defaultOpen onCountChange={(count) => reportCount(saved.id, "products", count)} />}
          {saved && activeSection === "fotos" && <BusinessPhotos key={"photos-" + saved.id} id={saved.id} defaultOpen onCountChange={(count) => reportCount(saved.id, "photos", count)} />}
          {saved && activeSection === "solicitudes" && <BusinessOrders key={"orders-" + saved.id} id={saved.id} defaultOpen />}
          {saved && activeSection === "inventario" && <BusinessInventory key={"inventory-" + saved.id} business={saved.id} defaultOpen />}
          {saved && activeSection === "ventas" && <div className="owner-section"><BusinessSales key={"sales-" + saved.id} business={saved.id} defaultOpen /><CommerceMetrics key={"commerce-" + saved.id} business={saved.id} defaultOpen /></div>}
          {saved && activeSection === "campanas" && <CampaignPanel key={"campaigns-" + saved.id} business={saved.id} defaultOpen />}
        </div>
      </div>
    </section>
  );
}

async function api(path: string, method = "GET", body?: unknown) {
  return fetch(`/api/marketplace${path}`, {
    method,
    headers: { "Content-Type": "application/json", "X-Requested-With": "marketplace" },
    body: body === undefined ? undefined : JSON.stringify(body),
    cache: "no-store",
  });
}

async function responseMessage(response: Response) {
  try { return (await response.json()).message ?? "No pudimos completar la operación."; }
  catch { return "No pudimos completar la operación."; }
}
