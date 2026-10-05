"use client";

import { useEffect, useState } from "react";
import { catalogApi, catalogError, Product } from "./catalog-api";
import { money } from "./order-api";
import { useSaleOperation } from "./use-sale-operation";

type Line = { productId: string; name: string; quantity: number; unitPrice: number; currency: string; origin: string };
type SourceOrder = { id: string; version: number; lines: Omit<Line, "origin">[] };
export default function SaleEditor({ business, order, onSaved }: { business: string; order?: SourceOrder; onSaved: () => void }) {
  const [products, setProducts] = useState<Product[]>([]);
  const [lines, setLines] = useState<Line[]>(() => order?.lines.map(l => ({ ...l, origin: "requested" })) ?? []);
  const [selected, setSelected] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const operation = useSaleOperation(`cerca-sale:${business}:${order?.id ?? "direct"}`, onSaved);
  useEffect(() => {
    const controller = new AbortController();
    void catalogApi(`/businesses/${business}/products`, "GET", undefined, controller.signal).then(async response => {
      if (!response.ok) throw new Error(await catalogError(response));
      const data = await response.json(); if (!controller.signal.aborted) setProducts(data);
    }).catch(e => { if (!controller.signal.aborted) setError(e.message); }).finally(() => { if (!controller.signal.aborted) setLoading(false); });
    return () => controller.abort();
  }, [business]);
  function add() {
    const product = products.find(p => p.id === selected);
    if (!product || lines.some(l => l.productId === selected) || lines.length >= 20) return;
    setLines([...lines, { productId: product.id, name: product.name, quantity: 1, unitPrice: product.price, currency: product.currency,
      origin: order?.lines.some(l => l.productId === selected) ? "requested" : order ? "conversation" : "direct" }]);
  }
  function change(id: string, field: string, value: string | number) { setLines(items => items.map(l => l.productId === id ? { ...l, [field]: value } : l)); }
  return <section className="sales-editor"><h3>{order ? "Venta de esta solicitud" : "Venta presencial"}</h3>
    <p>Revisá lo acordado: productos, cantidades y precios finales. Al registrar se descuenta stock. Cargá primero las existencias en Inventario.</p>
    {error && <p role="alert">{error}</p>}{operation.error && <p role="alert">{operation.error}</p>}
    {operation.intent && <p>Hay una operación pendiente de respuesta. Reintentá para recuperar el mismo resultado.</p>}
    <form onSubmit={e => { e.preventDefault(); void operation.submit(() => ({ path: `/businesses/${business}/sales/${crypto.randomUUID()}`,
      body: { ...(order ? { orderId: order.id, orderVersion: order.version } : {}), lines: lines.map(line => ({ productId: line.productId, quantity: line.quantity, unitPrice: line.unitPrice, currency: line.currency, origin: line.origin })) } })); }}>
      <fieldset disabled={operation.locked || loading}>
        <div className="catalog-toolbar"><label>Agregar producto a la venta<select value={selected} onChange={e => setSelected(e.target.value)}><option value="">Elegí un producto</option>{products.filter(p => !lines.some(l => l.productId === p.id)).map(p => <option key={p.id} value={p.id}>{p.name}</option>)}</select></label>
          <button type="button" className="button button-outline" onClick={add}>Agregar a venta</button></div>
        {lines.map(line => <div className="sale-line" key={line.productId}><strong>{line.name}</strong>
          <label>Cantidad vendida de {line.name}<input required type="number" min={1} max={99} value={line.quantity} onChange={e => change(line.productId, "quantity", Number(e.target.value))} /></label>
          <label>Precio final de {line.name}<input required type="number" min={0} max={9999999999.99} step="0.01" value={line.unitPrice} onChange={e => change(line.productId, "unitPrice", Number(e.target.value))} /></label>
          <label>Moneda de {line.name}<select value={line.currency} onChange={e => change(line.productId, "currency", e.target.value)}><option>ARS</option><option>USD</option></select></label>
          {order && <label>Origen de {line.name}<select value={line.origin} onChange={e => change(line.productId, "origin", e.target.value)}>{order.lines.some(l => l.productId === line.productId) && <option value="requested">En la solicitud</option>}<option value="conversation">Acordado en conversación</option></select></label>}
          <p>{money(line.unitPrice * line.quantity, line.currency)}</p><button type="button" className="text-link" onClick={() => setLines(lines.filter(l => l.productId !== line.productId))}>Quitar {line.name} de venta</button>
        </div>)}
      </fieldset>
      <button className="button button-primary" disabled={operation.pending || operation.restoring || (!operation.intent && (!lines.length || loading || !!error))}>{operation.pending ? "Guardando…" : operation.intent ? "Reintentar venta" : "Registrar venta"}</button>
    </form>
  </section>;
}
