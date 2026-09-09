import { checkApiHealth } from "@/lib/check-api-health";
import OwnerOnboarding from "./owner-onboarding";

export const dynamic = "force-dynamic";

export default async function Home() {
  const apiAvailable = await checkApiHealth(process.env.NEXT_PUBLIC_API_URL);
  return (
    <main>
      <header className="hero"><div><p className="eyebrow">Marketplace hiperlocal · Córdoba</p><h1>Tu comercio, cerca de tus clientes.</h1>
        <p>Creá el perfil de tu negocio y preparalo para aparecer en la guía local.</p></div>
        <p role="status" className={apiAvailable ? "online" : "offline"}>{apiAvailable ? "API disponible" : "API no disponible"}</p></header>
      <OwnerOnboarding />
    </main>
  );
}
