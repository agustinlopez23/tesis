"use client";

import { useEffect, useRef, useState } from "react";
import { catalogApi, catalogError } from "./catalog-api";

type Draft = { name: string; description: string; category: string };
type ApplyDraft = (draft: Draft, photo: File | null) => void;
const photoTypes = ["image/jpeg", "image/png", "image/webp"];
export default function CatalogAssistance({ business, onApply }: { business: string; onApply: ApplyDraft }) {
  const [open, setOpen] = useState(false);
  return <section className="catalog-assistance"><button type="button" className="text-link" aria-expanded={open} onClick={() => setOpen(!open)}>{open ? "Cerrar ayuda de redacción" : "Ayuda para redactar"}</button>
    {open && <DraftAssistant business={business} onApply={(draft, photo) => { onApply(draft, photo); setOpen(false); }} />}</section>;
}
function DraftAssistant({ business, onApply }: { business: string; onApply: ApplyDraft }) {
  const [enabled, setEnabled] = useState<boolean | null>(null), [error, setError] = useState("");
  // The consent text names whichever provider the API is configured with.
  const [provider, setProvider] = useState("el proveedor de IA");
  const [facts, setFacts] = useState(""), [consent, setConsent] = useState(false), [pending, setPending] = useState(false);
  const [draft, setDraft] = useState<Draft | null>(null), [photo, setPhoto] = useState<File | null>(null);
  // The photo stays in the browser; the API only forwards it to the provider and never stores it.
  function selectPhoto(input: HTMLInputElement) {
    const file = input.files?.[0] ?? null;
    if (file && (!photoTypes.includes(file.type) || file.size > 5 * 1024 * 1024)) {
      input.value = ""; setPhoto(null); setError("Elegí una foto JPEG, PNG o WebP de hasta 5 MB."); return;
    }
    setPhoto(file); setError("");
  }
  const current = useRef<AbortController | null>(null);
  useEffect(() => {
    const controller = new AbortController();
    void catalogApi(`/businesses/${business}/catalog-assistance`, "GET", undefined, controller.signal).then(async response => {
      if (!response.ok) throw new Error(await catalogError(response));
      const data = await response.json(); if (controller.signal.aborted) return;
      if (typeof data.provider === "string" && data.provider.trim()) setProvider(data.provider.trim());
      setEnabled(data.enabled === true);
    }).catch(() => { if (!controller.signal.aborted) setEnabled(false); });
    return () => { controller.abort(); current.current?.abort(); };
  }, [business]);
  async function generate() {
    if (!consent || pending || facts.trim().length < 3) return;
    const controller = new AbortController(); current.current = controller;
    setPending(true); setError(""); setDraft(null);
    try {
      let body: unknown = { facts: facts.trim(), consent: true };
      if (photo) { const form = new FormData(); form.append("facts", facts.trim()); form.append("consent", "true"); form.append("file", photo); body = form; }
      const response = await catalogApi(`/businesses/${business}/catalog-assistance`, "POST", body, controller.signal);
      if (!response.ok) throw new Error(response.status === 429 ? "Se alcanzó el límite temporal. Podés seguir manualmente o intentarlo más tarde." : await catalogError(response));
      const result = await response.json(); if (!controller.signal.aborted) setDraft(result);
    } catch (e) {
      // A client timeout (or network failure) must still leave manual entry available.
      if (!controller.signal.aborted) setError(e instanceof Error && e.name !== "TimeoutError" && e.name !== "TypeError" ? e.message : "La asistencia no respondió a tiempo. Podés continuar con la carga manual.");
    }
    finally { if (!controller.signal.aborted) setPending(false); }
  }
  return <div className="catalog-form"><h3>Borrador con asistencia de IA</h3>
    {enabled === null ? <p role="status">Consultando disponibilidad…</p> : !enabled ? <p>La asistencia no está configurada o no está habilitada para este negocio. Podés continuar con la carga manual.</p> : <>
      <p>Subí una foto del producto y escribí sus datos mínimos comprobados. Se enviarán la foto y el texto a {provider} para sugerir nombre, descripción y categoría. No incluyas datos personales ni información de clientes. La foto no se guarda, salvo que después decidas usarla en el producto.</p>
      <label>Foto para el borrador (opcional)<input type="file" accept={photoTypes.join(",")} disabled={pending} onChange={e => selectPhoto(e.target)} /></label>
      <label>Datos mínimos del producto<textarea maxLength={1500} disabled={pending} value={facts} onChange={e => setFacts(e.target.value)} placeholder="Por ejemplo: café en granos, paquete de 250 g, tueste medio." /></label>
      <label className="order-contact-check"><input type="checkbox" checked={consent} disabled={pending} onChange={e => setConsent(e.target.checked)} />Acepto enviar la foto y los datos a {provider} para preparar un borrador</label>
      <button type="button" className="button button-outline" disabled={!consent || pending || facts.trim().length < 3} onClick={() => void generate()}>{pending ? "Preparando borrador…" : "Generar borrador"}</button>
      {error && <p role="alert">{error}</p>}
      {draft && <div><p>Revisá los hechos y corregí la propuesta. Usarla reemplaza los tres textos del formulario; todavía tendrás que guardar el producto.</p>
        <label>Nombre sugerido<input maxLength={160} value={draft.name} onChange={e => setDraft({ ...draft, name: e.target.value })} /></label>
        <label>Descripción sugerida<textarea maxLength={2000} value={draft.description} onChange={e => setDraft({ ...draft, description: e.target.value })} /></label>
        <label>Categoría sugerida<input maxLength={80} value={draft.category} onChange={e => setDraft({ ...draft, category: e.target.value })} /></label>
        <div className="catalog-toolbar"><button type="button" className="button button-outline" disabled={!draft.name.trim() || !draft.description.trim() || !draft.category.trim()} onClick={() => onApply(draft, photo)}>Usar este borrador</button><button type="button" className="text-link" onClick={() => setDraft(null)}>Descartar propuesta</button></div>
      </div>}
    </>}
  </div>;
}
