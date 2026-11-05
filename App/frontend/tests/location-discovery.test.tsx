import { act, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, expect, it, vi } from "vitest";
import Explore from "../src/app/explorar/explore";

const business = { id: "geo", name: "Negocio cercano", description: "Productos locales",
  address: "Córdoba", category: "Almacenes", whatsApp: "+5493515551234", distanceKm: 0.75 };
beforeEach(() => {
  window.history.replaceState(null, "", "/explorar");
  Element.prototype.scrollIntoView = vi.fn();
});
function mockDirectory() {
  return vi.spyOn(globalThis, "fetch").mockImplementation(async (url) =>
    String(url).includes("business-categories") ? Response.json([{ name: "Almacenes" }]) :
      Response.json({ items: [business], hasMore: false }));
}
function lastQuery(fetcher: ReturnType<typeof mockDirectory>) {
  return new URL(String(fetcher.mock.calls.at(-1)![0]), "http://localhost").searchParams;
}

it("elige zona y radio, combina búsqueda y restaura la zona desde un enlace", async () => {
  window.history.replaceState(null, "", "/explorar?zone=centro&radiusKm=2");
  const fetcher = mockDirectory();
  render(<Explore />);
  await screen.findByRole("heading", { name: business.name });
  expect(screen.getByLabelText("Zona de referencia")).toHaveValue("centro");
  expect(lastQuery(fetcher).get("lat")).toBe("-31.416281");
  expect(lastQuery(fetcher).get("radiusKm")).toBe("2");
  expect(screen.getByText("750 m aprox.")).toBeInTheDocument();
  fireEvent.change(screen.getByLabelText("Radio de búsqueda"), { target: { value: "5" } });
  await waitFor(() => expect(lastQuery(fetcher).get("radiusKm")).toBe("5"));
  fireEvent.click(screen.getByRole("button", { name: "Almacenes" }));
  await waitFor(() => expect(lastQuery(fetcher).get("category")).toBe("Almacenes"));
  expect(lastQuery(fetcher).get("radiusKm")).toBe("5");
  expect(window.location.search).toContain("zone=centro");
  fireEvent.click(screen.getByRole("button", { name: "Quitar ubicación" }));
  await waitFor(() => expect(lastQuery(fetcher).has("lat")).toBe(false));
  expect(lastQuery(fetcher).get("category")).toBe("Almacenes");
});

it("solicita permiso sólo al hacer clic y ofrece zona manual cuando se deniega", async () => {
  const locate = vi.fn((_success, fail) => fail({ code: 1 }));
  Object.defineProperty(navigator, "geolocation", { configurable: true, value: { getCurrentPosition: locate } });
  mockDirectory();
  render(<Explore />);
  await screen.findByRole("heading", { name: business.name });
  expect(locate).not.toHaveBeenCalled();
  fireEvent.click(screen.getByRole("button", { name: "Usar mi ubicación" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("Elegí una zona");
  expect(screen.getByLabelText("Zona de referencia")).toBeEnabled();
});

it("ignora una ubicación tardía después de elegir una zona manual", async () => {
  let located!: PositionCallback;
  Object.defineProperty(navigator, "geolocation", { configurable: true,
    value: { getCurrentPosition: vi.fn((success) => { located = success; }) } });
  const fetcher = mockDirectory();
  render(<Explore />);
  await screen.findByRole("heading", { name: business.name });
  fireEvent.click(screen.getByRole("button", { name: "Usar mi ubicación" }));
  fireEvent.change(screen.getByLabelText("Zona de referencia"), { target: { value: "centro" } });
  await waitFor(() => expect(lastQuery(fetcher).get("lat")).toBe("-31.416281"));
  act(() => located({ coords: { latitude: 10, longitude: 20 } } as GeolocationPosition));
  expect(lastQuery(fetcher).get("lat")).toBe("-31.416281");
  expect(screen.getByLabelText("Zona de referencia")).toHaveValue("centro");
});

it("mantiene la ubicación del dispositivo fuera de la URL y la restaura al volver", async () => {
  Object.defineProperty(navigator, "geolocation", { configurable: true, value: {
    getCurrentPosition: vi.fn((success) => success({ coords: { latitude: -31.4, longitude: -64.2 } })),
  } });
  const fetcher = mockDirectory();
  render(<Explore />);
  await screen.findByRole("heading", { name: business.name });
  fireEvent.click(screen.getByRole("button", { name: "Usar mi ubicación" }));
  await waitFor(() => expect(lastQuery(fetcher).get("lat")).toBe("-31.4"));
  expect(window.location.search).not.toMatch(/lat|lon|31\.4|64\.2/);
  const savedState = window.history.state;
  const savedUrl = window.location.href;
  fireEvent.click(screen.getByRole("button", { name: "Quitar ubicación" }));
  await waitFor(() => expect(lastQuery(fetcher).has("lat")).toBe(false));
  window.history.replaceState(savedState, "", savedUrl);
  fireEvent(window, new PopStateEvent("popstate"));
  await waitFor(() => expect(lastQuery(fetcher).get("lat")).toBe("-31.4"));
  expect(screen.getByText("Tu ubicación actual")).toBeInTheDocument();
});

it("explica un enlace sin coordenadas y descarta zona, radio e historial inválidos", async () => {
  window.history.replaceState({ discoveryLocation: { kind: "device", lat: 1000, lon: 0 } }, "",
    "/explorar?near=me&zone=desconocida&radiusKm=-2");
  const fetcher = mockDirectory();
  render(<Explore />);
  await screen.findByRole("heading", { name: business.name });
  expect(lastQuery(fetcher).has("lat")).toBe(false);
  expect(screen.getByText(/Este enlace no incluye la ubicación/)).toBeInTheDocument();
  fireEvent.change(screen.getByLabelText("Zona de referencia"), { target: { value: "centro" } });
  await waitFor(() => expect(lastQuery(fetcher).get("radiusKm")).toBe("2"));
  expect(screen.queryByText(/Este enlace no incluye la ubicación/)).not.toBeInTheDocument();
});

it("muestra el radio sólo cuando hay un área elegida y lo aplica desde el chip de zona", async () => {
  const fetcher = mockDirectory();
  render(<Explore />);
  await screen.findByRole("heading", { name: business.name });
  expect(screen.queryByLabelText("Radio de búsqueda")).not.toBeInTheDocument();
  fireEvent.change(screen.getByLabelText("Zona de referencia"), { target: { value: "alberdi" } });
  await waitFor(() => expect(lastQuery(fetcher).get("lat")).toBe("-31.4093"));
  expect(screen.getByLabelText("Radio de búsqueda")).toHaveValue("2");
  expect(screen.getByRole("button", { name: "Quitar ubicación" })).toBeInTheDocument();
});

it("reinicia la página al cambiar el radio y permite recuperar una zona sin resultados", async () => {
  window.history.replaceState(null, "", "/explorar?zone=centro&radiusKm=2&page=2");
  const fetcher = vi.spyOn(globalThis, "fetch").mockImplementation(async (url) =>
    String(url).includes("business-categories") ? Response.json([]) : Response.json({ items: [], hasMore: false }));
  render(<Explore />);
  await screen.findByText("No encontramos negocios con esa búsqueda.");
  expect(screen.getByText(/Ampliá el radio/)).toBeInTheDocument();
  fireEvent.change(screen.getByLabelText("Radio de búsqueda"), { target: { value: "5" } });
  await waitFor(() => expect(new URL(String(fetcher.mock.calls.at(-1)![0]), "http://localhost").searchParams.get("page")).toBe("1"));
  expect(window.location.search).not.toContain("page=");
  fireEvent.click(screen.getByRole("button", { name: "Ver todos los negocios" }));
  await screen.findByText("Todavía no hay negocios publicados.");
  expect(window.location.search).toBe("");
});
