import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { StrictMode } from "react";
import { beforeEach, expect, it, vi } from "vitest";
import RankingPreview from "../src/app/revista/ranking-preview";

const business = { id: "shop", name: "Almacén del Patio", description: "Productos locales", category: "Almacenes",
  address: "Colón 123", whatsApp: "+5493515551234", distanceKm: .8 };
let expiresAt: string;
const page = (extra = {}) => Response.json({ items: [business], cursor: "first", hasMore: true, nextCursor: "second",
  expiresAt, rankingVersion: "policy", measurement: { shop: "protected" }, ...extra });
const categories = () => Response.json([{ id: 1, name: "Almacenes" }, { id: 2, name: "Gastronomía" }]);
beforeEach(() => {
  window.history.replaceState(null, "", "/revista"); sessionStorage.clear();
  expiresAt = new Date(Date.now() + 30 * 60 * 1000).toISOString();
});
function mockApi(...pages: Response[]) {
  return vi.spyOn(globalThis, "fetch").mockImplementation(async url => {
    if (String(url).endsWith("business-categories")) return categories();
    if (String(url).endsWith("public/events")) return Response.json({});
    const next = pages.shift();
    if (!next) throw new Error("Unexpected request");
    return next;
  });
}
async function discover() {
  await screen.findByRole("option", { name: "Almacenes" });
  fireEvent.change(screen.getByLabelText("Rubro"), { target: { value: "1" } });
  fireEvent.click(screen.getByRole("button", { name: "Descubrir negocios" }));
}

it("crea solo al elegir y confirmar, reutiliza las tarjetas y conserva tokens de atribución", async () => {
  const fetcher = mockApi(page());
  render(<StrictMode><RankingPreview /></StrictMode>);
  expect(screen.getByRole("button", { name: "Descubrir negocios" })).toBeDisabled();
  await discover();
  expect(await screen.findByRole("heading", { name: business.name })).toBeInTheDocument();
  expect(fetcher.mock.calls.filter(([, options]) => options?.method === "POST")).toHaveLength(1);
  expect(screen.getByText("Revista · vista previa")).toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Anterior" })).toBeDisabled();
  expect(screen.getByRole("link", { name: /Contactar.*WhatsApp/ })).toHaveAttribute("href", "https://wa.me/5493515551234");
  const profile = screen.getByRole("link", { name: "Conocer el negocio" });
  profile.addEventListener("click", event => event.preventDefault());
  fireEvent.click(profile);
  expect(sessionStorage.getItem("cerca:discovery-referral:v1")).toContain("protected");
});

it("permite atravesar una página vacía y volver con datos revalidados", async () => {
  mockApi(page(), page({ items: [], cursor: "second", nextCursor: "third" }), page({ items: [], hasMore: false, nextCursor: null }));
  render(<RankingPreview />);
  await discover();
  await screen.findByRole("heading", { name: business.name });
  fireEvent.click(screen.getByRole("button", { name: "Siguiente" }));
  expect(await screen.findByText("No hay negocios disponibles en esta página.")).toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Siguiente" })).toBeEnabled();
  fireEvent.click(screen.getByRole("button", { name: "Anterior" }));
  expect(await screen.findByText("No encontramos negocios disponibles.")).toBeInTheDocument();
  expect(screen.queryByRole("heading", { name: business.name })).not.toBeInTheDocument();
});

it.each([429, 503, 502])("ofrece guía y reintento de la misma consulta ante %s", async status => {
  const fetcher = mockApi(new Response(null, { status }), page());
  render(<RankingPreview />);
  await discover();
  await screen.findByRole("alert");
  expect(screen.getByRole("link", { name: "Ir a la guía de negocios" })).toHaveAttribute("href", "/explorar");
  fireEvent.click(screen.getByRole("button", { name: "Reintentar" }));
  await screen.findByRole("heading", { name: business.name });
  const posts = fetcher.mock.calls.filter(([, options]) => options?.method === "POST");
  expect(posts[1][1]?.body).toBe(posts[0][1]?.body);
});

it("ante 410 exige actualizar, sin reemplazar silenciosamente la lista", async () => {
  const fetcher = mockApi(new Response(null, { status: 410 }), page());
  render(<RankingPreview />);
  await discover();
  await screen.findByText("Esta lista ya no está disponible.");
  expect(screen.queryByRole("button", { name: "Reintentar" })).not.toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Actualizar resultados" }));
  await screen.findByRole("heading", { name: business.name });
  const posts = fetcher.mock.calls.filter(([, options]) => options?.method === "POST");
  expect(posts[1][1]?.body).not.toBe(posts[0][1]?.body);
});

it("ante 404 ofrece la guía y al cambiar filtros retira la lista anterior", async () => {
  mockApi(page(), new Response(null, { status: 404 }));
  render(<RankingPreview />);
  await discover();
  await screen.findByRole("heading", { name: business.name });
  fireEvent.change(screen.getByLabelText("Zona"), { target: { value: "alberdi" } });
  expect(screen.queryByRole("heading", { name: business.name })).not.toBeInTheDocument();
  expect(screen.getByLabelText("Zona")).toHaveValue("alberdi");
  fireEvent.click(screen.getByRole("button", { name: "Descubrir negocios" }));
  await screen.findByText("La revista está pausada.");
  expect(screen.queryByRole("button", { name: "Reintentar" })).not.toBeInTheDocument();
});

it("recupera carga de rubros sin inventar opciones ni crear una lista", async () => {
  const fetcher = vi.spyOn(globalThis, "fetch").mockRejectedValueOnce(new Error("Offline")).mockResolvedValueOnce(categories());
  render(<RankingPreview />);
  await screen.findByText("No pudimos cargar los rubros.");
  expect(screen.getByLabelText("Rubro")).toBeDisabled();
  fireEvent.click(screen.getByRole("button", { name: "Reintentar rubros" }));
  await waitFor(() => expect(screen.getByLabelText("Rubro")).toBeEnabled());
  expect(fetcher.mock.calls.every(([url]) => String(url).endsWith("business-categories"))).toBe(true);
});
