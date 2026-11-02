import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { expect, it, vi } from "vitest";
import OwnerOnboarding from "../src/app/owner-onboarding";
import ProductDiscovery from "../src/app/product-discovery";
import PublicCatalog from "../src/app/public-catalog";
import BusinessInventory from "../src/app/business-inventory";

Element.prototype.scrollIntoView = vi.fn();
const product = { id: "p1", businessId: "b1", businessName: "Local", businessCategory: "Almacenes", name: "Yerba", description: "500 g", category: "Almacén",
  price: 1250, currency: "ARS", version: 1, hasPhoto: false, distanceKm: 0.4, lowStock: true };

it("HU-01 informa que la cuenta suspendida no está habilitada", async () => {
  vi.spyOn(globalThis, "fetch").mockImplementation(async () => Response.json(
    { code: "account_suspended", message: "Tu cuenta no está habilitada. Contactá a la administración." }, { status: 403 }));
  render(<OwnerOnboarding />);
  fireEvent.click(screen.getByRole("button", { name: "Ya tengo cuenta" }));
  fireEvent.change(screen.getByLabelText("Correo"), { target: { value: "ana@example.test" } });
  fireEvent.change(screen.getByLabelText("Contraseña"), { target: { value: "clave-correcta-123" } });
  fireEvent.click(screen.getByRole("button", { name: "Ingresar" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("Tu cuenta no está habilitada");
});

it("HU-08 indica el orden por ranking con equidad y la disponibilidad baja", async () => {
  window.history.replaceState({}, "", "/productos?q=yerba&zone=centro&radiusKm=2");
  const fetcher = vi.spyOn(globalThis, "fetch").mockImplementation(async url => Response.json(String(url).includes("business-categories") ? []
    : { items: [product], page: 1, hasNextPage: false, order: "ranking", policy: "equity-v1" }));
  render(<ProductDiscovery />);
  expect(await screen.findByText(/Ordenado por ranking hiperlocal con equidad \(equity-v1\)/)).toBeInTheDocument();
  expect(screen.getByText("Disponibilidad baja")).toBeInTheDocument();
  expect(fetcher.mock.calls.some(([url]) => String(url).includes("zone=centro"))).toBe(true);
  window.history.replaceState({}, "", "/");
});

it("HU-08 sin resultados en la zona ofrece ampliar el radio y buscar en toda la ciudad", async () => {
  window.history.replaceState({}, "", "/productos?q=yerba&zone=centro&radiusKm=2");
  const fetcher = vi.spyOn(globalThis, "fetch").mockImplementation(async url => Response.json(String(url).includes("business-categories") ? []
    : { items: [], page: 1, hasNextPage: false, order: "distance", policy: null }));
  render(<ProductDiscovery />);
  expect(await screen.findByText(/No encontramos productos disponibles en esta zona/)).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Ampliar radio a 5 km" }));
  await waitFor(() => expect(window.location.search).toContain("radiusKm=5"));
  await waitFor(() => expect(fetcher.mock.calls.some(([url]) => String(url).includes("radiusKm=5"))).toBe(true));
  fireEvent.click(await screen.findByRole("button", { name: "Buscar en toda la ciudad" }));
  await waitFor(() => expect(window.location.search).not.toContain("zone="));
  expect(window.location.search).toContain("q=yerba");
  window.history.replaceState({}, "", "/");
});

it("HU-06 el catálogo público muestra disponibilidad baja", async () => {
  vi.spyOn(globalThis, "fetch").mockResolvedValue(Response.json({ items: [{ id: "p1", name: "Yerba", description: "500 g", category: "Almacén", price: 1250,
    currency: "ARS", available: true, status: "published", version: 1, hasPhoto: false, lowStock: true }], page: 1, hasNextPage: false }));
  render(<PublicCatalog id="b1" />);
  expect(await screen.findByText("Disponibilidad baja")).toBeInTheDocument();
});

it("HU-06 el inventario marca saldo bajo e informa el saldo disponible al rechazar un ajuste", async () => {
  sessionStorage.clear();
  vi.spyOn(globalThis, "fetch").mockImplementation(async (url, init) => init?.method === "PUT"
    ? Response.json({ code: "insufficient_stock", availableQuantity: 2, message: "El ajuste dejaría el saldo por debajo de cero. Saldo disponible: 2 unidades." }, { status: 409 })
    : Response.json(String(url).includes("movements") ? { items: [], page: 1, hasNextPage: false }
      : [{ productId: "p1", name: "Yerba", quantity: 2, version: 1, lowStock: true }]));
  render(<BusinessInventory business="b1" />);
  fireEvent.click(screen.getByRole("button", { name: "Ver inventario" }));
  expect(await screen.findByText("Disponibilidad baja")).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Ajustar Yerba" }));
  fireEvent.change(screen.getByLabelText("Unidades a sumar o restar"), { target: { value: "-5" } });
  fireEvent.change(screen.getByLabelText("Motivo del ajuste"), { target: { value: "Rotura" } });
  fireEvent.click(screen.getByRole("button", { name: "Guardar ajuste" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("Saldo disponible: 2 unidades");
});
