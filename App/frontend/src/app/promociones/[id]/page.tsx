import PromotionDetail from "../../promotion-detail";

export const metadata = { title: "Promoción local · Cerca", robots: { index: false, follow: false } };
export default async function PromotionPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <PromotionDetail key={id} id={id} />;
}
