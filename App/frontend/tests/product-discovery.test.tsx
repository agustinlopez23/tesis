import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { expect, it, vi } from "vitest";
import ProductDiscovery from "../src/app/product-discovery";
const product = { id: "p1", businessId: "b1", businessName: "Local", businessCategory: "Gastronomía", name: "Café", description: "250 g", category: "Café", price: 1250, currency: "ARS", version: 1, hasPhoto: false, distanceKm: null };
it("busca entre negocios, conserva filtros al paginar y enlaza el catálogo", async () => {
  window.history.replaceState({}, "", "/productos?q=cafe");
  const fetcher = vi.spyOn(globalThis, "fetch").mockImplementation(async url => Response.json(String(url).includes("business-categories") ? [{ name: "Gastronomía" }] : { items: [product], page: 1, hasNextPage: !String(url).includes("page=2"), order: "name" }));
  render(<ProductDiscovery />);
  expect(await screen.findByRole("link", { name: "Ver catálogo de Local" })).toHaveAttribute("href", "/negocios/b1#catalog-title");
  expect(fetcher.mock.calls.some(([url]) => String(url).includes("q=cafe"))).toBe(true);
  fireEvent.click(screen.getByRole("button", { name: "Siguiente" }));
  await waitFor(() => expect(window.location.search).toContain("page=2"));
  expect(window.location.search).toContain("q=cafe");
  fireEvent.change(screen.getByLabelText("Rubro del negocio"), { target: { value: "Gastronomía" } });
  await waitFor(() => expect(fetcher.mock.calls.some(([url]) => String(url).includes("category=Gastronom"))).toBe(true));
  expect(window.location.search).not.toContain("page=2");
  window.history.replaceState({}, "", "/");
});
it("retira resultados anteriores ante error y permite reintentar", async () => {
  window.history.replaceState({}, "", "/productos"); let count = 0;
  vi.spyOn(globalThis, "fetch").mockImplementation(async url => {
    if (String(url).includes("business-categories")) return Response.json([]);
    return ++count === 2 ? new Response(null, { status: 503 }) : Response.json({ items: [product], page: 1, hasNextPage: false, order: "name" });
  });
  render(<ProductDiscovery />);
  await screen.findByText("Café", { selector: "h2" });
  fireEvent.click(screen.getByRole("button", { name: "Actualizar productos" }));
  expect(await screen.findByRole("alert")).toBeInTheDocument();
  expect(screen.queryByRole("link", { name: "Ver catálogo de Local" })).not.toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Actualizar productos" }));
  expect(await screen.findByRole("link", { name: "Ver catálogo de Local" })).toBeInTheDocument();
  window.history.replaceState({}, "", "/");
});
