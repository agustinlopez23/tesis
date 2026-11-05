import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { expect, it, vi } from "vitest";
import AdminCategories from "../src/app/administracion/rubros/categories";
import AdminModeration from "../src/app/administracion/moderacion/moderation";
import AdminAudit from "../src/app/administracion/auditoria/audit";
import AdminNav from "../src/app/administracion/admin-nav";
import OwnerOnboarding from "../src/app/owner-onboarding";
import { DELETE, GET, POST, PUT } from "../src/app/api/marketplace/[...path]/route";

Element.prototype.scrollIntoView = vi.fn();
const id = "d71cd8d6-7245-492f-92de-4caa0cf11e50";
const calls = (fetcher: { mock: { calls: unknown[][] } }) => fetcher.mock.calls.map(([url, init]) => [String(url), (init as RequestInit | undefined)?.method ?? "GET", (init as RequestInit | undefined)?.body]);

it("la navegación de administración enlaza rubros, moderación y auditoría", () => {
  render(<AdminNav current="rubros" />);
  const nav = screen.getByRole("navigation", { name: "Administración" });
  expect(within(nav).getByRole("link", { name: "Rubros" })).toHaveAttribute("aria-current", "page");
  expect(within(nav).getByRole("link", { name: "Moderación" })).toHaveAttribute("href", "/administracion/moderacion");
  expect(within(nav).getByRole("link", { name: "Auditoría" })).toHaveAttribute("href", "/administracion/auditoria");
  expect(within(nav).getByRole("link", { name: "Planes" })).toHaveAttribute("href", "/administracion/planes");
});

it("el administrador crea un rubro y deshabilita otro", async () => {
  const fetcher = vi.spyOn(globalThis, "fetch")
    .mockResolvedValueOnce(Response.json([{ id: 1, name: "Almacenes", slug: "almacenes", isActive: true, businessCount: 2 }]))
    .mockResolvedValueOnce(Response.json({ id: 4, name: "Ferretería", slug: "ferreteria", isActive: true, businessCount: 0 }, { status: 201 }))
    .mockResolvedValueOnce(Response.json([{ id: 1, name: "Almacenes", slug: "almacenes", isActive: true, businessCount: 2 }, { id: 4, name: "Ferretería", slug: "ferreteria", isActive: true, businessCount: 0 }]))
    .mockResolvedValueOnce(Response.json({ id: 1, name: "Almacenes", slug: "almacenes", isActive: false, businessCount: 2 }))
    .mockResolvedValueOnce(Response.json([{ id: 1, name: "Almacenes", slug: "almacenes", isActive: false, businessCount: 2 }, { id: 4, name: "Ferretería", slug: "ferreteria", isActive: true, businessCount: 0 }]));
  render(<AdminCategories />);
  expect(await screen.findByText("Almacenes")).toBeInTheDocument();
  fireEvent.change(screen.getByLabelText("Nombre del nuevo rubro"), { target: { value: "Ferretería" } });
  fireEvent.click(screen.getByRole("button", { name: "Crear rubro" }));
  expect(await screen.findByText("Ferretería")).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Deshabilitar Almacenes" }));
  expect(await screen.findByRole("button", { name: "Habilitar Almacenes" })).toBeInTheDocument();
  expect(calls(fetcher)).toEqual(expect.arrayContaining([
    ["/api/marketplace/admin/categories", "POST", JSON.stringify({ name: "Ferretería" })],
    ["/api/marketplace/admin/categories/1", "PUT", JSON.stringify({ name: "Almacenes", isActive: false })],
  ]));
});

it("el administrador elimina un rubro sin uso y ve cuántos negocios bloquean otro", async () => {
  const list = [{ id: 1, name: "Almacenes", slug: "almacenes", isActive: true, businessCount: 2 }, { id: 4, name: "Ferretería", slug: "ferreteria", isActive: true, businessCount: 0 }];
  const fetcher = vi.spyOn(globalThis, "fetch")
    .mockResolvedValueOnce(Response.json(list))
    .mockResolvedValueOnce(new Response(null, { status: 204 }))
    .mockResolvedValueOnce(Response.json([list[0]]))
    .mockResolvedValueOnce(Response.json({ code: "category_in_use", usageCount: 2, message: "No se puede eliminar el rubro: lo usan 2 negocios." }, { status: 409 }));
  render(<AdminCategories />);
  fireEvent.click(await screen.findByRole("button", { name: "Eliminar Ferretería" }));
  expect(fetcher).toHaveBeenCalledTimes(1);
  fireEvent.click(screen.getByRole("button", { name: "Confirmar eliminación de Ferretería" }));
  expect(await screen.findByRole("status")).toHaveTextContent("Rubro eliminado.");
  await waitFor(() => expect(screen.queryByText("Ferretería")).not.toBeInTheDocument());
  fireEvent.click(screen.getByRole("button", { name: "Eliminar Almacenes" }));
  fireEvent.click(screen.getByRole("button", { name: "Confirmar eliminación de Almacenes" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("No se puede eliminar el rubro: lo usan 2 negocios.");
  expect(screen.getByText("Almacenes")).toBeInTheDocument();
  expect(calls(fetcher)).toEqual(expect.arrayContaining([
    ["/api/marketplace/admin/categories/4", "DELETE", undefined],
    ["/api/marketplace/admin/categories/1", "DELETE", undefined],
  ]));
});

it("una cuenta sin rol administrador no ve los rubros", async () => {
  vi.spyOn(globalThis, "fetch").mockResolvedValue(new Response(null, { status: 403 }));
  render(<AdminCategories />);
  expect(await screen.findByRole("alert")).toHaveTextContent("cuenta administradora");
  expect(screen.queryByRole("button", { name: "Crear rubro" })).not.toBeInTheDocument();
});

it("suspender un negocio exige motivo y refresca el estado", async () => {
  const fetcher = vi.spyOn(globalThis, "fetch")
    .mockResolvedValueOnce(Response.json([{ id, email: "ana@example.test", displayName: "Ana", isSuspended: false, businessCount: 1 }]))
    .mockResolvedValueOnce(Response.json([{ id, name: "Café local", ownerEmail: "ana@example.test", category: "Gastronomía", status: "published", isSuspended: false }]))
    .mockResolvedValueOnce(Response.json({ saved: true }))
    .mockResolvedValueOnce(Response.json([{ id, name: "Café local", ownerEmail: "ana@example.test", category: "Gastronomía", status: "published", isSuspended: true }]));
  render(<AdminModeration />);
  expect(await screen.findByText("ana@example.test")).toBeInTheDocument();
  fireEvent.click(screen.getByRole("tab", { name: "Negocios" }));
  fireEvent.click(await screen.findByRole("button", { name: "Suspender Café local" }));
  const reason = screen.getByLabelText("Motivo");
  expect(reason).toBeRequired();
  fireEvent.change(reason, { target: { value: "Datos falsos" } });
  fireEvent.click(screen.getByRole("button", { name: "Confirmar suspensión" }));
  expect(await screen.findByRole("status")).toHaveTextContent("Negocio suspendido");
  expect(await screen.findByRole("button", { name: "Reactivar Café local" })).toBeInTheDocument();
  expect(calls(fetcher)).toContainEqual([`/api/marketplace/admin/moderation/businesses/${id}/suspend`, "POST", JSON.stringify({ reason: "Datos falsos" })]);
});

it("las pestañas de estado filtran la lista de moderación sin pedir datos nuevos", async () => {
  const fetcher = vi.spyOn(globalThis, "fetch").mockResolvedValue(Response.json([
    { id, email: "ana@example.test", displayName: "Ana", isSuspended: false, businessCount: 1 },
    { id: "a2", email: "bruno@example.test", displayName: "Bruno", isSuspended: true, businessCount: 0 },
  ]));
  render(<AdminModeration />);
  expect(await screen.findByText("ana@example.test")).toBeInTheDocument();
  const filters = screen.getByRole("navigation", { name: "Filtrar por estado" });
  expect(within(filters).getByRole("button", { name: /^Todas/ })).toHaveAttribute("aria-current", "true");
  expect(screen.getByText("bruno@example.test")).toBeInTheDocument();
  fireEvent.click(within(filters).getByRole("button", { name: /^Suspendidas/ }));
  expect(screen.queryByText("ana@example.test")).not.toBeInTheDocument();
  expect(screen.getByText("bruno@example.test")).toBeInTheDocument();
  fireEvent.click(within(filters).getByRole("button", { name: /^Activas/ }));
  expect(screen.getByText("ana@example.test")).toBeInTheDocument();
  expect(screen.queryByText("bruno@example.test")).not.toBeInTheDocument();
  expect(fetcher).toHaveBeenCalledTimes(1);
});

it("muestra el error de moderación de la API", async () => {
  vi.spyOn(globalThis, "fetch")
    .mockResolvedValueOnce(Response.json([{ id, email: "ana@example.test", displayName: "Ana", isSuspended: false, businessCount: 0 }]))
    .mockResolvedValueOnce(Response.json({ message: "El estado ya cambió o la cuenta es administradora. Actualizá la lista." }, { status: 409 }));
  render(<AdminModeration />);
  fireEvent.click(await screen.findByRole("button", { name: "Suspender ana@example.test" }));
  fireEvent.change(screen.getByLabelText("Motivo"), { target: { value: "Reclamos" } });
  fireEvent.click(screen.getByRole("button", { name: "Confirmar suspensión" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("El estado ya cambió");
});

it("la auditoría lista acciones con actor, motivo y destino", async () => {
  vi.spyOn(globalThis, "fetch").mockResolvedValue(Response.json({ page: 1, hasNextPage: false, items: [
    { id: 2, actorEmail: "admin@example.test", targetType: "business", targetId: id, targetLabel: "Café local", action: "suspend", reason: "Datos falsos", createdAt: "2026-11-01T12:00:00Z" },
  ] }));
  render(<AdminAudit />);
  const row = (await screen.findByText("Café local")).closest("tr")!;
  expect(within(row).getByText("Suspensión")).toBeInTheDocument();
  expect(within(row).getByText("Negocio")).toBeInTheDocument();
  expect(within(row).getByText("admin@example.test")).toBeInTheDocument();
  expect(within(row).getByText("Datos falsos")).toBeInTheDocument();
});

it("el formulario del negocio oculta rubros deshabilitados salvo el actual", async () => {
  vi.spyOn(globalThis, "fetch").mockImplementation(async (input) => {
    const url = String(input);
    if (url.endsWith("/auth/session")) return Response.json({ id: "1", email: "ana@example.test", displayName: "Ana", roles: ["business_owner"] });
    if (url.endsWith("/business-categories")) return Response.json([
      { id: 1, name: "Almacenes", slug: "almacenes", isActive: true },
      { id: 2, name: "Gastronomía", slug: "gastronomia", isActive: false },
    ]);
    if (url.endsWith("/businesses")) return Response.json([{ id, name: "Café local", description: "", address: "", categoryId: 2, whatsApp: "+5493515551234", latitude: -31.4, longitude: -64.1, status: "published" }]);
    return new Response(null, { status: 404 });
  });
  render(<OwnerOnboarding />);
  // The dashboard opens on the summary of the existing business; the rule applies to the new-business form.
  fireEvent.click(await screen.findByRole("button", { name: "Nuevo negocio" }));
  const select = await screen.findByLabelText("Rubro");
  await waitFor(() => expect(within(select).queryByRole("option", { name: "Gastronomía" })).not.toBeInTheDocument());
  expect(within(select).getByRole("option", { name: "Almacenes" })).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: /^Editar/ }));
  expect(within(screen.getByLabelText("Rubro")).getByRole("option", { name: "Gastronomía (deshabilitado)" })).toBeInTheDocument();
});

it("el proxy permite las rutas de administración y sus filtros", async () => {
  vi.stubEnv("APP_ORIGIN", "http://localhost:3000");
  vi.stubEnv("API_BASE_URL", "http://localhost:5080");
  const fetcher = vi.spyOn(globalThis, "fetch").mockImplementation(async () => Response.json({}));
  const context = (path: string[]) => ({ params: Promise.resolve({ path }) });
  const headers = { Origin: "http://localhost:3000", "X-Requested-With": "marketplace" };
  await GET(new Request("http://localhost:3000/api/marketplace/admin/moderation/products?q=caf%C3%A9&page=2"), context(["admin", "moderation", "products"]));
  expect(fetcher).toHaveBeenLastCalledWith("http://localhost:5080/admin/moderation/products?q=caf%C3%A9&page=2", expect.anything());
  await GET(new Request("http://localhost:3000/api/marketplace/admin/audit?page=3"), context(["admin", "audit"]));
  expect(fetcher).toHaveBeenLastCalledWith("http://localhost:5080/admin/audit?page=3", expect.anything());
  expect((await GET(new Request("http://localhost:3000/api/marketplace/admin/categories"), context(["admin", "categories"]))).status).toBe(200);
  expect((await POST(new Request("http://localhost:3000/api/marketplace/admin/categories", { method: "POST", headers, body: "{}" }), context(["admin", "categories"]))).status).toBe(200);
  expect((await PUT(new Request("http://localhost:3000/api/marketplace/admin/categories/4", { method: "PUT", headers, body: "{}" }), context(["admin", "categories", "4"]))).status).toBe(200);
  expect((await DELETE(new Request("http://localhost:3000/api/marketplace/admin/categories/4", { method: "DELETE", headers }), context(["admin", "categories", "4"]))).status).toBe(200);
  expect((await POST(new Request(`http://localhost:3000/api/marketplace/admin/moderation/accounts/${id}/suspend`, { method: "POST", headers, body: "{}" }), context(["admin", "moderation", "accounts", id, "suspend"]))).status).toBe(200);
  expect((await POST(new Request(`http://localhost:3000/api/marketplace/admin/moderation/widgets/${id}/suspend`, { method: "POST", headers, body: "{}" }), context(["admin", "moderation", "widgets", id, "suspend"]))).status).toBe(404);
});
