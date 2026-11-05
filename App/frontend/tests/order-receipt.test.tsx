import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { expect, it, vi } from "vitest";
import OrderReceipt from "../src/app/order-receipt";

const key = "c".repeat(64);
const order = { id: "order1", reference: "CE-123", businessId: "b1", businessName: "Café local", whatsApp: "+5493515551234", customerName: "Ana",
  createdAt: "2026-10-05T00:00:00Z", accessExpiresAt: "2026-11-04T00:00:00Z", status: "pending", version: 1, contactReceivedAt: null, canContact: true,
  lines: [{ productId: "p1", name: "Café", unitPrice: 1250.5, currency: "ARS", quantity: 2 }], totals: { ARS: 2501 } };

it("oculta la clave del enlace privado por defecto y permite mostrarla", () => {
  render(<OrderReceipt order={order} accessKey={key} />);
  const field = screen.getByLabelText("Enlace privado") as HTMLInputElement;
  expect(field.value).not.toContain(key);
  expect(field.value).toMatch(/\/pedido\/order1#clave=•+$/);
  expect(screen.getByText(/funciona como una contraseña/)).toBeInTheDocument();
  const toggle = screen.getByRole("button", { name: "Mostrar enlace" });
  expect(toggle).toHaveAttribute("aria-pressed", "false");
  fireEvent.click(toggle);
  expect(field.value).toContain(`#clave=${key}`);
  const hide = screen.getByRole("button", { name: "Ocultar enlace" });
  expect(hide).toHaveAttribute("aria-pressed", "true");
  fireEvent.click(hide);
  expect(field.value).not.toContain(key);
});

it("copia el enlace real aunque esté oculto", async () => {
  const writeText = vi.fn().mockResolvedValue(undefined);
  Object.defineProperty(navigator, "clipboard", { value: { writeText }, configurable: true });
  render(<OrderReceipt order={order} accessKey={key} />);
  fireEvent.click(screen.getByRole("button", { name: "Copiar enlace privado" }));
  await waitFor(() => expect(writeText).toHaveBeenCalledWith(`${window.location.origin}/pedido/order1#clave=${key}`));
  expect(await screen.findByRole("status")).toHaveTextContent("Copiado.");
});

it("presenta la solicitud guardada como panel de éxito con WhatsApp como acción principal", () => {
  render(<OrderReceipt order={order} accessKey={key} />);
  expect(screen.getByRole("region", { name: "Resumen de solicitud" })).toHaveClass("order-receipt");
  expect(screen.getByText("Solicitud guardada")).toBeInTheDocument();
  expect(screen.getByRole("link", { name: "Abrir WhatsApp" })).toHaveClass("button-whatsapp");
  expect(screen.getByText("Pendiente")).toBeInTheDocument();
});
it("el enlace de consulta conserva la clave real", () => {
  render(<OrderReceipt order={order} accessKey={key} />);
  expect(screen.getByRole("link", { name: "Consultar mi solicitud" }).getAttribute("href")).toBe(`/pedido/order1#clave=${key}`);
});
