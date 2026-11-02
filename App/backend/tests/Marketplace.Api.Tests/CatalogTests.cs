using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;
using SkiaSharp;

namespace Marketplace.Api.Tests;

public sealed class CatalogTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    [Fact]
    public async Task Photos_are_processed_versioned_and_hidden_with_the_product_or_plan()
    {
        using var owner = await Account();
        using var admin = await Account(true);
        using var other = await Account();
        var business = await Business(owner);
        var period = await Activate(admin, business);
        var product = Guid.NewGuid();
        var path = $"/businesses/{business}/products/{product}";
        await owner.PutAsJsonAsync(path, Product());
        using var surface = SKSurface.Create(new SKImageInfo(40, 20));
        surface.Canvas.Clear(SKColors.Coral);
        using var image = surface.Snapshot();
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        var png = encoded.ToArray();
        static MultipartFormDataContent Upload(byte[] bytes, int version)
        {
            var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(bytes), "file", "photo.png");
            form.Add(new StringContent(version.ToString()), "version");
            return form;
        }
        Assert.Equal(HttpStatusCode.NotFound, (await other.PutAsync(path + "/photo", Upload(png, 1))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsync(path + "/photo", Upload([1, 2, 3], 1))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsync(path + "/photo", Upload(png, 1))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.DeleteAsync(path + "/photo?version=1")).StatusCode);
        var publicPhoto = "/public" + path + "/photo";
        var response = await owner.GetAsync(publicPhoto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/jpeg", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal(new byte[] { 255, 216 }, (await response.Content.ReadAsByteArrayAsync()).Take(2));
        Assert.True((await Public(business)).GetProperty("items")[0].GetProperty("hasPhoto").GetBoolean());
        await owner.PutAsJsonAsync(path, Product(2, "archived"));
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync(publicPhoto)).StatusCode);
        await owner.PutAsJsonAsync(path, Product(3));
        await admin.PostAsJsonAsync($"/admin/businesses/{business}/subscriptions/{period}/revoke", new { reason = "Baja del plan" });
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync(publicPhoto)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync(path + "/photo")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(path + "/photo")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.DeleteAsync(path + "/photo?version=4")).StatusCode);
        await Activate(admin, business);
        Assert.Equal(HttpStatusCode.OK, (await owner.DeleteAsync(path + "/photo?version=4")).StatusCode);
        Assert.False((await Public(business)).GetProperty("items")[0].GetProperty("hasPhoto").GetBoolean());
    }

    [Fact]
    public async Task Concurrent_first_grants_serialize_and_future_period_does_not_enable_catalog()
    {
        using var owner = await Account();
        using var admin = await Account(true);
        var business = await Business(owner);
        var tomorrow = DateTimeOffset.UtcNow.AddDays(1);
        var grant = new { startsAt = tomorrow, endsAt = tomorrow.AddDays(30), reason = "Plan futuro" };
        var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => admin.PutAsJsonAsync(
            $"/admin/businesses/{business}/subscriptions/{Guid.NewGuid()}", grant)));
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.Conflict);
        var plan = await owner.GetFromJsonAsync<JsonElement>($"/businesses/{business}/plan");
        Assert.False(plan.GetProperty("catalogEnabled").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/admin/businesses/{business}/subscriptions/{Guid.NewGuid()}",
            new { startsAt = tomorrow.AddDays(30), endsAt = tomorrow.AddDays(60), reason = "Renovación contigua" })).StatusCode);
    }

    [Fact]
    public async Task Admin_routes_require_live_role_and_mutations_require_trusted_origin()
    {
        using var admin = await Account(true);
        using var owner = await Account();
        using var anonymous = fixture.Client();
        var business = await Business(owner);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/admin/businesses")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync("/admin/businesses")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/admin/businesses?q=Café&page=1")).StatusCode);
        admin.DefaultRequestHeaders.Remove("Origin");
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PutAsJsonAsync($"/admin/businesses/{business}/subscriptions/{Guid.NewGuid()}", Grant())).StatusCode);
        admin.DefaultRequestHeaders.Add("Origin", "http://localhost:3000");
        var account = await admin.GetFromJsonAsync<JsonElement>("/auth/session");
        await Sql("DELETE FROM user_account_roles WHERE user_id = $1 AND role_id = 2", account.GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/admin/businesses")).StatusCode);
    }

    [Fact]
    public async Task Public_catalog_paginates_and_searches_only_published_products_in_its_business()
    {
        using var owner = await Account();
        using var admin = await Account(true);
        var business = await Business(owner);
        await Activate(admin, business);
        for (var i = 0; i < 21; i++)
            Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync($"/businesses/{business}/products/{Guid.NewGuid()}", Product())).StatusCode);
        var first = await Public(business);
        Assert.Equal(20, first.GetProperty("items").GetArrayLength());
        Assert.True(first.GetProperty("hasNextPage").GetBoolean());
        var second = await owner.GetFromJsonAsync<JsonElement>($"/public/businesses/{business}/products?page=2&q=CAFÉ");
        Assert.Single(second.GetProperty("items").EnumerateArray());
        Assert.False(second.GetProperty("hasNextPage").GetBoolean());
        Assert.DoesNotContain(second.GetProperty("items")[0].GetProperty("id").GetGuid(),
            first.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetGuid()));
        var otherBusiness = await Business(owner);
        await Activate(admin, otherBusiness);
        Assert.Empty((await Public(otherBusiness)).GetProperty("items").EnumerateArray());
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.GetAsync($"/public/businesses/{business}/products?page=0")).StatusCode);
    }

    [Fact]
    public async Task Plan_is_admin_only_idempotent_and_rejects_overlapping_periods()
    {
        using var owner = await Account();
        using var admin = await Account(true);
        var business = await Business(owner);
        var path = $"/admin/businesses/{business}/subscriptions/{Guid.NewGuid()}";
        var grant = Grant();
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PutAsJsonAsync(path, grant)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync(path, grant)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync(path, grant)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync(path, Grant("Changed"))).StatusCode);
        var overlapping = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => admin.PutAsJsonAsync(
            $"/admin/businesses/{business}/subscriptions/{Guid.NewGuid()}", grant)));
        Assert.All(overlapping, response => Assert.Equal(HttpStatusCode.Conflict, response.StatusCode));
        var plan = await owner.GetFromJsonAsync<JsonElement>($"/businesses/{business}/plan");
        Assert.True(plan.GetProperty("catalogEnabled").GetBoolean());
        Assert.Single(plan.GetProperty("periods").EnumerateArray());
        var future = DateTimeOffset.UtcNow.AddDays(90).AddTicks(7);
        var precise = new { startsAt = future, endsAt = future.AddDays(30), reason = "Precisión de fecha" };
        var nextPath = $"/admin/businesses/{business}/subscriptions/{Guid.NewGuid()}";
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync(nextPath, precise)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync(nextPath, precise)).StatusCode);
    }

    [Fact]
    public async Task Catalog_requires_plan_and_ownership_and_preserves_data_on_expiration()
    {
        using var owner = await Account();
        using var other = await Account();
        using var admin = await Account(true);
        var business = await Business(owner);
        var product = Guid.NewGuid();
        var path = $"/businesses/{business}/products/{product}";
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PutAsJsonAsync(path, Product())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/businesses/{business}/products")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PutAsJsonAsync(path, Product())).StatusCode);
        Assert.False(await HasPublicCatalog(owner, business));
        await Activate(admin, business);
        Assert.True(await HasPublicCatalog(owner, business));
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync(path, Product())).StatusCode);
        Assert.Single((await Public(business)).GetProperty("items").EnumerateArray());
        await Sql("UPDATE business_subscriptions SET starts_at = now() - interval '2 days', ends_at = now() - interval '1 day' WHERE business_id = $1", business);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/public/businesses/{business}/products")).StatusCode);
        Assert.Single((await owner.GetFromJsonAsync<JsonElement>($"/businesses/{business}/products")).EnumerateArray());
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PutAsJsonAsync(path, Product(1))).StatusCode);
        Assert.False(await HasPublicCatalog(owner, business));
    }

    private static async Task<bool> HasPublicCatalog(HttpClient client, Guid business) =>
        (await client.GetFromJsonAsync<JsonElement>($"/public/businesses/{business}")).GetProperty("hasPublicCatalog").GetBoolean();

    [Fact]
    public async Task Products_validate_prices_preserve_ids_and_detect_stale_edits()
    {
        using var owner = await Account();
        using var admin = await Account(true);
        var business = await Business(owner);
        await Activate(admin, business);
        var path = $"/businesses/{business}/products/{Guid.NewGuid()}";
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsJsonAsync(path, Product(price: -1))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsJsonAsync(path, Product(price: 1.001m))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync(path, Product())).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PutAsJsonAsync(path, Product())).StatusCode);
        var edits = await Task.WhenAll(owner.PutAsJsonAsync(path, Product(1, "draft")), owner.PutAsJsonAsync(path, Product(1, "archived")));
        Assert.Single(edits, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(edits, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Empty((await Public(business)).GetProperty("items").EnumerateArray());
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync(path, Product(2))).StatusCode);
        Assert.Single((await Public(business)).GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Revocation_keeps_audit_and_public_catalog_hides_drafts_and_suspended_owners()
    {
        using var owner = await Account();
        using var admin = await Account(true);
        var business = await Business(owner);
        var subscription = await Activate(admin, business);
        await owner.PutAsJsonAsync($"/businesses/{business}/products/{Guid.NewGuid()}", Product());
        await owner.PutAsJsonAsync($"/businesses/{business}/products/{Guid.NewGuid()}", Product(status: "draft"));
        Assert.Single((await Public(business)).GetProperty("items").EnumerateArray());
        await Sql("UPDATE businesses SET status = 'draft' WHERE id = $1", business);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/public/businesses/{business}/products")).StatusCode);
        await Sql("UPDATE businesses SET status = 'published' WHERE id = $1", business);
        await Sql("UPDATE user_accounts SET is_suspended = true WHERE id = (SELECT owner_id FROM businesses WHERE id = $1)", business);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/public/businesses/{business}/products")).StatusCode);
        await Sql("UPDATE user_accounts SET is_suspended = false WHERE id = (SELECT owner_id FROM businesses WHERE id = $1)", business);
        var revoke = $"/admin/businesses/{business}/subscriptions/{subscription}/revoke";
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync(revoke, new { reason = "Corrección de activación" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync(revoke, new { reason = "Corrección de activación" })).StatusCode);
        var plan = await owner.GetFromJsonAsync<JsonElement>($"/businesses/{business}/plan");
        Assert.False(plan.GetProperty("catalogEnabled").GetBoolean());
        Assert.Equal("Corrección de activación", plan.GetProperty("periods")[0].GetProperty("revocationReason").GetString());
    }

    private static object Product(int version = 0, string status = "published", decimal price = 1250.50m) => new
    { name = "Café de especialidad", description = "Paquete de 250 g", category = "Café", price, currency = "ARS", available = true, status, version };
    private static object Grant(string reason = "Activación manual") => new
    { startsAt = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero), endsAt = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(30), TimeSpan.Zero), reason };
    private async Task<Guid> Activate(HttpClient admin, Guid business)
    {
        var id = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/admin/businesses/{business}/subscriptions/{id}", Grant())).StatusCode);
        return id;
    }
    private async Task<JsonElement> Public(Guid id) => await fixture.Client().GetFromJsonAsync<JsonElement>($"/public/businesses/{id}/products");
    private async Task<Guid> Business(HttpClient owner)
    {
        var response = await owner.PostAsJsonAsync("/businesses", new { name = "Café local", description = "", address = "Córdoba", categoryId = 2,
            whatsapp = "+5493515551234", latitude = -31.4167, longitude = -64.1833, status = "published" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
    private async Task<HttpClient> Account(bool administrator = false)
    {
        var client = fixture.Client();
        var email = $"catalog-{Guid.NewGuid():N}@example.test";
        var password = "Local-test-password-123";
        var registered = await client.PostAsJsonAsync("/auth/register", new { email, password, displayName = "Catálogo" });
        registered.EnsureSuccessStatusCode();
        if (administrator) await Sql("INSERT INTO user_account_roles (user_id, role_id) SELECT id, 2 FROM user_accounts WHERE email = $1", email);
        var login = await client.PostAsJsonAsync("/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Add("Cookie", Assert.Single(login.Headers.GetValues("Set-Cookie")).Split(';')[0]);
        return client;
    }
    private async Task Sql(string sql, object value)
    {
        await using var db = new NpgsqlConnection(fixture.ConnectionString);
        await db.OpenAsync();
        await using var command = new NpgsqlCommand(sql, db);
        command.Parameters.AddWithValue(value);
        await command.ExecuteNonQueryAsync();
    }
}
