"use client";

import { useEffect, useRef, useState } from "react";
import { Icon } from "../ui";
import { DiscoveryLocation, radii, zones } from "./discovery-location";

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
  const zoneName = zones.find((zone) => zone.id === area?.zone)?.name;
  return <div className="location-filter">
    <div className="location-intro"><span className="mini-icon"><Icon name="pin" /></span><div><strong>Más cerca de vos</strong><p>Elegí desde dónde querés explorar.</p></div></div>
    <div className="location-controls">
      <label className="zone-select">Zona de referencia
        <select value={area?.kind === "zone" ? area.zone : ""} onChange={(event) => {
          const zone = zones.find((item) => item.id === event.target.value);
          change(zone ? { kind: "zone", zone: zone.id, lat: zone.lat, lon: zone.lon, radiusKm: area?.radiusKm ?? 2 } : null);
        }}><option value="">Sin zona elegida</option>{zones.map((zone) => <option key={zone.id} value={zone.id}>{zone.name}</option>)}</select>
      </label>
      <span className="location-or">o</span>
      <button className="button button-outline locate-button" onClick={locate} disabled={locating}><Icon name="pin" />{locating ? "Buscando ubicación…" : "Usar mi ubicación"}</button>
      <label className="radius-select">Radio de búsqueda
        <select value={area?.radiusKm ?? 2} disabled={!area} onChange={(event) => { if (area) change({ ...area, radiusKm: Number(event.target.value) }); }}>
          {radii.map((radius) => <option key={radius} value={radius}>{radius < 1 ? radius * 1000 + " m" : radius + " km"}</option>)}
        </select>
      </label>
    </div>
    <div className="location-summary" aria-live="polite">
      {area ? <p><strong>{area.kind === "device" ? "Tu ubicación actual" : zoneName}</strong><span> · Hasta {area.radiusKm < 1 ? area.radiusKm * 1000 + " m" : area.radiusKm + " km"} en línea recta.</span></p> :
        <p>{locating ? "Esperando la ubicación del navegador." : "Explorás la guía general. Podés elegir una zona sin compartir tu ubicación."}</p>}
      {(area || locating) && <button className="text-link" onClick={() => change(null)} aria-label={area ? "Quitar ubicación" : "Cancelar ubicación"}>{area ? "Quitar" : "Cancelar"}<Icon name="close" /></button>}
    </div>
    {area?.kind === "zone" && <p className="location-note">Buscamos alrededor del punto de referencia; el radio puede incluir barrios vecinos.</p>}
    {area?.kind === "device" && <p className="location-note">La precisión depende del dispositivo. Tus coordenadas no se incluyen al compartir este enlace.</p>}
    {error && <p role="alert" className="location-error">{error}</p>}
  </div>;
}
