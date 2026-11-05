"use client";

import { useId, useRef } from "react";
import { Icon } from "./ui";

export const photoTypes = ["image/jpeg", "image/png", "image/webp"];

/**
 * Photo input for phones: one button opens the rear camera directly (capture) and the
 * other opens the gallery or file browser. Desktop browsers ignore capture, so the camera
 * button is hidden on fine pointers by CSS.
 */
export default function PhotoPicker({ label, onSelect, disabled = false, fileName, describedBy }: {
  label: string;
  onSelect: (file: File | null) => void;
  disabled?: boolean;
  fileName?: string;
  describedBy?: string;
}) {
  const id = useId();
  const camera = useRef<HTMLInputElement>(null), gallery = useRef<HTMLInputElement>(null);
  // Reset the input so choosing the same file again still fires onChange.
  function pick(input: HTMLInputElement) { const file = input.files?.[0] ?? null; input.value = ""; onSelect(file); }
  return <div className="photo-picker">
    <label htmlFor={id} className="photo-picker-label">{label}</label>
    <div className="photo-picker-actions">
      <button type="button" className="button button-outline photo-picker-camera" disabled={disabled} onClick={() => camera.current?.click()}><Icon name="camera" />Sacar foto</button>
      <button type="button" className="button button-outline" disabled={disabled} aria-describedby={describedBy} onClick={() => gallery.current?.click()}><Icon name="image" />Elegir de la galería</button>
    </div>
    <input ref={camera} type="file" accept={photoTypes.join(",")} capture="environment" hidden tabIndex={-1} disabled={disabled} aria-label={label + " con la cámara"} onChange={(e) => pick(e.target)} />
    <input ref={gallery} id={id} type="file" accept={photoTypes.join(",")} hidden tabIndex={-1} disabled={disabled} aria-describedby={describedBy} onChange={(e) => pick(e.target)} />
    <p className="photo-picker-file" aria-live="polite">{fileName || "Ninguna foto elegida"}</p>
  </div>;
}
