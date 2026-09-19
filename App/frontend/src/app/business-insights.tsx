"use client";

import { useEffect, useState } from "react";
import { Icon } from "./ui";

type Business = { id: string; name: string; status: string };
type Metric = "impressions" | "profileViews" | "contacts";
type Day = { date: string; impressions: number; profileViews: number; contacts: number };
type Report = { days: number; updatedAt: string; timeZone: string; impressions: number; profileViews: number; contacts: number; daily: Day[] };
const metrics: { key: Metric; label: string; description: string; icon: "eye" | "store" | "message" }[] = [
  { key: "impressions", label: "Apariciones en la guía", description: "Tu tarjeta estuvo visible en los resultados.", icon: "eye" },
  { key: "profileViews", label: "Visitas al perfil", description: "Abrieron la página de tu negocio.", icon: "store" },
  { key: "contacts", label: "Clics en WhatsApp", description: "Eligieron contactarte desde Cerca.", icon: "message" },
];
const number = (value: number) => value.toLocaleString("es-AR");
const dayLabel = (date: string) => new Date(date + "T12:00:00Z").toLocaleDateString("es-AR", { day: "numeric", month: "short", timeZone: "UTC" });

export function BusinessInsights({ businesses }: { businesses: Business[] }) {
  const [open, setOpen] = useState(false);
  const [selected, setSelected] = useState("");
  const business = businesses.find(item => item.id === selected) ?? businesses[0];
  if (!business) return null;
  return <section className={"business-insights " + (open ? "is-open" : "")} aria-labelledby="insights-title">
    <div className="insights-heading"><div><p className="eyebrow">De la guía a tu negocio</p><h2 id="insights-title">Cada encuentro cuenta.</h2>
      <p>Conocé cómo descubren tu negocio y cuándo deciden contactarte.</p></div>
      <button className="button button-outline" aria-expanded={open} aria-controls="insights-content" onClick={() => setOpen(!open)}>{open ? "Cerrar actividad" : "Ver actividad"}<Icon name={open ? "close" : "arrow"} /></button></div>
    {open && <div id="insights-content"><div className="insights-business"><div><label htmlFor="activity-business">Negocio</label><select id="activity-business" value={business.id} onChange={event => setSelected(event.target.value)}>{businesses.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></div>
      <span className={"publication-badge " + (business.status === "published" ? "published" : "")}>{business.status === "published" ? "Publicado en la guía" : "Borrador"}</span></div>
      <ActivityReport key={business.id} business={business} /></div>}
  </section>;
}

function ActivityReport({ business }: { business: Business }) {
  const [days, setDays] = useState(7);
  const [attempt, setAttempt] = useState(0);
  const [metric, setMetric] = useState<Metric>("impressions");
  const [report, setReport] = useState<Report | null>(null);
  const [error, setError] = useState(false);
  useEffect(() => {
    let active = true;
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 10000);
    void fetch(`/api/marketplace/businesses/${business.id}/activity?days=${days}`, { cache: "no-store", signal: controller.signal })
      .then(async response => {
        if (!response.ok) throw new Error("Activity unavailable");
        const data: Report = await response.json();
        if (active) setReport(data);
      }).catch(() => { if (active) setError(true); }).finally(() => clearTimeout(timeout));
    return () => { active = false; clearTimeout(timeout); controller.abort(); };
  }, [business.id, days, attempt]);
  function refresh(period = days) { setReport(null); setError(false); setDays(period); setAttempt(value => value + 1); }
  const current = metrics.find(item => item.key === metric)!;
  return <>
    <div className="insights-toolbar"><div><label htmlFor="activity-period">Período</label><select id="activity-period" value={days} onChange={event => refresh(Number(event.target.value))}><option value={7}>Últimos 7 días</option><option value={30}>Últimos 30 días</option></select></div>
      <span>Hoy incluido · Hora de Córdoba</span><button className="text-link" onClick={() => refresh()} disabled={!report && !error}>Actualizar <Icon name="arrow" /></button></div>
    {error ? <div className="insights-error"><p role="alert">No pudimos cargar la actividad. Revisá tu conexión e intentá de nuevo.</p><button className="button button-outline" onClick={() => refresh()}>Reintentar</button></div> :
      !report ? <div className="insights-loading" role="status">Cargando la actividad de {business.name}…</div> : <>
        <div className="metric-cards" role="group" aria-label="Elegir métrica para el gráfico">{metrics.map(item => <button key={item.key} className={"metric-card " + (metric === item.key ? "selected" : "")} aria-label={item.label + " " + number(report[item.key])} aria-pressed={metric === item.key} onClick={() => setMetric(item.key)}>
          <span className="metric-label">{item.label}<Icon name={item.icon} /></span><strong>{number(report[item.key])}</strong><span className="metric-description">{item.description}</span></button>)}</div>
        {report.impressions + report.profileViews + report.contacts === 0 ? <div className="insights-empty"><span className="mini-icon"><Icon name="eye" /></span><div><h3>Todavía no hay actividad en este período.</h3><p>{business.status === "draft" ? "Tu negocio está en borrador. Publicalo para que puedan descubrirlo." : "Tu negocio ya tiene su lugar. Cuando lo descubran, vas a ver la actividad acá."}</p></div></div> :
          <div className="activity-chart"><div className="chart-heading"><div><p className="eyebrow">Día a día</p><h3>{current.label}</h3></div><span>{number(report[metric])} en el período</span></div>
            <DailyChart daily={report.daily} metric={metric} label={current.label} />
            <details className="activity-table"><summary>Ver datos por día</summary><div><table aria-label="Actividad por día"><thead><tr><th scope="col">Día</th>{metrics.map(item => <th key={item.key} scope="col">{item.label}</th>)}</tr></thead><tbody>{report.daily.map(day => <tr key={day.date}><th scope="row">{dayLabel(day.date)}</th>{metrics.map(item => <td key={item.key}>{number(day[item.key])}</td>)}</tr>)}</tbody></table></div></details>
          </div>}
        <p className="insights-updated">Actualizado a las {new Date(report.updatedAt).toLocaleTimeString("es-AR", { hour: "2-digit", minute: "2-digit", timeZone: report.timeZone })} · Hoy todavía está en curso.</p>
      </>}
    <div className="insights-note"><Icon name="message" /><p>Un clic abre WhatsApp. No confirma un mensaje enviado ni una venta.</p></div>
    <details className="activity-method"><summary>Cómo contamos la actividad</summary><p>Una aparición requiere al menos la mitad de la tarjeta visible durante un segundo, con la pestaña activa. Una visita cuenta cuando el perfil termina de cargar y la pestaña está visible.</p>
      <p>No contamos tus propias visitas mientras estás conectado. En una misma pestaña, deduplicamos cada acción por negocio y lugar del clic durante 30 minutos. Son acciones registradas, no personas únicas; un contacto en la guía y otro en el perfil pueden corresponder a la misma persona.</p>
      <p>Guardamos la actividad durante 30 días. Puede faltar actividad si el navegador bloquea la medición o falla la conexión. La guía muestra resultados orgánicos; todavía no incluye anuncios.</p></details>
  </>;
}

function DailyChart({ daily, metric, label }: { daily: Day[]; metric: Metric; label: string }) {
  const [selected, setSelected] = useState<string | null>(null);
  const max = Math.max(1, ...daily.map(day => day[metric]));
  const selectedDay = daily.find(day => day.date === selected);
  return <><div className="chart-plot"><div className="chart-scale"><span>{number(max)}</span><span>0</span></div>
    <div className="daily-bars" role="group" aria-label={"Evolución diaria de " + label + ". Los valores están disponibles en la tabla."}>
      {daily.map(day => <button key={day.date} className={day.date === selected ? "selected" : ""} aria-label={`${dayLabel(day.date)}: ${day[metric]} ${label}`} aria-pressed={day.date === selected}
        onFocus={() => setSelected(day.date)} onMouseEnter={() => setSelected(day.date)} onClick={() => setSelected(day.date)}><span style={{ height: `${Math.max(day[metric] ? 3 : 0, day[metric] / max * 100)}%` }} /></button>)}
    </div></div><div className="chart-dates"><span>{daily[0] && dayLabel(daily[0].date)}</span><span>{daily.at(-1) && dayLabel(daily.at(-1)!.date)}</span></div>
    <p className="chart-detail" aria-live="polite">{selectedDay ? `${dayLabel(selectedDay.date)} · ${number(selectedDay[metric])} ${label.toLocaleLowerCase("es-AR")}` : "Elegí un día para ver su detalle."}</p></>;
}
