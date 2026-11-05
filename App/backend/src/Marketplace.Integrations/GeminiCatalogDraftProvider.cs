using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Marketplace.Integrations;

/// <summary>Strategy for the Gemini API generateContent method with a JSON response schema.</summary>
public sealed class GeminiCatalogDraftProvider(HttpClient client, CatalogAiOptions options) : CatalogDraftProviderBase(client, options)
{
    public override string Name => "Google Gemini";

    protected override HttpRequestMessage CreateRequest(string facts, byte[]? jpegPhoto)
    {
        // The key goes in a header, never in the URL, so it does not reach access logs.
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(Options.Model!)}:generateContent";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("x-goog-api-key", Options.ApiKey);
        request.Content = JsonContent.Create(new
        {
            systemInstruction = new { parts = new[] { new { text = Instructions } } },
            contents = new[] { new { role = "user", parts = Parts(facts, jpegPhoto) } },
            generationConfig = new { responseMimeType = "application/json", responseJsonSchema = DraftSchema, maxOutputTokens = 4096 }
        });
        return request;
    }

    protected override string? ExtractText(JsonElement root)
    {
        if (root.TryGetProperty("promptFeedback", out var feedback) && feedback.TryGetProperty("blockReason", out _)) return null;
        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() != 1) return null;
        var candidate = candidates[0];
        if (candidate.GetProperty("finishReason").GetString() != "STOP") return null;
        var text = new StringBuilder();
        foreach (var part in candidate.GetProperty("content").GetProperty("parts").EnumerateArray())
        {
            // Thought summaries are not part of the answer.
            if (part.TryGetProperty("thought", out var thought) && thought.ValueKind == JsonValueKind.True) continue;
            if (part.TryGetProperty("text", out var value)) text.Append(value.GetString());
        }
        return text.Length == 0 ? null : text.ToString();
    }

    // The photo travels inline as base64 bytes; nothing is uploaded to provider file storage.
    private static object[] Parts(string facts, byte[]? jpegPhoto) => jpegPhoto is null
        ? [new { text = facts }]
        : [new { text = facts }, new { inlineData = new { mimeType = "image/jpeg", data = Convert.ToBase64String(jpegPhoto) } }];
}
