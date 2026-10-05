import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { expect, it, vi } from "vitest";
import CommerceMetrics from "../src/app/commerce-metrics";
const metrics = { days: 7, from: "2026-09-29T03:00:00Z", asOf: "2026-10-05T12:00:00Z", orders: 2, contactsReceived: 1, converted: 1,
  conversionRate: .5, activeSales: 3, voidedSales: 1, amounts: { requested: { ARS: 200 }, conversation: { USD: 20 }, direct: { ARS: 50 } } };
it("separa conversión de solicitudes e importes reportados por origen y moneda", async () => {
  const fetcher = vi.spyOn(globalThis, "fetch").mockImplementation(async url => Response.json({ ...metrics, days: String(url).includes("30") ? 30 : 7 }));
  render(<CommerceMetrics business="b1" />);
  expect(fetcher).not.toHaveBeenCalled();
  fireEvent.click(screen.getByRole("button", { name: "Ver resultados comerciales" }));
  expect(await screen.findByText("50,0 %")).toBeInTheDocument();
  expect(screen.getByText("Acordado en conversación")).toBeInTheDocument();
  expect(screen.getByText(/USD/)).toHaveTextContent("20");
  fireEvent.click(screen.getByRole("button", { name: "30 días" }));
  await waitFor(() => expect(fetcher.mock.calls.some(([url]) => String(url).endsWith("days=30"))).toBe(true));
});
it("muestra sin datos para cohorte vacía y retira cifras al fallar actualización", async () => {
  vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(Response.json({ ...metrics, orders: 0, converted: 0, conversionRate: null }))
    .mockResolvedValueOnce(new Response(null, { status: 503 }));
  render(<CommerceMetrics business="b1" />);
  fireEvent.click(screen.getByRole("button", { name: "Ver resultados comerciales" }));
  expect(await screen.findByText("Sin solicitudes en el período")).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Actualizar resultados comerciales" }));
  expect(await screen.findByRole("alert")).toBeInTheDocument();
  expect(screen.queryByText("Sin solicitudes en el período")).not.toBeInTheDocument();
});
