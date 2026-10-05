import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { expect, it, vi } from "vitest";
import BusinessOrders from "../src/app/business-orders";

const order = { id: "o1", reference: "CE-123", businessId: "b1", businessName: "Café local", customerName: "Ana", status: "pending", version: 1,
  createdAt: "2026-10-05T00:00:00Z", contactReceivedAt: null, canContact: true, lines: [], totals: {} };
it("permite gestionar una solicitud sin consultar ni exigir un plan vigente", async () => {
  const fetcher = vi.spyOn(globalThis, "fetch").mockImplementation(async (url, init) => {
    if (init?.method === "PUT") return Response.json({ ...order, status: "in_attention", version: 2, contactReceivedAt: "2026-10-05T10:00:00Z" });
    return Response.json(String(url).includes("/orders?") ? { items: [order], page: 1, hasNextPage: false } : order);
  });
  render(<BusinessOrders id="b1" />);
  fireEvent.click(screen.getByRole("button", { name: "Ver solicitudes" }));
  fireEvent.click(await screen.findByRole("button", { name: "Abrir CE-123" }));
  fireEvent.change(await screen.findByLabelText("Estado de la solicitud"), { target: { value: "in_attention" } });
  fireEvent.click(screen.getByLabelText("Confirmo que recibí el contacto del cliente"));
  fireEvent.click(screen.getByRole("button", { name: "Guardar estado" }));
  await waitFor(() => expect(fetcher.mock.calls.some(([, init]) => init?.method === "PUT")).toBe(true));
  expect(fetcher.mock.calls.some(([url]) => String(url).includes("/plan"))).toBe(false);
  expect(await screen.findByText("Estado guardado.")).toBeInTheDocument();
});
it("muestra conflictos sin confirmar cambios que el servidor rechazó", async () => {
  vi.spyOn(globalThis, "fetch").mockImplementation(async (url, init) => init?.method === "PUT"
    ? Response.json({ message: "El estado cambió. Actualizá." }, { status: 409 })
    : Response.json(String(url).includes("/orders?") ? { items: [order], page: 1, hasNextPage: false } : order));
  render(<BusinessOrders id="b1" />);
  fireEvent.click(screen.getByRole("button", { name: "Ver solicitudes" }));
  fireEvent.click(await screen.findByRole("button", { name: "Abrir CE-123" }));
  fireEvent.click(await screen.findByRole("button", { name: "Guardar estado" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("El estado cambió");
  expect(screen.queryByText("Estado guardado.")).not.toBeInTheDocument();
});
