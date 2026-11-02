using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Marketplace.Api;

// Resolves the end-user address for per-client limits. The web proxy reaches the API server-side,
// so the connection address is the proxy's; the forwarded address is trusted only with the shared secret.
public static class ClientAddress
{
    public const string SecretHeader = "X-Marketplace-Proxy-Secret";
    public const string ClientIpHeader = "X-Marketplace-Client-IP";

    public static string Resolve(HttpContext context, string? sharedSecret)
    {
        var connection = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (string.IsNullOrEmpty(sharedSecret)) return connection;
        var provided = context.Request.Headers[SecretHeader].ToString();
        // Hashing both values gives equal-length inputs, so the comparison time does not leak the secret.
        if (!CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(provided)),
                SHA256.HashData(Encoding.UTF8.GetBytes(sharedSecret))))
            return connection;
        var forwarded = context.Request.Headers[ClientIpHeader].ToString().Trim();
        return forwarded.Length is > 0 and <= 45 && IPAddress.TryParse(forwarded, out var address)
            ? address.ToString()
            : connection;
    }
}
