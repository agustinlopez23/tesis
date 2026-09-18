using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Marketplace.DAL;
using Marketplace.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.Api.Tests;

public sealed class PublicDirectoryTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    [Fact]
    public async Task Anonymous_directory_respects_publication_suspension_privacy_and_pagination()
    {
        using var owner = fixture.Client();
        var email = $"directory-{Guid.NewGuid():N}@example.test";
        const string password = "Directory-test-password-123";
        var registration = await owner.PostAsJsonAsync("/auth/register", new { email, password, displayName = "Responsable privado" });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        var ownerId = (await registration.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var login = await owner.PostAsJsonAsync("/auth/login", new { email, password });
        owner.DefaultRequestHeaders.Add("Cookie", Assert.Single(login.Headers.GetValues("Set-Cookie")).Split(';')[0]);
        var draft = new { name = "Almacén público", description = "Productos locales", address = "Colón 123",
            categoryId = 1, whatsapp = "+5493515551234", latitude = -31.4, longitude = -64.18, status = "draft" };
        var created = await owner.PostAsJsonAsync("/businesses", draft);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        using var visitor = fixture.Client(false);
        var response = await visitor.GetAsync("/public/businesses");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray());

        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync($"/businesses/{id}", draft with { status = "published" })).StatusCode);
        var page = await visitor.GetFromJsonAsync<JsonElement>("/public/businesses");
        var item = Assert.Single(page.GetProperty("items").EnumerateArray());
        Assert.Equal(id, item.GetProperty("id").GetGuid());
        Assert.Equal("Almacenes", item.GetProperty("category").GetString());
        Assert.Equal(draft.whatsapp, item.GetProperty("whatsApp").GetString());
        Assert.Equal(new[] { "address", "category", "description", "id", "name", "whatsApp" },
            item.EnumerateObject().Select(x => x.Name).Order().ToArray());
        Assert.False(page.GetProperty("hasMore").GetBoolean());
        var search = await visitor.GetFromJsonAsync<JsonElement>("/public/businesses?q=PRODUCTOS&category=Almacenes");
        Assert.Equal(id, Assert.Single(search.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        var accentless = await visitor.GetFromJsonAsync<JsonElement>("/public/businesses?q=ALMACEN");
        Assert.Equal(id, Assert.Single(accentless.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        Assert.Empty((await visitor.GetFromJsonAsync<JsonElement>("/public/businesses?q=sin-coincidencias")).GetProperty("items").EnumerateArray());
        Assert.Empty((await visitor.GetFromJsonAsync<JsonElement>("/public/businesses?q=%25")).GetProperty("items").EnumerateArray());
        Assert.Empty((await visitor.GetFromJsonAsync<JsonElement>("/public/businesses?category=Indumentaria")).GetProperty("items").EnumerateArray());
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.GetAsync($"/businesses/{id}")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync($"/businesses/{id}", draft)).StatusCode);
        Assert.Empty((await visitor.GetFromJsonAsync<JsonElement>("/public/businesses")).GetProperty("items").EnumerateArray());
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync($"/businesses/{id}", draft with { status = "published" })).StatusCode);
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        var account = await db.UserAccounts.FindAsync(ownerId);
        account!.IsSuspended = true;
        await db.SaveChangesAsync();
        Assert.Empty((await visitor.GetFromJsonAsync<JsonElement>("/public/businesses")).GetProperty("items").EnumerateArray());

        account.IsSuspended = false;
        for (var i = 0; i < 20; i++) db.Businesses.Add(new BusinessProfile {
            OwnerId = ownerId, CategoryId = 1, Name = "Otro negocio", Description = "", Address = "", WhatsApp = draft.whatsapp,
            Location = new GeoLocation(-31.4, -64.18), Status = "published" });
        await db.SaveChangesAsync();
        var first = await visitor.GetFromJsonAsync<JsonElement>("/public/businesses?page=1");
        var second = await visitor.GetFromJsonAsync<JsonElement>("/public/businesses?page=2");
        Assert.Equal(20, first.GetProperty("items").GetArrayLength());
        Assert.True(first.GetProperty("hasMore").GetBoolean());
        Assert.Single(second.GetProperty("items").EnumerateArray());
        Assert.False(second.GetProperty("hasMore").GetBoolean());
        var ids = first.GetProperty("items").EnumerateArray().Concat(second.GetProperty("items").EnumerateArray())
            .Select(x => x.GetProperty("id").GetGuid()).ToArray();
        Assert.Equal(21, ids.Distinct().Count());
        Assert.Equal(id, ids[0]);
        Assert.Equal(ids.Skip(1).Order().ToArray(), ids.Skip(1).ToArray());
        var empty = await visitor.GetFromJsonAsync<JsonElement>("/public/businesses?page=3");
        Assert.Empty(empty.GetProperty("items").EnumerateArray());
        Assert.False(empty.GetProperty("hasMore").GetBoolean());
        db.Businesses.Add(new BusinessProfile {
            OwnerId = ownerId, CategoryId = 1, Name = "Zeta del barrio", Description = "Solo este resultado",
            Address = "Dirección singular", WhatsApp = draft.whatsapp, Location = new GeoLocation(-31.4, -64.18), Status = "published" });
        await db.SaveChangesAsync();
        var beyondFirstPage = await visitor.GetFromJsonAsync<JsonElement>("/public/businesses?q=singular&category=Almacenes");
        Assert.Equal("Zeta del barrio", Assert.Single(beyondFirstPage.GetProperty("items").EnumerateArray()).GetProperty("name").GetString());
        Assert.False(beyondFirstPage.GetProperty("hasMore").GetBoolean());
        Assert.Equal(HttpStatusCode.BadRequest, (await visitor.GetAsync("/public/businesses?q=" + new string('a', 151))).StatusCode);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("10001")]
    [InlineData("1.5")]
    [InlineData("texto")]
    public async Task Invalid_page_is_rejected(string page)
    {
        using var visitor = fixture.Client(false);
        Assert.Equal(HttpStatusCode.BadRequest, (await visitor.GetAsync($"/public/businesses?page={page}")).StatusCode);
    }
}
