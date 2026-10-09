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
  fireEvent.change(screen.getByLabelText("Hechos del producto"), { target: { value: "Café en paquete de 250 g" } });
  fireEvent.click(screen.getByRole("checkbox")); fireEvent.click(generate);
  const title = await screen.findByLabelText("Nombre sugerido");
  expect(apply).not.toHaveBeenCalled(); fireEvent.change(title, { target: { value: "Mi café" } });
  fireEvent.click(screen.getByRole("button", { name: "Usar este borrador" }));
  expect(apply).toHaveBeenCalledWith({ name: "Mi café", description: "Paquete de 250 g", category: "Alimentos" });
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
  const facts = await screen.findByLabelText("Hechos del producto");
  fireEvent.change(facts, { target: { value: "Café 250 g" } }); fireEvent.click(screen.getByRole("checkbox"));
  fireEvent.click(screen.getByRole("button", { name: "Generar borrador" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("carga manual");
  expect(facts).toHaveValue("Café 250 g"); expect(apply).not.toHaveBeenCalled();
});
