import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, expect, it, vi } from "vitest";
import Explore from "../src/app/explorar/page";

const business = { id: "1", name: "Almacén Córdoba", description: "Productos locales",
  address: "Colón 123", category: "Almacenes", whatsApp: "+5493515551234" };
beforeEach(() => {
  window.history.replaceState(null, "", "/explorar");
  Element.prototype.scrollIntoView = vi.fn();
});

const result = (items = [business], hasMore = false) => Response.json({ items, hasMore });

it("muestra negocios y contacto sin cuenta, con navegación entre páginas", async () => {
  const fetcher = vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(Response.json([{ name: "Almacenes" }])).mockResolvedValueOnce(result([business], true))
    .mockResolvedValueOnce(result([{ ...business, id: "2", name: "Segundo negocio" }]))
    .mockResolvedValueOnce(result([business], true));
  render(<Explore />);
  expect(screen.getByRole("status")).toHaveTextContent("Cargando");
  expect(await screen.findByRole("heading", { name: business.name })).toBeInTheDocument();
  expect(screen.getByText(business.description)).toBeInTheDocument();
  expect(screen.getByText(business.address)).toBeInTheDocument();
  expect(screen.getByRole("link", { name: /WhatsApp/ })).toHaveAttribute("href", "https://wa.me/5493515551234");
  expect(screen.getByRole("button", { name: "Anterior" })).toBeDisabled();
  fireEvent.click(screen.getByRole("button", { name: "Siguiente" }));
  expect(await screen.findByRole("heading", { name: "Segundo negocio" })).toBeInTheDocument();
  expect(fetcher).toHaveBeenLastCalledWith("/api/marketplace/public/businesses?page=2", expect.objectContaining({ cache: "no-store" }));
  expect(screen.getByRole("button", { name: "Siguiente" })).toBeDisabled();
  fireEvent.click(screen.getByRole("button", { name: "Anterior" }));
  expect(await screen.findByRole("heading", { name: business.name })).toBeInTheDocument();
});

it("explica cuando todavía no hay negocios publicados", async () => {
  vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(Response.json([])).mockResolvedValue(result([]));
  render(<Explore />);
  expect(await screen.findByText("Todavía no hay negocios publicados.")).toBeInTheDocument();
  expect(screen.queryByRole("link", { name: /WhatsApp/ })).not.toBeInTheDocument();
});

it.each(["http", "network"])("permite reintentar un fallo %s sin simular un resultado vacío", async (failure) => {
  const fetcher = vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(Response.json([]));
  if (failure === "http") fetcher.mockResolvedValueOnce(new Response(null, { status: 503 }));
  else fetcher.mockRejectedValueOnce(new Error("Offline"));
  fetcher.mockResolvedValueOnce(result());
  render(<Explore />);
  expect(await screen.findByRole("alert")).toHaveTextContent("No pudimos cargar los negocios.");
  expect(screen.queryByText("Todavía no hay negocios publicados.")).not.toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Reintentar" }));
  expect(await screen.findByRole("heading", { name: business.name })).toBeInTheDocument();
});

it("busca en el servidor, combina rubro, refleja filtros en la URL y permite limpiarlos", async () => {
  const fetcher = vi.spyOn(globalThis, "fetch")
    .mockResolvedValueOnce(Response.json([{ name: "Almacenes" }, { name: "Gastronomía" }]))
    .mockResolvedValueOnce(result())
    .mockResolvedValueOnce(result())
    .mockResolvedValueOnce(result([]))
    .mockResolvedValueOnce(result());
  render(<Explore />);
  await screen.findByRole("heading", { name: business.name });
  fireEvent.change(screen.getByRole("searchbox"), { target: { value: "Colón" } });
  fireEvent.submit(screen.getByRole("search"));
  await screen.findByRole("heading", { name: business.name });
  expect(fetcher).toHaveBeenLastCalledWith("/api/marketplace/public/businesses?page=1&q=Col%C3%B3n", expect.anything());
  fireEvent.click(screen.getByRole("button", { name: "Gastronomía" }));
  await screen.findByText("No encontramos negocios con esa búsqueda.");
  expect(new URLSearchParams(window.location.search).get("category")).toBe("Gastronomía");
  expect(fetcher).toHaveBeenLastCalledWith("/api/marketplace/public/businesses?page=1&q=Col%C3%B3n&category=Gastronom%C3%ADa", expect.anything());
  fireEvent.click(screen.getByRole("button", { name: "Limpiar filtros" }));
  await screen.findByRole("heading", { name: business.name });
  expect(window.location.search).toBe("");
  expect(screen.getByRole("searchbox")).toHaveValue("");
});

it("restaura una búsqueda compartida y responde al historial del navegador", async () => {
  window.history.replaceState(null, "", "/explorar?q=Col%C3%B3n&category=Almacenes&page=2");
  const fetcher = vi.spyOn(globalThis, "fetch")
    .mockResolvedValueOnce(Response.json([{ name: "Almacenes" }]))
    .mockResolvedValueOnce(result())
    .mockResolvedValueOnce(result());
  render(<Explore />);
  await screen.findByRole("heading", { name: business.name });
  expect(screen.getByRole("searchbox")).toHaveValue("Colón");
  expect(fetcher).toHaveBeenLastCalledWith("/api/marketplace/public/businesses?page=2&q=Col%C3%B3n&category=Almacenes", expect.anything());
  window.history.replaceState(null, "", "/explorar");
  fireEvent(window, new PopStateEvent("popstate"));
  await screen.findByRole("heading", { name: business.name });
  expect(fetcher).toHaveBeenLastCalledWith("/api/marketplace/public/businesses?page=1", expect.anything());
  expect(screen.getByRole("searchbox")).toHaveValue("");
});
