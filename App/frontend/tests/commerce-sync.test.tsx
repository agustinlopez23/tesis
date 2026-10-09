import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { expect, it, vi } from "vitest";
import BusinessInventory from "../src/app/business-inventory";
import BusinessOrders from "../src/app/business-orders";
import BusinessSales from "../src/app/business-sales";
import CommerceMetrics from "../src/app/commerce-metrics";
import SaleEditor from "../src/app/sale-editor";

const line = { productId: "p1", name: "Café", quantity: 1, unitPrice: 100, currency: "ARS" };
it("registrar una venta actualiza inventario y métricas abiertos del mismo negocio", async () => {
  sessionStorage.clear(); let sold = false;
  const fetcher = vi.spyOn(globalThis, "fetch").mockImplementation(async (url, init) => {
    const path = String(url);
    if (init?.method === "PUT") { sold = true; return Response.json({ id: "s1" }); }
    if (path.includes("/commerce")) return Response.json({ days: 7, from: "2026-10-01T00:00:00Z", asOf: "2026-10-05T00:00:00Z", orders: 1, contactsReceived: 0, converted: sold ? 1 : 0, conversionRate: sold ? 1 : 0, activeSales: sold ? 1 : 0, voidedSales: 0, amounts: {} });
    if (path.includes("/movements")) return Response.json({ items: [], hasNextPage: false });
    if (path.endsWith("/inventory")) return Response.json([{ productId: "p1", name: "Café", quantity: sold ? 4 : 5, version: sold ? 2 : 1 }]);
    return Response.json([{ id: "p1", name: "Café", price: 100, currency: "ARS" }]);
  });
  const inventory = render(<BusinessInventory business="b1" />);
  const other = render(<BusinessInventory business="b2" />);
  fireEvent.click(within(inventory.container).getByRole("button", { name: "Ver inventario" }));
  fireEvent.click(within(other.container).getByRole("button", { name: "Ver inventario" }));
  expect(await within(inventory.container).findByText(/5 unidades/)).toBeInTheDocument();
  expect(await within(other.container).findByText(/5 unidades/)).toBeInTheDocument();
  render(<CommerceMetrics business="b1" />); fireEvent.click(screen.getByRole("button", { name: "Ver resultados comerciales" }));
  expect(await screen.findByText("0 vigentes · 0 anuladas")).toBeInTheDocument();
  render(<SaleEditor business="b1" order={{ id: "o1", version: 1, lines: [line] }} onSaved={vi.fn()} />);
  await waitFor(() => expect(screen.getByRole("button", { name: "Registrar venta" })).toBeEnabled());
  fireEvent.click(screen.getByRole("button", { name: "Registrar venta" }));
  expect(await within(inventory.container).findByText(/4 unidades/)).toBeInTheDocument();
  expect(await screen.findByText("1 vigentes · 0 anuladas")).toBeInTheDocument();
  expect(within(other.container).getByText(/5 unidades/)).toBeInTheDocument();
  expect(fetcher.mock.calls.filter(([url]) => String(url).endsWith("b2/inventory"))).toHaveLength(1);
});
it("anular una venta refresca la solicitud seleccionada sin cerrarla", async () => {
  sessionStorage.clear(); let voided = false;
  const order = { id: "o1", businessId: "b1", reference: "CE-123", customerName: "Ana", businessName: "Local", createdAt: "2026-10-05T00:00:00Z", version: 2, status: "converted", contactReceivedAt: null, lines: [line], totals: { ARS: 100 } };
  vi.spyOn(globalThis, "fetch").mockImplementation(async (url, init) => {
    if (init?.method === "PUT") { voided = true; return Response.json({ id: "s1" }); }
    if (String(url).endsWith("/orders/o1")) return Response.json({ ...order, status: voided ? "cancelled" : "converted" });
    if (String(url).includes("/orders?")) return Response.json({ items: [{ ...order, status: voided ? "cancelled" : "converted" }], hasNextPage: false });
    return Response.json({ items: [{ id: "s1", orderReference: "CE-123", createdAt: order.createdAt, voidedAt: voided ? order.createdAt : null, lines: [{ ...line, origin: "requested" }], totals: {} }], hasNextPage: false });
  });
  const orders = render(<BusinessOrders id="b1" />); render(<BusinessSales business="b1" />);
  fireEvent.click(screen.getByRole("button", { name: "Ver solicitudes" }));
  fireEvent.click(await screen.findByRole("button", { name: "Abrir CE-123" }));
  const receipt = await within(orders.container).findByRole("region", { name: "Resumen de solicitud" });
  expect(within(receipt).getByText("Convertida en venta")).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Ver ventas" })); fireEvent.click(await screen.findByRole("button", { name: "Anular venta CE-123" }));
  await waitFor(() => expect(screen.getByRole("button", { name: "Confirmar anulación" })).toBeEnabled());
  fireEvent.change(screen.getByLabelText("Motivo de anulación"), { target: { value: "Venta anulada" } });
  fireEvent.click(screen.getByRole("button", { name: "Confirmar anulación" }));
  await waitFor(() => expect(within(orders.container).getByRole("region", { name: "Resumen de solicitud" })).toHaveTextContent("Cancelada"));
});
