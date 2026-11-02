using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Marketplace.Api.Tests;

// Acceptance criteria HU-01, HU-06 and HU-08 as defended in the thesis user stories.
public sealed class AcceptanceCriteriaTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    private const string Password = "Local-test-password-123";
    private const double CentroLat = -31.416281, CentroLon = -64.184022;

    [Fact]
    public async Task Suspended_account_is_told_it_is_disabled_only_after_a_correct_password()
    {
        var (owner, _, _, email) = await Setup("Suspendida");
        owner.Dispose();
        await Sql("UPDATE user_accounts SET is_suspended = true WHERE email = $1", email);

        var wrong = await Login(email, "Wrong-password-0000");
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.DoesNotContain("habilitada", await wrong.Content.ReadAsStringAsync());

        var rejected = await Login(email, Password);
        Assert.Equal(HttpStatusCode.Forbidden, rejected.StatusCode);
        Assert.False(rejected.Headers.Contains("Set-Cookie"));
        var body = await rejected.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("account_suspended", body.GetProperty("code").GetString());
        Assert.Contains("no está habilitada", body.GetProperty("message").GetString());

        // A locked account keeps the generic answer even with the right password.
        await Sql("UPDATE user_accounts SET locked_until = now() + interval '15 minutes' WHERE email = $1", email);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(email, Password)).StatusCode);
    }

    [Fact]
    public async Task Negative_adjustment_reports_available_balance_and_low_stock_is_flagged()
    {
        var (owner, business, product, _) = await Setup("Stock");
        using var client = owner; using var guest = fixture.Client();
        var token = Guid.NewGuid().ToString("N");
        await Sql($"UPDATE catalog_products SET name = 'Yerba {token}' WHERE id = $1", product);

        (await Adjust(owner, business, product, 3, 0)).EnsureSuccessStatusCode();
        var refused = await Adjust(owner, business, product, -5, 1);
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        var problem = await refused.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, problem.GetProperty("availableQuantity").GetInt32());
        Assert.Contains("Saldo disponible: 3", problem.GetProperty("message").GetString());

        Assert.False(await LowStock(guest, owner, business, product, token));
        (await Adjust(owner, business, product, -1, 1)).EnsureSuccessStatusCode();
        Assert.True(await LowStock(guest, owner, business, product, token));
        (await Adjust(owner, business, product, 8, 2)).EnsureSuccessStatusCode();
        Assert.False(await LowStock(guest, owner, business, product, token));
    }

    [Fact]
    public async Task Search_tolerates_typos_in_name_or_description()
    {
        var (owner, business, product, _) = await Setup("Tipeo");
        using var client = owner; using var guest = fixture.Client();
        await Sql("UPDATE catalog_products SET name = 'Yerba mate suave', description = 'Paquete de 500 g' WHERE id = $1", product);
        var described = await Product(owner, business, "Paquete surtido", "Galletitas de chocolate");

        Assert.Contains(product, await Ids(guest, "yreba"));
        Assert.Contains(described, await Ids(guest, "chocolte"));
        Assert.DoesNotContain(product, await Ids(guest, "zzzz"));
    }

    [Fact]
    public async Task Zone_search_orders_products_by_equity_ranking_and_keeps_distance_without_ranking()
    {
        var token = Guid.NewGuid().ToString("N")[..10];
        var (nearOwner, near, nearProduct, _) = await Setup("Cerca", CentroLat, CentroLon);
        var (farOwner, far, farProduct, _) = await Setup("Lejos", CentroLat + .0045, CentroLon);
        using var a = nearOwner; using var b = farOwner;
        foreach (var id in new[] { nearProduct, farProduct })
            await Sql($"UPDATE catalog_products SET name = 'Queso {token}' WHERE id = $1", id);
        // The farther business carries an unmet exposure deficit in its zone/category magazine ledger.
        await Sql("INSERT INTO ranking_contexts (context_key, sequence) VALUES ('magazine/centro/category/2/radius/4/eligibility/v1', 1) ON CONFLICT DO NOTHING", far);
        for (var i = 0; i < 5; i++)
        {
            var opportunity = Guid.NewGuid();
            await Sql($"""
                INSERT INTO ranking_opportunities (id, context_key, sequence, occurred_at, payload)
                VALUES ('{opportunity}', 'magazine/centro/category/2/radius/4/eligibility/v1', {1000 + i}, now() - interval '1 hour', jsonb_build_object());
                INSERT INTO ranking_contributions (opportunity_id, business_id, expected, received) VALUES ('{opportunity}', $1, 0.9, 0);
                """, far);
        }
        var query = $"/public/products?q=queso%20{token}&zone=centro&radiusKm=2";

        using var plain = fixture.Client();
        var distance = await plain.GetFromJsonAsync<JsonElement>(query);
        Assert.Equal("distance", distance.GetProperty("order").GetString());
        Assert.Equal(JsonValueKind.Null, distance.GetProperty("policy").ValueKind);
        Assert.Equal([nearProduct, farProduct], Items(distance));

        await using var ranked = fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["Ranking:Enabled"] = "true" })));
        using var guest = ranked.CreateClient();
        var result = await guest.GetFromJsonAsync<JsonElement>(query);
        Assert.Equal("ranking", result.GetProperty("order").GetString());
        Assert.Equal("equity-v1", result.GetProperty("policy").GetString());
        Assert.Equal([farProduct, nearProduct], Items(result));
        Assert.Equal(0, await Count("SELECT count(*) FROM ranking_opportunities WHERE context_key LIKE 'product%'"));

        var unzoned = await guest.GetFromJsonAsync<JsonElement>(FormattableString.Invariant($"/public/products?q=queso%20{token}&lat={CentroLat}&lon={CentroLon}&radiusKm=2"));
        Assert.Equal("distance", unzoned.GetProperty("order").GetString());
        Assert.Equal([nearProduct, farProduct], Items(unzoned));
        var named = await guest.GetFromJsonAsync<JsonElement>($"/public/products?q=queso%20{token}");
        Assert.Equal("name", named.GetProperty("order").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await guest.GetAsync($"/public/products?q=queso&zone=marte&radiusKm=2")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await guest.GetAsync($"/public/products?q=queso&zone=centro")).StatusCode);
    }

    private static Guid[] Items(JsonElement page) => page.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ToArray();
    private static async Task<Guid[]> Ids(HttpClient guest, string q) =>
        Items(await guest.GetFromJsonAsync<JsonElement>($"/public/products?q={Uri.EscapeDataString(q)}"));
    private static async Task<bool> LowStock(HttpClient guest, HttpClient owner, Guid business, Guid product, string token)
    {
        var search = (await guest.GetFromJsonAsync<JsonElement>($"/public/products?q={token}")).GetProperty("items")
            .EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == product).GetProperty("lowStock").GetBoolean();
        var catalog = (await guest.GetFromJsonAsync<JsonElement>($"/public/businesses/{business}/products")).GetProperty("items")
            .EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == product).GetProperty("lowStock").GetBoolean();
        var inventory = (await owner.GetFromJsonAsync<JsonElement>($"/businesses/{business}/inventory"))
            .EnumerateArray().Single(x => x.GetProperty("productId").GetGuid() == product).GetProperty("lowStock").GetBoolean();
        Assert.Equal(search, catalog); Assert.Equal(search, inventory);
        return search;
    }
    private async Task<HttpResponseMessage> Login(string email, string password) =>
        await fixture.Client().PostAsJsonAsync("/auth/login", new { email, password });
    private static Task<HttpResponseMessage> Adjust(HttpClient owner, Guid business, Guid product, int delta, int version) =>
        owner.PutAsJsonAsync($"/businesses/{business}/inventory/adjustments/{Guid.NewGuid()}", new { productId = product, delta, version, reason = "Recuento" });
    private static async Task<Guid> Product(HttpClient owner, Guid business, string name, string description = "")
    {
        var id = Guid.NewGuid();
        (await owner.PutAsJsonAsync($"/businesses/{business}/products/{id}", new { name, description, category = "Almacén", price = 1250m, currency = "ARS", available = true, status = "published", version = 0 })).EnsureSuccessStatusCode();
        return id;
    }
    private async Task<(HttpClient Owner, Guid Business, Guid Product, string Email)> Setup(string name, double lat = -31.4167, double lon = -64.1833)
    {
        var client = fixture.Client(); var email = $"criteria-{Guid.NewGuid():N}@example.test";
        (await client.PostAsJsonAsync("/auth/register", new { email, password = Password, displayName = name })).EnsureSuccessStatusCode();
        var login = await client.PostAsJsonAsync("/auth/login", new { email, password = Password }); login.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Add("Cookie", Assert.Single(login.Headers.GetValues("Set-Cookie")).Split(';')[0]);
        var created = await client.PostAsJsonAsync("/businesses", new { name, description = "", address = "Córdoba", categoryId = 2, whatsApp = "+5493515551234", latitude = lat, longitude = lon, status = "published" });
        created.EnsureSuccessStatusCode(); var business = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await Sql("INSERT INTO business_subscriptions (id,business_id,starts_at,ends_at,granted_by,granted_at,reason) SELECT gen_random_uuid(),id,now()-interval '1 day',now()+interval '30 days',owner_id,now(),'Test' FROM businesses WHERE id=$1", business);
        return (client, business, await Product(client, business, "Producto"), email);
    }
    private async Task Sql(string sql, object value)
    {
        await using var db = new NpgsqlConnection(fixture.ConnectionString); await db.OpenAsync();
        await using var batch = new NpgsqlBatch(db);
        foreach (var statement in sql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var command = new NpgsqlBatchCommand(statement);
            if (statement.Contains("$1")) command.Parameters.Add(new NpgsqlParameter { Value = value });
            batch.BatchCommands.Add(command);
        }
        await batch.ExecuteNonQueryAsync();
    }
    private async Task<long> Count(string sql)
    {
        await using var db = new NpgsqlConnection(fixture.ConnectionString); await db.OpenAsync();
        await using var command = new NpgsqlCommand(sql, db);
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }
}
