using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Marketplace.Api;

public sealed record ActivityContext(Guid BusinessId, string Source, int? Position, string? RankingVersion);

public sealed class ActivityTokens(IDataProtectionProvider provider)
{
    private readonly ITimeLimitedDataProtector protector = provider.CreateProtector("Cerca.Activity.v1").ToTimeLimitedDataProtector();
    public string Issue(ActivityContext context) => protector.Protect(JsonSerializer.Serialize(context), TimeSpan.FromMinutes(30));
    public ActivityContext? Read(string? token)
    {
        if (string.IsNullOrEmpty(token) || token.Length > 2048) return null;
        try { return JsonSerializer.Deserialize<ActivityContext>(protector.Unprotect(token)); }
        catch (CryptographicException) { return null; }
        catch (JsonException) { return null; }
    }
}
