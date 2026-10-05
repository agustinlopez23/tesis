import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { expect, it, vi } from "vitest";
import SaleEditor from "../src/app/sale-editor";
import BusinessInventory from "../src/app/business-inventory";
import BusinessSales from "../src/app/business-sales";

const product = { id: "p1", name: "Café", price: 100, currency: "ARS" };
const sale = { id: "s1", orderId: "o1", orderReference: "CE-123", createdAt: "2026-10-05T00:00:00Z", voidedAt: null,
  lines: [{ productId: "p1", name: "Café", quantity: 1, unitPrice: 100, currency: "ARS", origin: "requested", returnedQuantity: 0 }], totals: { requested: { ARS: 100 } } };
it("anular una venta no supone devolución y exige motivo explícito", async () => {
  sessionStorage.clear();
  const fetcher = vi.spyOn(globalThis, "fetch").mockImplementation(async (_url, init) => init?.method === "PUT"
    ? Response.json({ ...sale, voidedAt: "2026-10-05T12:00:00Z" }) : Response.json({ items: [sale], page: 1, hasNextPage: false }));
  render(<BusinessSales business="b1" />);
  fireEvent.click(screen.getByRole("button", { name: "Ver ventas" }));
  fireEvent.click(await screen.findByRole("button", { name: "Anular venta CE-123" }));
  await waitFor(() => expect(screen.getByRole("button", { name: "Confirmar anulación" })).toBeEnabled());
  expect(screen.getByLabelText("Unidades devueltas de Café")).toHaveValue(0);
  fireEvent.change(screen.getByLabelText("Motivo de anulación"), { target: { value: "Corrección sin devolución" } });
  fireEvent.click(screen.getByRole("button", { name: "Confirmar anulación" }));
  await waitFor(() => expect(fetcher.mock.calls.some(([, i]) => i?.method === "PUT")).toBe(true));
  expect(JSON.parse(String(fetcher.mock.calls.find(([, i]) => i?.method === "PUT")![1]!.body)).returns).toEqual([]);
});
it("conserva el intento de venta al perder respuesta y permite reintentar sin duplicar", async () => {
  sessionStorage.clear(); const bodies: string[] = [], paths: string[] = [];
  vi.spyOn(globalThis, "fetch").mockImplementation(async (url, init) => {
    if (init?.method === "PUT") { bodies.push(String(init.body)); paths.push(String(url)); if (bodies.length === 1) throw new Error("lost"); return Response.json(sale); }
    if (String(url).includes("/products")) return Response.json([product]);
    return Response.json({ items: [] });
  });
  const props = { business: "b1", order: { id: "o1", version: 1, lines: [{ productId: "p1", name: "Café", quantity: 1, unitPrice: 100, currency: "ARS" }] }, onSaved: vi.fn() };
  const first = render(<SaleEditor {...props} />);
  await waitFor(() => expect(screen.getByRole("button", { name: "Registrar venta" })).toBeEnabled());
  fireEvent.click(await screen.findByRole("button", { name: "Registrar venta" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("Reintentá");
  first.unmount(); render(<SaleEditor {...props} />);
  fireEvent.click(await screen.findByRole("button", { name: "Reintentar venta" }));
  await waitFor(() => expect(props.onSaved).toHaveBeenCalled());
  expect(paths[0]).toBe(paths[1]); expect(bodies[0]).toBe(bodies[1]);
  expect(JSON.parse(bodies[0]).lines[0].origin).toBe("requested");
});
it("un conflicto de stock no muestra venta confirmada y deja revisar los datos", async () => {
  sessionStorage.clear();
  vi.spyOn(globalThis, "fetch").mockImplementation(async (url, init) => init?.method === "PUT"
    ? Response.json({ message: "Stock insuficiente" }, { status: 409 }) : Response.json(String(url).includes("products") ? [product] : { items: [] }));
  render(<SaleEditor business="b1" onSaved={vi.fn()} />);
  fireEvent.change(await screen.findByLabelText("Agregar producto a la venta"), { target: { value: "p1" } });
  fireEvent.click(screen.getByRole("button", { name: "Agregar a venta" }));
  fireEvent.click(screen.getByRole("button", { name: "Registrar venta" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("Stock insuficiente");
  expect(screen.getByLabelText("Cantidad vendida de Café")).toBeEnabled();
});
it("inventario distingue saldo desconocido de cero y envía motivo y versión", async () => {
  sessionStorage.clear();
  const fetcher = vi.spyOn(globalThis, "fetch").mockImplementation(async (url, init) => init?.method === "PUT"
    ? Response.json({ quantity: 5 }) : Response.json(String(url).includes("movements") ? { items: [], page: 1, hasNextPage: false }
      : [{ productId: "p1", name: "Café", quantity: null, version: 0 }]));
  render(<BusinessInventory business="b1" />);
  fireEvent.click(screen.getByRole("button", { name: "Ver inventario" }));
  expect(await screen.findByText("Sin cargar")).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Ajustar Café" }));
  fireEvent.change(screen.getByLabelText("Unidades a sumar o restar"), { target: { value: "5" } });
  fireEvent.change(screen.getByLabelText("Motivo del ajuste"), { target: { value: "Carga inicial" } });
  fireEvent.click(screen.getByRole("button", { name: "Guardar ajuste" }));
  await waitFor(() => expect(fetcher.mock.calls.some(([, i]) => i?.method === "PUT")).toBe(true));
  const sent = JSON.parse(String(fetcher.mock.calls.find(([, i]) => i?.method === "PUT")![1]!.body));
  expect(sent).toEqual({ productId: "p1", delta: 5, version: 0, reason: "Carga inicial" });
});
