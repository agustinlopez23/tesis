import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { expect, it, vi } from "vitest";
import PublicCatalog from "../src/app/public-catalog";
import OrderTracking from "../src/app/pedido/[id]/tracking";
import { orderMessage } from "../src/app/order-api";

const product = { id: "p1", name: "Café", description: "250 g", category: "Café", price: 1250.5, currency: "ARS", available: true, status: "published", version: 1, hasPhoto: false };
const order = { id: "order1", reference: "CE-123", businessId: "b1", businessName: "Café local", whatsApp: "+5493515551234", customerName: "Ana",
  createdAt: "2026-10-05T00:00:00Z", accessExpiresAt: "2026-11-04T00:00:00Z", status: "pending", version: 1, contactReceivedAt: null, canContact: true,
  lines: [{ productId: "p1", name: "Café", unitPrice: 1250.5, currency: "ARS", quantity: 2 }], totals: { ARS: 2501 } };

it("actualizar catálogo renueva la selección y permite resolver un conflicto de precio", async () => {
  sessionStorage.clear(); let version = 1; const attempts: number[] = [];
  vi.spyOn(globalThis, "fetch").mockImplementation(async (_url, init) => {
    if (init?.method === "POST") {
      const sent = JSON.parse(String(init.body)); attempts.push(sent.lines[0].expectedVersion);
      return sent.lines[0].expectedVersion === version ? Response.json(order, { status: 201 }) : Response.json({ message: "Los datos cambiaron" }, { status: 409 });
    }
    return Response.json({ items: [{ ...product, version, price: version === 1 ? 1250.5 : 1500 }], page: 1, hasNextPage: false });
  });
  render(<PublicCatalog id="b1" />);
  fireEvent.click(await screen.findByRole("button", { name: "Agregar Café" }));
  fireEvent.change(screen.getByLabelText("Tu nombre o alias"), { target: { value: "Ana" } }); version = 2;
  fireEvent.click(screen.getByRole("button", { name: "Confirmar solicitud" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("Los datos cambiaron");
  fireEvent.click(screen.getByRole("button", { name: "Actualizar catálogo" }));
  expect(await screen.findByText(/Actualizamos tu selección/)).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Confirmar solicitud" }));
  expect(await screen.findByRole("link", { name: "Consultar mi solicitud" })).toBeInTheDocument();
  expect(attempts).toEqual([1, 2]);
});

it("reintenta la misma solicitud después de perder la respuesta y preserva identidad al recargar", async () => {
  sessionStorage.clear();
  let version = 1;
  const calls: string[] = [];
  vi.spyOn(globalThis, "fetch").mockImplementation(async (url, init) => {
    if (init?.method === "POST") { calls.push(String(init.body)); throw new Error("Lost response"); }
    if (String(url).includes("/public/orders/")) return new Response(null, { status: 404 });
    return Response.json({ items: [{ ...product, version }], page: 1, hasNextPage: false });
  });
  const first = render(<PublicCatalog id="b1" />);
  fireEvent.click(await screen.findByRole("button", { name: "Agregar Café" }));
  fireEvent.change(screen.getByLabelText("Tu nombre o alias"), { target: { value: "Ana" } });
  fireEvent.click(screen.getByRole("button", { name: "Confirmar solicitud" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("Reintentá");
  version = 2;
  fireEvent.click(screen.getByRole("button", { name: "Actualizar catálogo" }));
  await screen.findByRole("button", { name: "Agregar Café" });
  expect(screen.queryByText(/Actualizamos tu selección/)).not.toBeInTheDocument();
  first.unmount();
  render(<PublicCatalog id="b1" />);
  fireEvent.click(await screen.findByRole("button", { name: "Reintentar solicitud" }));
  await waitFor(() => expect(calls).toHaveLength(2));
  expect(calls[0]).toBe(calls[1]);
  const payload = JSON.parse(calls[0]);
  expect(payload.accessKey).toMatch(/^[0-9a-f]{64}$/);
  expect(payload.lines[0]).toEqual({ productId: "p1", quantity: 1, expectedVersion: 1 });
});
it("retira de la selección un producto que dejó de estar disponible al actualizar", async () => {
  sessionStorage.clear(); let available = true;
  vi.spyOn(globalThis, "fetch").mockImplementation(async () => Response.json({ items: [{ ...product, available }], page: 1, hasNextPage: false }));
  render(<PublicCatalog id="b1" />);
  fireEvent.click(await screen.findByRole("button", { name: "Agregar Café" }));
  available = false;
  fireEvent.click(screen.getByRole("button", { name: "Actualizar catálogo" }));
  expect(await screen.findByText(/Actualizamos tu selección/)).toBeInTheDocument();
  expect(screen.queryByLabelText("Cantidad de Café")).not.toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Confirmar solicitud" })).toBeDisabled();
});
it("muestra el resumen guardado y no incluye la clave privada en WhatsApp", async () => {
  sessionStorage.clear();
  vi.spyOn(globalThis, "fetch").mockImplementation(async (_url, init) => init?.method === "POST"
    ? Response.json(order, { status: 201 }) : Response.json({ items: [product], page: 1, hasNextPage: false }));
  render(<PublicCatalog id="b1" />);
  fireEvent.click(await screen.findByRole("button", { name: "Agregar Café" }));
  fireEvent.change(screen.getByLabelText("Tu nombre o alias"), { target: { value: "Ana" } });
  fireEvent.click(screen.getByRole("button", { name: "Confirmar solicitud" }));
  const link = await screen.findByRole("link", { name: "Abrir WhatsApp" });
  expect(link.getAttribute("href")).toContain("wa.me/5493515551234");
  expect(decodeURIComponent(link.getAttribute("href")!)).toContain("CE-123");
  expect(decodeURIComponent(link.getAttribute("href")!)).not.toContain("clave");
  expect(screen.getByRole("link", { name: "Consultar mi solicitud" }).getAttribute("href")).toMatch(/#clave=[0-9a-f]{64}$/);
  expect(screen.getByText(/Abrir WhatsApp no confirma/)).toBeInTheDocument();
});
it("permite preparar otra solicitud cuando venció un intento ya confirmado", async () => {
  sessionStorage.clear();
  sessionStorage.setItem("cerca-order-intent:b1", JSON.stringify({ requestId: "11111111-1111-1111-1111-111111111111", accessKey: "a".repeat(64), customerName: "Ana", lines: [], accessExpiresAt: "2000-01-01T00:00:00Z" }));
  const fetcher = vi.spyOn(globalThis, "fetch").mockImplementation(async url => String(url).includes("/public/orders/")
    ? new Response(null, { status: 404 }) : Response.json({ items: [product], page: 1, hasNextPage: false }));
  render(<PublicCatalog id="b1" />);
  const add = await screen.findByRole("button", { name: "Agregar Café" });
  await waitFor(() => expect(add).toBeEnabled());
  fireEvent.click(add);
  expect(screen.getByRole("button", { name: "Confirmar solicitud" })).toBeEnabled();
  expect(sessionStorage.getItem("cerca-order-intent:b1")).toBeNull();
  expect(fetcher.mock.calls.some(([url]) => String(url).includes("/public/orders/"))).toBe(false);
});
it("el seguimiento lee la clave del fragmento y la envía en cabecera", async () => {
  const key = "a".repeat(64);
  window.history.replaceState({}, "", `/pedido/order1#clave=${key}`);
  const fetcher = vi.spyOn(globalThis, "fetch").mockResolvedValue(Response.json(order));
  render(<OrderTracking id="order1" />);
  expect(await screen.findByText("CE-123")).toBeInTheDocument();
  expect(fetcher).toHaveBeenCalledWith("/api/marketplace/public/orders/order1", expect.objectContaining({ headers: expect.objectContaining({ "X-Order-Key": key }) }));
  expect(fetcher.mock.calls[0][0]).not.toContain(key);
  window.history.replaceState({}, "", "/");
});
it("el mensaje mantiene totales separados por moneda", () => {
  const message = orderMessage({ ...order, totals: { ARS: 2501, USD: 10 } });
  expect(message).toContain("ARS"); expect(message).toContain("USD");
  expect(message).toContain("Solicitud CE-123");
});
it("atribuye únicamente el contexto explícito del mismo negocio y lo congela al confirmar", async () => {
  sessionStorage.clear(); window.history.replaceState({}, "", "/negocios/b1?campaign=c1");
  sessionStorage.setItem("cerca-campaign:c1", JSON.stringify({ businessId: "b1", token: "signed-context", expires: Date.now() + 60000 }));
  const bodies: string[] = [];
  vi.spyOn(globalThis, "fetch").mockImplementation(async (_url, init) => {
    if (init?.method === "POST") { bodies.push(String(init.body)); throw new Error("respuesta perdida"); }
    return Response.json({ items: [product], page: 1, hasNextPage: false });
  });
  render(<PublicCatalog id="b1" />);
  fireEvent.click(await screen.findByRole("button", { name: "Agregar Café" }));
  fireEvent.change(screen.getByLabelText("Tu nombre o alias"), { target: { value: "Ana" } });
  fireEvent.click(screen.getByRole("button", { name: "Confirmar solicitud" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("Reintentá");
  window.history.replaceState({}, "", "/negocios/b1"); sessionStorage.removeItem("cerca-campaign:c1");
  fireEvent.click(screen.getByRole("button", { name: "Reintentar solicitud" }));
  await waitFor(() => expect(bodies).toHaveLength(2));
  expect(JSON.parse(bodies[0]).campaignToken).toBe("signed-context"); expect(bodies[1]).toBe(bodies[0]);
  window.history.replaceState({}, "", "/");
});
it("una clave distinta en el mismo enlace vuelve a validar y retira el resumen anterior", async () => {
  window.history.replaceState({}, "", "/pedido/order1#clave=" + "a".repeat(64));
  vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(Response.json(order)).mockResolvedValueOnce(new Response(null, { status: 404 }));
  render(<OrderTracking id="order1" />);
  expect(await screen.findByText("CE-123")).toBeInTheDocument();
  window.history.replaceState({}, "", "/pedido/order1#clave=" + "b".repeat(64));
  fireEvent(window, new HashChangeEvent("hashchange"));
  expect(await screen.findByRole("alert")).toHaveTextContent("no es válido o venció");
  expect(screen.queryByText("CE-123")).not.toBeInTheDocument();
  window.history.replaceState({}, "", "/");
});
