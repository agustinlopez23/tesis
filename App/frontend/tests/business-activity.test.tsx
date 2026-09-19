import { act, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { BusinessInsights } from "../src/app/business-insights";
import { profileMeasurementToken, rememberDiscovery, trackActivity, useImpression, useProfileVisit } from "../src/app/activity";

const id = "d71cd8d6-7245-492f-92de-4caa0cf11e50";
const report = { days: 7, updatedAt: "2026-09-18T20:00:00Z", timeZone: "America/Argentina/Cordoba",
  impressions: 3, profileViews: 2, contacts: 1, daily: [
    { date: "2026-09-17", impressions: 2, profileViews: 1, contacts: 0 },
    { date: "2026-09-18", impressions: 1, profileViews: 1, contacts: 1 }] };

beforeEach(() => { sessionStorage.clear(); window.history.replaceState({}, ""); });
afterEach(() => vi.unstubAllGlobals());

describe("actividad privada", () => {
  it("permite consultar período y evolución sin presentar clics como ventas", async () => {
    const fetcher = vi.spyOn(globalThis, "fetch").mockImplementation(async () => Response.json(report));
    render(<BusinessInsights businesses={[{ id, name: "Café", status: "published" }]} />);
    expect(fetcher).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: "Ver actividad" }));
    expect(await screen.findByRole("button", { name: /Apariciones en la guía 3/ })).toBeVisible();
    expect(screen.getByText(/Un clic abre WhatsApp/)).toBeVisible();
    fireEvent.change(screen.getByLabelText("Período"), { target: { value: "30" } });
    await waitFor(() => expect(fetcher).toHaveBeenLastCalledWith(`/api/marketplace/businesses/${id}/activity?days=30`, expect.anything()));
    fireEvent.click(await screen.findByRole("button", { name: /Clics en WhatsApp/ }));
    expect(screen.getByRole("group", { name: /Evolución diaria de Clics en WhatsApp/ })).toBeInTheDocument();
    fireEvent.click(screen.getByText("Ver datos por día"));
    expect(screen.getByRole("table", { name: "Actividad por día" })).toBeVisible();
  });
  it("no muestra ceros como resultado de un error y permite reintentar", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(new Response(null, { status: 503 })).mockResolvedValue(Response.json({ ...report, impressions: 0, profileViews: 0, contacts: 0, daily: [] }));
    render(<BusinessInsights businesses={[{ id, name: "Café", status: "draft" }]} />);
    fireEvent.click(screen.getByRole("button", { name: "Ver actividad" }));
    expect(await screen.findByRole("alert")).toHaveTextContent("No pudimos cargar la actividad");
    expect(screen.queryByText("Todavía no hay actividad en este período.")).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Reintentar" }));
    expect(await screen.findByText("Todavía no hay actividad en este período.")).toBeVisible();
    expect(screen.getByText(/está en borrador/)).toBeVisible();
  });
});

describe("observación en navegador", () => {
  it("atribuye sólo navegación explícita y conserva el contexto al recargar la misma página", () => {
    vi.useFakeTimers();
    expect(profileMeasurementToken(id, "direct")).toBe("direct");
    rememberDiscovery(id, "search-receipt");
    expect(profileMeasurementToken(id, "direct")).toBe("search-receipt");
    expect(profileMeasurementToken(id, "renewed-profile")).toBe("search-receipt");
    expect(profileMeasurementToken("another-business", "other")).toBe("other");
    vi.advanceTimersByTime(5 * 60 * 1000);
    expect(profileMeasurementToken(id, "direct")).toBe("direct");
  });
  it("cancela una aparición incompleta al desmontar y no suma intervalos separados", async () => {
    vi.useFakeTimers();
    const fetcher = vi.spyOn(globalThis, "fetch").mockResolvedValue(new Response(null, { status: 204 }));
    let intersect: (entries: Partial<IntersectionObserverEntry>[]) => void = () => {};
    vi.stubGlobal("IntersectionObserver", class { constructor(callback: typeof intersect) { intersect = callback; } observe() {} disconnect() {} });
    vi.spyOn(document, "visibilityState", "get").mockReturnValue("visible");
    const view = render(<Card />);
    act(() => intersect([{ isIntersecting: true, intersectionRatio: 1 }]));
    await act(() => vi.advanceTimersByTimeAsync(700));
    act(() => intersect([{ isIntersecting: false, intersectionRatio: 0 }]));
    act(() => intersect([{ isIntersecting: true, intersectionRatio: 1 }]));
    await act(() => vi.advanceTimersByTimeAsync(700));
    expect(fetcher).not.toHaveBeenCalled();
    view.unmount();
    await act(() => vi.advanceTimersByTimeAsync(2000));
    expect(fetcher).not.toHaveBeenCalled();
  });
  function Card() { const ref = useImpression(id, "receipt"); return <article ref={ref}>Café</article>; }
  function Profile() { useProfileVisit(id, "receipt"); return <h1>Café</h1>; }
  it("exige 50 % durante un segundo continuo y reinicia al ocultar la pestaña", async () => {
    vi.useFakeTimers();
    const fetcher = vi.spyOn(globalThis, "fetch").mockResolvedValue(new Response(null, { status: 204 }));
    let intersect: (entries: Partial<IntersectionObserverEntry>[]) => void = () => {};
    const disconnect = vi.fn();
    vi.stubGlobal("IntersectionObserver", class { constructor(callback: typeof intersect) { intersect = callback; } observe() {} disconnect = disconnect; });
    const visibility = vi.spyOn(document, "visibilityState", "get").mockReturnValue("visible");
    const view = render(<Card />);
    act(() => intersect([{ isIntersecting: true, intersectionRatio: .49 }]));
    await act(() => vi.advanceTimersByTimeAsync(1200));
    expect(fetcher).not.toHaveBeenCalled();
    act(() => intersect([{ isIntersecting: true, intersectionRatio: .5 }]));
    await act(() => vi.advanceTimersByTimeAsync(600));
    visibility.mockReturnValue("hidden");
    fireEvent(document, new Event("visibilitychange"));
    await act(() => vi.advanceTimersByTimeAsync(2000));
    expect(fetcher).not.toHaveBeenCalled();
    visibility.mockReturnValue("visible");
    fireEvent(document, new Event("visibilitychange"));
    await act(() => vi.advanceTimersByTimeAsync(999));
    expect(fetcher).not.toHaveBeenCalled();
    await act(() => vi.advanceTimersByTimeAsync(1));
    expect(fetcher).toHaveBeenCalledTimes(1);
    expect(JSON.parse(String(fetcher.mock.calls[0][1]?.body))).toMatchObject({ kind: "impression", surface: "directory", token: "receipt" });
    view.unmount();
    expect(disconnect).toHaveBeenCalled();
    render(<Card />);
    act(() => intersect([{ isIntersecting: true, intersectionRatio: 1 }]));
    await act(() => vi.advanceTimersByTimeAsync(1000));
    expect(fetcher).toHaveBeenCalledTimes(1);
  });
  it("espera al perfil visible y tolera fallos de medición sin bloquear el contacto", async () => {
    const visibility = vi.spyOn(document, "visibilityState", "get").mockReturnValue("hidden");
    const fetcher = vi.spyOn(globalThis, "fetch").mockRejectedValue(new Error("offline"));
    render(<Profile />);
    expect(fetcher).not.toHaveBeenCalled();
    visibility.mockReturnValue("visible");
    fireEvent(document, new Event("visibilitychange"));
    await waitFor(() => expect(fetcher).toHaveBeenCalledTimes(1));
    await expect(trackActivity(id, "receipt", "contact", "profile")).resolves.toBeUndefined();
  });
  it("rota la sesión a los 30 minutos y no mide si el almacenamiento está bloqueado", async () => {
    vi.useFakeTimers();
    const fetcher = vi.spyOn(globalThis, "fetch").mockResolvedValue(new Response(null, { status: 204 }));
    await trackActivity(id, "receipt", "contact", "profile");
    await trackActivity(id, "renewed-receipt", "contact", "profile");
    expect(fetcher).toHaveBeenCalledTimes(1);
    await vi.advanceTimersByTimeAsync(30 * 60 * 1000);
    await trackActivity(id, "renewed-receipt", "contact", "profile");
    expect(fetcher).toHaveBeenCalledTimes(2);
    const bodies = fetcher.mock.calls.map(call => JSON.parse(String(call[1]?.body)));
    expect(bodies[0].visitId).not.toBe(bodies[1].visitId);
    vi.spyOn(Storage.prototype, "getItem").mockImplementation(() => { throw new Error("blocked"); });
    await trackActivity(id, "receipt", "contact", "directory");
    expect(fetcher).toHaveBeenCalledTimes(2);
  });
});
