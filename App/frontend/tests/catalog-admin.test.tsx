import { fireEvent, render, screen } from "@testing-library/react";
import { expect, it, vi } from "vitest";
import AdminPlans from "../src/app/administracion/planes/plans";

it("el administrador puede reintentar cargar negocios después de un error", async () => {
  vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(new Response(null, { status: 503 }))
    .mockResolvedValueOnce(Response.json([{ id: "b1", name: "Café local", ownerEmail: "owner@example.test" }]));
  render(<AdminPlans />);
  expect(await screen.findByRole("alert")).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Reintentar carga" }));
  expect(await screen.findByText("Café local")).toBeInTheDocument();
});
it("una cuenta sin rol administrador no ve controles de activación", async () => {
  vi.spyOn(globalThis, "fetch").mockResolvedValue(new Response(null, { status: 403 }));
  render(<AdminPlans />);
  expect(await screen.findByRole("alert")).toHaveTextContent("cuenta administradora");
  expect(screen.queryByRole("button", { name: "Registrar vigencia" })).not.toBeInTheDocument();
});
