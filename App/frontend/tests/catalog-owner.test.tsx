import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { expect, it, vi } from "vitest";
import BusinessCatalog from "../src/app/business-catalog";

const product = { id: "p1", name: "Café molido", description: "250 g", category: "Café", price: 1234.5,
  currency: "ARS", available: true, status: "published", version: 1, hasPhoto: false };
it("exige guardar cambios de texto antes de subir una foto para no perderlos", async () => {
  vi.spyOn(globalThis, "fetch").mockImplementation(async (url) => String(url).endsWith("/plan")
    ? Response.json({ catalogEnabled: true, plan: "Impulso", periods: [] }) : Response.json([product]));
  render(<BusinessCatalog id="b1" />);
  fireEvent.click(screen.getByRole("button", { name: "Administrar catálogo" }));
  fireEvent.click(await screen.findByRole("button", { name: "Editar Café molido" }));
  expect(screen.getByLabelText("Foto del producto")).toBeEnabled();
  fireEvent.change(screen.getByLabelText("Nombre del producto"), { target: { value: "Nuevo nombre sin guardar" } });
  expect(screen.getByLabelText("Foto del producto")).toBeDisabled();
});
it("conserva lectura privada cuando el plan vence y bloquea edición", async () => {
  vi.spyOn(globalThis, "fetch").mockImplementation(async (url) => String(url).endsWith("/plan")
    ? Response.json({ catalogEnabled: false, plan: "Presencia", periods: [] }) : Response.json([product]));
  render(<BusinessCatalog id="b1" />);
  fireEvent.click(screen.getByRole("button", { name: "Administrar catálogo" }));
  expect(await screen.findByText("Café molido")).toBeInTheDocument();
  expect(screen.getByText(/Tus productos se conservan/)).toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Editar Café molido" })).toBeDisabled();
});
it("no pierde el borrador cuando guardar falla y no genera otro identificador", async () => {
  const fetcher = vi.spyOn(globalThis, "fetch").mockImplementation(async (url, init) => {
    if (init?.method === "PUT") return Response.json({ message: "Servicio temporalmente no disponible" }, { status: 503 });
    return String(url).endsWith("/plan") ? Response.json({ catalogEnabled: true, plan: "Impulso", periods: [] }) : Response.json([]);
  });
  render(<BusinessCatalog id="b1" />);
  fireEvent.click(screen.getByRole("button", { name: "Administrar catálogo" }));
  fireEvent.click(await screen.findByRole("button", { name: "Nuevo producto" }));
  fireEvent.change(screen.getByLabelText("Nombre del producto"), { target: { value: "Café" } });
  fireEvent.change(screen.getByLabelText("Categoría del producto"), { target: { value: "Bebidas" } });
  fireEvent.change(screen.getByLabelText("Precio"), { target: { value: "1200.50" } });
  fireEvent.click(screen.getByRole("button", { name: "Guardar producto" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("Servicio temporalmente");
  expect(screen.getByLabelText("Nombre del producto")).toHaveValue("Café");
  fireEvent.click(screen.getByRole("button", { name: "Guardar producto" }));
  await waitFor(() => expect(fetcher.mock.calls.filter(([, init]) => init?.method === "PUT")).toHaveLength(2));
  const writes = fetcher.mock.calls.filter(([, init]) => init?.method === "PUT");
  expect(writes[0][0]).toBe(writes[1][0]);
});
it("guarda los valores confirmados del borrador y ofrece usar la misma foto", async () => {
  const photoFile = new File([new Uint8Array([255, 216, 255])], "taza.jpg", { type: "image/jpeg" });
  let stored: Record<string, unknown> | null = null;
  const fetcher = vi.spyOn(globalThis, "fetch").mockImplementation(async (url, init) => {
    const path = String(url);
    if (path.endsWith("/catalog-assistance")) return Response.json(init?.method === "POST" ? { name: "Taza blanca", description: "Taza de cerámica", category: "Bazar" } : { enabled: true, provider: "OpenAI" });
    if (init?.method === "PUT" && path.endsWith("/photo")) return Response.json({ ...stored, version: 2, hasPhoto: true });
    if (init?.method === "PUT") { stored = { ...JSON.parse(String(init.body)), version: 1 }; return Response.json(stored); }
    return path.endsWith("/plan") ? Response.json({ catalogEnabled: true, plan: "Impulso", periods: [] }) : Response.json(stored ? [stored] : []);
  });
  render(<BusinessCatalog id="b1" />);
  fireEvent.click(screen.getByRole("button", { name: "Administrar catálogo" }));
  fireEvent.click(await screen.findByRole("button", { name: "Nuevo producto" }));
  fireEvent.click(screen.getByRole("button", { name: "Ayuda para redactar" }));
  fireEvent.change(await screen.findByLabelText("Foto para el borrador (opcional)"), { target: { files: [photoFile] } });
  fireEvent.change(screen.getByLabelText("Datos mínimos del producto"), { target: { value: "Taza 300 ml" } });
  fireEvent.click(screen.getByRole("checkbox", { name: /Acepto enviar la foto/ }));
  fireEvent.click(screen.getByRole("button", { name: "Generar borrador" }));
  fireEvent.change(await screen.findByLabelText("Nombre sugerido"), { target: { value: "Taza confirmada" } });
  fireEvent.click(screen.getByRole("button", { name: "Usar este borrador" }));
  expect(fetcher.mock.calls.some(([, init]) => init?.method === "PUT")).toBe(false);
  expect(screen.getByText(/Guardá el producto para usar la foto del borrador/)).toBeInTheDocument();
  fireEvent.change(screen.getByLabelText("Nombre del producto"), { target: { value: "Taza confirmada por el titular" } });
  fireEvent.change(screen.getByLabelText("Precio"), { target: { value: "1500" } });
  fireEvent.click(screen.getByRole("button", { name: "Guardar producto" }));
  expect(await screen.findByText("Producto guardado.")).toBeInTheDocument();
  const usePhoto = screen.getByRole("button", { name: "Usar la foto del borrador" });
  await waitFor(() => expect(usePhoto).toBeEnabled());
  fireEvent.click(usePhoto);
  await waitFor(() => expect(fetcher.mock.calls.filter(([url, init]) => init?.method === "PUT" && String(url).endsWith("/photo"))).toHaveLength(1));
  const save = fetcher.mock.calls.find(([url, init]) => init?.method === "PUT" && !String(url).endsWith("/photo"))!;
  expect(JSON.parse(String(save[1]!.body))).toMatchObject({ name: "Taza confirmada por el titular", description: "Taza de cerámica", category: "Bazar", price: 1500 });
  const upload = fetcher.mock.calls.find(([url, init]) => init?.method === "PUT" && String(url).endsWith("/photo"))![1]!.body as FormData;
  expect(upload.get("file")).toBe(photoFile); expect(upload.get("version")).toBe("1");
  expect(await screen.findByText("Foto guardada.")).toBeInTheDocument();
  expect(screen.queryByRole("button", { name: "Usar la foto del borrador" })).not.toBeInTheDocument();
});
it("al abrir el formulario de un producto lo lleva a la vista, sin moverse al aplicar cambios", async () => {
  vi.spyOn(globalThis, "fetch").mockImplementation(async (url) => String(url).endsWith("/plan")
    ? Response.json({ catalogEnabled: true, plan: "Impulso", periods: [] }) : Response.json([product]));
  const scroll = vi.fn(); Element.prototype.scrollIntoView = scroll;
  render(<BusinessCatalog id="b1" />);
  fireEvent.click(screen.getByRole("button", { name: "Administrar catálogo" }));
  fireEvent.click(await screen.findByRole("button", { name: "Nuevo producto" }));
  await waitFor(() => expect(scroll).toHaveBeenCalledTimes(1));
  expect(scroll.mock.contexts[0]).toBe(screen.getByRole("heading", { name: "Nuevo producto" }).closest("form"));
  fireEvent.change(screen.getByLabelText("Nombre del producto"), { target: { value: "Taza" } });
  fireEvent.click(screen.getByRole("button", { name: "Editar Café molido" }));
  await waitFor(() => expect(scroll).toHaveBeenCalledTimes(2));
  expect(screen.getByLabelText("Foto del producto con la cámara")).toHaveAttribute("capture", "environment");
});
