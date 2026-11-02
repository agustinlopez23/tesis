"use client";

import { useEffect, useState } from "react";
import { Order, readOrder } from "../../order-api";
import OrderReceipt from "../../order-receipt";

export default function OrderTracking({ id }: { id: string }) {
  const [order, setOrder] = useState<Order | null>(null);
  const [key, setKey] = useState("");
  const [state, setState] = useState("loading");
  const [attempt, setAttempt] = useState(0);
  useEffect(() => {
    function changed() { setOrder(null); setState("loading"); setAttempt(n => n + 1); }
    window.addEventListener("hashchange", changed);
    return () => window.removeEventListener("hashchange", changed);
  }, []);
  useEffect(() => {
    const controller = new AbortController();
    async function load() {
      const value = new URLSearchParams(window.location.hash.slice(1)).get("clave") ?? "";
      if (!/^[0-9a-f]{64}$/.test(value)) { setState("missing"); return; }
      setKey(value);
      try {
        const response = await readOrder(id, value, controller.signal);
        if (controller.signal.aborted) return;
        if (response.status === 404) { setOrder(null); setState("missing"); return; }
        if (!response.ok) throw new Error();
        const data = await response.json();
        if (!controller.signal.aborted) { setOrder(data); setState("ready"); }
      } catch { if (!controller.signal.aborted) { setOrder(null); setState("error"); } }
    }
    void load();
    return () => controller.abort();
  }, [id, attempt]);
  return <>
    {state === "loading" && <p role="status">Consultando solicitud…</p>}
    {state === "missing" && <p role="alert">El enlace privado no es válido o venció. Necesitás el enlace completo que recibiste al confirmar; la referencia sola no permite acceder.</p>}
    {state === "error" && <p role="alert">No pudimos consultar la solicitud. Intentá nuevamente.</p>}
    {state === "ready" && order && <OrderReceipt order={order} accessKey={key} headingLevel={2} />}
    {state !== "missing" && <button className="button button-outline" disabled={state === "loading"} onClick={() => { setOrder(null); setState("loading"); setAttempt(n => n + 1); }}>Actualizar estado</button>}
  </>;
}
