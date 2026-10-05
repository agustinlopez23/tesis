using System.Security.Cryptography;
using System.Text.Json;
using Marketplace.BLL;
using Microsoft.AspNetCore.DataProtection;

namespace Marketplace.Api;

public sealed class CampaignTokens(IDataProtectionProvider provider) : ICampaignTokens
{
    private readonly ITimeLimitedDataProtector protector = provider.CreateProtector("Cerca.Campaign.v1").ToTimeLimitedDataProtector();
    public string Issue(CampaignContext context) => protector.Protect(JsonSerializer.Serialize(context), TimeSpan.FromMinutes(30));
    public CampaignContext? Read(string? token)
    {
        if (string.IsNullOrEmpty(token) || token.Length > 2048) return null;
        try { return JsonSerializer.Deserialize<CampaignContext>(protector.Unprotect(token)); }
        catch (CryptographicException) { return null; }
        catch (JsonException) { return null; }
    }
}
