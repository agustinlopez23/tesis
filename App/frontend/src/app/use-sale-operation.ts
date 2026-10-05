"use client";

import { useEffect, useState } from "react";
import { catalogApi, catalogError } from "./catalog-api";

type Intent = { path: string; body: unknown };
// Persist the complete operation before any mutation that can change stock.
export function useSaleOperation(storageKey: string, onSaved: (data: unknown) => void) {
  const [intent, setIntent] = useState<Intent | null>(null);
  const [restoring, setRestoring] = useState(true);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");
  useEffect(() => {
    const timer = window.setTimeout(() => {
      try { const stored = sessionStorage.getItem(storageKey); if (stored) setIntent(JSON.parse(stored)); }
      catch { setError("No pudimos recuperar el intento guardado."); }
      setRestoring(false);
    }, 0);
    return () => window.clearTimeout(timer);
  }, [storageKey]);
  async function submit(next: () => Intent) {
    if (pending || restoring) return;
    setPending(true); setError("");
    try {
      const operation = intent ?? next();
      sessionStorage.setItem(storageKey, JSON.stringify(operation)); setIntent(operation);
      const response = await catalogApi(operation.path, "PUT", operation.body);
      if (!response.ok) {
        const message = await catalogError(response);
        if (response.status === 401) { setError(message); return; }
        if ([400, 403, 404, 409].includes(response.status)) {
          sessionStorage.removeItem(storageKey); setIntent(null); setError(message); return;
        }
        throw new Error(message);
      }
      const result = await response.json();
      sessionStorage.removeItem(storageKey); setIntent(null); onSaved(result);
    } catch { setError("No pudimos confirmar la respuesta. Reintentá la misma operación para evitar duplicados."); }
    finally { setPending(false); }
  }
  return { intent, pending, restoring, error, submit, locked: pending || restoring || !!intent };
}
