using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;
using SkiaSharp;

namespace Marketplace.Api.Tests;

public sealed class CampaignTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    [Fact]
    public async Task Concurrent_payments_from_different_businesses_cannot_exceed_zone_capacity()
    {
        using var admin = await Account(true);
        var ids = new List<Guid>();
        for (var i = 0; i < 4; i++)
        {
            var (owner, business, product) = await Setup(); using var client = owner;
            var id = Guid.NewGuid(); ids.Add(id);
            var body = new { version = 0, productId = product, photoSlot = (int?)null, title = "Campaña concurrente", description = "Contenido revisado", conditions = "Consultar existencias", zoneId = "centro", startsAt = DateTimeOffset.UtcNow.AddDays(10), endsAt = DateTimeOffset.UtcNow.AddDays(11), status = "pending_review" };
            (await owner.PutAsJsonAsync($"/businesses/{business}/campaigns/{id}", body)).EnsureSuccessStatusCode();
            (await Action(admin, id, 1, "approve")).EnsureSuccessStatusCode();
        }
        var results = await Task.WhenAll(ids.Select(id => Action(admin, id, 2, "confirm_payment")));
        Assert.Equal(3, results.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(1, results.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        foreach (var result in results) result.Dispose();
    }

    [Fact]
    public async Task Promotion_keeps_reviewed_image_and_runs_without_catalog_subscription()
    {
        var (owner, business, _) = await Setup(); using var client = owner; using var admin = await Account(true); using var guest = fixture.Client();
        using var surface = SKSurface.Create(new SKImageInfo(40, 20)); surface.Canvas.Clear(SKColors.Coral);
        using var image = surface.Snapshot(); using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        using var upload = new MultipartFormDataContent(); upload.Add(new ByteArrayContent(encoded.ToArray()), "file", "promotion.png"); upload.Add(new StringContent("Promoción"), "caption");
        (await owner.PutAsync($"/businesses/{business}/photos/1", upload)).EnsureSuccessStatusCode();
        await Scalar("UPDATE business_subscriptions SET ends_at=now()-interval '1 second' WHERE business_id=$1", business);
        var id = Guid.NewGuid();
        var body = new { version = 0, productId = (Guid?)null, photoSlot = 1, title = "Promoción local", description = "Conocé nuestra propuesta", conditions = "Consultá condiciones en el local", zoneId = "centro", startsAt = DateTimeOffset.UtcNow.AddMinutes(-1), endsAt = DateTimeOffset.UtcNow.AddDays(1), status = "pending_review" };
        (await owner.PutAsJsonAsync($"/businesses/{business}/campaigns/{id}", body)).EnsureSuccessStatusCode();
        (await Action(admin, id, 1, "approve")).EnsureSuccessStatusCode(); (await Action(admin, id, 2, "confirm_payment")).EnsureSuccessStatusCode();
        var before = await guest.GetByteArrayAsync($"/public/campaigns/{id}/image"); Assert.NotEmpty(before);
        (await owner.DeleteAsync($"/businesses/{business}/photos/1")).EnsureSuccessStatusCode();
        Assert.Equal(before, await guest.GetByteArrayAsync($"/public/campaigns/{id}/image"));
        Assert.Equal(HttpStatusCode.OK, (await guest.GetAsync($"/public/campaigns/{id}")).StatusCode);
        (await Action(admin, id, 3, "pause")).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Review_payment_events_and_order_attribution_are_explicit_and_idempotent()
    {
        var (owner, business, product) = await Setup(); using var client = owner; using var admin = await Account(true); using var guest = fixture.Client();
        var campaign = Guid.NewGuid(); var path = $"/businesses/{business}/campaigns/{campaign}";
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync(path, Body(product, "centro"))).StatusCode);
        Assert.Empty((await Public(guest, "centro")).GetProperty("items").EnumerateArray());
        var review = new { operationId = Guid.NewGuid(), version = 1, action = "approve", reason = "Contenido revisado", paymentReference = (string?)null };
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PutAsJsonAsync($"/admin/campaigns/{campaign}/actions", review)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/admin/campaigns/{campaign}/actions", review)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/admin/campaigns/{campaign}/actions", review)).StatusCode);
        Assert.Empty((await Public(guest, "centro")).GetProperty("items").EnumerateArray());
        Assert.Equal(HttpStatusCode.OK, (await Action(admin, campaign, 2, "confirm_payment")).StatusCode);
        var ad = Assert.Single((await Public(guest, "centro")).GetProperty("items").EnumerateArray());
        var token = ad.GetProperty("token").GetString();
        var eventBody = new { token, kind = "impression" };
        Assert.Equal(HttpStatusCode.NoContent, (await guest.PostAsJsonAsync("/public/campaign-events", eventBody)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await guest.PostAsJsonAsync("/public/campaign-events", eventBody)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PostAsJsonAsync("/public/campaign-events", new { token, kind = "click" })).StatusCode);
        var orderId = Guid.NewGuid();
        var orderBody = new { requestId = orderId, accessKey = new string('a', 64), customerName = "Ana", campaignToken = token,
            lines = new[] { new { productId = product, quantity = 1, expectedVersion = 1 } } };
        Assert.Equal(HttpStatusCode.Created, (await guest.PostAsJsonAsync($"/public/businesses/{business}/orders", orderBody)).StatusCode);
        var report = await owner.GetFromJsonAsync<JsonElement>(path + "/metrics");
        Assert.Equal(1, report.GetProperty("impressions").GetInt32()); Assert.Equal(0, report.GetProperty("clicks").GetInt32()); Assert.Equal(1, report.GetProperty("orders").GetInt32());
        Assert.Equal(1L, await Scalar("SELECT count(*) FROM customer_orders WHERE campaign_id=$1 AND source='campaign'", campaign));
        var pause = new { operationId = Guid.NewGuid(), version = 3, action = "pause", reason = "Pausa solicitada", paymentReference = (string?)null };
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync(path + "/pause", pause)).StatusCode);
        Assert.Empty((await Public(guest, "centro")).GetProperty("items").EnumerateArray());
        Assert.Equal(HttpStatusCode.OK, (await guest.PostAsJsonAsync($"/public/businesses/{business}/orders", orderBody)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await guest.PostAsJsonAsync("/public/campaign-events", new { token = "forged", kind = "click" })).StatusCode);
    }

    [Fact]
    public async Task Zone_capacity_is_reserved_without_changing_organic_results()
    {
        var (owner, business, product) = await Setup(); using var client = owner; using var admin = await Account(true); using var guest = fixture.Client();
        for (var i = 0; i < 4; i++)
        {
            var id = Guid.NewGuid();
            (await owner.PutAsJsonAsync($"/businesses/{business}/campaigns/{id}", Body(product, "alberdi"))).EnsureSuccessStatusCode();
            (await Action(admin, id, 1, "approve")).EnsureSuccessStatusCode();
            Assert.Equal(i < 3 ? HttpStatusCode.OK : HttpStatusCode.Conflict, (await Action(admin, id, 2, "confirm_payment")).StatusCode);
        }
        Assert.Equal(3, (await Public(guest, "alberdi")).GetProperty("items").GetArrayLength());
        var organic = await guest.GetFromJsonAsync<JsonElement>("/public/products");
        Assert.Equal(1, organic.GetProperty("items").EnumerateArray().Count(p => p.GetProperty("businessId").GetGuid() == business));
    }

    [Fact]
    public async Task Paid_campaign_hides_when_product_stock_plan_or_business_is_ineligible()
    {
        var (owner, business, product) = await Setup(); using var client = owner; using var admin = await Account(true); using var guest = fixture.Client();
        var id = Guid.NewGuid();
        (await owner.PutAsJsonAsync($"/businesses/{business}/campaigns/{id}", Body(product, "nueva-cordoba"))).EnsureSuccessStatusCode();
        (await Action(admin, id, 1, "approve")).EnsureSuccessStatusCode(); (await Action(admin, id, 2, "confirm_payment")).EnsureSuccessStatusCode();
        Assert.Single((await Public(guest, "nueva-cordoba")).GetProperty("items").EnumerateArray());
        (await owner.PutAsJsonAsync($"/businesses/{business}/inventory/adjustments/{Guid.NewGuid()}", new { productId = product, delta = 0, version = 0, reason = "Sin unidades" })).EnsureSuccessStatusCode();
        Assert.Empty((await Public(guest, "nueva-cordoba")).GetProperty("items").EnumerateArray());
        (await owner.PutAsJsonAsync($"/businesses/{business}/inventory/adjustments/{Guid.NewGuid()}", new { productId = product, delta = 2, version = 1, reason = "Reposición" })).EnsureSuccessStatusCode();
        await Scalar("UPDATE business_subscriptions SET ends_at=now()-interval '1 second' WHERE business_id=$1", business);
        Assert.Empty((await Public(guest, "nueva-cordoba")).GetProperty("items").EnumerateArray());
        Assert.Equal(1L, await Scalar("SELECT count(*) FROM campaigns WHERE id=$1 AND paid_at IS NOT NULL", id));
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/businesses/{business}/campaigns/{id}/metrics")).StatusCode);
    }

    [Fact]
    public async Task Campaigns_enforce_owner_role_version_and_content_validation()
    {
        var (owner, business, product) = await Setup(); using var client = owner; using var other = await Account(false); using var guest = fixture.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.GetAsync($"/businesses/{business}/campaigns")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/businesses/{business}/campaigns")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync("/admin/campaigns")).StatusCode);
        var path = $"/businesses/{business}/campaigns/{Guid.NewGuid()}";
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsJsonAsync(path, Body(product, "invalid-zone"))).StatusCode);
        (await owner.PutAsJsonAsync(path, Body(product, "centro"))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PutAsJsonAsync(path, Body(product, "alberdi"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await guest.GetAsync("/public/campaigns?zone=invalid")).StatusCode);
    }

    private static object Body(Guid product, string zone) => new { version = 0, productId = product, photoSlot = (int?)null, title = "Café del barrio", description = "Paquete 250 g", conditions = "Hasta agotar existencias", zoneId = zone,
        startsAt = DateTimeOffset.UtcNow.AddMinutes(-1), endsAt = DateTimeOffset.UtcNow.AddDays(1), status = "pending_review" };
    private static Task<HttpResponseMessage> Action(HttpClient admin, Guid id, int version, string action) => admin.PutAsJsonAsync($"/admin/campaigns/{id}/actions",
        new { operationId = Guid.NewGuid(), version, action, reason = "Verificación administrativa", paymentReference = action == "confirm_payment" ? "RECIBO-LOCAL-TEST" : null });
    private static async Task<JsonElement> Public(HttpClient guest, string zone) => await guest.GetFromJsonAsync<JsonElement>("/public/campaigns?zone=" + zone);
    private async Task<(HttpClient Owner, Guid Business, Guid Product)> Setup()
    {
        var owner = await Account(false);
        var created = await owner.PostAsJsonAsync("/businesses", new { name = "Local patrocinado", description = "", address = "Córdoba", categoryId = 2, whatsApp = "+5493515551234", latitude = -31.4167, longitude = -64.1833, status = "published" });
        created.EnsureSuccessStatusCode(); var business = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await Scalar("INSERT INTO business_subscriptions (id,business_id,starts_at,ends_at,granted_by,granted_at,reason) SELECT gen_random_uuid(),id,now()-interval '1 day',now()+interval '30 days',owner_id,now(),'Test' FROM businesses WHERE id=$1", business);
        var product = Guid.NewGuid();
        (await owner.PutAsJsonAsync($"/businesses/{business}/products/{product}", new { name = "Café", description = "", category = "Café", price = 100m, currency = "ARS", available = true, status = "published", version = 0 })).EnsureSuccessStatusCode();
        return (owner, business, product);
    }
    private async Task<HttpClient> Account(bool admin)
    {
        var client = fixture.Client(); var email = $"campaign-{Guid.NewGuid():N}@example.test"; var password = "Local-test-password-123";
        var registered = await client.PostAsJsonAsync("/auth/register", new { email, password, displayName = "Campañas" }); registered.EnsureSuccessStatusCode();
        if (admin)
        {
            await using var db = new NpgsqlConnection(fixture.ConnectionString); await db.OpenAsync();
            await using var cmd = new NpgsqlCommand("INSERT INTO user_account_roles (user_id,role_id) SELECT id,2 FROM user_accounts WHERE email=$1", db); cmd.Parameters.AddWithValue(email); await cmd.ExecuteNonQueryAsync();
        }
        var login = await client.PostAsJsonAsync("/auth/login", new { email, password }); login.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Add("Cookie", Assert.Single(login.Headers.GetValues("Set-Cookie")).Split(';')[0]); return client;
    }
    private async Task<long> Scalar(string sql, Guid id)
    {
        await using var db = new NpgsqlConnection(fixture.ConnectionString); await db.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, db); cmd.Parameters.AddWithValue(id); return Convert.ToInt64(await cmd.ExecuteScalarAsync());
    }
}
