import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { expect, it, vi } from "vitest";
import CatalogAssistance from "../src/app/catalog-assistance";

it("exige consentimiento, muestra borrador editable y no aplica hasta confirmarlo", async () => {
  const apply = vi.fn();
  const fetcher = vi.spyOn(globalThis, "fetch").mockImplementation(async (_url, init) => Response.json(init?.method === "POST" ? { name: "Café", description: "Paquete de 250 g", category: "Alimentos" } : { enabled: true, provider: "OpenAI" }));
  render(<CatalogAssistance business="b1" onApply={apply} />);
  fireEvent.click(screen.getByRole("button", { name: "Ayuda para redactar" }));
  const generate = await screen.findByRole("button", { name: "Generar borrador" });
  expect(generate).toBeDisabled();
  fireEvent.change(screen.getByLabelText("Datos mínimos del producto"), { target: { value: "Café en paquete de 250 g" } });
  fireEvent.click(screen.getByRole("checkbox")); fireEvent.click(generate);
  const title = await screen.findByLabelText("Nombre sugerido");
  expect(apply).not.toHaveBeenCalled(); fireEvent.change(title, { target: { value: "Mi café" } });
  fireEvent.click(screen.getByRole("button", { name: "Usar este borrador" }));
  expect(apply).toHaveBeenCalledWith({ name: "Mi café", description: "Paquete de 250 g", category: "Alimentos" }, null);
  const body = JSON.parse(String(fetcher.mock.calls.find(([, init]) => init?.method === "POST")![1]?.body));
  expect(body).toEqual({ facts: "Café en paquete de 250 g", consent: true });
});
it("sin proveedor permite seguir manualmente y no envía hechos", async () => {
  const fetcher = vi.spyOn(globalThis, "fetch").mockResolvedValue(Response.json({ enabled: false, provider: null }));
  render(<CatalogAssistance business="b1" onApply={vi.fn()} />);
  fireEvent.click(screen.getByRole("button", { name: "Ayuda para redactar" }));
  expect(await screen.findByText(/continuar con la carga manual/)).toBeInTheDocument();
  expect(screen.queryByRole("button", { name: "Generar borrador" })).not.toBeInTheDocument();
  await waitFor(() => expect(fetcher).toHaveBeenCalledTimes(1));
});
it("una falla conserva los hechos y no aplica una respuesta anterior", async () => {
  const apply = vi.fn();
  vi.spyOn(globalThis, "fetch").mockImplementation(async (_url, init) => init?.method === "POST" ? Response.json({ message: "Podés continuar con la carga manual." }, { status: 503 }) : Response.json({ enabled: true, provider: "OpenAI" }));
  render(<CatalogAssistance business="b1" onApply={apply} />);
  fireEvent.click(screen.getByRole("button", { name: "Ayuda para redactar" }));
  const facts = await screen.findByLabelText("Datos mínimos del producto");
  fireEvent.change(facts, { target: { value: "Café 250 g" } }); fireEvent.click(screen.getByRole("checkbox"));
  fireEvent.click(screen.getByRole("button", { name: "Generar borrador" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("carga manual");
  expect(facts).toHaveValue("Café 250 g"); expect(apply).not.toHaveBeenCalled();
});
it("envía la foto con los datos mínimos tras el consentimiento y entrega la foto al aplicar", async () => {
  const apply = vi.fn();
  const fetcher = vi.spyOn(globalThis, "fetch").mockImplementation(async (_url, init) => Response.json(init?.method === "POST" ? { name: "Taza blanca", description: "Taza de cerámica", category: "Bazar" } : { enabled: true, provider: "OpenAI" }));
  render(<CatalogAssistance business="b1" onApply={apply} />);
  fireEvent.click(screen.getByRole("button", { name: "Ayuda para redactar" }));
  const consent = await screen.findByRole("checkbox", { name: "Acepto enviar la foto y los datos a OpenAI para preparar un borrador" });
  expect(screen.getByText(/Se enviarán la foto y el texto a OpenAI/)).toBeInTheDocument();
  const photo = new File([new Uint8Array([255, 216, 255])], "taza.jpg", { type: "image/jpeg" });
  fireEvent.change(screen.getByLabelText("Foto para el borrador (opcional)"), { target: { files: [new File(["x"], "nota.gif", { type: "image/gif" })] } });
  expect(screen.getByRole("alert")).toHaveTextContent("JPEG, PNG o WebP");
  fireEvent.change(screen.getByLabelText("Foto para el borrador (opcional)"), { target: { files: [photo] } });
  expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  fireEvent.change(screen.getByLabelText("Datos mínimos del producto"), { target: { value: "Taza de 300 ml" } });
  fireEvent.click(consent); fireEvent.click(screen.getByRole("button", { name: "Generar borrador" }));
  const description = await screen.findByLabelText("Descripción sugerida");
  fireEvent.change(description, { target: { value: "Taza de cerámica de 300 ml" } });
  const body = fetcher.mock.calls.find(([, init]) => init?.method === "POST")![1]?.body as FormData;
  expect(body).toBeInstanceOf(FormData);
  expect(body.get("facts")).toBe("Taza de 300 ml"); expect(body.get("consent")).toBe("true"); expect(body.get("file")).toBe(photo);
  expect(apply).not.toHaveBeenCalled();
  fireEvent.click(screen.getByRole("button", { name: "Usar este borrador" }));
  expect(apply).toHaveBeenCalledWith({ name: "Taza blanca", description: "Taza de cerámica de 300 ml", category: "Bazar" }, photo);
});
it("descartar la propuesta no aplica nada", async () => {
  const apply = vi.fn();
  vi.spyOn(globalThis, "fetch").mockImplementation(async (_url, init) => init?.method === "POST" ? Response.json({ name: "Taza", description: "Taza", category: "Bazar" }) : Response.json({ enabled: true, provider: "OpenAI" }));
  render(<CatalogAssistance business="b1" onApply={apply} />);
  fireEvent.click(screen.getByRole("button", { name: "Ayuda para redactar" }));
  fireEvent.change(await screen.findByLabelText("Datos mínimos del producto"), { target: { value: "Taza 300 ml" } });
  fireEvent.click(screen.getByRole("checkbox")); fireEvent.click(screen.getByRole("button", { name: "Generar borrador" }));
  fireEvent.click(await screen.findByRole("button", { name: "Descartar propuesta" }));
  expect(screen.queryByLabelText("Nombre sugerido")).not.toBeInTheDocument(); expect(apply).not.toHaveBeenCalled();
});
it("una demora del proveedor informa y habilita la carga manual", async () => {
  vi.spyOn(globalThis, "fetch").mockImplementation(async (_url, init) => {
    if (init?.method === "POST") throw new DOMException("signal timed out", "TimeoutError");
    return Response.json({ enabled: true, provider: "OpenAI" });
  });
  render(<CatalogAssistance business="b1" onApply={vi.fn()} />);
  fireEvent.click(screen.getByRole("button", { name: "Ayuda para redactar" }));
  fireEvent.change(await screen.findByLabelText("Datos mínimos del producto"), { target: { value: "Taza 300 ml" } });
  fireEvent.click(screen.getByRole("checkbox")); fireEvent.click(screen.getByRole("button", { name: "Generar borrador" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("continuar con la carga manual");
  expect(screen.getByRole("button", { name: "Generar borrador" })).toBeEnabled();
});
it("el consentimiento nombra al proveedor configurado en la API", async () => {
  vi.spyOn(globalThis, "fetch").mockImplementation(async () => Response.json({ enabled: true, provider: "Google Gemini" }));
  render(<CatalogAssistance business="b1" onApply={vi.fn()} />);
  fireEvent.click(screen.getByRole("button", { name: "Ayuda para redactar" }));
  expect(await screen.findByRole("checkbox", { name: "Acepto enviar la foto y los datos a Google Gemini para preparar un borrador" })).not.toBeChecked();
  expect(screen.getByText(/Se enviarán la foto y el texto a Google Gemini/)).toBeInTheDocument();
  expect(screen.queryByText(/OpenAI/)).not.toBeInTheDocument();
});
