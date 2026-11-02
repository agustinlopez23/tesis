import { connection } from "next/server";
import { rankingEnabled } from "@/lib/ranking-preview";
import Explore from "./explore";

export default async function ExplorePage() {
  // Read the runtime switch per request so deployments can toggle it without rebuilding.
  await connection();
  return <Explore rankingEnabled={rankingEnabled()} />;
}
