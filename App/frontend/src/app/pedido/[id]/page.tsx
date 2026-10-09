import { SiteHeader, SiteFooter } from "../../ui";
import OrderTracking from "./tracking";

export const metadata = { title: "Mi solicitud | Cerca", robots: { index: false, follow: false }, referrer: "no-referrer" };
export default async function OrderPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <><SiteHeader /><main id="contenido" className="container order-page"><h1>Mi solicitud</h1><OrderTracking key={id} id={id} /></main><SiteFooter /></>;
}
