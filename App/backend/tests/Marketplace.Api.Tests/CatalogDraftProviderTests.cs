using System.Net;
using System.Text.Json;
using Marketplace.Integrations;

namespace Marketplace.Api.Tests;

public sealed class CatalogDraftProviderTests
{
    [Fact]
    public async Task Uses_bounded_structured_output_without_storage_tools_or_private_context()
    {
        var handler = new ReplyHandler("""{"status":"completed","output":[{"type":"message","content":[{"type":"output_text","text":"{\"name\":\"Café\",\"description\":\"Paquete de 250 g\",\"category\":\"Café\"}"}]}]}""");
        using var http = new HttpClient(handler);
        var provider = new OpenAiCatalogDraftProvider(http, new(true, "test-credential", "configured-model"));
        var draft = await provider.Generate("Café de 250 g", null, default);
        Assert.NotNull(draft); Assert.Equal("Café", draft.Name);
        Assert.Equal("https://api.openai.com/v1/responses", handler.Url);
        using var body = JsonDocument.Parse(handler.Body!); var root = body.RootElement;
        Assert.False(root.GetProperty("store").GetBoolean());
        Assert.Equal("json_schema", root.GetProperty("text").GetProperty("format").GetProperty("type").GetString());
        Assert.False(root.TryGetProperty("tools", out _));
        var parts = Assert.Single(root.GetProperty("input").EnumerateArray()).GetProperty("content");
        var text = Assert.Single(parts.EnumerateArray());
        Assert.Equal("input_text", text.GetProperty("type").GetString()); Assert.Equal("Café de 250 g", text.GetProperty("text").GetString());
    }
    [Fact]
    public async Task Sends_the_photo_as_a_low_detail_image_input_without_storage()
    {
        var handler = new ReplyHandler("""{"status":"completed","output":[{"type":"message","content":[{"type":"output_text","text":"{\"name\":\"Taza\",\"description\":\"Taza blanca\",\"category\":\"Bazar\"}"}]}]}""");
        using var http = new HttpClient(handler);
        using var provider = new OpenAiCatalogDraftProvider(http, new(true, "test-credential", "configured-model"));
        byte[] photo = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3];
        Assert.Equal("Taza", (await provider.Generate("Taza de cerámica", photo, default))!.Name);
        using var body = JsonDocument.Parse(handler.Body!); var root = body.RootElement;
        Assert.False(root.GetProperty("store").GetBoolean());
        Assert.True(root.GetProperty("text").GetProperty("format").GetProperty("strict").GetBoolean());
        var message = Assert.Single(root.GetProperty("input").EnumerateArray());
        Assert.Equal("user", message.GetProperty("role").GetString());
        var parts = message.GetProperty("content").EnumerateArray().ToArray();
        Assert.Equal(2, parts.Length);
        Assert.Equal("input_text", parts[0].GetProperty("type").GetString());
        Assert.Equal("input_image", parts[1].GetProperty("type").GetString());
        Assert.Equal("data:image/jpeg;base64," + Convert.ToBase64String(photo), parts[1].GetProperty("image_url").GetString());
        Assert.Equal("low", parts[1].GetProperty("detail").GetString());
    }
    [Theory]
    [InlineData("{\"status\":\"incomplete\",\"output\":[]}")]
    [InlineData("{\"status\":\"completed\",\"output\":[{\"type\":\"message\",\"content\":[{\"type\":\"refusal\"}]}]}")]
    [InlineData("{\"status\":\"completed\",\"output\":[{\"type\":\"message\",\"content\":[{\"type\":\"output_text\",\"text\":\"{}\"}]}]}")]
    [InlineData("not-json")]
    public async Task Unusable_outputs_allow_manual_fallback(string response)
    {
        using var http = new HttpClient(new ReplyHandler(response));
        Assert.Null(await new OpenAiCatalogDraftProvider(http, new(true, "test-credential", "configured-model")).Generate("Café", null, default));
    }
    [Fact]
    public async Task Disabled_provider_makes_no_request_and_oversize_is_rejected()
    {
        var handler = new ReplyHandler(new string('x', 131073)); using var http = new HttpClient(handler);
        using var provider = new OpenAiCatalogDraftProvider(http, new(false, "test-credential", "configured-model"));
        Assert.Null(await provider.Generate("Café", null, default)); Assert.Null(handler.Url);
        using var enabled = new OpenAiCatalogDraftProvider(new HttpClient(handler), new(true, "test-credential", "configured-model"));
        Assert.Null(await enabled.Generate("Café", null, default)); Assert.NotNull(handler.Url);
    }
    private sealed class ReplyHandler(string content) : HttpMessageHandler
    {
        public string? Url, Body;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Url = request.RequestUri!.ToString(); Body = await request.Content!.ReadAsStringAsync(ct);
            return new(HttpStatusCode.OK) { Content = new StringContent(content) };
        }
    }
}
