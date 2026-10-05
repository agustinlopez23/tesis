import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, expect, it, vi } from "vitest";
import { useRankingPreview } from "../src/app/revista/use-ranking-preview";

const selection = { zoneId: "centro", categoryId: 1 };
const business = { id: "1", name: "Negocio", category: "Almacenes", description: "", address: "Córdoba", whatsApp: "+5493515551234" };
const data = (extra = {}) => ({ items: [business], hasMore: true, cursor: "first", nextCursor: "second",
  expiresAt: new Date(Date.now() + 60000).toISOString(), rankingVersion: "policy", measurement: { "1": "protected" }, ...extra });
beforeEach(() => window.history.replaceState(null, "", "/revista"));

it("no crea al montar y reintenta con el mismo requestId incluso tras recargar", async () => {
  const fetcher = vi.spyOn(globalThis, "fetch").mockRejectedValueOnce(new Error("offline")).mockResolvedValueOnce(Response.json(data()));
  const first = renderHook(() => useRankingPreview());
  expect(fetcher).not.toHaveBeenCalled();
  await act(() => first.result.current.start(selection));
  expect(first.result.current.error).toBe(0);
  const body = fetcher.mock.calls[0][1]?.body;
  first.unmount();
  const second = renderHook(() => useRankingPreview());
  await waitFor(() => expect(second.result.current.data).not.toBeNull());
  expect(fetcher.mock.calls[1][1]?.body).toBe(body);
  expect(JSON.parse(String(body))).toMatchObject({ ...selection, radiusKm: 4 });
});

it("navega por GET, revalida al volver/recargar y no almacena tarjetas ni tokens en URL", async () => {
  const firstPage = data();
  const fetcher = vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(Response.json(firstPage))
    .mockResolvedValueOnce(Response.json({ ...firstPage, items: [], cursor: "second", nextCursor: "third" }))
    .mockResolvedValueOnce(Response.json({ ...firstPage, items: [] }))
    .mockResolvedValueOnce(Response.json({ ...firstPage, items: [] }));
  const first = renderHook(() => useRankingPreview());
  await act(() => first.result.current.start(selection));
  await act(() => first.result.current.go(2));
  expect(first.result.current.data?.items).toEqual([]);
  expect(first.result.current.data?.hasMore).toBe(true);
  await act(() => first.result.current.go(1));
  expect(first.result.current.data?.items).toEqual([]);
  first.unmount();
  const second = renderHook(() => useRankingPreview());
  await waitFor(() => expect(second.result.current.data).not.toBeNull());
  expect(fetcher.mock.calls.map(([, options]) => options?.method)).toEqual(["POST", "GET", "GET", "GET"]);
  expect(fetcher.mock.calls[1][0]).toContain("cursor=second");
  expect(fetcher.mock.calls[2][0]).toContain("cursor=first");
  expect(window.location.search).toBe("");
  expect(JSON.stringify(window.history.state)).not.toContain(business.name);
});

it.each([429, 503, 502])("conserva el intento tras %s y acepta recuperación", async (status) => {
  const fetcher = vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(new Response(null, { status })).mockResolvedValueOnce(Response.json(data()));
  const { result } = renderHook(() => useRankingPreview());
  await act(() => result.current.start(selection));
  expect(result.current.error).toBe(status);
  await act(() => result.current.retry());
  expect(fetcher.mock.calls[1][1]?.body).toBe(fetcher.mock.calls[0][1]?.body);
  expect(result.current.error).toBeNull();
});

it("descarta respuestas tardías al cambiar filtros y aborta la solicitud anterior", async () => {
  let resolve!: (response: Response) => void;
  const fetcher = vi.spyOn(globalThis, "fetch").mockImplementationOnce(() => new Promise(done => { resolve = done; }))
    .mockResolvedValueOnce(Response.json(data({ items: [] })));
  const { result } = renderHook(() => useRankingPreview());
  let pending!: Promise<void>;
  act(() => { pending = result.current.start(selection); });
  act(() => result.current.reset());
  expect(fetcher.mock.calls[0][1]?.signal?.aborted).toBe(true);
  await act(() => result.current.start({ ...selection, categoryId: 2 }));
  await act(async () => { resolve(Response.json(data())); await pending; });
  expect(result.current.data?.items).toEqual([]);
  expect(result.current.navigation?.selection.categoryId).toBe(2);
  expect(fetcher.mock.calls[1][1]?.body).not.toBe(fetcher.mock.calls[0][1]?.body);
});

it("retira tarjetas al vencer y actualizar crea una identidad nueva", async () => {
  vi.useFakeTimers();
  const fetcher = vi.spyOn(globalThis, "fetch").mockImplementation(async () => Response.json(data()));
  const { result } = renderHook(() => useRankingPreview());
  await act(() => result.current.start(selection));
  await act(() => vi.advanceTimersByTimeAsync(60001));
  expect(result.current.data).toBeNull();
  expect(result.current.error).toBe(410);
  await act(() => result.current.retry());
  expect(fetcher).toHaveBeenCalledTimes(1);
  await act(() => result.current.start(selection));
  expect(result.current.data).not.toBeNull();
  expect(fetcher.mock.calls[1][1]?.body).not.toBe(fetcher.mock.calls[0][1]?.body);
});

it("no mezcla una página con otra versión o vencimiento y atiende atrás del navegador", async () => {
  const first = data();
  const fetcher = vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(Response.json(first))
    .mockResolvedValueOnce(Response.json({ ...first, cursor: "second", rankingVersion: "other" }))
    .mockResolvedValueOnce(Response.json(first));
  const { result } = renderHook(() => useRankingPreview());
  await act(() => result.current.start(selection));
  const saved = window.history.state;
  await act(() => result.current.go(2));
  expect(result.current.data).toBeNull();
  expect(result.current.error).toBe(0);
  await act(async () => {
    window.history.replaceState(saved, "");
    window.dispatchEvent(new PopStateEvent("popstate"));
  });
  await waitFor(() => expect(result.current.navigation?.page).toBe(1));
  expect(fetcher.mock.calls[2][1]?.method).toBe("GET");
});
