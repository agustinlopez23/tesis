using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Marketplace.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

namespace Marketplace.Api.Tests;

public sealed class PublicBusinessProfileTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    private static readonly byte[] Png = CreatePng();
    private static byte[] CreatePng()
    {
        using var surface = SKSurface.Create(new SKImageInfo(40, 20));
        surface.Canvas.Clear(SKColors.Coral);
        using var image = surface.Snapshot();
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return encoded.ToArray();
    }

    [Fact]
    public async Task Published_profile_and_photos_obey_visibility_and_owner_isolation()
    {
        using var owner = await Owner();
        using var other = await Owner();
        using var visitor = fixture.Client(false);
        var id = await Create(owner);
        var slot = $"/businesses/{id}/photos/1";
        var publicPhoto = $"/public/businesses/{id}/photos/1";
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync($"/public/businesses/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.GetAsync($"/businesses/{id}/photos")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PutAsync(slot, Upload(Png))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsync(slot, Upload(Png))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync(publicPhoto)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(slot)).StatusCode);
        var privateImage = await owner.GetAsync(slot);
        Assert.Equal(HttpStatusCode.OK, privateImage.StatusCode);
        Assert.Equal("image/jpeg", privateImage.Content.Headers.ContentType?.MediaType);
        var bytes = await privateImage.Content.ReadAsByteArrayAsync();
        Assert.Equal(new byte[] { 255, 216 }, bytes.Take(2));
        Assert.Equal("no-store", privateImage.Headers.CacheControl?.ToString());
        Assert.Equal("nosniff", Assert.Single(privateImage.Headers.GetValues("X-Content-Type-Options")));

        await owner.PutAsJsonAsync($"/businesses/{id}", Business("published"));
        var profile = await visitor.GetFromJsonAsync<JsonElement>($"/public/businesses/{id}");
        Assert.Equal("Negocio con fotos", profile.GetProperty("name").GetString());
        Assert.Equal("Frente del local", Assert.Single(profile.GetProperty("photos").EnumerateArray()).GetProperty("caption").GetString());
        Assert.False(profile.TryGetProperty("ownerId", out _));
        Assert.False(profile.TryGetProperty("latitude", out _));
        Assert.Equal(HttpStatusCode.OK, (await visitor.GetAsync(publicPhoto)).StatusCode);
        var directory = await visitor.GetFromJsonAsync<JsonElement>("/public/businesses?q=Negocio%20con%20fotos");
        Assert.Equal(1, Assert.Single(directory.GetProperty("items").EnumerateArray()).GetProperty("coverPhotoSlot").GetInt32());
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        var business = await db.Businesses.SingleAsync(x => x.Id == id);
        var account = await db.UserAccounts.SingleAsync(x => x.Id == business.OwnerId);
        account.IsSuspended = true;
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync($"/public/businesses/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync(publicPhoto)).StatusCode);
        account.IsSuspended = false;
        await db.SaveChangesAsync();
        await owner.PutAsJsonAsync($"/businesses/{id}", Business("draft"));
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync(publicPhoto)).StatusCode);
    }

    [Fact]
    public async Task Invalid_upload_is_atomic_and_slots_can_be_replaced_and_deleted()
    {
        using var owner = await Owner();
        using var other = await Owner();
        var id = await Create(owner);
        var slot = $"/businesses/{id}/photos/1";
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsync(slot, Upload(Png))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsync(slot, Upload([1, 2, 3]))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsync(slot, Upload(Png, new string('x', 161)))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsync($"/businesses/{id}/photos/6", Upload(Png))).StatusCode);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await owner.PutAsync(slot, Upload(new byte[5 * 1024 * 1024 + 1]))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync(slot)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsync(slot, Upload(Png, "Interior"))).StatusCode);
        var photos = await owner.GetFromJsonAsync<JsonElement>($"/businesses/{id}/photos");
        Assert.Equal("Interior", Assert.Single(photos.EnumerateArray()).GetProperty("caption").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync(slot)).StatusCode);
        using var untrusted = fixture.Client(false);
        foreach (var cookie in owner.DefaultRequestHeaders.GetValues("Cookie")) untrusted.DefaultRequestHeaders.Add("Cookie", cookie);
        Assert.Equal(HttpStatusCode.Forbidden, (await untrusted.DeleteAsync(slot)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync(slot)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync(slot)).StatusCode);
        Assert.Empty((await owner.GetFromJsonAsync<JsonElement>($"/businesses/{id}/photos")).EnumerateArray());
    }

    [Fact]
    public async Task Concurrent_replacements_keep_a_single_slot_and_five_slots_are_the_limit()
    {
        using var owner = await Owner();
        var id = await Create(owner);
        var responses = await Task.WhenAll(owner.PutAsync($"/businesses/{id}/photos/1", Upload(Png, "A")),
            owner.PutAsync($"/businesses/{id}/photos/1", Upload(Png, "B")));
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        Assert.Single((await owner.GetFromJsonAsync<JsonElement>($"/businesses/{id}/photos")).EnumerateArray());
        for (var slot = 2; slot <= 5; slot++)
            Assert.Equal(HttpStatusCode.OK, (await owner.PutAsync($"/businesses/{id}/photos/{slot}", Upload(Png))).StatusCode);
        Assert.Equal(5, (await owner.GetFromJsonAsync<JsonElement>($"/businesses/{id}/photos")).GetArrayLength());
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsync($"/businesses/{id}/photos/6", Upload(Png))).StatusCode);
    }

    private async Task<HttpClient> Owner()
    {
        var client = fixture.Client();
        var email = $"photos-{Guid.NewGuid():N}@example.test";
        const string password = "Local-photos-test-2026!";
        (await client.PostAsJsonAsync("/auth/register", new { email, password, displayName = "Fotos" })).EnsureSuccessStatusCode();
        var login = await client.PostAsJsonAsync("/auth/login", new { email, password });
        client.DefaultRequestHeaders.Add("Cookie", Assert.Single(login.Headers.GetValues("Set-Cookie")).Split(';')[0]);
        return client;
    }
    private static async Task<Guid> Create(HttpClient owner)
    {
        var response = await owner.PostAsJsonAsync("/businesses", Business("draft"));
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
    private static object Business(string status) => new { name = "Negocio con fotos", description = "Descripción completa",
        address = "Córdoba", categoryId = 1, whatsapp = "+5493515551234", latitude = -31.4, longitude = -64.18, status };
    private static MultipartFormDataContent Upload(byte[] bytes, string caption = "Frente del local")
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", "foto.png");
        form.Add(new StringContent(caption), "caption");
        return form;
    }
}
