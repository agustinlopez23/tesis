using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Marketplace.Integrations;

/// <summary>Strategy for the OpenAI Responses API with strict Structured Outputs.</summary>
public sealed class OpenAiCatalogDraftProvider(HttpClient client, CatalogAiOptions options) : CatalogDraftProviderBase(client, options)
{
    public override string Name => "OpenAI";

    protected override HttpRequestMessage CreateRequest(string facts, byte[]? jpegPhoto)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Options.ApiKey);
        request.Content = JsonContent.Create(new
        {
            model = Options.Model, store = false, max_output_tokens = 1000,
            instructions = Instructions,
            input = new[] { new { role = "user", content = Parts(facts, jpegPhoto) } },
            text = new { format = new { type = "json_schema", name = "catalog_draft", strict = true, schema = DraftSchema } }
        });
        return request;
    }

    protected override string? ExtractText(JsonElement root)
    {
        if (root.GetProperty("status").GetString() != "completed") return null;
        string? text = null;
        foreach (var item in root.GetProperty("output").EnumerateArray())
        {
            if (item.GetProperty("type").GetString() != "message") continue;
            foreach (var content in item.GetProperty("content").EnumerateArray())
            {
                if (content.GetProperty("type").GetString() == "refusal") return null;
                if (content.GetProperty("type").GetString() != "output_text") continue;
                if (text != null) return null;
                text = content.GetProperty("text").GetString();
            }
        }
        return text;
    }

    // The photo travels inline as a data URL with low detail; nothing is uploaded to provider storage.
    private static object[] Parts(string facts, byte[]? jpegPhoto) => jpegPhoto is null
        ? [new { type = "input_text", text = facts }]
        : [new { type = "input_text", text = facts },
           new { type = "input_image", image_url = "data:image/jpeg;base64," + Convert.ToBase64String(jpegPhoto), detail = "low" }];
}
