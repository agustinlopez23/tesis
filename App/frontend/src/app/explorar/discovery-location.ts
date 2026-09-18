// Reference points, not neighborhood boundaries. Sources are recorded in the change design.
export const zones = [
  { id: "centro", name: "Centro · Plaza San Martín", lat: -31.416281, lon: -64.184022 },
  { id: "nueva-cordoba", name: "Nueva Córdoba · Buen Pastor", lat: -31.423889, lon: -64.186389 },
  { id: "alberdi", name: "Alberdi · Plaza Colón", lat: -31.4093, lon: -64.19555 },
];
export const radii = [0.5, 1, 2, 5, 10, 25, 50];
export type DiscoveryLocation = { kind: "zone" | "device"; zone?: string; lat: number; lon: number; radiusKm: number };

export function readArea(params: URLSearchParams, state: unknown): DiscoveryLocation | null {
  const radius = Number(params.get("radiusKm") ?? 2);
  const radiusKm = radii.includes(radius) ? radius : 2;
  const zone = zones.find((item) => item.id === params.get("zone"));
  if (zone) return { kind: "zone", zone: zone.id, lat: zone.lat, lon: zone.lon, radiusKm };
  const saved = (state as { discoveryLocation?: DiscoveryLocation } | null)?.discoveryLocation;
  if (params.get("near") === "me" && saved?.kind === "device" &&
    Number.isFinite(saved.lat) && Math.abs(saved.lat) <= 90 &&
    Number.isFinite(saved.lon) && Math.abs(saved.lon) <= 180) return { ...saved, radiusKm };
  return null;
}

export function areaParams(params: URLSearchParams, area: DiscoveryLocation | null, publicUrl = false) {
  if (!area) return;
  params.set("radiusKm", String(area.radiusKm));
  if (publicUrl) {
    if (area.kind === "zone" && area.zone) params.set("zone", area.zone);
    else params.set("near", "me");
  } else {
    params.set("lat", String(area.lat));
    params.set("lon", String(area.lon));
  }
}

export function distanceLabel(distance: number) {
  if (distance < 0.1) return "A menos de 100 m";
  if (distance < 1) return Math.round(distance * 100) * 10 + " m aprox.";
  return new Intl.NumberFormat("es-AR", { maximumFractionDigits: 1 }).format(distance) + " km aprox.";
}
