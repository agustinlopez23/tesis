"use client";

import { useState } from "react";
import Link from "next/link";
import { money, Order, orderMessage, orderStatus, privateOrderPath } from "./order-api";
import { Badge, Icon } from "./ui";

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
  // The success header is plain markup (not an Alert) so the copy notice below stays the only live region.
  return <section className="order-receipt" aria-label="Resumen de solicitud">
    <div className="order-receipt-head"><span className="order-receipt-icon"><Icon name="check" /></span>
      <div><p className="eyebrow">Solicitud guardada</p><Heading>{order.reference}</Heading><p>{order.businessName} · {order.customerName}</p></div></div>
    <p className="order-receipt-status"><Badge tone={order.contactReceivedAt ? "success" : "info"}>{orderStatus(order.status)}</Badge> {order.contactReceivedAt ? "Contacto recibido, confirmado por el negocio." : "El negocio aún no confirmó haber recibido el contacto."}</p>
    <ul className="order-lines">{order.lines.map(line => <li key={line.productId}><span>{line.quantity} × {line.name}</span><strong>{money(line.unitPrice * line.quantity, line.currency)}</strong></li>)}</ul>
    <div className="order-totals">{Object.entries(order.totals).map(([currency, total]) => <p key={currency}>Total de referencia: <strong>{money(total, currency)}</strong></p>)}</div>
    <p className="field-help">Precios guardados al solicitar. El negocio debe confirmar disponibilidad y precio final.</p>
    {accessKey && <>
      <div className="order-receipt-actions">{order.canContact && <a className="button button-whatsapp" href={`https://wa.me/${order.whatsApp.replace(/^\+/, "")}?text=${encodeURIComponent(message)}`} target="_blank" rel="noopener noreferrer" referrerPolicy="no-referrer"><Icon name="message" />Abrir WhatsApp</a>}
        <button type="button" className="button button-outline" onClick={() => void copy(message)}>Copiar resumen</button></div>
      <p className="field-help">Abrir WhatsApp no confirma el envío del mensaje ni una venta. Podés volver a abrirlo sin crear otra solicitud.</p>
      <details className="order-message"><summary>Ver mensaje para copiar</summary><textarea className="order-copy" readOnly aria-label="Resumen para WhatsApp" value={message} /></details>
      <div className="order-private-link"><Link className="text-link" href={privatePath!} prefetch={false}><Icon name="eye" />Consultar mi solicitud</Link>
        <button type="button" className="text-link" onClick={() => void copy(window.location.origin + privatePath)}>Copiar enlace privado</button>
        <p className="field-help">Guardá este enlace. Quien lo tenga podrá consultar tu solicitud hasta el {new Date(order.accessExpiresAt).toLocaleDateString("es-AR")}. La referencia sola no permite recuperarla.</p>
        <p className="field-help">El enlace es privado y funciona como una contraseña: no lo compartas ni lo muestres en pantalla.</p>
        <div className="order-private-field">
          <input readOnly aria-label="Enlace privado" value={typeof window === "undefined" ? visiblePath! : window.location.origin + visiblePath} />
          <button type="button" className="text-link" aria-pressed={showLink} onClick={() => setShowLink(value => !value)}>{showLink ? "Ocultar enlace" : "Mostrar enlace"}</button>
        </div>
      </div>
    </>}
    {notice && <p role="status" className="order-notice">{notice}</p>}
  </section>;
}
