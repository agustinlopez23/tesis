using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Marketplace.Api.Tests;

// Acceptance criteria HU-09, HU-17, HU-20/21 and HU-22 as defended in the thesis user stories.
public sealed class DefenseCriteriaTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    private const string Password = "Local-test-password-123";

    [Fact]
    public async Task Public_profile_lists_only_active_promotions_with_measurement_tokens()
    {
        var (owner, business, product) = await Setup(); using var client = owner; using var admin = await Account(true); using var guest = fixture.Client();
        var active = Guid.NewGuid(); var pending = Guid.NewGuid();
        (await owner.PutAsJsonAsync($"/businesses/{business}/campaigns/{active}", Body(product, "nueva-cordoba", "Promo vigente"))).EnsureSuccessStatusCode();
        (await owner.PutAsJsonAsync($"/businesses/{business}/campaigns/{pending}", Body(product, "centro", "Promo en revisión"))).EnsureSuccessStatusCode();
        (await Action(admin, active, 1, "approve")).EnsureSuccessStatusCode();
        (await Action(admin, active, 2, "confirm_payment")).EnsureSuccessStatusCode();

        var profile = await guest.GetFromJsonAsync<JsonElement>($"/public/businesses/{business}");
        var promotion = Assert.Single(profile.GetProperty("promotions").EnumerateArray());
        Assert.Equal(active, promotion.GetProperty("id").GetGuid());
        Assert.Equal("Promo vigente", promotion.GetProperty("title").GetString());
        Assert.False(string.IsNullOrEmpty(promotion.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task Rejection_reason_is_visible_to_owner_and_administrator()
    {
        var (owner, business, product) = await Setup(); using var client = owner; using var admin = await Account(true);
        var id = Guid.NewGuid();
        (await owner.PutAsJsonAsync($"/businesses/{business}/campaigns/{id}", Body(product, "alberdi", "Promo a corregir"))).EnsureSuccessStatusCode();
        (await Action(admin, id, 1, "reject", "La imagen no muestra el producto")).EnsureSuccessStatusCode();

        var own = (await owner.GetFromJsonAsync<JsonElement>($"/businesses/{business}/campaigns")).GetProperty("items").EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == id);
        Assert.Equal("rejected", own.GetProperty("status").GetString());
        Assert.Equal("La imagen no muestra el producto", own.GetProperty("rejectionReason").GetString());
        var listed = (await admin.GetFromJsonAsync<JsonElement>("/admin/campaigns")).GetProperty("items").EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == id);
        Assert.Equal("La imagen no muestra el producto", listed.GetProperty("rejectionReason").GetString());
    }

    [Fact]
    public async Task Stored_ranking_snapshot_names_the_applied_policy()
    {
        var (owner, _, _) = await Setup(); owner.Dispose();
        await using var ranked = fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["Ranking:Enabled"] = "true" })));
        using var guest = ranked.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        guest.DefaultRequestHeaders.Add("Origin", "http://localhost:3000");
        guest.DefaultRequestHeaders.Add("X-Requested-With", "marketplace");
        var request = Guid.NewGuid();
        var created = await guest.PostAsJsonAsync("/public/ranking-snapshots", new { requestId = request, zoneId = "centro", categoryId = 2, radiusKm = 4 });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("equity-v1", (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("policy").GetString());
        Assert.Equal("equity-v1", await Text("SELECT policy_label FROM ranking_snapshots WHERE request_id = $1", request));
        Assert.Equal("equity-v1", await Text("SELECT o.policy_label FROM ranking_opportunities o JOIN ranking_snapshots s ON s.id = o.id WHERE s.request_id = $1", request));
    }

    [Fact]
    public async Task Substantive_owner_updates_are_recorded_by_type_once_per_window()
    {
        var (owner, business, product) = await Setup(); using var client = owner;
        var profile = new { name = "Local actualizado", description = "Nueva propuesta", address = "Córdoba", categoryId = 2, whatsApp = "+5493515551234", latitude = -31.4167, longitude = -64.1833, status = "published" };
        (await owner.PutAsJsonAsync($"/businesses/{business}", profile)).EnsureSuccessStatusCode();
        (await owner.PutAsJsonAsync($"/businesses/{business}", profile with { description = "Otra propuesta" })).EnsureSuccessStatusCode();
        (await owner.PutAsJsonAsync($"/businesses/{business}/products/{Guid.NewGuid()}", new { name = "Té", description = "", category = "Té", price = 90m, currency = "ARS", available = true, status = "published", version = 0 })).EnsureSuccessStatusCode();
        (await owner.PutAsJsonAsync($"/businesses/{business}/campaigns/{Guid.NewGuid()}", Body(product, "centro", "Promo de prueba"))).EnsureSuccessStatusCode();
        // A refused write is not an update.
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsJsonAsync($"/businesses/{business}/campaigns/{Guid.NewGuid()}", Body(product, "marte", "Promo inválida"))).StatusCode);

        Assert.Equal(1, await Count("SELECT count(*) FROM owner_activity WHERE business_id = $1 AND kind = 'profile_update'", business));
        Assert.Equal(1, await Count("SELECT count(*) FROM owner_activity WHERE business_id = $1 AND kind = 'catalog_update'", business));
        Assert.Equal(1, await Count("SELECT count(*) FROM owner_activity WHERE business_id = $1 AND kind = 'promotion_update'", business));
        Assert.Equal(1, await Count("SELECT count(*) FROM owner_activity WHERE business_id = $1 AND kind = 'profile_update' AND occurred_at > now() - interval '5 minutes'", business));

        // Outside the deduplication window the same kind of update counts again.
        await Count("UPDATE owner_activity SET occurred_at = occurred_at - interval '2 hours' WHERE business_id = $1", business);
        (await owner.PutAsJsonAsync($"/businesses/{business}", profile)).EnsureSuccessStatusCode();
        Assert.Equal(2, await Count("SELECT count(*) FROM owner_activity WHERE business_id = $1 AND kind = 'profile_update'", business));
    }

    private static object Body(Guid product, string zone, string title) => new { version = 0, productId = product, photoSlot = (int?)null, title, description = "Paquete 250 g", conditions = "Hasta agotar existencias", zoneId = zone,
        startsAt = DateTimeOffset.UtcNow.AddMinutes(-1), endsAt = DateTimeOffset.UtcNow.AddDays(1), status = "pending_review" };
    private static Task<HttpResponseMessage> Action(HttpClient admin, Guid id, int version, string action, string reason = "Verificación administrativa") => admin.PutAsJsonAsync($"/admin/campaigns/{id}/actions",
        new { operationId = Guid.NewGuid(), version, action, reason, paymentReference = action == "confirm_payment" ? "RECIBO-LOCAL-TEST" : null });
    private async Task<(HttpClient Owner, Guid Business, Guid Product)> Setup()
    {
        var owner = await Account(false);
        var created = await owner.PostAsJsonAsync("/businesses", new { name = "Local con promociones", description = "", address = "Córdoba", categoryId = 2, whatsApp = "+5493515551234", latitude = -31.4167, longitude = -64.1833, status = "published" });
        created.EnsureSuccessStatusCode(); var business = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await Count("INSERT INTO business_subscriptions (id,business_id,starts_at,ends_at,granted_by,granted_at,reason) SELECT gen_random_uuid(),id,now()-interval '1 day',now()+interval '30 days',owner_id,now(),'Test' FROM businesses WHERE id=$1", business);
        var product = Guid.NewGuid();
        (await owner.PutAsJsonAsync($"/businesses/{business}/products/{product}", new { name = "Café", description = "", category = "Café", price = 100m, currency = "ARS", available = true, status = "published", version = 0 })).EnsureSuccessStatusCode();
        return (owner, business, product);
    }
    private async Task<HttpClient> Account(bool admin)
    {
        var client = fixture.Client(); var email = $"defense-{Guid.NewGuid():N}@example.test";
        (await client.PostAsJsonAsync("/auth/register", new { email, password = Password, displayName = "Defensa" })).EnsureSuccessStatusCode();
        if (admin)
        {
            await using var db = new NpgsqlConnection(fixture.ConnectionString); await db.OpenAsync();
            await using var cmd = new NpgsqlCommand("INSERT INTO user_account_roles (user_id,role_id) SELECT id,2 FROM user_accounts WHERE email=$1", db); cmd.Parameters.AddWithValue(email); await cmd.ExecuteNonQueryAsync();
        }
        var login = await client.PostAsJsonAsync("/auth/login", new { email, password = Password }); login.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Add("Cookie", Assert.Single(login.Headers.GetValues("Set-Cookie")).Split(';')[0]); return client;
    }
    private async Task<long> Count(string sql, Guid id)
    {
        await using var db = new NpgsqlConnection(fixture.ConnectionString); await db.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, db); cmd.Parameters.AddWithValue(id);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync() ?? 0L);
    }
    private async Task<string?> Text(string sql, Guid id)
    {
        await using var db = new NpgsqlConnection(fixture.ConnectionString); await db.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, db); cmd.Parameters.AddWithValue(id);
        return await cmd.ExecuteScalarAsync() as string;
    }
}
