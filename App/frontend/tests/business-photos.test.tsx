import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { expect, it, vi } from "vitest";
import BusinessPhotos from "../src/app/business-photos";

const photo = { slot: 1, caption: "Frente", width: 100, height: 100 };
it("carga una foto con descripción y confirma antes de eliminarla", async () => {
  const fetcher = vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(Response.json([]))
    .mockResolvedValueOnce(Response.json(photo)).mockResolvedValueOnce(new Response(null, { status: 204 }));
  render(<BusinessPhotos id="1" />);
  fireEvent.click(screen.getByRole("button", { name: "Administrar fotos" }));
  await screen.findByText("Todavía no agregaste fotos.");
  fireEvent.change(screen.getByLabelText("Archivo de foto"), { target: {
    files: [new File(["pixels"], "foto.png", { type: "image/png" })] } });
  fireEvent.change(screen.getByLabelText("Descripción de la foto"), { target: { value: "Frente" } });
  fireEvent.click(screen.getByRole("button", { name: "Guardar foto" }));
  expect(await screen.findByRole("img", { name: "Frente" })).toBeInTheDocument();
  const body = fetcher.mock.calls[1][1]?.body as FormData;
  expect(body.get("caption")).toBe("Frente");
  expect(body.get("file")).toBeInstanceOf(File);
  fireEvent.click(screen.getByRole("button", { name: "Eliminar foto 1" }));
  expect(fetcher).toHaveBeenCalledTimes(2);
  fireEvent.click(screen.getByRole("button", { name: "Cancelar eliminación" }));
  expect(screen.getByRole("img", { name: "Frente" })).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Eliminar foto 1" }));
  fireEvent.click(screen.getByRole("button", { name: "Confirmar eliminación" }));
  await screen.findByText("Todavía no agregaste fotos.");
  expect(fetcher).toHaveBeenLastCalledWith("/api/marketplace/businesses/1/photos/1", expect.objectContaining({ method: "DELETE" }));
});
it("conserva la foto y el formulario cuando falla un reemplazo", async () => {
  vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(Response.json([photo]))
    .mockResolvedValueOnce(new Response(null, { status: 503 }));
  render(<BusinessPhotos id="1" />);
  fireEvent.click(screen.getByRole("button", { name: "Administrar fotos" }));
  await screen.findByRole("img", { name: "Frente" });
  fireEvent.click(screen.getByRole("button", { name: "Reemplazar foto 1" }));
  fireEvent.change(screen.getByLabelText("Archivo de foto"), { target: {
    files: [new File(["pixels"], "nueva.png", { type: "image/png" })] } });
  fireEvent.click(screen.getByRole("button", { name: "Guardar foto" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("No pudimos guardar");
  expect(screen.getByRole("img", { name: "Frente" })).toBeInTheDocument();
  expect(screen.getByLabelText("Descripción de la foto")).toHaveValue("Frente");
});
it("permite reintentar la lista y rechaza un archivo demasiado grande antes de enviarlo", async () => {
  const fetcher = vi.spyOn(globalThis, "fetch").mockRejectedValueOnce(new Error("offline")).mockResolvedValueOnce(Response.json([]));
  render(<BusinessPhotos id="1" />);
  fireEvent.click(screen.getByRole("button", { name: "Administrar fotos" }));
  await screen.findByRole("alert");
  fireEvent.click(screen.getByRole("button", { name: "Reintentar fotos" }));
  await screen.findByText("Todavía no agregaste fotos.");
  fireEvent.change(screen.getByLabelText("Archivo de foto"), { target: {
    files: [new File([new Uint8Array(5 * 1024 * 1024 + 1)], "grande.png", { type: "image/png" })] } });
  await waitFor(() => expect(screen.getByRole("alert")).toHaveTextContent("5 MB"));
  expect(fetcher).toHaveBeenCalledTimes(2);
});
