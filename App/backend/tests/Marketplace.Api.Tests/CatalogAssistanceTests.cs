using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;
using Marketplace.BLL;
using Marketplace.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using SkiaSharp;

namespace Marketplace.Api.Tests;

public sealed class CatalogAssistanceTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    [Theory]
    [InlineData("valid", 200)]
    [InlineData("oversized", 503)]
    [InlineData("failure", 503)]
    public async Task Generated_draft_is_validated_and_never_persists_a_product(string outcome, int expected)
    {
        var provider = new StubProvider(outcome);
        await using var app = fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services => services.AddSingleton<ICatalogDraftProvider>(provider)));
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:3000"); client.DefaultRequestHeaders.Add("X-Requested-With", "marketplace");
        var email = $"draft-{Guid.NewGuid():N}@example.test"; var password = "Local-assistance-test-123";
        (await client.PostAsJsonAsync("/auth/register", new { email, password, displayName = "Titular" })).EnsureSuccessStatusCode();
        var login = await client.PostAsJsonAsync("/auth/login", new { email, password }); login.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Add("Cookie", Assert.Single(login.Headers.GetValues("Set-Cookie")).Split(';')[0]);
        var created = await client.PostAsJsonAsync("/businesses", new { name = "Local", description = "", address = "", categoryId = 1, whatsApp = "+5493515551234", latitude = -31.4, longitude = -64.18, status = "draft" });
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await using var db = new NpgsqlConnection(fixture.ConnectionString); await db.OpenAsync();
        await using var cmd = new NpgsqlCommand("INSERT INTO business_subscriptions (id,business_id,starts_at,ends_at,granted_by,granted_at,reason) SELECT gen_random_uuid(),id,now()-interval '1 day',now()+interval '1 day',owner_id,now(),'Test' FROM businesses WHERE id=$1", db); cmd.Parameters.AddWithValue(id); await cmd.ExecuteNonQueryAsync();
        var path = $"/businesses/{id}/catalog-assistance";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(path, new { facts = "Café", consent = false })).StatusCode);
        Assert.Equal(0, provider.Calls);
        var response = await client.PostAsJsonAsync(path, new { facts = "Café de 250 g", consent = true });
        Assert.Equal(expected, (int)response.StatusCode); Assert.Equal(1, provider.Calls);
        if (expected == 200) Assert.Equal("Café", (await response.Content.ReadFromJsonAsync<CatalogDraft>())!.Name);
        Assert.Empty((await client.GetFromJsonAsync<JsonElement>($"/businesses/{id}/products")).EnumerateArray());
    }
    [Fact]
    public async Task Photo_draft_is_normalized_sent_once_and_leaves_no_trace()
    {
        var provider = new StubProvider("valid");
        await using var app = fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services => services.AddSingleton<ICatalogDraftProvider>(provider)));
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:3000"); client.DefaultRequestHeaders.Add("X-Requested-With", "marketplace");
        var email = $"draft-{Guid.NewGuid():N}@example.test"; var password = "Local-assistance-test-123";
        (await client.PostAsJsonAsync("/auth/register", new { email, password, displayName = "Titular" })).EnsureSuccessStatusCode();
        var login = await client.PostAsJsonAsync("/auth/login", new { email, password }); login.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Add("Cookie", Assert.Single(login.Headers.GetValues("Set-Cookie")).Split(';')[0]);
        var created = await client.PostAsJsonAsync("/businesses", new { name = "Local", description = "", address = "", categoryId = 1, whatsApp = "+5493515551234", latitude = -31.4, longitude = -64.18, status = "draft" });
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await using var db = new NpgsqlConnection(fixture.ConnectionString); await db.OpenAsync();
        await using var cmd = new NpgsqlCommand("INSERT INTO business_subscriptions (id,business_id,starts_at,ends_at,granted_by,granted_at,reason) SELECT gen_random_uuid(),id,now()-interval '1 day',now()+interval '1 day',owner_id,now(),'Test' FROM businesses WHERE id=$1", db); cmd.Parameters.AddWithValue(id); await cmd.ExecuteNonQueryAsync();
        var path = $"/businesses/{id}/catalog-assistance";
        static MultipartFormDataContent Form(byte[] file, bool consent = true)
        {
            var form = new MultipartFormDataContent { { new StringContent("Taza de cerámica"), "facts" }, { new StringContent(consent ? "true" : "false"), "consent" } };
            form.Add(new ByteArrayContent(file), "file", "foto.png"); return form;
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(path, Form("no es una imagen"u8.ToArray()))).StatusCode);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await client.PostAsync(path, Form(new byte[5 * 1024 * 1024 + 1]))).StatusCode);
        using var surface = SKSurface.Create(new SKImageInfo(2000, 1000)); surface.Canvas.Clear(SKColors.Coral);
        using var image = surface.Snapshot(); using var png = image.Encode(SKEncodedImageFormat.Png, 100);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(path, Form(png.ToArray(), consent: false))).StatusCode);
        Assert.Equal(0, provider.Calls);
        var response = await client.PostAsync(path, Form(png.ToArray()));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Equal(1, provider.Calls);
        Assert.Equal("Café", (await response.Content.ReadFromJsonAsync<CatalogDraft>())!.Name);
        Assert.Equal("Taza de cerámica", provider.Facts);
        // The provider receives a re-encoded, downscaled JPEG, never the original upload.
        using var sent = SKBitmap.Decode(provider.Photo); Assert.Equal(1024, sent.Width); Assert.Equal(512, sent.Height);
        Assert.Equal(0xFF, provider.Photo![0]); Assert.Equal(0xD8, provider.Photo[1]);
        Assert.Empty((await client.GetFromJsonAsync<JsonElement>($"/businesses/{id}/products")).EnumerateArray());
        await using var count = new NpgsqlCommand("SELECT count(*) FROM catalog_products WHERE business_id=$1", db); count.Parameters.AddWithValue(id);
        Assert.Equal(0L, (long)(await count.ExecuteScalarAsync())!);
    }
    private sealed class StubProvider(string outcome) : ICatalogDraftProvider
    {
        public string Name => "OpenAI";
        public bool Enabled => true;
        public int Calls;
        public string? Facts; public byte[]? Photo;
        public Task<CatalogDraft?> Generate(string facts, byte[]? photo, CancellationToken ct)
        {
            Calls++; Facts = facts; Photo = photo; return Task.FromResult<CatalogDraft?>(outcome == "failure" ? null : new(outcome == "oversized" ? new string('x', 161) : "Café", "Paquete de 250 g", "Café"));
        }
    }

    [Fact]
    public async Task Assistance_is_private_opt_in_and_disabled_without_provider_configuration()
    {
        using var client = fixture.Client(); using var guest = fixture.Client();
        var email = $"draft-{Guid.NewGuid():N}@example.test"; var password = "Local-assistance-test-123";
        (await client.PostAsJsonAsync("/auth/register", new { email, password, displayName = "Titular" })).EnsureSuccessStatusCode();
        var login = await client.PostAsJsonAsync("/auth/login", new { email, password }); login.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Add("Cookie", Assert.Single(login.Headers.GetValues("Set-Cookie")).Split(';')[0]);
        var response = await client.PostAsJsonAsync("/businesses", new { name = "Café", description = "", address = "Córdoba", categoryId = 2, whatsApp = "+5493515551234", latitude = -31.4, longitude = -64.18, status = "draft" });
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var path = $"/businesses/{id}/catalog-assistance";
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/businesses/{Guid.NewGuid()}/catalog-assistance")).StatusCode);
        var status = await client.GetAsync(path); Assert.Equal(HttpStatusCode.OK, status.StatusCode);
        Assert.False((await status.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("enabled").GetBoolean());
        Assert.Equal("no-store", status.Headers.CacheControl!.ToString());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(path, new { facts = "Café 250 gramos", consent = true })).StatusCode);
        await using var db = new NpgsqlConnection(fixture.ConnectionString); await db.OpenAsync();
        await using var cmd = new NpgsqlCommand("INSERT INTO business_subscriptions (id,business_id,starts_at,ends_at,granted_by,granted_at,reason) SELECT gen_random_uuid(),id,now()-interval '1 day',now()+interval '1 day',owner_id,now(),'Test' FROM businesses WHERE id=$1", db); cmd.Parameters.AddWithValue(id); await cmd.ExecuteNonQueryAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(path, new { facts = "Café 250 gramos", consent = false })).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsJsonAsync(path, new { facts = "Café 250 gramos", consent = true })).StatusCode);
    }
}
