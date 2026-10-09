import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { expect, it, vi } from "vitest";
import BusinessCatalog from "../src/app/business-catalog";

const product = { id: "p1", name: "Café molido", description: "250 g", category: "Café", price: 1234.5,
  currency: "ARS", available: true, status: "published", version: 1, hasPhoto: false };
it("exige guardar cambios de texto antes de subir una foto para no perderlos", async () => {
  vi.spyOn(globalThis, "fetch").mockImplementation(async (url) => String(url).endsWith("/plan")
    ? Response.json({ catalogEnabled: true, plan: "Impulso", periods: [] }) : Response.json([product]));
  render(<BusinessCatalog id="b1" />);
  fireEvent.click(screen.getByRole("button", { name: "Administrar catálogo" }));
  fireEvent.click(await screen.findByRole("button", { name: "Editar Café molido" }));
  expect(screen.getByLabelText("Foto del producto")).toBeEnabled();
  fireEvent.change(screen.getByLabelText("Nombre del producto"), { target: { value: "Nuevo nombre sin guardar" } });
  expect(screen.getByLabelText("Foto del producto")).toBeDisabled();
});
it("conserva lectura privada cuando el plan vence y bloquea edición", async () => {
  vi.spyOn(globalThis, "fetch").mockImplementation(async (url) => String(url).endsWith("/plan")
    ? Response.json({ catalogEnabled: false, plan: "Presencia", periods: [] }) : Response.json([product]));
  render(<BusinessCatalog id="b1" />);
  fireEvent.click(screen.getByRole("button", { name: "Administrar catálogo" }));
  expect(await screen.findByText("Café molido")).toBeInTheDocument();
  expect(screen.getByText(/Tus productos se conservan/)).toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Editar Café molido" })).toBeDisabled();
});
it("no pierde el borrador cuando guardar falla y no genera otro identificador", async () => {
  const fetcher = vi.spyOn(globalThis, "fetch").mockImplementation(async (url, init) => {
    if (init?.method === "PUT") return Response.json({ message: "Servicio temporalmente no disponible" }, { status: 503 });
    return String(url).endsWith("/plan") ? Response.json({ catalogEnabled: true, plan: "Impulso", periods: [] }) : Response.json([]);
  });
  render(<BusinessCatalog id="b1" />);
  fireEvent.click(screen.getByRole("button", { name: "Administrar catálogo" }));
  fireEvent.click(await screen.findByRole("button", { name: "Nuevo producto" }));
  fireEvent.change(screen.getByLabelText("Nombre del producto"), { target: { value: "Café" } });
  fireEvent.change(screen.getByLabelText("Categoría del producto"), { target: { value: "Bebidas" } });
  fireEvent.change(screen.getByLabelText("Precio"), { target: { value: "1200.50" } });
  fireEvent.click(screen.getByRole("button", { name: "Guardar producto" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("Servicio temporalmente");
  expect(screen.getByLabelText("Nombre del producto")).toHaveValue("Café");
  fireEvent.click(screen.getByRole("button", { name: "Guardar producto" }));
  await waitFor(() => expect(fetcher.mock.calls.filter(([, init]) => init?.method === "PUT")).toHaveLength(2));
  const writes = fetcher.mock.calls.filter(([, init]) => init?.method === "PUT");
  expect(writes[0][0]).toBe(writes[1][0]);
});
