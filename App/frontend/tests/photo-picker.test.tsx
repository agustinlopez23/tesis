import { fireEvent, render, screen } from "@testing-library/react";
import { expect, it, vi } from "vitest";
import PhotoPicker from "../src/app/photo-picker";

it("ofrece sacar la foto con la cámara trasera o elegirla de la galería", () => {
  render(<PhotoPicker label="Foto del producto" onSelect={vi.fn()} />);
  expect(screen.getByRole("button", { name: "Sacar foto" })).toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Elegir de la galería" })).toBeInTheDocument();
  const camera = screen.getByLabelText("Foto del producto con la cámara");
  expect(camera).toHaveAttribute("capture", "environment");
  expect(camera).toHaveAttribute("accept", "image/jpeg,image/png,image/webp");
  const gallery = screen.getByLabelText("Foto del producto");
  expect(gallery).not.toHaveAttribute("capture");
  expect(gallery).toHaveAttribute("accept", "image/jpeg,image/png,image/webp");
  expect(screen.getByText("Ninguna foto elegida")).toBeInTheDocument();
});

it("cada botón abre su selector y entrega la foto elegida", () => {
  const onSelect = vi.fn();
  render(<PhotoPicker label="Foto del producto" onSelect={onSelect} fileName="taza.jpg" />);
  const camera = screen.getByLabelText<HTMLInputElement>("Foto del producto con la cámara");
  const gallery = screen.getByLabelText<HTMLInputElement>("Foto del producto");
  const cameraClick = vi.spyOn(camera, "click"), galleryClick = vi.spyOn(gallery, "click");
  fireEvent.click(screen.getByRole("button", { name: "Sacar foto" }));
  fireEvent.click(screen.getByRole("button", { name: "Elegir de la galería" }));
  expect(cameraClick).toHaveBeenCalledOnce(); expect(galleryClick).toHaveBeenCalledOnce();
  const photo = new File([new Uint8Array([255, 216, 255])], "foto.jpg", { type: "image/jpeg" });
  fireEvent.change(camera, { target: { files: [photo] } });
  expect(onSelect).toHaveBeenCalledWith(photo);
  expect(screen.getByText("taza.jpg")).toBeInTheDocument();
});

it("deshabilitado no permite elegir ni sacar fotos", () => {
  render(<PhotoPicker label="Foto del producto" onSelect={vi.fn()} disabled />);
  expect(screen.getByRole("button", { name: "Sacar foto" })).toBeDisabled();
  expect(screen.getByRole("button", { name: "Elegir de la galería" })).toBeDisabled();
  expect(screen.getByLabelText("Foto del producto")).toBeDisabled();
  expect(screen.getByLabelText("Foto del producto con la cámara")).toBeDisabled();
});
