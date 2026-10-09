"use client";

import { useEffect, useEffectEvent } from "react";

const eventName = "cerca:business-changed";
// Only confirmed writes invalidate other open panels in this browser tab.
export function businessChanged(business: string) {
  window.dispatchEvent(new CustomEvent(eventName, { detail: business }));
}
export function useBusinessRefresh(business: string, refresh: () => void) {
  const update = useEffectEvent(refresh);
  useEffect(() => {
    const listener = (event: Event) => { if ((event as CustomEvent<string>).detail === business) update(); };
    window.addEventListener(eventName, listener);
    return () => window.removeEventListener(eventName, listener);
  }, [business]);
}
