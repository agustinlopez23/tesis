import { fireEvent, render, screen } from "@testing-library/react";
import { expect, it, vi } from "vitest";
import BusinessProfile from "../src/app/business-profile";

const profile = { id: "1", name: "Café del barrio", description: "Nuestra historia completa.",
  address: "Colón 123", category: "Gastronomía", whatsApp: "+5493515551234",
  photos: [{ slot: 1, caption: "Frente del café", width: 800, height: 600 },
    { slot: 3, caption: "Patio con mesas", width: 800, height: 600 }] };
it("muestra el perfil y permite elegir fotos con teclado o botones", async () => {
  vi.spyOn(globalThis, "fetch").mockResolvedValue(Response.json(profile));
  render(<BusinessProfile id="1" />);
  expect(await screen.findByRole("heading", { name: profile.name })).toBeInTheDocument();
  expect(screen.getByText(profile.description)).toBeInTheDocument();
  expect(new URL(screen.getByRole("img", { name: "Frente del café" }).getAttribute("src")!, window.location.origin).pathname).toBe("/api/marketplace/public/businesses/1/photos/1");
  fireEvent.click(screen.getByRole("button", { name: "Ver foto 2: Patio con mesas" }));
  expect(new URL(screen.getByRole("img", { name: "Patio con mesas" }).getAttribute("src")!, window.location.origin).pathname).toBe("/api/marketplace/public/businesses/1/photos/3");
  expect(screen.getByRole("link", { name: /Contactar por WhatsApp/ }).getAttribute("href")).toMatch(/^https:\/\/wa\.me\/5493515551234\?text=.*Caf%C3%A9%20del%20barrio/);
  expect(screen.getByRole("link", { name: /Volver a la guía/ })).toHaveAttribute("href", "/explorar");
});
it("distingue un perfil no disponible de un error recuperable", async () => {
  vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(new Response(null, { status: 503 }))
    .mockResolvedValueOnce(new Response(null, { status: 404 }));
  render(<BusinessProfile id="1" />);
  expect(await screen.findByRole("alert")).toHaveTextContent("No pudimos cargar");
  fireEvent.click(screen.getByRole("button", { name: "Reintentar" }));
  expect(await screen.findByRole("heading", { name: "Este negocio no está disponible." })).toBeInTheDocument();
  expect(screen.queryByRole("link", { name: /Contactar por WhatsApp/ })).not.toBeInTheDocument();
});
it("presenta un negocio sin fotos sin inventar imágenes", async () => {
  vi.spyOn(globalThis, "fetch").mockResolvedValue(Response.json({ ...profile, photos: [] }));
  render(<BusinessProfile id="1" />);
  expect(await screen.findByText("Este negocio todavía no compartió fotos.")).toBeInTheDocument();
  expect(screen.queryByRole("img")).not.toBeInTheDocument();
});
it("no pide el catálogo de un negocio sin plan Impulso", async () => {
  const fetcher = vi.spyOn(globalThis, "fetch").mockResolvedValue(Response.json({ ...profile, hasPublicCatalog: false }));
  render(<BusinessProfile id="1" />);
  expect(await screen.findByRole("heading", { name: profile.name })).toBeInTheDocument();
  expect(screen.queryByRole("heading", { name: "Productos del negocio" })).not.toBeInTheDocument();
  expect(fetcher.mock.calls.some(([url]) => String(url).includes("/products"))).toBe(false);
});
