export type Campaign = { id: string; businessId: string; businessName: string; productId: string | null; photoSlot: number | null; hasImage: boolean;
  title: string; description: string; conditions: string; zoneId: string; startsAt: string; endsAt: string; status: string; deliveryState: string; version: number; paymentReference: string | null; rejectionReason?: string | null };
export type PublicCampaign = Pick<Campaign, "id" | "businessId" | "businessName" | "productId" | "hasImage" | "title" | "description" | "conditions" | "zoneId" | "endsAt"> & { token: string; whatsApp: string };
export const campaignState = (value: string) => ({ draft: "Borrador", pending_review: "En revisión", awaiting_payment: "Pendiente de pago", scheduled: "Programada", active: "Activa", blocked: "Oculta por elegibilidad", paused: "Pausada", finished: "Finalizada", rejected: "Rechazada" }[value] ?? value);
export function rememberCampaign(campaign: PublicCampaign) {
  try { sessionStorage.setItem(`cerca-campaign:${campaign.id}`, JSON.stringify({ businessId: campaign.businessId, token: campaign.token, expires: Date.now() + 30 * 60000 })); } catch { /* Browsing remains available without attribution storage. */ }
}
export function campaignAttribution(businessId: string): string | undefined {
  try {
    const id = new URLSearchParams(window.location.search).get("campaign"); if (!id) return;
    const saved = JSON.parse(sessionStorage.getItem(`cerca-campaign:${id}`) ?? "null");
    if (saved?.businessId === businessId && saved.expires > Date.now()) return saved.token;
  } catch { /* Unknown source stays unknown. */ }
}
export async function campaignEvent(token: string, kind: "impression" | "click") {
  try { await fetch("/api/marketplace/public/campaign-events", { method: "POST", cache: "no-store", keepalive: true,
    headers: { "Content-Type": "application/json", "X-Requested-With": "marketplace" }, body: JSON.stringify({ token, kind }) }); } catch { /* Measurement must not block navigation. */ }
}
