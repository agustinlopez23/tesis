"use client";

import { FormEvent, useEffect, useState } from "react";
import Link from "next/link";
import { Icon } from "./ui";
import BusinessPhotos from "./business-photos";

type Owner = { id: string; email: string; displayName: string };
type Category = { id: number; name: string; slug: string };
type Business = {
  id: string; name: string; description: string; address: string; categoryId: number;
  whatsApp: string; latitude: number; longitude: number; status: string;
};

const emptyBusiness = {
  id: "", name: "", description: "", address: "", categoryId: "",
  whatsApp: "", latitude: "", longitude: "", status: "draft",
};

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
    setBusinesses(await businessResponse.json());
    const loadedCategories: Category[] = await categoryResponse.json();
    setCategories(loadedCategories);
    setBusiness((current) => ({ ...current, categoryId: current.categoryId || String(loadedCategories[0]?.id ?? "") }));
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
      setBusiness({ ...saved, categoryId: String(saved.categoryId), latitude: String(saved.latitude), longitude: String(saved.longitude) });
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
  return (
    <section className="dashboard" aria-labelledby="dashboard-title">
      <header className="panel-header"><div><p className="eyebrow">Tu espacio en Cerca</p><h1 id="dashboard-title">Hola, {owner.displayName}<span className="brand-dot">.</span></h1><p className="muted">Un buen negocio merece que lo encuentren.</p></div>
        <button className="button button-outline" type="button" disabled={pending} onClick={logout}><Icon name="logout" />Cerrar sesión</button></header>
      <div className="dashboard-summary"><div><Icon name="store" /><strong>{businesses.length}</strong><span>{businesses.length === 1 ? "negocio" : "negocios"}</span></div><div><span className="status-dot"/><strong>{published}</strong><span>publicados</span></div><div><span className="status-dot draft"/><strong>{businesses.length - published}</strong><span>en borrador</span></div><Link className="text-link" href="/explorar">Ver guía pública <Icon name="arrow" /></Link></div>
      {error && <p role="alert" className="error">{error}</p>}
      {success && <div role="status" className="success-message"><Icon name="check" />{success}<button type="button" className="icon-button" aria-label="Cerrar confirmación" onClick={() => setSuccess("")}><Icon name="close" /></button></div>}
      <div className="panel-grid">
        <aside className="business-sidebar"><div className="sidebar-heading"><h2>Tus negocios</h2><span>{businesses.length}</span></div>
          {businesses.length === 0 ? <div className="sidebar-empty"><Icon name="store" /><p>Todavía no cargaste negocios.</p><span>Completá el perfil de tu primer negocio para empezar.</span></div> : businesses.map((item) => (
            <article className={"business-item " + (business.id === item.id ? "editing" : "")} key={item.id}><span className={"publication-badge " + (item.status === "published" ? "published" : "")}>{item.status === "published" ? "Publicado" : "Borrador"}</span><strong>{item.name}</strong><span className="muted">{item.address || "Sin dirección"}</span>
              <button className="text-link" type="button" disabled={pending} onClick={() => {
                setBusiness({ ...item, categoryId: String(item.categoryId), latitude: String(item.latitude), longitude: String(item.longitude) });
                setSuccess(""); setError(""); setLocationMessage("");
                document.getElementById("business-form")?.scrollIntoView({ block: "start" });
              }}>Editar <Icon name="arrow" /></button></article>
          ))}
          {business.id && <button className="button button-outline full-width" disabled={pending} type="button" onClick={() => { setBusiness({ ...emptyBusiness, categoryId: String(categories[0]?.id ?? "") }); setError(""); setLocationMessage(""); }}><Icon name="plus" />Nuevo negocio</button>}
          <div className="sidebar-tip"><Icon name="eye" /><strong>Vos elegís cuándo aparecer.</strong><p>Un borrador es privado. Cuando lo publiques, cualquier persona podrá encontrarlo en la guía.</p></div>
        </aside>
        <div className="business-editor"><form id="business-form" className="business-form" onSubmit={saveBusiness}>
          <div className="form-heading"><div><p className="eyebrow">Perfil del comercio</p><h2>{business.id ? "Editar negocio" : "Nuevo negocio"}</h2></div><span className="form-note">* Datos obligatorios</span></div>
          <fieldset disabled={pending}><legend><span>01</span> Contanos sobre tu negocio</legend>
            <label htmlFor="business-name">Nombre del negocio</label><input id="business-name" required aria-required="true" placeholder="El nombre que conoce tu barrio" maxLength={160} value={business.name} onChange={(e) => setBusiness({ ...business, name: e.target.value })} />
            <label htmlFor="business-category">Rubro</label><select id="business-category" required value={business.categoryId} onChange={(e) => setBusiness({ ...business, categoryId: e.target.value })}>{categories.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select>
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
          <div className="form-actions">{business.id && <button className="button button-outline" disabled={pending} type="button" onClick={() => { setBusiness({ ...emptyBusiness, categoryId: String(categories[0]?.id ?? "") }); setError(""); setLocationMessage(""); }}>Cancelar edición</button>}<button className="button button-primary" disabled={pending || locating} type="submit">{pending ? "Guardando…" : "Guardar negocio"}<Icon name="check" /></button></div>
        </form>
        {business.id ? <><BusinessPhotos key={business.id} id={business.id} />
          {businesses.find((item) => item.id === business.id)?.status === "published" && <Link className="text-link" href={"/negocios/" + business.id}>Ver perfil público <Icon name="arrow" /></Link>}</> :
          <p className="photo-save-hint"><Icon name="eye" />Guardá el negocio para agregar sus fotos. Podés empezar con un borrador.</p>}
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
