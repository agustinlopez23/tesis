import { isIP } from "node:net";

// The API only sees this server's address, so the visitor address travels in a header that the API
// trusts only together with the shared secret. Vercel sets x-real-ip and overwrites x-forwarded-for.
export function clientAddressHeaders(headers: Headers, secret: string | undefined): Record<string, string> {
  if (!secret) return {};
  const candidate = (headers.get("x-real-ip") ?? headers.get("x-forwarded-for")?.split(",")[0] ?? "").trim();
  const forwarded: Record<string, string> = { "X-Marketplace-Proxy-Secret": secret };
  if (candidate.length <= 45 && isIP(candidate)) forwarded["X-Marketplace-Client-IP"] = candidate;
  return forwarded;
}
