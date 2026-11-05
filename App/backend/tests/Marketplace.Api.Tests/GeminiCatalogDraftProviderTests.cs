using System.Net;
using System.Text.Json;
using Marketplace.Integrations;

namespace Marketplace.Api.Tests;

public sealed class GeminiCatalogDraftProviderTests
{
    private const string Draft = "{\\\"name\\\":\\\"Taza\\\",\\\"description\\\":\\\"Taza blanca\\\",\\\"category\\\":\\\"Bazar\\\"}";
    private static readonly string Completed = $$"""{"candidates":[{"content":{"role":"model","parts":[{"text":"{{Draft}}"}]},"finishReason":"STOP"}]}""";

    [Fact]
    public async Task Sends_photo_inline_with_json_schema_and_key_only_in_header()
    {
        var handler = new ReplyHandler(Completed); using var http = new HttpClient(handler);
        using var provider = new GeminiCatalogDraftProvider(http, new(true, "test-credential", "gemini-test-model", "Gemini"));
        byte[] photo = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3];
        var draft = await provider.Generate("Taza de cerámica", photo, default);
        Assert.Equal(new("Taza", "Taza blanca", "Bazar"), draft);
        Assert.Equal("https://generativelanguage.googleapis.com/v1beta/models/gemini-test-model:generateContent", handler.Url);
        Assert.DoesNotContain("test-credential", handler.Url);
        Assert.Equal("test-credential", handler.ApiKey);
        using var body = JsonDocument.Parse(handler.Body!); var root = body.RootElement;
        Assert.Contains("No inventes", root.GetProperty("systemInstruction").GetProperty("parts")[0].GetProperty("text").GetString());
        var config = root.GetProperty("generationConfig");
        Assert.Equal("application/json", config.GetProperty("responseMimeType").GetString());
        Assert.False(config.GetProperty("responseJsonSchema").GetProperty("additionalProperties").GetBoolean());
        Assert.False(root.TryGetProperty("tools", out _));
        var message = Assert.Single(root.GetProperty("contents").EnumerateArray());
        Assert.Equal("user", message.GetProperty("role").GetString());
        var parts = message.GetProperty("parts").EnumerateArray().ToArray();
        Assert.Equal(2, parts.Length);
        Assert.Equal("Taza de cerámica", parts[0].GetProperty("text").GetString());
        var image = parts[1].GetProperty("inlineData");
        Assert.Equal("image/jpeg", image.GetProperty("mimeType").GetString());
        Assert.Equal(Convert.ToBase64String(photo), image.GetProperty("data").GetString());
    }

    [Fact]
    public async Task Text_only_request_has_a_single_text_part_and_skips_thoughts()
    {
        var reply = $$"""{"candidates":[{"content":{"parts":[{"text":"pensando","thought":true},{"text":"{{Draft}}"}]},"finishReason":"STOP"}]}""";
        var handler = new ReplyHandler(reply); using var http = new HttpClient(handler);
        using var provider = new GeminiCatalogDraftProvider(http, new(true, "test-credential", "gemini-test-model", "Gemini"));
        Assert.Equal("Taza", (await provider.Generate("Taza", null, default))!.Name);
        using var body = JsonDocument.Parse(handler.Body!);
        var part = Assert.Single(body.RootElement.GetProperty("contents")[0].GetProperty("parts").EnumerateArray());
        Assert.False(part.TryGetProperty("inlineData", out _));
    }

    [Theory]
    [InlineData("""{"promptFeedback":{"blockReason":"SAFETY"}}""")]
    [InlineData("""{"candidates":[]}""")]
    [InlineData("""{"candidates":[{"content":{"parts":[{"text":"{}"}]},"finishReason":"SAFETY"}]}""")]
    [InlineData("""{"candidates":[{"content":{"parts":[{"text":"{\"name\":\"Taza\""}]},"finishReason":"MAX_TOKENS"}]}""")]
    [InlineData("""{"candidates":[{"content":{"parts":[{"text":"{}"}]},"finishReason":"STOP"}]}""")]
    [InlineData("""{"candidates":[{"content":{"parts":[]},"finishReason":"STOP"}]}""")]
    [InlineData("not-json")]
    public async Task Unusable_outputs_allow_manual_fallback(string response)
    {
        using var http = new HttpClient(new ReplyHandler(response));
        using var provider = new GeminiCatalogDraftProvider(http, new(true, "test-credential", "gemini-test-model", "Gemini"));
        Assert.Null(await provider.Generate("Taza", null, default));
    }

    [Fact]
    public async Task Disabled_or_unkeyed_provider_makes_no_request()
    {
        var handler = new ReplyHandler(Completed);
        using var disabled = new GeminiCatalogDraftProvider(new HttpClient(handler), new(false, "test-credential", "gemini-test-model", "Gemini"));
        using var unkeyed = new GeminiCatalogDraftProvider(new HttpClient(handler), new(true, null, "gemini-test-model", "Gemini"));
        Assert.False(disabled.Enabled); Assert.False(unkeyed.Enabled);
        Assert.Null(await disabled.Generate("Taza", null, default)); Assert.Null(await unkeyed.Generate("Taza", null, default));
        Assert.Null(handler.Url);
    }

    [Theory]
    [InlineData(null, typeof(OpenAiCatalogDraftProvider), "OpenAI")]
    [InlineData("", typeof(OpenAiCatalogDraftProvider), "OpenAI")]
    [InlineData("openai", typeof(OpenAiCatalogDraftProvider), "OpenAI")]
    [InlineData(" Gemini ", typeof(GeminiCatalogDraftProvider), "Google Gemini")]
    public void Factory_selects_the_strategy_from_configuration(string? configured, Type expected, string name)
    {
        var options = new CatalogAiOptions(true, "test-credential", "configured-model", configured);
        CatalogDraftProviderFactory.Validate(options);
        var provider = CatalogDraftProviderFactory.Create(options, new HttpClient());
        Assert.IsType(expected, provider); Assert.Equal(name, provider.Name);
        ((IDisposable)provider).Dispose();
    }

    [Fact]
    public void Factory_rejects_an_unknown_provider_even_when_disabled()
    {
        var error = Assert.Throws<InvalidOperationException>(() => CatalogDraftProviderFactory.Validate(new(false, null, null, "Claude")));
        Assert.Contains("OpenAI, Gemini", error.Message);
    }

    private sealed class ReplyHandler(string content) : HttpMessageHandler
    {
        public string? Url, Body, ApiKey;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Url = request.RequestUri!.ToString(); Body = await request.Content!.ReadAsStringAsync(ct);
            ApiKey = request.Headers.TryGetValues("x-goog-api-key", out var values) ? values.Single() : null;
            return new(HttpStatusCode.OK) { Content = new StringContent(content) };
        }
    }
}
