import { render, screen } from "@testing-library/react";
import { expect, it, vi } from "vitest";
import Home from "../src/app/estado/page";
import { checkApiHealth } from "../src/lib/check-api-health";

vi.mock("../src/lib/check-api-health", () => ({ checkApiHealth: vi.fn() }));

it.each([true, false])("pasa URL configurada y presenta disponibilidad %s", async (available) => {
  const sentinel = "http://sentinel.example:9183/api";
  vi.stubEnv("NEXT_PUBLIC_API_URL", sentinel);
  vi.mocked(checkApiHealth).mockResolvedValue(available);
  render(await Home());
  expect(checkApiHealth).toHaveBeenCalledExactlyOnceWith(sentinel);
  expect(screen.getByRole("status")).toHaveTextContent(
    available ? "API disponible" : "API no disponible",
  );
  expect(screen.queryByText(available ? "API no disponible" : "API disponible")).not.toBeInTheDocument();
});
