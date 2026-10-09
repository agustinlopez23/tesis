import type { Metadata } from "next";
import type { ReactNode } from "react";
import Link from "next/link";
import { rankingPreviewEnabled } from "@/lib/ranking-preview";
import "./globals.css";

export const metadata: Metadata = {
  title: "Cerca · Comercios de Córdoba",
  description: "Descubrí comercios locales de Córdoba y conectá directamente por WhatsApp. Tu próximo favorito está cerca.",
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return <html lang="es-AR"><body><a className="skip-link" href="#contenido">Saltar al contenido</a>
    {rankingPreviewEnabled() && <aside className="preview-access"><span>Vista previa local</span><Link href="/revista" prefetch={false}>Probar la revista de tu zona →</Link></aside>}
    {children}</body></html>;
}
