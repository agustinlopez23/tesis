using Marketplace.BLL;

namespace Marketplace.Integrations;

/// <summary>
/// Factory: picks the catalog AI strategy from configuration, so changing provider
/// is a setting (CatalogAi:Provider) and the business layer never changes.
/// </summary>
public static class CatalogDraftProviderFactory
{
    public static readonly IReadOnlyList<string> Supported = ["OpenAI", "Gemini"];

    /// <summary>Fails at startup on an unknown provider instead of at the first request.</summary>
    public static void Validate(CatalogAiOptions options) => _ = Key(options.Provider);

    public static ICatalogDraftProvider Create(CatalogAiOptions options, HttpClient client) => Key(options.Provider) switch
    {
        "gemini" => new GeminiCatalogDraftProvider(client, options),
        _ => new OpenAiCatalogDraftProvider(client, options)
    };

    private static string Key(string? provider)
    {
        var key = string.IsNullOrWhiteSpace(provider) ? "openai" : provider.Trim().ToLowerInvariant();
        return key is "openai" or "gemini"
            ? key
            : throw new InvalidOperationException($"CatalogAi:Provider '{provider}' is not supported. Use one of: {string.Join(", ", Supported)}.");
    }
}
