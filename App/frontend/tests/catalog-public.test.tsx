import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { expect, it, vi } from "vitest";
import PublicCatalog from "../src/app/public-catalog";
import { GET, PUT } from "../src/app/api/marketplace/[...path]/route";

const product = { id: "p1", name: "Café molido", description: "250 g", category: "Café", price: 1234.5,
  currency: "ARS", available: true, status: "published", version: 1, hasPhoto: false };
it("retira productos visibles cuando una nueva consulta encuentra el plan vencido", async () => {
  vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(Response.json({ items: [product], page: 1, hasNextPage: false }))
    .mockResolvedValueOnce(new Response(null, { status: 404 }));
  render(<PublicCatalog id="b1" />);
  expect(await screen.findByText("Café molido")).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Actualizar catálogo" }));
  await waitFor(() => expect(screen.queryByText("Café molido")).not.toBeInTheDocument());
});
it("proxy conserva filtros y versión pero rechaza escritura pública", async () => {
  vi.stubEnv("APP_ORIGIN", "http://localhost:3000");
  vi.stubEnv("API_BASE_URL", "http://localhost:5080");
  const fetcher = vi.spyOn(globalThis, "fetch").mockResolvedValue(Response.json({}));
  const id = "d71cd8d6-7245-492f-92de-4caa0cf11e50";
  const path = ["public", "businesses", id, "products"];
  const url = "http://localhost:3000/api/marketplace/" + path.join("/");
  expect((await GET(new Request(url + "?page=2&q=cafe"), { params: Promise.resolve({ path }) })).status).toBe(200);
  expect(fetcher).toHaveBeenCalledWith(`http://localhost:5080/${path.join("/")}?page=2&q=cafe`, expect.anything());
  expect((await PUT(new Request(url, { method: "PUT" }), { params: Promise.resolve({ path }) })).status).toBe(404);
});
