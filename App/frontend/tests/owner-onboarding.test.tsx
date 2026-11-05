import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { expect, it, vi } from "vitest";
import OwnerOnboarding from "../src/app/owner-onboarding";

Element.prototype.scrollIntoView = vi.fn();

it("explica las credenciales incorrectas sin revelar si existe la cuenta", async () => {
  vi.spyOn(globalThis, "fetch").mockImplementation(async () => new Response(null, { status: 401 }));
  render(<OwnerOnboarding />);
  fireEvent.click(screen.getByRole("button", { name: "Ya tengo cuenta" }));
  fireEvent.change(screen.getByLabelText("Correo"), { target: { value: "ana@example.test" } });
  fireEvent.change(screen.getByLabelText("Contraseña"), { target: { value: "clave-incorrecta" } });
  fireEvent.click(screen.getByRole("button", { name: "Ingresar" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("Revisá el correo y la contraseña e intentá nuevamente.");
  expect(screen.getByLabelText("Correo")).toHaveValue("ana@example.test");
});

it("muestra el error de registro sin borrar datos no sensibles", async () => {
  vi.spyOn(globalThis, "fetch").mockResolvedValue(new Response('{"message":"El correo ya está registrado."}', {
    status: 409, headers: { "Content-Type": "application/json" },
  }));
  render(<OwnerOnboarding />);
  fireEvent.change(screen.getByLabelText("Nombre"), { target: { value: "Ana" } });
  fireEvent.change(screen.getByLabelText("Correo"), { target: { value: "ana@example.test" } });
  fireEvent.change(screen.getByLabelText("Contraseña"), { target: { value: "clave-segura-123" } });
  fireEvent.click(screen.getByRole("button", { name: "Crear cuenta" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("El correo ya está registrado");
  expect(screen.getByLabelText("Nombre")).toHaveValue("Ana");
  expect(screen.getByLabelText("Correo")).toHaveValue("ana@example.test");
});

it("ingresa, muestra lista vacía y permite cerrar sesión", async () => {
  const fetcher = vi.spyOn(globalThis, "fetch")
    .mockResolvedValueOnce(new Response("null", { status: 401 }))
    .mockResolvedValueOnce(Response.json({ id: "1", email: "ana@example.test", displayName: "Ana", roles: ["business_owner"] }))
    .mockResolvedValueOnce(Response.json([]))
    .mockResolvedValueOnce(Response.json([{ id: 1, name: "Almacenes", slug: "almacenes" }]))
    .mockResolvedValueOnce(new Response(null, { status: 204 }));
  render(<OwnerOnboarding />);
  fireEvent.click(screen.getByRole("button", { name: "Ya tengo cuenta" }));
  fireEvent.change(screen.getByLabelText("Correo"), { target: { value: "ana@example.test" } });
  fireEvent.change(screen.getByLabelText("Contraseña"), { target: { value: "clave-segura-123" } });
  fireEvent.click(screen.getByRole("button", { name: "Ingresar" }));
  expect(await screen.findByText("Todavía no cargaste negocios.")).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Cerrar sesión" }));
  await waitFor(() => expect(screen.getByRole("button", { name: "Crear cuenta" })).toBeInTheDocument());
  expect(fetcher).toHaveBeenLastCalledWith("/api/marketplace/auth/logout", expect.objectContaining({ method: "POST" }));
});

it("guarda un negocio desde el panel y lo muestra en la lista", async () => {
  const saved = {
    id: "d71cd8d6-7245-492f-92de-4caa0cf11e50", name: "Almacén Norte", description: "Cercanía",
    address: "Córdoba", categoryId: 1, whatsApp: "+5493515551234", latitude: -31.41,
    longitude: -64.18, status: "draft",
  };
  vi.spyOn(globalThis, "fetch")
    .mockResolvedValueOnce(new Response("null", { status: 401 }))
    .mockResolvedValueOnce(Response.json({ id: "1", email: "ana@example.test", displayName: "Ana", roles: ["business_owner"] }))
    .mockResolvedValueOnce(Response.json([]))
    .mockResolvedValueOnce(Response.json([{ id: 1, name: "Almacenes", slug: "almacenes" }]))
    .mockResolvedValueOnce(Response.json(saved, { status: 201 }));
  render(<OwnerOnboarding />);
  fireEvent.click(screen.getByRole("button", { name: "Ya tengo cuenta" }));
  fireEvent.change(screen.getByLabelText("Correo"), { target: { value: "ana@example.test" } });
  fireEvent.change(screen.getByLabelText("Contraseña"), { target: { value: "clave-segura-123" } });
  fireEvent.click(screen.getByRole("button", { name: "Ingresar" }));
  await screen.findByText("Todavía no cargaste negocios.");
  fireEvent.change(screen.getByLabelText("Nombre del negocio"), { target: { value: saved.name } });
  fireEvent.change(screen.getByLabelText("Dirección"), { target: { value: saved.address } });
  fireEvent.change(screen.getByLabelText("WhatsApp"), { target: { value: saved.whatsApp } });
  fireEvent.change(screen.getByLabelText("Latitud"), { target: { value: String(saved.latitude) } });
  fireEvent.change(screen.getByLabelText("Longitud"), { target: { value: String(saved.longitude) } });
  fireEvent.click(screen.getByRole("button", { name: "Guardar negocio" }));
  expect(await screen.findByText("Almacén Norte")).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Editar" }));
  expect(screen.getByLabelText("WhatsApp")).toHaveValue(saved.whatsApp);
});

it("conserva el WhatsApp al recuperar un negocio y publicarlo", async () => {
  const saved = { id: "d71cd8d6-7245-492f-92de-4caa0cf11e50", name: "Almacén Norte", description: "",
    address: "Córdoba", categoryId: 1, whatsApp: "+5493515551234", latitude: -31.41, longitude: -64.18, status: "draft" };
  const fetcher = vi.spyOn(globalThis, "fetch")
    .mockResolvedValueOnce(Response.json({ id: "1", email: "ana@example.test", displayName: "Ana" }))
    .mockResolvedValueOnce(Response.json([saved]))
    .mockResolvedValueOnce(Response.json([{ id: 1, name: "Almacenes", slug: "almacenes" }]))
    .mockResolvedValueOnce(Response.json({ ...saved, status: "published" }));
  render(<OwnerOnboarding />);
  fireEvent.click(await screen.findByRole("button", { name: "Editar" }));
  expect(screen.getByLabelText("WhatsApp")).toHaveValue(saved.whatsApp);
  fireEvent.change(screen.getByLabelText("Estado"), { target: { value: "published" } });
  fireEvent.click(screen.getByRole("button", { name: "Guardar negocio" }));
  await waitFor(() => expect(fetcher).toHaveBeenLastCalledWith(`/api/marketplace/businesses/${saved.id}`,
    expect.objectContaining({ method: "PUT" })));
  expect(JSON.parse(fetcher.mock.calls.at(-1)?.[1]?.body as string)).toMatchObject({ status: "published", whatsApp: saved.whatsApp });
  expect(await screen.findByRole("status")).toHaveTextContent("Negocio publicado.");
  expect(screen.getByRole("heading", { name: "Editar negocio" })).toBeInTheDocument();
  expect(screen.getByRole("link", { name: "Ver perfil público" })).toHaveAttribute("href", "/negocios/" + saved.id);
  // Photos live in their own section; reaching it opens the panel without a second click.
  fireEvent.click(screen.getByRole("button", { name: "Fotos" }));
  expect(screen.getByRole("button", { name: "Cerrar fotos" })).toHaveAttribute("aria-expanded", "true");
});

const business = { id: "d71cd8d6-7245-492f-92de-4caa0cf11e50", name: "Almacén Norte", description: "", address: "Córdoba", categoryId: 1,
  whatsApp: "+5493515551234", latitude: -31.41, longitude: -64.18, status: "draft" };
function mockDashboard(item = business) {
  return vi.spyOn(globalThis, "fetch").mockImplementation(async (url) => {
    const path = String(url);
    if (path.endsWith("/auth/session")) return Response.json({ id: "1", email: "ana@example.test", displayName: "Ana" });
    if (path.endsWith("/business-categories")) return Response.json([{ id: 1, name: "Almacenes", slug: "almacenes" }]);
    if (path.endsWith("/businesses")) return Response.json([item]);
    if (path.includes("/orders")) return Response.json({ items: [], page: 1, hasNextPage: false });
    return Response.json([]);
  });
}

it("la navegación por secciones muestra solo la sección elegida y abre su panel", async () => {
  mockDashboard();
  render(<OwnerOnboarding />);
  const nav = await screen.findByRole("navigation", { name: "Secciones de tu negocio" });
  // Owners with businesses land on the summary of the first one.
  expect(within(nav).getByRole("button", { name: "Resumen" })).toHaveAttribute("aria-current", "true");
  expect(screen.getByRole("button", { name: "Almacén Norte" })).toHaveAttribute("aria-pressed", "true");
  expect(screen.getByRole("heading", { name: "Guía de inicio" })).toBeInTheDocument();
  expect(screen.queryByRole("button", { name: /^(Ver|Cerrar) solicitudes$/ })).not.toBeInTheDocument();
  expect(screen.queryByLabelText("Nombre del negocio")).not.toBeInTheDocument();
  fireEvent.click(within(nav).getByRole("button", { name: "Solicitudes" }));
  expect(within(nav).getByRole("button", { name: "Solicitudes" })).toHaveAttribute("aria-current", "true");
  expect(screen.queryByRole("heading", { name: "Guía de inicio" })).not.toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Cerrar solicitudes" })).toHaveAttribute("aria-expanded", "true");
  fireEvent.click(within(nav).getByRole("button", { name: "Negocio" }));
  expect(screen.getByLabelText("Nombre del negocio")).toHaveValue("Almacén Norte");
});

it("la guía de inicio cuenta los pasos completados y lleva a la sección correspondiente", async () => {
  mockDashboard();
  render(<OwnerOnboarding />);
  fireEvent.click(await screen.findByRole("button", { name: "Resumen" }));
  expect(await screen.findByText("2 de 5 pasos completados")).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Publicar negocio" }));
  expect(screen.getByLabelText("Estado")).toBeInTheDocument();
});

it("la guía reconoce un negocio publicado", async () => {
  mockDashboard({ ...business, status: "published" });
  render(<OwnerOnboarding />);
  fireEvent.click(await screen.findByRole("button", { name: "Almacén Norte" }));
  expect(await screen.findByText("3 de 5 pasos completados")).toBeInTheDocument();
  expect(screen.getByRole("link", { name: "Ver perfil público" })).toHaveAttribute("href", "/negocios/" + business.id);
});

it("mantiene el panel y permite reintentar cuando falla el cierre de sesión", async () => {
  vi.spyOn(globalThis, "fetch")
    .mockResolvedValueOnce(Response.json({ id: "1", email: "ana@example.test", displayName: "Ana" }))
    .mockResolvedValueOnce(Response.json([]))
    .mockResolvedValueOnce(Response.json([{ id: 1, name: "Almacenes", slug: "almacenes" }]))
    .mockRejectedValueOnce(new Error("Offline"))
    .mockResolvedValueOnce(new Response(null, { status: 204 }));
  render(<OwnerOnboarding />);
  fireEvent.click(await screen.findByRole("button", { name: "Cerrar sesión" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("No pudimos cerrar la sesión");
  expect(screen.getByRole("heading", { name: "Hola, Ana." })).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Cerrar sesión" }));
  expect(await screen.findByRole("button", { name: "Crear cuenta" })).toBeInTheDocument();
});
