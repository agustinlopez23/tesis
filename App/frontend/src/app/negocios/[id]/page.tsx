import BusinessProfile from "../../business-profile";

export default async function BusinessPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <BusinessProfile key={id} id={id} />;
}
