"use client";

import { useCallback, useEffect, useState } from "react";
import { Icon } from "./ui";

// One toast at a time, bottom centre, dismissed automatically or by the user.
export function Toast({ message, onClose, duration = 4000 }: { message: string; onClose: () => void; duration?: number }) {
  useEffect(() => {
    const timer = setTimeout(onClose, duration);
    return () => clearTimeout(timer);
  }, [message, duration, onClose]);
  return <div className="toast" role="status"><Icon name="check" /><span>{message}</span><button type="button" className="icon-button" aria-label="Cerrar aviso" onClick={onClose}><Icon name="close" /></button></div>;
}

export function useToast() {
  const [message, setMessage] = useState("");
  const close = useCallback(() => setMessage(""), []);
  const toast = message ? <Toast message={message} onClose={close} /> : null;
  return { show: setMessage, toast };
}
