"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { zones } from "../explorar/discovery-location";
import type { Business } from "../business-card";

export type RankingSelection = { zoneId: string; categoryId: number };
export type RankingPage = { items: Business[]; hasMore: boolean; cursor: string; nextCursor: string | null;
  expiresAt: string; rankingVersion: string; measurement: Record<string, string>; policy?: string };
type Navigation = { selection: RankingSelection; requestId: string; startedAt: number; page: number;
  cursors: string[]; expiresAt?: string; rankingVersion?: string };
type State = { navigation: Navigation | null; data: RankingPage | null; busy: boolean; error: number | null };
const idle: State = { navigation: null, data: null, busy: false, error: null };
const route = "/api/marketplace/public/ranking-snapshots";
const historyKey = "cercaRankingPreview";
const deadline = (nav: Navigation) => nav.expiresAt ? Date.parse(nav.expiresAt) : nav.startedAt + 30 * 60 * 1000;

function savedNavigation(value: unknown): Navigation | null {
  if (!value || typeof value !== "object") return null;
  const nav = value as Navigation;
  return nav.selection && zones.some(zone => zone.id === nav.selection.zoneId) &&
    Number.isInteger(nav.selection.categoryId) && nav.selection.categoryId > 0 &&
    typeof nav.requestId === "string" && /^[0-9a-f-]{36}$/i.test(nav.requestId) &&
    Number.isFinite(nav.startedAt) && nav.startedAt <= Date.now() && Number.isFinite(deadline(nav)) &&
    Number.isInteger(nav.page) && nav.page >= 1 && nav.page <= 500 &&
    Array.isArray(nav.cursors) && nav.cursors.length <= 500 &&
    nav.cursors.every(cursor => typeof cursor === "string" && cursor.length > 0 && cursor.length <= 2048) &&
    (nav.page === 1 || Boolean(nav.cursors[nav.page - 1])) ? nav : null;
}

function remember(nav: Navigation | null, push = false) {
  const state = { ...window.history.state, [historyKey]: nav };
  // Tokens live only in this history entry, never in links, storage or cached cards.
  if (push) window.history.pushState(state, "");
  else window.history.replaceState(state, "");
}

export function useRankingPreview() {
  const [state, setState] = useState<State>(idle);
  const generation = useRef(0);
  const pending = useRef<AbortController | null>(null);
  const cancel = useCallback(() => {
    ++generation.current;
    pending.current?.abort();
  }, []);
  const load = useCallback(async (nav: Navigation, push = false) => {
    const current = ++generation.current;
    pending.current?.abort();
    remember(nav, push);
    if (deadline(nav) <= Date.now()) {
      setState({ navigation: nav, data: null, busy: false, error: 410 });
      return;
    }
    const controller = new AbortController();
    pending.current = controller;
    setState({ navigation: nav, data: null, busy: true, error: null });
    try {
      const cursor = nav.cursors[nav.page - 1];
      const response = await fetch(cursor ? route + "/page?cursor=" + encodeURIComponent(cursor) : route, {
        method: cursor ? "GET" : "POST", cache: "no-store",
        signal: AbortSignal.any([controller.signal, AbortSignal.timeout(15000)]),
        ...(cursor ? {} : { headers: { "Content-Type": "application/json", "X-Requested-With": "marketplace" },
          body: JSON.stringify({ requestId: nav.requestId, ...nav.selection, radiusKm: 4 }) }),
      });
      if (current !== generation.current) return;
      if (!response.ok) {
        setState({ navigation: nav, data: null, busy: false, error: response.status });
        return;
      }
      const data: RankingPage = await response.json();
      if (current !== generation.current) return;
      if (!Array.isArray(data.items) || !data.cursor || !Number.isFinite(Date.parse(data.expiresAt)) ||
        (data.hasMore && !data.nextCursor) || !data.rankingVersion ||
        (nav.expiresAt && nav.expiresAt !== data.expiresAt) ||
        (nav.rankingVersion && nav.rankingVersion !== data.rankingVersion)) throw new Error("Invalid ranking page");
      const cursors = nav.cursors.slice(0, nav.page);
      cursors[nav.page - 1] = data.cursor;
      if (data.nextCursor) cursors.push(data.nextCursor);
      const next = { ...nav, cursors, expiresAt: data.expiresAt, rankingVersion: data.rankingVersion };
      remember(next);
      setState({ navigation: next, data: Date.parse(data.expiresAt) > Date.now() ? data : null,
        busy: false, error: Date.parse(data.expiresAt) > Date.now() ? null : 410 });
    } catch {
      if (current === generation.current) setState({ navigation: nav, data: null, busy: false, error: 0 });
    }
  }, []);

  const reset = useCallback(() => {
    cancel();
    remember(null);
    setState(idle);
  }, [cancel]);

  useEffect(() => {
    function restore() {
      const saved = savedNavigation(window.history.state?.[historyKey]);
      if (saved) void load(saved);
      else reset();
    }
    restore();
    window.addEventListener("popstate", restore);
    return () => {
      window.removeEventListener("popstate", restore);
      cancel();
    };
  }, [load, reset, cancel]);

  useEffect(() => {
    if (!state.navigation || state.error === 410) return;
    const nav = state.navigation;
    const timeout = setTimeout(() => {
      ++generation.current;
      pending.current?.abort();
      setState({ navigation: nav, data: null, busy: false, error: 410 });
    }, Math.max(0, deadline(nav) - Date.now()));
    return () => clearTimeout(timeout);
  }, [state.navigation, state.error]);

  const start = useCallback((selection: RankingSelection, push = true) => load({ selection,
    requestId: crypto.randomUUID(), startedAt: Date.now(), page: 1, cursors: [] }, push), [load]);

  return { ...state, reset, start,
    retry: async () => { if (state.navigation) await load(state.navigation); },
    go: async (page: number) => {
      if (state.navigation?.cursors[page - 1] && !state.busy) await load({ ...state.navigation, page }, true);
    },
  };
}
