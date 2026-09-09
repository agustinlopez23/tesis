import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { expect, it, vi } from "vitest";
import OwnerOnboarding from "../src/app/owner-onboarding";

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
    address: "Córdoba", categoryId: 1, whatsapp: "+5493515551234", latitude: -31.41,
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
  fireEvent.change(screen.getByLabelText("WhatsApp"), { target: { value: saved.whatsapp } });
  fireEvent.change(screen.getByLabelText("Latitud"), { target: { value: String(saved.latitude) } });
  fireEvent.change(screen.getByLabelText("Longitud"), { target: { value: String(saved.longitude) } });
  fireEvent.click(screen.getByRole("button", { name: "Guardar negocio" }));
  expect(await screen.findByText("Almacén Norte")).toBeInTheDocument();
});
