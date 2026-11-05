"use client";

import { useEffect, useRef, useState } from "react";
import { Icon } from "../ui";
import { DiscoveryLocation, radii, zones } from "./discovery-location";

export const radiusLabel = (radius: number) => radius < 1 ? radius * 1000 + " m" : radius + " km";
// Zone names carry their reference point ("Centro · Plaza San Martín"); the chip shows the short name.
const zoneShortName = (name: string) => name.split(" · ")[0];

export function LocationFilter({ area, onChange }: {
  area: DiscoveryLocation | null; onChange: (area: DiscoveryLocation | null) => void;
}) {
  const request = useRef(0);
  const [locating, setLocating] = useState(false);
  const [error, setError] = useState("");
  useEffect(() => {
    const invalidate = () => { request.current++; };
    const cancel = () => { invalidate(); setLocating(false); setError(""); };
    window.addEventListener("popstate", cancel);
    return () => { invalidate(); window.removeEventListener("popstate", cancel); };
  }, []);

  function change(next: DiscoveryLocation | null) {
    request.current++;
    setLocating(false);
    setError("");
    onChange(next);
  }
  function locate() {
    const id = ++request.current;
    setError("");
    if (!navigator.geolocation) {
      setError("Este navegador no ofrece ubicación. Elegí una zona de referencia.");
      return;
    }
    setLocating(true);
    navigator.geolocation.getCurrentPosition((position) => {
      if (id !== request.current) return;
      const { latitude: lat, longitude: lon } = position.coords;
      if (!Number.isFinite(lat) || Math.abs(lat) > 90 || !Number.isFinite(lon) || Math.abs(lon) > 180) {
        setLocating(false); setError("No pudimos determinar tu ubicación. Elegí una zona de referencia."); return;
      }
      change({ kind: "device", lat, lon, radiusKm: area?.radiusKm ?? 2 });
    }, (failure) => {
      if (id !== request.current) return;
      setLocating(false);
      setError(failure.code === 1 ? "No se habilitó el permiso. Elegí una zona o seguí explorando sin ubicación." :
        "No pudimos determinar tu ubicación. Elegí una zona o volvé a intentarlo.");
    }, { timeout: 10000, maximumAge: 60000, enableHighAccuracy: false });
  }
  const zone = zones.find((item) => item.id === area?.zone);
  const device = area?.kind === "device";
  return <div className="location-filter" role="group" aria-label="Filtrar por ubicación">
    <div className="chip-row">
      <label className={"chip chip-select " + (zone ? "selected" : "")}><Icon name="pin" /><span className="sr-only">Zona de referencia</span>
        <select value={zone?.id ?? ""} onChange={(event) => {
          const next = zones.find((item) => item.id === event.target.value);
          change(next ? { kind: "zone", zone: next.id, lat: next.lat, lon: next.lon, radiusKm: area?.radiusKm ?? 2 } : null);
        }}><option value="">Elegir zona</option>{zones.map((item) => <option key={item.id} value={item.id}>{zoneShortName(item.name)}</option>)}</select>
        <Icon name="chevron" className="chip-caret" />
      </label>
      <button type="button" className={"chip " + (device ? "selected" : "")} aria-pressed={device} onClick={locate} disabled={locating}>
        <Icon name="map" />{locating ? "Buscando ubicación…" : "Usar mi ubicación"}
      </button>
      {area && <label className="chip chip-select"><span className="sr-only">Radio de búsqueda</span>
        <select value={area.radiusKm} onChange={(event) => change({ ...area, radiusKm: Number(event.target.value) })}>
          {radii.map((radius) => <option key={radius} value={radius}>Hasta {radiusLabel(radius)}</option>)}
        </select>
        <Icon name="chevron" className="chip-caret" />
      </label>}
      {(area || locating) && <button type="button" className="chip chip-clear" onClick={() => change(null)} aria-label={area ? "Quitar ubicación" : "Cancelar ubicación"}>{area ? "Quitar" : "Cancelar"}<Icon name="close" /></button>}
    </div>
    <p className="location-summary" aria-live="polite">
      {area ? <><strong>{device ? "Tu ubicación actual" : zone?.name}</strong> · hasta {radiusLabel(area.radiusKm)} en línea recta{device ? "; tus coordenadas no se incluyen al compartir el enlace." : "; el radio puede incluir barrios vecinos."}</>
        : locating ? "Esperando la ubicación del navegador." : "Elegí una zona o usá tu ubicación para ver distancias."}
    </p>
    {error && <p role="alert" className="location-error"><Icon name="alert" />{error}</p>}
  </div>;
}
