import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, expect, it, vi } from "vitest";
import Explore from "../src/app/explorar/explore";

const business = { id: "shop", name: "Almacén del Patio", description: "Productos locales",
  address: "Colón 123", category: "Almacenes", whatsApp: "+5493515551234", distanceKm: .8 };
const listed = { ...business, id: "listed", name: "Negocio de la guía" };
const expiresAt = () => new Date(Date.now() + 30 * 60 * 1000).toISOString();
const rankedPage = (extra = {}) => Response.json({ items: [business], cursor: "first", hasMore: true, nextCursor: "second",
  expiresAt: expiresAt(), rankingVersion: "policy", policy: "equity-v1", measurement: { shop: "protected" }, ...extra });
beforeEach(() => {
  window.history.replaceState(null, "", "/explorar");
  Element.prototype.scrollIntoView = vi.fn();
});
function mockApi(ranking: () => Response) {
  return vi.spyOn(globalThis, "fetch").mockImplementation(async (url) => {
    const path = String(url);
    if (path.includes("business-categories")) return Response.json([{ id: 1, name: "Almacenes" }]);
    if (path.includes("ranking-snapshots")) return ranking();
    return Response.json({ items: [listed], hasMore: false });
  });
}

it("ordena por el ranking hiperlocal con equidad cuando hay zona y rubro", async () => {
  window.history.replaceState(null, "", "/explorar?zone=centro&category=Almacenes");
  const fetcher = mockApi(() => rankedPage());
  render(<Explore rankingEnabled />);
  expect(await screen.findByRole("heading", { name: business.name })).toBeInTheDocument();
  expect(screen.getByText(/ranking hiperlocal con equidad/i, { selector: "p" })).toBeInTheDocument();
  const call = fetcher.mock.calls.find(([url]) => String(url).endsWith("public/ranking-snapshots"))!;
  expect(call[1]?.method).toBe("POST");
  expect(JSON.parse(String(call[1]?.body))).toMatchObject({ zoneId: "centro", categoryId: 1, radiusKm: 4 });
  expect(screen.queryByRole("heading", { name: listed.name })).not.toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Siguiente" }));
  await waitFor(() => expect(fetcher).toHaveBeenLastCalledWith(
    "/api/marketplace/public/ranking-snapshots/page?cursor=second", expect.objectContaining({ method: "GET" })));
});

it("invita a elegir zona y rubro y conserva la guía habitual", async () => {
  window.history.replaceState(null, "", "/explorar?category=Almacenes");
  const fetcher = mockApi(() => rankedPage());
  render(<Explore rankingEnabled />);
  expect(await screen.findByRole("heading", { name: listed.name })).toBeInTheDocument();
  expect(screen.getByText(/Elegí una zona y un rubro/)).toBeInTheDocument();
  expect(fetcher.mock.calls.some(([url]) => String(url).includes("ranking-snapshots"))).toBe(false);
});

it("vuelve a la guía habitual si el ranking no está disponible", async () => {
  window.history.replaceState(null, "", "/explorar?zone=centro&category=Almacenes");
  mockApi(() => new Response(null, { status: 404 }));
  render(<Explore rankingEnabled />);
  expect(await screen.findByRole("heading", { name: listed.name })).toBeInTheDocument();
  expect(screen.getByText(/Te mostramos la guía habitual/)).toBeInTheDocument();
});

it("no consulta el ranking cuando está deshabilitado", async () => {
  window.history.replaceState(null, "", "/explorar?zone=centro&category=Almacenes");
  const fetcher = mockApi(() => rankedPage());
  render(<Explore />);
  expect(await screen.findByRole("heading", { name: listed.name })).toBeInTheDocument();
  expect(fetcher.mock.calls.some(([url]) => String(url).includes("ranking-snapshots"))).toBe(false);
  expect(screen.queryByText(/Elegí una zona y un rubro/)).not.toBeInTheDocument();
});
