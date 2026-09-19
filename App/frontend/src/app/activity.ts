"use client";

import { useEffect, useRef } from "react";

type Kind = "impression" | "profile_view" | "contact";
type Surface = "directory" | "profile";
type Visit = { id: string; expires: number; sent: string[] };
type Referral = { id: string; token: string; expires: number };
const storageKey = "cerca:measurement:v1";
const referralKey = "cerca:discovery-referral:v1";

export async function trackActivity(id: string, token: string | undefined, kind: Kind, surface: Surface) {
  if (!token) return;
  try {
    const saved = sessionStorage.getItem(storageKey);
    let visit: Visit | null = saved ? JSON.parse(saved) : null;
    if (!visit || visit.expires <= Date.now()) visit = { id: crypto.randomUUID(), expires: Date.now() + 30 * 60 * 1000, sent: [] };
    const key = [id, kind, surface].join(":");
    if (visit.sent.includes(key) || visit.sent.length >= 1000) return;
    visit.sent.push(key);
    // Mark before sending: navigation, StrictMode and double-clicks share the same identity.
    sessionStorage.setItem(storageKey, JSON.stringify(visit));
    await fetch("/api/marketplace/public/events", { method: "POST", keepalive: true, cache: "no-store",
      headers: { "Content-Type": "application/json", "X-Requested-With": "marketplace" },
      body: JSON.stringify({ token, visitId: visit.id, kind, surface }), signal: AbortSignal.timeout(5000) });
  } catch { /* Measurement is best effort; storage/network failure must not interrupt navigation. */ }
}

export function rememberDiscovery(id: string, token?: string) {
  if (!token) return;
  try { sessionStorage.setItem(referralKey, JSON.stringify({ id, token, expires: Date.now() + 5 * 60 * 1000 })); }
  catch { /* No attribution if storage is unavailable. */ }
}

export function profileMeasurementToken(id: string, fallback?: string): string | undefined {
  try {
    const existing = window.history.state?.cercaActivity as Referral | undefined;
    if (existing?.id === id && existing.expires > Date.now()) return existing.token;
    const saved = sessionStorage.getItem(referralKey);
    if (!saved) return fallback;
    const referral: Referral = JSON.parse(saved);
    sessionStorage.removeItem(referralKey);
    if (referral.id === id && referral.expires > Date.now()) {
      window.history.replaceState({ ...window.history.state, cercaActivity: referral }, "");
      return referral.token;
    }
  } catch { /* Preserve unknown origin instead of inventing attribution. */ }
  return fallback;
}

export function useImpression(id: string, token?: string) {
  const ref = useRef<HTMLElement>(null);
  useEffect(() => {
    if (!token || !ref.current || typeof IntersectionObserver === "undefined") return;
    let qualifies = false;
    let sent = false;
    let timer: ReturnType<typeof setTimeout> | undefined;
    function update() {
      if (!qualifies || document.visibilityState !== "visible") { clearTimeout(timer); timer = undefined; return; }
      if (timer !== undefined || sent) return;
      timer = setTimeout(() => {
        if (document.visibilityState !== "visible" || !qualifies) return;
        sent = true;
        void trackActivity(id, token, "impression", "directory");
      }, 1000);
    }
    const observer = new IntersectionObserver((entries) => {
      const entry = entries.at(-1);
      qualifies = Boolean(entry?.isIntersecting && entry.intersectionRatio >= .5);
      update();
    }, { threshold: [0, .5] });
    observer.observe(ref.current);
    document.addEventListener("visibilitychange", update);
    return () => { clearTimeout(timer); observer.disconnect(); document.removeEventListener("visibilitychange", update); };
  }, [id, token]);
  return ref;
}

export function useProfileVisit(id: string, token?: string) {
  useEffect(() => {
    if (!token) return;
    function visit() {
      if (document.visibilityState === "visible") void trackActivity(id, profileMeasurementToken(id, token), "profile_view", "profile");
    }
    visit();
    document.addEventListener("visibilitychange", visit);
    return () => document.removeEventListener("visibilitychange", visit);
  }, [id, token]);
}
