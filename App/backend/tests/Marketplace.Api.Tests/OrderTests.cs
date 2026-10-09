using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Npgsql;

namespace Marketplace.Api.Tests;

public sealed class OrderTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    [Fact]
    public async Task Invalid_quantities_duplicate_lines_and_oversized_payloads_are_rejected()
    {
        var (owner, business, product) = await Setup(); using var account = owner; using var guest = fixture.Client();
        var path = $"/public/businesses/{business}/orders";
        foreach (var quantity in new[] { 0, -1, 100 })
            Assert.Equal(HttpStatusCode.BadRequest, (await guest.PostAsJsonAsync(path, Body(Guid.NewGuid(), Key(), product, quantity))).StatusCode);
        var duplicate = new { requestId = Guid.NewGuid(), accessKey = Key(), customerName = "Ana", lines = Enumerable.Repeat(new { productId = product, quantity = 1, expectedVersion = 1 }, 2) };
        Assert.Equal(HttpStatusCode.BadRequest, (await guest.PostAsJsonAsync(path, duplicate)).StatusCode);
        using var large = new StringContent(new string(' ', 17000), System.Text.Encoding.UTF8, "application/json");
        var response = await guest.PostAsync(path, large);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal(0L, await Scalar("SELECT count(*) FROM customer_orders WHERE business_id = $1", business));
    }

    [Fact]
    public async Task Missing_plan_unpublished_or_suspended_business_cannot_receive_new_orders()
    {
        var (owner, business, product) = await Setup(); using var account = owner; using var guest = fixture.Client();
        var path = $"/public/businesses/{business}/orders";
        await Sql("UPDATE businesses SET status = 'draft' WHERE id = $1", business);
        Assert.Equal(HttpStatusCode.Forbidden, (await guest.PostAsJsonAsync(path, Body(Guid.NewGuid(), Key(), product))).StatusCode);
        await Sql("UPDATE businesses SET status = 'published' WHERE id = $1", business);
        await Sql("UPDATE user_accounts SET is_suspended = true WHERE id = (SELECT owner_id FROM businesses WHERE id = $1)", business);
        Assert.Equal(HttpStatusCode.Forbidden, (await guest.PostAsJsonAsync(path, Body(Guid.NewGuid(), Key(), product))).StatusCode);
        await Sql("UPDATE user_accounts SET is_suspended = false WHERE id = (SELECT owner_id FROM businesses WHERE id = $1)", business);
        await Sql("UPDATE business_subscriptions SET ends_at = now() - interval '1 second' WHERE business_id = $1", business);
        Assert.Equal(HttpStatusCode.Forbidden, (await guest.PostAsJsonAsync(path, Body(Guid.NewGuid(), Key(), product))).StatusCode);
        using var untrusted = fixture.Client(false);
        Assert.Equal(HttpStatusCode.Forbidden, (await untrusted.PostAsJsonAsync(path, Body(Guid.NewGuid(), Key(), product))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.GetAsync($"/businesses/{business}/orders")).StatusCode);
    }

    [Fact]
    public async Task Cross_business_lines_are_rejected_and_totals_keep_currencies_separate()
    {
        var (owner, business, product) = await Setup(); using var account = owner; using var guest = fixture.Client();
        var (otherOwner, _, otherProduct) = await Setup(); using var other = otherOwner;
        var path = $"/public/businesses/{business}/orders";
        Assert.Equal(HttpStatusCode.Conflict, (await guest.PostAsJsonAsync(path, Body(Guid.NewGuid(), Key(), otherProduct))).StatusCode);
        var usd = Guid.NewGuid();
        (await owner.PutAsJsonAsync($"/businesses/{business}/products/{usd}", new { name = "Importado", description = "", category = "Café", price = 10.25m, currency = "USD", available = true, status = "published", version = 0 })).EnsureSuccessStatusCode();
        var id = Guid.NewGuid(); var key = Key();
        var response = await guest.PostAsJsonAsync(path, new { requestId = id, accessKey = key, customerName = "Ana", lines = new[] {
            new { productId = product, quantity = 2, expectedVersion = 1 }, new { productId = usd, quantity = 3, expectedVersion = 1 } } });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2501m, order.GetProperty("totals").GetProperty("ARS").GetDecimal());
        Assert.Equal(30.75m, order.GetProperty("totals").GetProperty("USD").GetDecimal());
        var reference = order.GetProperty("reference").GetString();
        var page = await owner.GetFromJsonAsync<JsonElement>($"/businesses/{business}/orders?reference={reference}&status=pending");
        Assert.Single(page.GetProperty("items").EnumerateArray());
        Assert.Equal(1L, await Scalar("SELECT count(*) FROM catalog_products WHERE id=$1 AND version=1 AND price=1250.50", product));
        Assert.Equal(0L, await Scalar("SELECT count(*) FROM customer_orders WHERE access_key_hash=$1", key));
    }

    [Fact]
    public async Task Creation_snapshots_prices_and_replays_concurrently_without_duplicates()
    {
        var (owner, business, product) = await Setup();
        using var account = owner;
        using var guest = fixture.Client();
        var id = Guid.NewGuid(); var key = Key(); var body = Body(id, key, product);
        var responses = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => guest.PostAsJsonAsync($"/public/businesses/{business}/orders", body)));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(2, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
        var order = await responses[0].Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1250.50m, order.GetProperty("lines")[0].GetProperty("unitPrice").GetDecimal());
        Assert.Equal(2501m, order.GetProperty("totals").GetProperty("ARS").GetDecimal());
        Assert.Equal("pending", order.GetProperty("status").GetString());
        Assert.Null(order.GetProperty("contactReceivedAt").GetString());
        await Sql("UPDATE catalog_products SET price = 9999, name = 'Nuevo', status = 'archived', version = 2 WHERE id = $1", product);
        await Sql("UPDATE business_subscriptions SET revoked_at = now(), revoked_by = granted_by, revocation_reason = 'Prueba' WHERE business_id = $1", business);
        Assert.Equal(HttpStatusCode.OK, (await guest.PostAsJsonAsync($"/public/businesses/{business}/orders", body)).StatusCode);
        var stored = await Read(guest, id, key);
        Assert.Equal(1250.50m, stored.GetProperty("lines")[0].GetProperty("unitPrice").GetDecimal());
        Assert.Equal("Café", stored.GetProperty("lines")[0].GetProperty("name").GetString());
        Assert.Equal(1L, await Scalar("SELECT count(*) FROM customer_orders WHERE business_id = $1", business));
        Assert.Equal(0L, await Scalar("SELECT count(*) FROM order_changes WHERE order_id = $1", id));
        Assert.DoesNotContain(key, stored.ToString());
        Assert.Equal(HttpStatusCode.Conflict, (await guest.PostAsJsonAsync($"/public/businesses/{business}/orders", Body(id, key, product, 3))).StatusCode);
    }

    [Fact]
    public async Task Private_access_requires_key_and_owner_isolation_and_expires()
    {
        var (owner, business, product) = await Setup(); using var account = owner;
        using var guest = fixture.Client();
        var id = Guid.NewGuid(); var key = Key();
        Assert.Equal(HttpStatusCode.Created, (await guest.PostAsJsonAsync($"/public/businesses/{business}/orders", Body(id, key, product))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await guest.GetAsync($"/public/orders/{id}")).StatusCode);
        using var invalid = new HttpRequestMessage(HttpMethod.Get, $"/public/orders/{id}");
        invalid.Headers.Add("X-Order-Key", Key());
        Assert.Equal(HttpStatusCode.NotFound, (await guest.SendAsync(invalid)).StatusCode);
        using var other = await Account();
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/businesses/{business}/orders/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/businesses/{business}/orders")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/businesses/{business}/orders/{id}")).StatusCode);
        var read = await Read(guest, id, key);
        Assert.Equal("Ana", read.GetProperty("customerName").GetString());
        await Sql("UPDATE customer_orders SET access_expires_at = now() - interval '1 second' WHERE id = $1", id);
        using var expired = new HttpRequestMessage(HttpMethod.Get, $"/public/orders/{id}"); expired.Headers.Add("X-Order-Key", key);
        Assert.Equal(HttpStatusCode.NotFound, (await guest.SendAsync(expired)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/businesses/{business}/orders/{id}")).StatusCode);
    }

    [Theory]
    [InlineData("UPDATE catalog_products SET available = false WHERE id = $1")]
    [InlineData("UPDATE catalog_products SET status = 'draft' WHERE id = $1")]
    [InlineData("UPDATE catalog_products SET version = 2 WHERE id = $1")]
    public async Task Changed_or_unavailable_product_cannot_be_requested(string sql)
    {
        var (owner, business, product) = await Setup(); using var account = owner;
        await Sql(sql, product); using var guest = fixture.Client();
        Assert.Equal(HttpStatusCode.Conflict, (await guest.PostAsJsonAsync($"/public/businesses/{business}/orders", Body(Guid.NewGuid(), Key(), product))).StatusCode);
        Assert.Equal(0L, await Scalar("SELECT count(*) FROM customer_orders WHERE business_id = $1", business));
    }

    [Fact]
    public async Task Owner_resolves_existing_order_after_plan_expires_and_logs_each_change_once()
    {
        var (owner, business, product) = await Setup(); using var account = owner;
        using var guest = fixture.Client(); var id = Guid.NewGuid(); var key = Key();
        await guest.PostAsJsonAsync($"/public/businesses/{business}/orders", Body(id, key, product));
        await Sql("UPDATE business_subscriptions SET ends_at = now() - interval '1 second' WHERE business_id = $1", business);
        var path = $"/businesses/{business}/orders/{id}";
        var updates = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => owner.PutAsJsonAsync(path, new { version = 1, status = "in_attention", contactReceived = true })));
        Assert.Single(updates, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(updates, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(1L, await Scalar("SELECT count(*) FROM order_changes WHERE order_id = $1", id));
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsJsonAsync(path, new { version = 2, status = "converted", contactReceived = true })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync(path, new { version = 2, status = "cancelled", contactReceived = true })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PutAsJsonAsync(path, new { version = 3, status = "pending", contactReceived = true })).StatusCode);
        var read = await Read(guest, id, key);
        Assert.Equal("cancelled", read.GetProperty("status").GetString());
        Assert.NotNull(read.GetProperty("contactReceivedAt").GetString());
        Assert.False(read.GetProperty("canContact").GetBoolean());
    }

    private static string Key() => Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
    private static object Body(Guid id, string key, Guid product, int quantity = 2) => new
    { requestId = id, accessKey = key, customerName = "Ana", lines = new[] { new { productId = product, quantity, expectedVersion = 1 } } };
    private static async Task<JsonElement> Read(HttpClient client, Guid id, string key)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/public/orders/{id}"); request.Headers.Add("X-Order-Key", key);
        var response = await client.SendAsync(request); response.EnsureSuccessStatusCode();
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
    private async Task<(HttpClient Owner, Guid Business, Guid Product)> Setup()
    {
        var owner = await Account();
        var created = await owner.PostAsJsonAsync("/businesses", new { name = "Café local", description = "", address = "Córdoba", categoryId = 2,
            whatsApp = "+5493515551234", latitude = -31.4167, longitude = -64.1833, status = "published" });
        created.EnsureSuccessStatusCode();
        var business = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await Sql("INSERT INTO business_subscriptions (id,business_id,starts_at,ends_at,granted_by,granted_at,reason) SELECT gen_random_uuid(),id,now()-interval '1 day',now()+interval '30 days',owner_id,now(),'Test' FROM businesses WHERE id=$1", business);
        var product = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync($"/businesses/{business}/products/{product}", new { name = "Café", description = "250 g", category = "Café", price = 1250.50m, currency = "ARS", available = true, status = "published", version = 0 })).StatusCode);
        return (owner, business, product);
    }
    private async Task<HttpClient> Account()
    {
        var client = fixture.Client(); var email = $"order-{Guid.NewGuid():N}@example.test"; var password = "Local-test-password-123";
        (await client.PostAsJsonAsync("/auth/register", new { email, password, displayName = "Pedidos" })).EnsureSuccessStatusCode();
        var login = await client.PostAsJsonAsync("/auth/login", new { email, password }); login.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Add("Cookie", Assert.Single(login.Headers.GetValues("Set-Cookie")).Split(';')[0]);
        return client;
    }
    private async Task Sql(string sql, object value) => await Scalar(sql, value);
    private async Task<long> Scalar(string sql, object value)
    {
        await using var db = new NpgsqlConnection(fixture.ConnectionString); await db.OpenAsync();
        await using var command = new NpgsqlCommand(sql, db); command.Parameters.AddWithValue(value);
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }
}
