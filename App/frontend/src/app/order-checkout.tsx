"use client";

import { money } from "./order-api";
import OrderReceipt from "./order-receipt";
import { useOrderCart } from "./use-order-cart";

export default function OrderCheckout({ cart }: { cart: ReturnType<typeof useOrderCart> }) {
  if (!cart.selection.length && !cart.intent && !cart.order && !cart.error && !cart.notice) return null;
  if (cart.order) return <><OrderReceipt order={cart.order} accessKey={cart.intent?.accessKey} /><button className="text-link" type="button" onClick={cart.reset}>Preparar otra solicitud</button></>;
  const totals: Record<string, number> = {};
  for (const { product, quantity } of cart.selection) totals[product.currency] = (totals[product.currency] ?? 0) + product.price * quantity;
  return <section className="order-checkout" aria-labelledby="order-checkout-title"><h3 id="order-checkout-title">Tu selección</h3>
    {cart.error && <p className="error" role="alert">{cart.error}</p>}
    {cart.notice && <p role="status">{cart.notice}</p>}
    {cart.restoring && <p role="status">Recuperando solicitud…</p>}
    <form onSubmit={(e) => { e.preventDefault(); void cart.submit(); }}><fieldset disabled={cart.pending || cart.restoring || !!cart.intent}>
      <ul className="order-lines">{cart.selection.map(({ product, quantity }) => <li key={product.id}><div><strong>{product.name}</strong><p>{money(product.price, product.currency)} c/u</p></div>
        <label>Cantidad de {product.name}<input type="number" min="1" max="99" required value={quantity} onChange={(e) => cart.quantity(product.id, Number(e.target.value))} /></label>
        <button className="text-link" type="button" onClick={() => cart.quantity(product.id, 0)}>Quitar {product.name}</button>
      </li>)}</ul>
      {Object.entries(totals).map(([currency, total]) => <p key={currency}>Total de referencia: <strong>{money(total, currency)}</strong></p>)}
      <label>Tu nombre o alias<input required maxLength={80} autoComplete="given-name" value={cart.name} onChange={(e) => cart.setName(e.target.value)} /></label>
    </fieldset>
    <p className="field-help">La solicitud se guarda para que el negocio la atienda. Después podrás enviar el resumen por WhatsApp. No reserva unidades ni confirma una compra.</p>
    <button type="submit" className="button button-primary" disabled={cart.pending || cart.restoring || (!cart.intent && !cart.selection.length)}>{cart.pending ? "Guardando solicitud…" : cart.intent ? "Reintentar solicitud" : "Confirmar solicitud"}</button>
    </form>
  </section>;
}
