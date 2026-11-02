"use client";

import { useState } from "react";
import Link from "next/link";
import { money, Order, orderMessage, orderStatus, privateOrderPath } from "./order-api";

export default function OrderReceipt({ order, accessKey, headingLevel = 3 }: { order: Order; accessKey?: string; headingLevel?: 2 | 3 }) {
  const Heading = headingLevel === 2 ? "h2" : "h3";
  const [notice, setNotice] = useState("");
  const [showLink, setShowLink] = useState(false);
  const message = orderMessage(order);
  const privatePath = accessKey ? privateOrderPath(order.id, accessKey) : null;
  // The key works like a password: keep it masked on screen unless the customer asks to see it.
  const visiblePath = privatePath && (showLink ? privatePath : privateOrderPath(order.id, "•".repeat(12)));
  async function copy(value: string) {
    try { await navigator.clipboard.writeText(value); setNotice("Copiado."); }
    catch { setNotice("No pudimos copiar automáticamente. Seleccioná y copiá el texto de abajo."); }
  }
  return <section className="order-receipt" aria-label="Resumen de solicitud">
    <p className="eyebrow">Solicitud guardada</p><Heading>{order.reference}</Heading>
    <p>{order.businessName} · {order.customerName}</p>
    <p><strong>{orderStatus(order.status)}</strong> · {order.contactReceivedAt ? "Contacto recibido, confirmado por el negocio." : "El negocio aún no confirmó haber recibido el contacto."}</p>
    <ul className="order-lines">{order.lines.map(line => <li key={line.productId}><span>{line.quantity} × {line.name}</span><strong>{money(line.unitPrice * line.quantity, line.currency)}</strong></li>)}</ul>
    {Object.entries(order.totals).map(([currency, total]) => <p key={currency}>Total de referencia: <strong>{money(total, currency)}</strong></p>)}
    <p className="field-help">Precios guardados al solicitar. El negocio debe confirmar disponibilidad y precio final.</p>
    {accessKey && <>
      <div className="catalog-toolbar">{order.canContact && <a className="button button-primary" href={`https://wa.me/${order.whatsApp.replace(/^\+/, "")}?text=${encodeURIComponent(message)}`} target="_blank" rel="noopener noreferrer" referrerPolicy="no-referrer">Abrir WhatsApp</a>}
        <button type="button" className="button button-outline" onClick={() => void copy(message)}>Copiar resumen</button></div>
      <p className="field-help">Abrir WhatsApp no confirma el envío del mensaje ni una venta. Podés volver a abrirlo sin crear otra solicitud.</p>
      <details><summary>Ver mensaje para copiar</summary><textarea className="order-copy" readOnly aria-label="Resumen para WhatsApp" value={message} /></details>
      <div className="order-private-link"><Link className="text-link" href={privatePath!} prefetch={false}>Consultar mi solicitud</Link>
        <button type="button" className="text-link" onClick={() => void copy(window.location.origin + privatePath)}>Copiar enlace privado</button>
        <p className="field-help">Guardá este enlace. Quien lo tenga podrá consultar tu solicitud hasta el {new Date(order.accessExpiresAt).toLocaleDateString("es-AR")}. La referencia sola no permite recuperarla.</p>
        <p className="field-help">El enlace es privado y funciona como una contraseña: no lo compartas ni lo muestres en pantalla.</p>
        <div className="order-private-field">
          <input readOnly aria-label="Enlace privado" value={typeof window === "undefined" ? visiblePath! : window.location.origin + visiblePath} />
          <button type="button" className="text-link" aria-pressed={showLink} onClick={() => setShowLink(value => !value)}>{showLink ? "Ocultar enlace" : "Mostrar enlace"}</button>
        </div>
      </div>
    </>}
    {notice && <p role="status">{notice}</p>}
  </section>;
}
