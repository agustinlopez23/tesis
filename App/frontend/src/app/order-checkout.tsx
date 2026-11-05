"use client";

import { money } from "./order-api";
import OrderReceipt from "./order-receipt";
import { useOrderCart } from "./use-order-cart";
import { Alert, Icon } from "./ui";

export default function OrderCheckout({ cart }: { cart: ReturnType<typeof useOrderCart> }) {
  if (!cart.selection.length && !cart.intent && !cart.order && !cart.error && !cart.notice) return null;
  if (cart.order) return <div className="order-done"><OrderReceipt order={cart.order} accessKey={cart.intent?.accessKey} /><button className="text-link" type="button" onClick={cart.reset}><Icon name="plus" />Preparar otra solicitud</button></div>;
  const totals: Record<string, number> = {};
  for (const { product, quantity } of cart.selection) totals[product.currency] = (totals[product.currency] ?? 0) + product.price * quantity;
  const locked = cart.pending || cart.restoring || !!cart.intent;
  return <section className="order-checkout" aria-labelledby="order-checkout-title" tabIndex={-1}>
    <div className="order-checkout-heading"><span className="order-checkout-icon"><Icon name="cart" /></span><div><p className="eyebrow">Tu solicitud</p><h3 id="order-checkout-title">Tu selección</h3><p className="muted">Revisá cantidades, dejá tu nombre y confirmá. Después la enviás por WhatsApp.</p></div></div>
    {cart.error && <p className="error" role="alert">{cart.error}</p>}
    {cart.notice && <Alert tone="info" role="status">{cart.notice}</Alert>}
    {cart.restoring && <Alert tone="neutral" role="status">Recuperando solicitud…</Alert>}
    <form onSubmit={(e) => { e.preventDefault(); void cart.submit(); }}><fieldset disabled={locked}>
      <ul className="order-lines">{cart.selection.map(({ product, quantity }) => <li key={product.id}>
        <div className="order-line-copy"><strong>{product.name}</strong><p>{money(product.price, product.currency)} c/u</p></div>
        <div className="order-line-qty">
          <button type="button" className="icon-button order-step" aria-label={`Restar ${product.name}`} disabled={quantity <= 1} onClick={() => cart.quantity(product.id, quantity - 1)}>−</button>
          <label><span className="sr-only">Cantidad de {product.name}</span><input type="number" min="1" max="99" required value={quantity} onChange={(e) => cart.quantity(product.id, Number(e.target.value))} /></label>
          <button type="button" className="icon-button order-step" aria-label={`Sumar ${product.name}`} disabled={quantity >= 99} onClick={() => cart.quantity(product.id, quantity + 1)}>+</button>
        </div>
        <strong className="order-line-total">{money(product.price * quantity, product.currency)}</strong>
        <button className="text-link order-remove" type="button" onClick={() => cart.quantity(product.id, 0)}><Icon name="close" />Quitar {product.name}</button>
      </li>)}</ul>
      <div className="order-totals">{Object.entries(totals).map(([currency, total]) => <p key={currency}>Total de referencia: <strong>{money(total, currency)}</strong></p>)}</div>
      <label className="order-name">Tu nombre o alias<input required maxLength={80} autoComplete="given-name" value={cart.name} onChange={(e) => cart.setName(e.target.value)} /></label>
    </fieldset>
    <p className="field-help">Se guarda para que el negocio la atienda y después la envíes por WhatsApp. No reserva unidades ni confirma una compra.</p>
    <button type="submit" className="button button-primary order-submit" disabled={cart.pending || cart.restoring || (!cart.intent && !cart.selection.length)}>{cart.pending ? "Guardando solicitud…" : cart.intent ? "Reintentar solicitud" : "Confirmar solicitud"}<Icon name="arrow" /></button>
    </form>
  </section>;
}
