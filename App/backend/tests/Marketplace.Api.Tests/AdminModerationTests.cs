using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Npgsql;

namespace Marketplace.Api.Tests;

public sealed class AdminModerationTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    private const string Password = "Local-test-password-123";

    [Fact]
    public async Task Admin_routes_reject_owners_anonymous_and_untrusted_mutations()
    {
        var (owner, _) = await Account();
        using var ownerClient = owner;
        using var anonymous = fixture.Client();
        foreach (var path in new[] { "/admin/categories", "/admin/moderation/accounts", "/admin/moderation/businesses", "/admin/moderation/products", "/admin/audit" })
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await ownerClient.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(path)).StatusCode);
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await ownerClient.PostAsJsonAsync("/admin/categories", new { name = "Ferretería" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ownerClient.PostAsJsonAsync($"/admin/moderation/accounts/{Guid.NewGuid()}/suspend", new { reason = "Prueba" })).StatusCode);

        var (admin, _) = await Account(true);
        using var adminClient = admin;
        using var untrusted = fixture.Client(false);
        foreach (var header in adminClient.DefaultRequestHeaders.GetValues("Cookie")) untrusted.DefaultRequestHeaders.Add("Cookie", header);
        Assert.Equal(HttpStatusCode.Forbidden, (await untrusted.PostAsJsonAsync("/admin/categories", new { name = "Ferretería" })).StatusCode);
    }

    [Fact]
    public async Task Suspended_owner_cannot_log_in_loses_sessions_and_disappears_until_reinstated()
    {
        var (owner, email) = await Account();
        using var ownerClient = owner;
        var (admin, adminEmail) = await Account(true);
        using var adminClient = admin;
        var name = "Moderado " + Guid.NewGuid().ToString("N")[..8];
        var business = await Business(ownerClient, name);
        var ownerId = (await ownerClient.GetFromJsonAsync<JsonElement>("/auth/session")).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.BadRequest, (await adminClient.PostAsJsonAsync($"/admin/moderation/accounts/{ownerId}/suspend", new { reason = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await adminClient.PostAsJsonAsync($"/admin/moderation/accounts/{ownerId}/suspend", new { reason = "Reclamos reiterados" })).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await ownerClient.GetAsync("/auth/session")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(email, Password)).StatusCode);
        Assert.False(await Listed(name));
        Assert.Equal(HttpStatusCode.NotFound, (await fixture.Client().GetAsync($"/public/businesses/{business}")).StatusCode);
        var accounts = await adminClient.GetFromJsonAsync<JsonElement>($"/admin/moderation/accounts?q={Uri.EscapeDataString(email)}");
        Assert.True(accounts[0].GetProperty("isSuspended").GetBoolean());

        Assert.Equal(HttpStatusCode.OK, (await adminClient.PostAsJsonAsync($"/admin/moderation/accounts/{ownerId}/reinstate", new { reason = "Descargo aceptado" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(email, Password)).StatusCode);
        Assert.True(await Listed(name));

        var audit = await adminClient.GetFromJsonAsync<JsonElement>("/admin/audit");
        var rows = audit.GetProperty("items").EnumerateArray().Where(x => x.GetProperty("targetId").GetString() == ownerId.ToString()).ToArray();
        Assert.Equal(["reinstate", "suspend"], rows.Select(x => x.GetProperty("action").GetString()!));
        Assert.All(rows, x => Assert.Equal("account", x.GetProperty("targetType").GetString()));
        Assert.All(rows, x => Assert.Equal(adminEmail, x.GetProperty("actorEmail").GetString()));
        Assert.Equal("Reclamos reiterados", rows[1].GetProperty("reason").GetString());

        var adminId = (await adminClient.GetFromJsonAsync<JsonElement>("/auth/session")).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Conflict, (await adminClient.PostAsJsonAsync($"/admin/moderation/accounts/{adminId}/suspend", new { reason = "Autosuspensión" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await adminClient.PostAsJsonAsync($"/admin/moderation/accounts/{Guid.NewGuid()}/suspend", new { reason = "Inexistente" })).StatusCode);
    }

    [Fact]
    public async Task Suspended_business_and_hidden_product_leave_public_surfaces_until_reinstated()
    {
        var (owner, _) = await Account();
        using var ownerClient = owner;
        var (admin, _) = await Account(true);
        using var adminClient = admin;
        var name = "Negocio " + Guid.NewGuid().ToString("N")[..8];
        var business = await Business(ownerClient, name);
        Assert.Equal(HttpStatusCode.OK, (await adminClient.PutAsJsonAsync($"/admin/businesses/{business}/subscriptions/{Guid.NewGuid()}", new
        { startsAt = DateTimeOffset.UtcNow.AddMinutes(-1), endsAt = DateTimeOffset.UtcNow.AddDays(30), reason = "Activación" })).StatusCode);
        var product = Guid.NewGuid();
        var productName = "Producto " + Guid.NewGuid().ToString("N")[..8];
        Assert.Equal(HttpStatusCode.OK, (await ownerClient.PutAsJsonAsync($"/businesses/{business}/products/{product}", new
        { name = productName, description = "Paquete", category = "Café", price = 100m, currency = "ARS", available = true, status = "published", version = 0 })).StatusCode);
        Assert.True(await ProductListed(productName));

        Assert.Equal(HttpStatusCode.OK, (await adminClient.PostAsJsonAsync($"/admin/moderation/products/{product}/suspend", new { reason = "Producto prohibido" })).StatusCode);
        Assert.False(await ProductListed(productName));
        Assert.Equal(0, (await fixture.Client().GetFromJsonAsync<JsonElement>($"/public/businesses/{business}/products")).GetProperty("items").GetArrayLength());
        Assert.Equal(HttpStatusCode.Conflict, (await fixture.Client().PostAsJsonAsync($"/public/businesses/{business}/orders", Order(product))).StatusCode);
        var products = await adminClient.GetFromJsonAsync<JsonElement>($"/admin/moderation/products?q={productName}");
        Assert.True(products[0].GetProperty("isSuspended").GetBoolean());
        Assert.Single((await ownerClient.GetFromJsonAsync<JsonElement>($"/businesses/{business}/products")).EnumerateArray());
        Assert.Equal(HttpStatusCode.OK, (await adminClient.PostAsJsonAsync($"/admin/moderation/products/{product}/reinstate", new { reason = "Revisado" })).StatusCode);
        Assert.True(await ProductListed(productName));

        Assert.Equal(HttpStatusCode.OK, (await adminClient.PostAsJsonAsync($"/admin/moderation/businesses/{business}/suspend", new { reason = "Datos falsos" })).StatusCode);
        Assert.False(await Listed(name));
        Assert.False(await ProductListed(productName));
        Assert.Equal(HttpStatusCode.NotFound, (await fixture.Client().GetAsync($"/public/businesses/{business}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fixture.Client().GetAsync($"/public/businesses/{business}/products")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await fixture.Client().PostAsJsonAsync($"/public/businesses/{business}/orders", Order(product))).StatusCode);
        // The owner keeps access to the private profile while suspended.
        Assert.Equal(HttpStatusCode.OK, (await ownerClient.GetAsync($"/businesses/{business}")).StatusCode);
        var businesses = await adminClient.GetFromJsonAsync<JsonElement>($"/admin/moderation/businesses?q={name}");
        Assert.True(businesses[0].GetProperty("isSuspended").GetBoolean());

        Assert.Equal(HttpStatusCode.OK, (await adminClient.PostAsJsonAsync($"/admin/moderation/businesses/{business}/reinstate", new { reason = "Datos corregidos" })).StatusCode);
        Assert.True(await Listed(name));
        Assert.Equal(HttpStatusCode.OK, (await fixture.Client().GetAsync($"/public/businesses/{business}")).StatusCode);

        var audit = (await adminClient.GetFromJsonAsync<JsonElement>("/admin/audit")).GetProperty("items").EnumerateArray().ToArray();
        Assert.Contains(audit, x => x.GetProperty("targetType").GetString() == "product" && x.GetProperty("action").GetString() == "suspend" && x.GetProperty("reason").GetString() == "Producto prohibido");
        Assert.Contains(audit, x => x.GetProperty("targetType").GetString() == "business" && x.GetProperty("targetId").GetString() == business.ToString() && x.GetProperty("action").GetString() == "reinstate");
        Assert.Equal(HttpStatusCode.Conflict, (await adminClient.PostAsJsonAsync($"/admin/moderation/businesses/{business}/reinstate", new { reason = "Repetido" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await adminClient.PostAsJsonAsync($"/admin/moderation/widgets/{business}/suspend", new { reason = "Destino inválido" })).StatusCode);
    }

    [Fact]
    public async Task Admin_creates_renames_and_disables_categories_without_breaking_existing_businesses()
    {
        var (owner, _) = await Account();
        using var ownerClient = owner;
        var (admin, _) = await Account(true);
        using var adminClient = admin;
        var categoryName = "Ferretería " + Guid.NewGuid().ToString("N")[..6];
        var created = await adminClient.PostAsJsonAsync("/admin/categories", new { name = categoryName });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var category = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = category.GetProperty("id").GetInt16();
        Assert.True(category.GetProperty("isActive").GetBoolean());
        Assert.Equal(HttpStatusCode.Conflict, (await adminClient.PostAsJsonAsync("/admin/categories", new { name = categoryName })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await adminClient.PostAsJsonAsync("/admin/categories", new { name = " " })).StatusCode);

        var businessName = "Rubro " + Guid.NewGuid().ToString("N")[..8];
        var business = await Business(ownerClient, businessName, id);
        var renamed = categoryName + " y bazar";
        Assert.Equal(HttpStatusCode.OK, (await adminClient.PutAsJsonAsync($"/admin/categories/{id}", new { name = renamed, isActive = false })).StatusCode);

        var categories = await fixture.Client().GetFromJsonAsync<JsonElement>("/business-categories");
        var listed = categories.EnumerateArray().Single(x => x.GetProperty("id").GetInt16() == id);
        Assert.False(listed.GetProperty("isActive").GetBoolean());
        Assert.Equal(renamed, listed.GetProperty("name").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await ownerClient.PostAsJsonAsync("/businesses", BusinessBody("Nuevo", id))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ownerClient.PutAsJsonAsync($"/businesses/{business}", BusinessBody(businessName + " editado", id))).StatusCode);
        Assert.True(await Listed(businessName));
        var other = await Business(ownerClient, "Otro " + Guid.NewGuid().ToString("N")[..8]);
        Assert.Equal(HttpStatusCode.BadRequest, (await ownerClient.PutAsJsonAsync($"/businesses/{other}", BusinessBody("Cambio", id))).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await adminClient.PutAsJsonAsync($"/admin/categories/{id}", new { name = renamed, isActive = true })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await ownerClient.PostAsJsonAsync("/businesses", BusinessBody("Nuevo", id))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await adminClient.PutAsJsonAsync("/admin/categories/999", new { name = "Nada", isActive = true })).StatusCode);

        var audit = (await adminClient.GetFromJsonAsync<JsonElement>("/admin/audit")).GetProperty("items").EnumerateArray()
            .Where(x => x.GetProperty("targetType").GetString() == "category" && x.GetProperty("targetId").GetString() == id.ToString())
            .Select(x => x.GetProperty("action").GetString()!).ToArray();
        Assert.Equal(["update", "update", "create"], audit);
    }

    [Fact]
    public async Task Five_failed_logins_lock_the_account_and_success_resets_the_counter()
    {
        var (client, email) = await Account();
        client.Dispose();
        for (var attempt = 0; attempt < 4; attempt++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await Login(email, "Wrong-password-0000")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(email, Password)).StatusCode);
        for (var attempt = 0; attempt < 4; attempt++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await Login(email, "Wrong-password-0000")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Login(email, Password)).StatusCode);

        for (var attempt = 0; attempt < 5; attempt++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await Login(email, "Wrong-password-0000")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(email, Password)).StatusCode);
        Assert.True(await Scalar<bool>("SELECT locked_until > now() + interval '14 minutes' FROM user_accounts WHERE email = $1", email));

        await Sql("UPDATE user_accounts SET locked_until = now() - interval '1 second' WHERE email = $1", email);
        Assert.Equal(HttpStatusCode.OK, (await Login(email, Password)).StatusCode);
        Assert.Equal(0, await Scalar<int>("SELECT failed_login_count FROM user_accounts WHERE email = $1", email));
    }

    private async Task<HttpResponseMessage> Login(string email, string password) =>
        await fixture.Client().PostAsJsonAsync("/auth/login", new { email, password });
    private async Task<bool> Listed(string name) =>
        (await fixture.Client().GetFromJsonAsync<JsonElement>($"/public/businesses?q={Uri.EscapeDataString(name)}")).GetProperty("items").GetArrayLength() > 0;
    private async Task<bool> ProductListed(string name) =>
        (await fixture.Client().GetFromJsonAsync<JsonElement>($"/public/products?q={Uri.EscapeDataString(name)}")).GetProperty("items").GetArrayLength() > 0;
    private static object Order(Guid product) => new
    { requestId = Guid.NewGuid(), accessKey = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32)), customerName = "Ana", lines = new[] { new { productId = product, quantity = 1, expectedVersion = 1 } } };
    private static object BusinessBody(string name, short categoryId = 2) => new
    { name, description = "", address = "Córdoba", categoryId, whatsapp = "+5493515551234", latitude = -31.4167, longitude = -64.1833, status = "published" };
    private static async Task<Guid> Business(HttpClient owner, string name, short categoryId = 2)
    {
        var response = await owner.PostAsJsonAsync("/businesses", BusinessBody(name, categoryId));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
    private async Task<(HttpClient Client, string Email)> Account(bool administrator = false)
    {
        var client = fixture.Client();
        var email = $"moderation-{Guid.NewGuid():N}@example.test";
        (await client.PostAsJsonAsync("/auth/register", new { email, password = Password, displayName = "Moderación" })).EnsureSuccessStatusCode();
        if (administrator) await Sql("INSERT INTO user_account_roles (user_id, role_id) SELECT id, 2 FROM user_accounts WHERE email = $1", email);
        var login = await client.PostAsJsonAsync("/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Add("Cookie", Assert.Single(login.Headers.GetValues("Set-Cookie")).Split(';')[0]);
        return (client, email);
    }
    private async Task Sql(string sql, object value)
    {
        await using var db = new NpgsqlConnection(fixture.ConnectionString);
        await db.OpenAsync();
        await using var command = new NpgsqlCommand(sql, db);
        command.Parameters.AddWithValue(value);
        await command.ExecuteNonQueryAsync();
    }
    private async Task<T> Scalar<T>(string sql, object value)
    {
        await using var db = new NpgsqlConnection(fixture.ConnectionString);
        await db.OpenAsync();
        await using var command = new NpgsqlCommand(sql, db);
        command.Parameters.AddWithValue(value);
        return (T)(await command.ExecuteScalarAsync())!;
    }
}
