import { checkApiHealth } from "@/lib/check-api-health";

export const dynamic = "force-dynamic";

export default async function Home() {
  const apiAvailable = await checkApiHealth(process.env.NEXT_PUBLIC_API_URL);
  return (
    <main>
      <h1>Marketplace local</h1>
      <p role="status">{apiAvailable ? "API disponible" : "API no disponible"}</p>
    </main>
  );
}
