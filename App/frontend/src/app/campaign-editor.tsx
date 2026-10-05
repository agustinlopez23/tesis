"use client";

import { useEffect, useState } from "react";
import { Campaign } from "./campaign-api";
import { catalogApi, catalogError, Product } from "./catalog-api";
import { zones } from "./explorar/discovery-location";
import { useSaleOperation } from "./use-sale-operation";

function localDate(value: string | number) {
  const date = new Date(value);
  return new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
}
export default function CampaignEditor({ business, campaign, onSaved }: { business: string; campaign?: Campaign; onSaved: () => void }) {
  const [kind, setKind] = useState(campaign?.photoSlot ? "promotion" : "product");
  const [product, setProduct] = useState(campaign?.productId ?? ""), [slot, setSlot] = useState(campaign?.photoSlot ?? 1);
  const [title, setTitle] = useState(campaign?.title ?? ""), [description, setDescription] = useState(campaign?.description ?? ""), [conditions, setConditions] = useState(campaign?.conditions ?? "");
  const [zone, setZone] = useState(campaign?.zoneId ?? "centro"), [status, setStatus] = useState("pending_review");
  const [starts, setStarts] = useState(() => localDate(campaign?.startsAt ?? Date.now() + 86400000));
  const [ends, setEnds] = useState(() => localDate(campaign?.endsAt ?? Date.now() + 8 * 86400000));
  const [products, setProducts] = useState<Product[]>([]), [error, setError] = useState("");
  const operation = useSaleOperation(`cerca-campaign-save:${business}:${campaign?.id ?? "new"}`, onSaved);
  useEffect(() => {
    const controller = new AbortController();
    void catalogApi(`/businesses/${business}/products`, "GET", undefined, controller.signal).then(async response => {
      if (!response.ok) throw new Error(await catalogError(response));
      const data = await response.json(); if (!controller.signal.aborted) setProducts(data);
    }).catch(e => { if (!controller.signal.aborted) setError(e.message); });
    return () => controller.abort();
  }, [business]);
  return <form className="catalog-form" onSubmit={e => { e.preventDefault(); void operation.submit(() => ({
    path: `/businesses/${business}/campaigns/${campaign?.id ?? crypto.randomUUID()}`,
    body: { version: campaign?.version ?? 0, productId: kind === "product" ? product : null, photoSlot: kind === "promotion" ? slot : null,
      title, description, conditions, zoneId: zone, startsAt: new Date(starts).toISOString(), endsAt: new Date(ends).toISOString(), status },
  })); }}>
    <h3>{campaign ? "Editar campaña" : "Nueva campaña"}</h3>
    {error && <p role="alert">{error}</p>}{operation.error && <p role="alert">{operation.error}</p>}
    <fieldset disabled={operation.locked}>
      <label>Tipo de campaña<select value={kind} onChange={e => setKind(e.target.value)}><option value="product">Producto del catálogo · requiere plan vigente</option><option value="promotion">Promoción del negocio · admite plan gratuito</option></select></label>
      {kind === "product" ? <label>Producto anunciado<select required value={product} onChange={e => setProduct(e.target.value)}><option value="">Elegí un producto</option>{products.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}</select></label>
        : <label>Foto de la galería del negocio<select value={slot} onChange={e => setSlot(Number(e.target.value))}>{[1, 2, 3, 4, 5].map(n => <option key={n} value={n}>Foto {n}</option>)}</select></label>}
      <p className="field-help">Se guarda una copia de la imagen actual para revisión. Cargá primero la foto en el catálogo o la galería del negocio.</p>
      <label>Título del anuncio<input required minLength={3} maxLength={160} value={title} onChange={e => setTitle(e.target.value)} /></label>
      <label>Descripción del anuncio<input required minLength={3} maxLength={1000} value={description} onChange={e => setDescription(e.target.value)} /></label>
      <label>Condiciones de la propuesta<input required minLength={3} maxLength={1000} value={conditions} onChange={e => setConditions(e.target.value)} /></label>
      <label>Zona de difusión<select value={zone} onChange={e => setZone(e.target.value)}>{zones.map(z => <option key={z.id} value={z.id}>{z.name}</option>)}</select></label>
      <div className="catalog-fields"><label>Inicio de difusión<input required type="datetime-local" value={starts} onChange={e => setStarts(e.target.value)} /></label><label>Fin de difusión<input required type="datetime-local" min={starts} value={ends} onChange={e => setEnds(e.target.value)} /></label></div>
      <p className="field-help">Hasta 30 días. Horarios según tu dispositivo. La difusión empieza después de la aprobación y del pago confirmado, dentro del período elegido.</p>
      <label>Enviar como<select value={status} onChange={e => setStatus(e.target.value)}><option value="pending_review">Solicitud de revisión</option><option value="draft">Borrador privado</option></select></label>
    </fieldset><button className="button button-primary" disabled={operation.pending || operation.restoring}>{operation.intent ? "Reintentar campaña" : "Guardar campaña"}</button>
  </form>;
}
