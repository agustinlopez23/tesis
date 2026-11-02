// Opens WhatsApp with a prefilled message that names the business (and the product when there is one).
export function whatsAppLink(number: string, businessName: string, productName?: string): string {
  const topic = productName ? `por el producto «${productName}»` : "para hacerte una consulta";
  const message = `Hola, ${businessName}. Te escribo desde Cerca ${topic}.`;
  return `https://wa.me/${number.replace(/\D/g, "")}?text=${encodeURIComponent(message)}`;
}
