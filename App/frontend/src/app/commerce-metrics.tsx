"use client";

import { useEffect, useState } from "react";
import { catalogApi, catalogError } from "./catalog-api";
import { money } from "./order-api";
import { useBusinessRefresh } from "./business-refresh";

type Metrics = { days: number; from: string; asOf: string; orders: number; contactsReceived: number; converted: number;
  conversionRate: number | null; activeSales: number; voidedSales: number; amounts: Record<string, Record<string, number>> };
export default function CommerceMetrics({ business }: { business: string }) {
  const [open, setOpen] = useState(false);
  return <section className="catalog-panel"><div className="catalog-heading"><h2>Resultados comerciales</h2><button className="button button-outline" aria-expanded={open} onClick={() => setOpen(!open)}>{open ? "Cerrar resultados comerciales" : "Ver resultados comerciales"}</button></div>{open && <Results key={business} business={business} />}</section>;
}
function Results({ business }: { business: string }) {
  const [days, setDays] = useState(7), [attempt, setAttempt] = useState(0), [loading, setLoading] = useState(true);
  const [data, setData] = useState<Metrics | null>(null), [error, setError] = useState("");
  useEffect(() => {
    const controller = new AbortController();
    void catalogApi(`/businesses/${business}/commerce?days=${days}`, "GET", undefined, controller.signal).then(async response => {
      if (!response.ok) throw new Error(await catalogError(response));
      const result = await response.json(); if (!controller.signal.aborted) setData(result);
    }).catch(e => { if (!controller.signal.aborted) { setData(null); setError(e.message); } }).finally(() => { if (!controller.signal.aborted) setLoading(false); });
    return () => controller.abort();
  }, [business, days, attempt]);
  function refresh(value = days) { setData(null); setError(""); setLoading(true); setDays(value); setAttempt(n => n + 1); }
  useBusinessRefresh(business, refresh);
  return <>
    <div className="catalog-toolbar">{[7, 30].map(value => <button className="button button-outline" key={value} aria-pressed={days === value} onClick={() => refresh(value)}>{value} días</button>)}<button className="text-link" disabled={loading} onClick={() => refresh()}>Actualizar resultados comerciales</button></div>
    {error && <p role="alert">{error}</p>}{loading && <p role="status">Consultando resultados…</p>}
    {data && <>
      <p>Desde {new Date(data.from).toLocaleDateString("es-AR", { timeZone: "America/Argentina/Cordoba" })} hasta {new Date(data.asOf).toLocaleString("es-AR", { timeZone: "America/Argentina/Cordoba" })}, hora de Córdoba.</p>
      <h3>Solicitudes creadas en el período</h3>
      <dl className="commerce-counts"><div><dt>Solicitudes</dt><dd>{data.orders}</dd></div><div><dt>Contactos confirmados</dt><dd>{data.contactsReceived}</dd></div><div><dt>Con venta vigente</dt><dd>{data.converted}</dd></div><div><dt>Conversión al consultar</dt><dd>{data.conversionRate === null ? "Sin solicitudes en el período" : `${(data.conversionRate * 100).toLocaleString("es-AR", { minimumFractionDigits: 1, maximumFractionDigits: 1 })} %`}</dd></div></dl>
      <p>La conversión sigue solo esas solicitudes hasta ahora. Puede cambiar al registrar o anular ventas; todavía no es un seguimiento concluido.</p>
      <h3>Ventas registradas en el período</h3><p>{data.activeSales} vigentes · {data.voidedSales} anuladas</p>
      <p>Incluye ventas de solicitudes anteriores al período. Los importes excluyen ventas anuladas.</p>
      {Object.entries(data.amounts).length === 0 && <p>Sin importes vigentes.</p>}
      {Object.entries(data.amounts).map(([origin, totals]) => <div className="commerce-amount" key={origin}><strong>{{ requested: "Productos solicitados", conversation: "Acordado en conversación", direct: "Ventas presenciales" }[origin] ?? origin}</strong>{Object.entries(totals).map(([currency, amount]) => <p key={currency}>{money(amount, currency)}</p>)}</div>)}
      <p className="field-help">Datos reportados por el negocio. No verifican pagos ni mensajes enviados por WhatsApp. Las ventas presenciales no se atribuyen a solicitudes.</p>
    </>}
  </>;
}
