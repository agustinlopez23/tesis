"use client";

import { FormEvent, useEffect, useState } from "react";

type Owner = { id: string; email: string; displayName: string };
type Category = { id: number; name: string; slug: string };
type Business = {
  id: string; name: string; description: string; address: string; categoryId: number;
  whatsapp: string; latitude: number; longitude: number; status: string;
};

const emptyBusiness = {
  id: "", name: "", description: "", address: "", categoryId: "",
  whatsapp: "", latitude: "", longitude: "", status: "draft",
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
      setBusiness({ ...emptyBusiness, categoryId: String(categories[0]?.id ?? "") });
    } catch {
      setError("No pudimos guardar el negocio.");
    } finally {
      setPending(false);
    }
  }

  async function logout() {
    await api("/auth/logout", "POST").catch(() => undefined);
    setOwner(null);
    setBusinesses([]);
    setMode("register");
  }

  if (!owner) return (
    <section className="card auth-card" aria-labelledby="access-title">
      <p className="eyebrow">Panel para comerciantes</p>
      <h2 id="access-title">{mode === "register" ? "Creá tu cuenta" : "Ingresá a tu cuenta"}</h2>
      <form onSubmit={submitIdentity}>
        {mode === "register" && <label>Nombre<input required maxLength={120} value={identity.displayName}
          onChange={(event) => setIdentity({ ...identity, displayName: event.target.value })} /></label>}
        <label>Correo<input required type="email" maxLength={254} value={identity.email}
          onChange={(event) => setIdentity({ ...identity, email: event.target.value })} /></label>
        <label>Contraseña<input required type="password" minLength={12} maxLength={128} value={identity.password}
          onChange={(event) => setIdentity({ ...identity, password: event.target.value })} /></label>
        {error && <p role="alert" className="error">{error}</p>}
        <button disabled={pending} type="submit">{pending ? "Enviando…" : mode === "register" ? "Crear cuenta" : "Ingresar"}</button>
      </form>
      <button className="link-button" type="button" onClick={() => { setMode(mode === "register" ? "login" : "register"); setError(""); }}>
        {mode === "register" ? "Ya tengo cuenta" : "Quiero registrarme"}
      </button>
    </section>
  );

  return (
    <section className="dashboard" aria-labelledby="dashboard-title">
      <header className="panel-header"><div><p className="eyebrow">Panel privado</p><h2 id="dashboard-title">Hola, {owner.displayName}</h2></div>
        <button className="secondary" type="button" onClick={logout}>Cerrar sesión</button></header>
      {error && <p role="alert" className="error">{error}</p>}
      <div className="panel-grid">
        <div className="card"><h3>Tus negocios</h3>
          {businesses.length === 0 ? <p>Todavía no cargaste negocios.</p> : businesses.map((item) => (
            <article className="business-item" key={item.id}><strong>{item.name}</strong><span>{item.address}</span>
              <button className="link-button" type="button" onClick={() => setBusiness({
                ...item, categoryId: String(item.categoryId), latitude: String(item.latitude), longitude: String(item.longitude),
              })}>Editar</button></article>
          ))}
        </div>
        <form className="card" onSubmit={saveBusiness}><h3>{business.id ? "Editar negocio" : "Nuevo negocio"}</h3>
          <label>Nombre del negocio<input required maxLength={160} value={business.name} onChange={(e) => setBusiness({ ...business, name: e.target.value })} /></label>
          <label>Descripción<textarea maxLength={2000} value={business.description} onChange={(e) => setBusiness({ ...business, description: e.target.value })} /></label>
          <label>Dirección<input maxLength={250} value={business.address} onChange={(e) => setBusiness({ ...business, address: e.target.value })} /></label>
          <label>Rubro<select required value={business.categoryId} onChange={(e) => setBusiness({ ...business, categoryId: e.target.value })}>{categories.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
          <label>WhatsApp<input required placeholder="+5493515551234" value={business.whatsapp} onChange={(e) => setBusiness({ ...business, whatsapp: e.target.value })} /></label>
          <div className="coordinates"><label>Latitud<input required type="number" step="any" value={business.latitude} onChange={(e) => setBusiness({ ...business, latitude: e.target.value })} /></label>
            <label>Longitud<input required type="number" step="any" value={business.longitude} onChange={(e) => setBusiness({ ...business, longitude: e.target.value })} /></label></div>
          <label>Estado<select value={business.status} onChange={(e) => setBusiness({ ...business, status: e.target.value })}><option value="draft">Borrador</option><option value="published">Publicado</option></select></label>
          <button disabled={pending} type="submit">{pending ? "Guardando…" : "Guardar negocio"}</button>
        </form>
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
