using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;

namespace Marketplace.Api.Tests;

public sealed class SalesTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    [Fact]
    public async Task Commerce_metrics_keep_cohort_conversion_and_period_sales_separate()
    {
        var (owner, business, product) = await Setup(); using var client = owner;
        (await Adjust(owner, business, product, 10, 0)).EnsureSuccessStatusCode();
        var currentOrder = await Order(business, product); var oldOrder = await Order(business, product);
        await Scalar("UPDATE customer_orders SET created_at=now()-interval '40 days' WHERE id=$1", oldOrder);
        foreach (var order in new[] { currentOrder, oldOrder })
            (await owner.PutAsJsonAsync($"/businesses/{business}/sales/{Guid.NewGuid()}", new { orderId = order, orderVersion = 1, lines = new[] { Line(product, 1, 100m, "requested") } })).EnsureSuccessStatusCode();
        var direct = Guid.NewGuid();
        (await owner.PutAsJsonAsync($"/businesses/{business}/sales/{direct}", new { lines = new[] { Line(product, 1, 50m) } })).EnsureSuccessStatusCode();
        var response = await owner.GetAsync($"/businesses/{business}/commerce?days=7");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        var metrics = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, metrics.GetProperty("orders").GetInt32());
        Assert.Equal(1, metrics.GetProperty("converted").GetInt32());
        Assert.Equal(1m, metrics.GetProperty("conversionRate").GetDecimal());
        Assert.Equal(3, metrics.GetProperty("activeSales").GetInt32());
        Assert.Equal(200m, metrics.GetProperty("amounts").GetProperty("requested").GetProperty("ARS").GetDecimal());
        Assert.Equal(50m, metrics.GetProperty("amounts").GetProperty("direct").GetProperty("ARS").GetDecimal());
        (await owner.PutAsJsonAsync($"/businesses/{business}/sales/{direct}/void", new { operationId = Guid.NewGuid(), reason = "Corrección comercial", returns = Array.Empty<object>() })).EnsureSuccessStatusCode();
        metrics = await owner.GetFromJsonAsync<JsonElement>($"/businesses/{business}/commerce?days=7");
        Assert.Equal(2, metrics.GetProperty("activeSales").GetInt32());
        Assert.Equal(1, metrics.GetProperty("voidedSales").GetInt32());
        Assert.False(metrics.GetProperty("amounts").TryGetProperty("direct", out _));
    }

    [Fact]
    public async Task Commerce_metrics_restrict_owner_and_period_and_show_empty_cohort_as_unknown()
    {
        var (owner, business, _) = await Setup(); using var client = owner; using var guest = fixture.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.GetAsync($"/businesses/{business}/commerce")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.GetAsync($"/businesses/{business}/commerce?days=8")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/businesses/{Guid.NewGuid()}/commerce")).StatusCode);
        var metrics = await owner.GetFromJsonAsync<JsonElement>($"/businesses/{business}/commerce?days=30");
        Assert.Equal(JsonValueKind.Null, metrics.GetProperty("conversionRate").ValueKind);
        Assert.Equal(0, metrics.GetProperty("activeSales").GetInt32());
    }

    [Fact]
    public async Task Invalid_sale_lines_returns_and_untrusted_writes_do_not_change_inventory()
    {
        var (owner, business, product) = await Setup(); using var client = owner;
        (await Adjust(owner, business, product, 5, 0)).EnsureSuccessStatusCode();
        var path = $"/businesses/{business}/sales/{Guid.NewGuid()}";
        foreach (var line in new[] { Line(product, 0), Line(product, 1, -1), Line(product, 1, 1.001m), Line(product, 1, 1, "requested") })
            Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsJsonAsync(path, new { lines = new[] { line } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsJsonAsync(path, new { lines = new[] { Line(product, 1), Line(product, 1) } })).StatusCode);
        using var guest = fixture.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.GetAsync($"/businesses/{business}/inventory")).StatusCode);
        using var untrusted = fixture.Client(false);
        Assert.Equal(HttpStatusCode.Forbidden, (await untrusted.PutAsJsonAsync(path, new { lines = new[] { Line(product, 1) } })).StatusCode);
        using var large = new StringContent(new string(' ', 16385), System.Text.Encoding.UTF8, "application/json");
        var oversized = await owner.PutAsync(path, large);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, oversized.StatusCode);
        Assert.Equal("no-store", oversized.Headers.CacheControl?.ToString());
        var id = Guid.NewGuid();
        (await owner.PutAsJsonAsync($"/businesses/{business}/sales/{id}", new { lines = new[] { Line(product, 2) } })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PutAsJsonAsync($"/businesses/{business}/sales/{id}/void", new { operationId = Guid.NewGuid(), reason = "Exceso de devolución", returns = new[] { new { productId = product, quantity = 3 } } })).StatusCode);
        Assert.Equal(3L, await Scalar("SELECT quantity FROM inventory_balances WHERE product_id=$1", product));
        Assert.Equal(0L, await Scalar("SELECT count(*) FROM sales WHERE id=$1 AND voided_at IS NOT NULL", id));
    }

    [Fact]
    public async Task Stock_adjustment_is_audited_versioned_and_idempotent()
    {
        var (owner, business, product) = await Setup(); using var client = owner;
        var id = Guid.NewGuid(); var body = new { productId = product, delta = 5, version = 0, reason = "Carga inicial" };
        var path = $"/businesses/{business}/inventory/adjustments/{id}";
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync(path, body)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync(path, body)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PutAsJsonAsync(path, new { productId = product, delta = 6, version = 0, reason = "Carga inicial" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Adjust(owner, business, product, -6, 1)).StatusCode);
        Assert.Equal(5L, await Scalar("SELECT quantity FROM inventory_balances WHERE product_id=$1", product));
        Assert.Equal(1L, await Scalar("SELECT count(*) FROM inventory_movements WHERE product_id=$1", product));
        var history = await owner.GetFromJsonAsync<JsonElement>($"/businesses/{business}/inventory/movements?productId={product}");
        Assert.Single(history.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Linked_sale_preserves_order_and_deducts_once_with_separate_origins()
    {
        var (owner, business, product) = await Setup(); using var client = owner;
        var added = await Product(owner, business, "Extra");
        (await Adjust(owner, business, product, 5, 0)).EnsureSuccessStatusCode();
        (await Adjust(owner, business, added, 3, 0)).EnsureSuccessStatusCode();
        var order = await Order(business, product); var sale = Guid.NewGuid();
        var body = new { orderId = order, orderVersion = 1, lines = new[] { Line(product, 2, 900m, "requested"), Line(added, 1, 200m, "conversation") } };
        var path = $"/businesses/{business}/sales/{sale}";
        var results = await Task.WhenAll(owner.PutAsJsonAsync(path, body), owner.PutAsJsonAsync(path, body));
        Assert.All(results, r => Assert.True(r.IsSuccessStatusCode));
        var saved = await results[0].Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1800m, saved.GetProperty("totals").GetProperty("requested").GetProperty("ARS").GetDecimal());
        Assert.Equal(200m, saved.GetProperty("totals").GetProperty("conversation").GetProperty("ARS").GetDecimal());
        Assert.Equal(3L, await Scalar("SELECT quantity FROM inventory_balances WHERE product_id=$1", product));
        Assert.Equal(1L, await Scalar("SELECT count(*) FROM sales WHERE order_id=$1", order));
        Assert.Equal(1L, await Scalar("SELECT count(*) FROM customer_orders WHERE id=$1 AND status='converted'", order));
        Assert.Equal(1250L, await Scalar("SELECT unit_price FROM order_lines WHERE order_id=$1", order));
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PutAsJsonAsync($"/businesses/{business}/sales/{Guid.NewGuid()}", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PutAsJsonAsync($"/businesses/{business}/orders/{order}", new { version = 2, status = "cancelled", contactReceived = false })).StatusCode);
    }

    [Fact]
    public async Task Concurrent_sales_cannot_oversell_and_failed_multiline_sale_is_atomic()
    {
        var (owner, business, product) = await Setup(); using var client = owner;
        var other = await Product(owner, business, "Otro");
        (await Adjust(owner, business, product, 2, 0)).EnsureSuccessStatusCode();
        (await Adjust(owner, business, other, 0, 0)).EnsureSuccessStatusCode();
        var failed = new { lines = new[] { Line(product, 1), Line(other, 1) } };
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PutAsJsonAsync($"/businesses/{business}/sales/{Guid.NewGuid()}", failed)).StatusCode);
        Assert.Equal(2L, await Scalar("SELECT quantity FROM inventory_balances WHERE product_id=$1", product));
        var body = new { lines = new[] { Line(product, 2) } };
        var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => owner.PutAsJsonAsync($"/businesses/{business}/sales/{Guid.NewGuid()}", body)));
        Assert.Single(results, r => r.IsSuccessStatusCode);
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(0L, await Scalar("SELECT quantity FROM inventory_balances WHERE product_id=$1", product));
        using var guest = fixture.Client();
        var catalog = await guest.GetFromJsonAsync<JsonElement>($"/public/businesses/{business}/products");
        Assert.False(catalog.GetProperty("items").EnumerateArray().Single(p => p.GetProperty("id").GetGuid() == product).GetProperty("available").GetBoolean());
        Assert.Equal(HttpStatusCode.Conflict, (await guest.PostAsJsonAsync($"/public/businesses/{business}/orders", OrderBody(Guid.NewGuid(), product))).StatusCode);
    }

    [Fact]
    public async Task Voiding_sale_only_returns_explicit_units_and_retries_without_duplicates()
    {
        var (owner, business, product) = await Setup(); using var client = owner;
        (await Adjust(owner, business, product, 5, 0)).EnsureSuccessStatusCode();
        var id = Guid.NewGuid();
        (await owner.PutAsJsonAsync($"/businesses/{business}/sales/{id}", new { lines = new[] { Line(product, 3) } })).EnsureSuccessStatusCode();
        var path = $"/businesses/{business}/sales/{id}/void";
        var body = new { operationId = Guid.NewGuid(), reason = "Solo una unidad devuelta", returns = new[] { new { productId = product, quantity = 1 } } };
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync(path, body)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync(path, body)).StatusCode);
        Assert.Equal(3L, await Scalar("SELECT quantity FROM inventory_balances WHERE product_id=$1", product));
        Assert.Equal(1L, await Scalar("SELECT count(*) FROM inventory_movements WHERE sale_id=$1 AND kind='return'", id));
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PutAsJsonAsync(path, new { operationId = Guid.NewGuid(), reason = "Otra anulación", returns = Array.Empty<object>() })).StatusCode);
    }

    [Fact]
    public async Task Ownership_validation_and_expired_plan_are_enforced_without_losing_existing_orders()
    {
        var (owner, business, product) = await Setup(); using var client = owner;
        var (stranger, otherBusiness, otherProduct) = await Setup(); using var other = stranger;
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/businesses/{business}/inventory")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/businesses/{business}/sales")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Adjust(stranger, business, product, 3, 0)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Adjust(owner, business, otherProduct, 3, 0)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Adjust(owner, business, product, 1000001, 0)).StatusCode);
        (await Adjust(owner, business, product, 5, 0)).EnsureSuccessStatusCode();
        var order = await Order(business, product);
        await Scalar("UPDATE business_subscriptions SET ends_at=now()-interval '1 second' WHERE business_id=$1", business);
        var direct = new { lines = new[] { Line(product, 1) } };
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PutAsJsonAsync($"/businesses/{business}/sales/{Guid.NewGuid()}", direct)).StatusCode);
        var linked = new { orderId = order, orderVersion = 1, lines = new[] { Line(product, 1, 100m, "requested") } };
        Assert.True((await owner.PutAsJsonAsync($"/businesses/{business}/sales/{Guid.NewGuid()}", linked)).IsSuccessStatusCode);
    }

    private static object Line(Guid product, int quantity, decimal price = 100m, string origin = "direct") => new { productId = product, quantity, unitPrice = price, currency = "ARS", origin };
    private Task<HttpResponseMessage> Adjust(HttpClient owner, Guid business, Guid product, int delta, int version) => owner.PutAsJsonAsync($"/businesses/{business}/inventory/adjustments/{Guid.NewGuid()}", new { productId = product, delta, version, reason = "Recuento" });
    private static object OrderBody(Guid id, Guid product) => new { requestId = id, accessKey = new string('a', 64), customerName = "Ana", lines = new[] { new { productId = product, quantity = 1, expectedVersion = 1 } } };
    private async Task<Guid> Order(Guid business, Guid product)
    {
        using var guest = fixture.Client(); var id = Guid.NewGuid();
        (await guest.PostAsJsonAsync($"/public/businesses/{business}/orders", OrderBody(id, product))).EnsureSuccessStatusCode();
        return id;
    }
    private static async Task<Guid> Product(HttpClient owner, Guid business, string name)
    {
        var id = Guid.NewGuid();
        (await owner.PutAsJsonAsync($"/businesses/{business}/products/{id}", new { name, description = "", category = "Café", price = 1250m, currency = "ARS", available = true, status = "published", version = 0 })).EnsureSuccessStatusCode();
        return id;
    }
    private async Task<(HttpClient Owner, Guid Business, Guid Product)> Setup()
    {
        var client = fixture.Client(); var email = $"sales-{Guid.NewGuid():N}@example.test"; var password = "Local-test-password-123";
        (await client.PostAsJsonAsync("/auth/register", new { email, password, displayName = "Ventas" })).EnsureSuccessStatusCode();
        var login = await client.PostAsJsonAsync("/auth/login", new { email, password }); login.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Add("Cookie", Assert.Single(login.Headers.GetValues("Set-Cookie")).Split(';')[0]);
        var created = await client.PostAsJsonAsync("/businesses", new { name = "Local", description = "", address = "Córdoba", categoryId = 2, whatsApp = "+5493515551234", latitude = -31.4167, longitude = -64.1833, status = "published" });
        created.EnsureSuccessStatusCode(); var business = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await Scalar("INSERT INTO business_subscriptions (id,business_id,starts_at,ends_at,granted_by,granted_at,reason) SELECT gen_random_uuid(),id,now()-interval '1 day',now()+interval '30 days',owner_id,now(),'Test' FROM businesses WHERE id=$1", business);
        return (client, business, await Product(client, business, "Café"));
    }
    private async Task<long> Scalar(string sql, Guid id)
    {
        await using var db = new NpgsqlConnection(fixture.ConnectionString); await db.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, db); cmd.Parameters.AddWithValue(id);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync());
    }
}
