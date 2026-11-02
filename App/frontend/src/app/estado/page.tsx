import { checkApiHealth } from "@/lib/check-api-health";
import { SiteHeader } from "../ui";

export const dynamic = "force-dynamic";
export default async function StatusPage() {
  const available = await checkApiHealth(process.env.API_BASE_URL ?? process.env.NEXT_PUBLIC_API_URL);
  return <><SiteHeader /><main id="contenido" tabIndex={-1} className="container status-page"><h1>Estado del servicio</h1><p role="status">{available ? "API disponible" : "API no disponible"}</p></main></>;
}
