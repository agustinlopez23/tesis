"use client";

import { useEffect, useRef, useState } from "react";
import { catalogApi, catalogError } from "./catalog-api";

type Draft = { name: string; description: string; category: string };
export default function CatalogAssistance({ business, onApply }: { business: string; onApply: (draft: Draft) => void }) {
  const [open, setOpen] = useState(false);
  return <section className="catalog-assistance"><button type="button" className="text-link" aria-expanded={open} onClick={() => setOpen(!open)}>{open ? "Cerrar ayuda de redacción" : "Ayuda para redactar"}</button>
    {open && <DraftAssistant business={business} onApply={draft => { onApply(draft); setOpen(false); }} />}</section>;
}
function DraftAssistant({ business, onApply }: { business: string; onApply: (draft: Draft) => void }) {
  const [enabled, setEnabled] = useState<boolean | null>(null), [error, setError] = useState("");
  const [facts, setFacts] = useState(""), [consent, setConsent] = useState(false), [pending, setPending] = useState(false);
  const [draft, setDraft] = useState<Draft | null>(null);
  const current = useRef<AbortController | null>(null);
  useEffect(() => {
    const controller = new AbortController();
    void catalogApi(`/businesses/${business}/catalog-assistance`, "GET", undefined, controller.signal).then(async response => {
      if (!response.ok) throw new Error(await catalogError(response));
      const data = await response.json(); if (!controller.signal.aborted) setEnabled(data.enabled === true);
    }).catch(() => { if (!controller.signal.aborted) setEnabled(false); });
    return () => { controller.abort(); current.current?.abort(); };
  }, [business]);
  async function generate() {
    if (!consent || pending || facts.trim().length < 3) return;
    const controller = new AbortController(); current.current = controller;
    setPending(true); setError(""); setDraft(null);
    try {
      const response = await catalogApi(`/businesses/${business}/catalog-assistance`, "POST", { facts: facts.trim(), consent: true }, controller.signal);
      if (!response.ok) throw new Error(response.status === 429 ? "Se alcanzó el límite temporal. Podés seguir manualmente o intentarlo más tarde." : await catalogError(response));
      const result = await response.json(); if (!controller.signal.aborted) setDraft(result);
    } catch (e) { if (!controller.signal.aborted) setError(e instanceof Error ? e.message : "La asistencia no respondió. Podés continuar con la carga manual."); }
    finally { if (!controller.signal.aborted) setPending(false); }
  }
  return <div className="catalog-form"><h3>Borrador con asistencia de IA</h3>
    {enabled === null ? <p role="status">Consultando disponibilidad…</p> : !enabled ? <p>La asistencia no está configurada o no está habilitada para este negocio. Podés continuar con la carga manual.</p> : <>
      <p>Escribí datos comprobados del producto. Se enviará solo este texto a OpenAI para sugerir una redacción. No incluyas datos personales ni información de clientes.</p>
      <label>Hechos del producto<textarea maxLength={1500} disabled={pending} value={facts} onChange={e => setFacts(e.target.value)} placeholder="Por ejemplo: café en granos, paquete de 250 g, tueste medio." /></label>
      <label className="order-contact-check"><input type="checkbox" checked={consent} disabled={pending} onChange={e => setConsent(e.target.checked)} />Acepto enviar este texto a OpenAI para preparar un borrador</label>
      <button type="button" className="button button-outline" disabled={!consent || pending || facts.trim().length < 3} onClick={() => void generate()}>{pending ? "Preparando borrador…" : "Generar borrador"}</button>
      {error && <p role="alert">{error}</p>}
      {draft && <div><p>Revisá los hechos y corregí la propuesta. Usarla reemplaza los tres textos del formulario; todavía tendrás que guardar el producto.</p>
        <label>Nombre sugerido<input maxLength={160} value={draft.name} onChange={e => setDraft({ ...draft, name: e.target.value })} /></label>
        <label>Descripción sugerida<textarea maxLength={2000} value={draft.description} onChange={e => setDraft({ ...draft, description: e.target.value })} /></label>
        <label>Categoría sugerida<input maxLength={80} value={draft.category} onChange={e => setDraft({ ...draft, category: e.target.value })} /></label>
        <div className="catalog-toolbar"><button type="button" className="button button-outline" disabled={!draft.name.trim() || !draft.description.trim() || !draft.category.trim()} onClick={() => onApply(draft)}>Usar este borrador</button><button type="button" className="text-link" onClick={() => setDraft(null)}>Descartar propuesta</button></div>
      </div>}
    </>}
  </div>;
}
