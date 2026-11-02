import { fireEvent, render, screen, within } from "@testing-library/react";
import { beforeEach, expect, it, vi } from "vitest";
import BusinessProfile from "../src/app/business-profile";
import BusinessCatalog from "../src/app/business-catalog";
import CampaignPanel from "../src/app/campaign-panel";
import Explore from "../src/app/explorar/explore";
import AdminAudit from "../src/app/administracion/auditoria/audit";
import OrderReceipt from "../src/app/order-receipt";
import { whatsAppLink } from "../src/app/whatsapp";

const promotion = { id: "c1", businessId: "1", businessName: "Café del barrio", productId: null, hasImage: false, title: "2x1 en medialunas",
  description: "Todas las mañanas", conditions: "Hasta las 11", zoneId: "centro", endsAt: "2026-12-01T00:00:00Z", whatsApp: "+5493515551234", token: "protected" };
const profile = { id: "1", name: "Café del barrio", description: "Historia", address: "Colón 123", category: "Gastronomía",
  whatsApp: "+5493515551234", photos: [], promotions: [promotion] };
const campaign = { id: "c9", businessId: "b1", businessName: "Local", productId: "p1", photoSlot: null, hasImage: false, title: "Promo rechazada",
  description: "Paquete", conditions: "Hasta agotar", zoneId: "centro", startsAt: "2026-10-05T00:00:00Z", endsAt: "2026-10-06T00:00:00Z",
  status: "rejected", deliveryState: "rejected", version: 2, paymentReference: null, rejectionReason: "La imagen no muestra el producto" };
beforeEach(() => { sessionStorage.clear(); window.history.replaceState(null, "", "/"); });

it("HU-09: el contacto abre WhatsApp con un mensaje que nombra al negocio o al producto", () => {
  expect(whatsAppLink("+5493515551234", "Café & Té")).toBe("https://wa.me/5493515551234?text=" +
    encodeURIComponent("Hola, Café & Té. Te escribo desde Cerca para hacerte una consulta."));
  expect(decodeURIComponent(whatsAppLink("+5493515551234", "Café", "Medialunas").split("?text=")[1])).toContain("«Medialunas»");
});

it("HU-09: el perfil muestra las promociones vigentes y contacta con contexto", async () => {
  vi.spyOn(globalThis, "fetch").mockResolvedValue(Response.json(profile));
  render(<BusinessProfile id="1" />);
  const section = await screen.findByRole("region", { name: "Promociones vigentes" });
  expect(within(section).getByRole("heading", { name: promotion.title })).toBeInTheDocument();
  const contact = screen.getByRole("link", { name: /Contactar por WhatsApp/ });
  expect(contact.getAttribute("href")).toBe(whatsAppLink(profile.whatsApp, profile.name));
});

it("HU-09: sin promociones vigentes no muestra la sección", async () => {
  vi.spyOn(globalThis, "fetch").mockResolvedValue(Response.json({ ...profile, promotions: [] }));
  render(<BusinessProfile id="1" />);
  expect(await screen.findByRole("heading", { name: profile.name })).toBeInTheDocument();
  expect(screen.queryByRole("region", { name: "Promociones vigentes" })).not.toBeInTheDocument();
});

it("HU-15: el responsable compara las capacidades de Presencia e Impulso sin precios", async () => {
  vi.spyOn(globalThis, "fetch").mockImplementation(async (url) => String(url).endsWith("/plan")
    ? Response.json({ catalogEnabled: false, plan: "Presencia", periods: [] }) : Response.json([]));
  render(<BusinessCatalog id="b1" />);
  fireEvent.click(screen.getByRole("button", { name: "Administrar catálogo" }));
  const table = await screen.findByRole("table", { name: "Capacidades de los planes" });
  expect(within(table).getByRole("columnheader", { name: /Presencia/ })).toBeInTheDocument();
  expect(within(table).getByRole("columnheader", { name: /Impulso/ })).toBeInTheDocument();
  expect(within(table).getByRole("rowheader", { name: /Catálogo público de productos/ })).toBeInTheDocument();
  expect(table.textContent).not.toMatch(/\$|ARS|precio del plan/i);
  expect(screen.getByText(/Tu plan actual: Presencia/)).toBeInTheDocument();
});

it("HU-17: el responsable y la administración ven el motivo del rechazo", async () => {
  vi.spyOn(globalThis, "fetch").mockResolvedValue(Response.json({ items: [campaign], page: 1, hasNextPage: false }));
  const { unmount } = render(<CampaignPanel business="b1" />);
  fireEvent.click(screen.getByRole("button", { name: "Ver campañas" }));
  expect(await screen.findByText("Motivo del rechazo: La imagen no muestra el producto")).toBeInTheDocument();
  unmount();
  vi.spyOn(globalThis, "fetch").mockResolvedValue(Response.json({ items: [campaign], page: 1, hasNextPage: false }));
  render(<CampaignPanel admin />);
  expect(await screen.findByText("Motivo del rechazo: La imagen no muestra el producto")).toBeInTheDocument();
});

it("HU-20/21: el modo ranking muestra la versión aplicada", async () => {
  window.history.replaceState(null, "", "/explorar?zone=centro&category=Almacenes");
  Element.prototype.scrollIntoView = vi.fn();
  vi.spyOn(globalThis, "fetch").mockImplementation(async (url) => {
    const path = String(url);
    if (path.includes("business-categories")) return Response.json([{ id: 1, name: "Almacenes" }]);
    if (path.includes("ranking-snapshots")) return Response.json({ items: [{ id: "shop", name: "Almacén", description: "", address: "Colón 1", category: "Almacenes", whatsApp: "+5493515551234" }],
      cursor: "first", hasMore: false, nextCursor: null, expiresAt: new Date(Date.now() + 1800000).toISOString(), rankingVersion: "hash", policy: "equity-v1", measurement: { shop: "token" } });
    return Response.json({ items: [], hasMore: false });
  });
  render(<Explore rankingEnabled />);
  expect(await screen.findByText("Versión del ranking: equity-v1")).toBeInTheDocument();
});

it("accesibilidad: la tabla de auditoría es una región desplazable con foco", async () => {
  vi.spyOn(globalThis, "fetch").mockResolvedValue(Response.json({ items: [{ id: 1, actorEmail: "admin@example.test", targetType: "business",
    targetId: "b1", targetLabel: "Local", action: "suspend", reason: "Prueba", createdAt: "2026-11-02T00:00:00Z" }], page: 1, hasNextPage: false }));
  render(<AdminAudit />);
  const region = await screen.findByRole("region", { name: "Tabla de auditoría" });
  expect(region).toHaveAttribute("tabindex", "0");
});

it("accesibilidad: el comprobante usa el nivel de encabezado indicado", () => {
  const order = { id: "o1", reference: "CE-1", businessId: "b1", businessName: "Café", whatsApp: "+5493515551234", customerName: "Ana",
    createdAt: "2026-10-05T00:00:00Z", accessExpiresAt: "2026-11-04T00:00:00Z", status: "pending", version: 1, contactReceivedAt: null, canContact: true,
    lines: [], totals: {} };
  render(<OrderReceipt order={order} headingLevel={2} />);
  expect(screen.getByRole("heading", { level: 2, name: "CE-1" })).toBeInTheDocument();
});
