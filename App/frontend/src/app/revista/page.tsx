import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { connection } from "next/server";
import { rankingEnabled } from "@/lib/ranking-preview";
import RankingPreview from "./ranking-preview";

export const metadata: Metadata = { title: "Revista local · Cerca", robots: { index: false, follow: false } };

export default async function MagazinePage() {
  await connection();
  if (!rankingEnabled()) notFound();
  return <RankingPreview />;
}
