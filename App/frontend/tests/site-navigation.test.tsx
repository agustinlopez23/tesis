import { render, screen, within } from "@testing-library/react";
import { expect, it } from "vitest";
import { SiteHeader } from "../src/app/ui";

it.each([
  ["explore", "Explorar negocios"],
  ["products", "Productos"],
  ["owner", "Mi negocio"],
] as const)("permite cambiar de sección y anuncia solo la actual: %s", (active, label) => {
  render(<SiteHeader active={active} />);
  const nav = within(screen.getByRole("navigation", { name: "Navegación principal" }));
  expect(nav.getByRole("link", { name: "Productos" })).toHaveAttribute("href", "/productos");
  expect(nav.getByRole("link", { name: "Explorar negocios" })).toHaveAttribute("href", "/explorar");
  expect(nav.getByRole("link", { name: "Mi negocio" })).toHaveAttribute("href", "/mi-negocio");
  expect(nav.getAllByRole("link", { current: "page" })).toHaveLength(1);
  expect(nav.getByRole("link", { current: "page" })).toHaveAccessibleName(label);
});
