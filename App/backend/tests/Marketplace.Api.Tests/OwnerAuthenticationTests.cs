using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Marketplace.Api.Tests;

public sealed class OwnerAuthenticationTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    [Fact]
    public async Task Registration_assigns_owner_and_normalizes_email_without_exposing_credentials()
    {
        using var client = fixture.Client();
        var email = $"Owner-{Guid.NewGuid():N}@EXAMPLE.test";
        var password = $"Local-test-{Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync("/auth/register", new
        { email = " " + email + " ", displayName = " Responsable ", password, role = "administrator" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(email.ToLowerInvariant(), body.GetProperty("email").GetString());
        Assert.Equal("Responsable", body.GetProperty("displayName").GetString());
        Assert.Equal("business_owner", Assert.Single(body.GetProperty("roles").EnumerateArray()).GetString());
        Assert.False(body.TryGetProperty("passwordHash", out _));
        Assert.DoesNotContain(password, await response.Content.ReadAsStringAsync());
        var duplicate = await client.PostAsJsonAsync("/auth/register", new { email, displayName = "Otro", password });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Login_cookie_is_private_and_logout_revokes_replay()
    {
        using var client = fixture.Client();
        var email = $"owner-{Guid.NewGuid():N}@example.test";
        var password = $"Local-test-{Guid.NewGuid():N}";
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/auth/register", new { email, password, displayName = "Dueño" })).StatusCode);
        var login = await client.PostAsJsonAsync("/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var cookieHeader = Assert.Single(login.Headers.GetValues("Set-Cookie"));
        Assert.Contains("httponly", cookieHeader.ToLowerInvariant());
        Assert.Contains("samesite=lax", cookieHeader.ToLowerInvariant());
        client.DefaultRequestHeaders.Add("Cookie", cookieHeader.Split(';')[0]);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/auth/session")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/auth/session")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/auth/logout", null)).StatusCode);
    }

    [Fact]
    public async Task Password_and_session_are_stored_as_hashes_and_expired_session_is_rejected()
    {
        using var client = fixture.Client();
        var email = $"owner-{Guid.NewGuid():N}@example.test";
        var password = $"Local-test-{Guid.NewGuid():N}";
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/auth/register", new { email, password, displayName = "Dueño" })).StatusCode);
        var login = await client.PostAsJsonAsync("/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie")).Split(';')[0];
        var rawToken = cookie.Split('=')[1];
        await using var db = new NpgsqlConnection(fixture.ConnectionString);
        await db.OpenAsync();
        await using (var read = new NpgsqlCommand("SELECT password_hash, token_hash FROM user_accounts u JOIN user_sessions s ON s.user_id = u.id WHERE u.email = $1", db))
        {
            read.Parameters.AddWithValue(email);
            await using var reader = await read.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.NotEqual(password, reader.GetString(0));
            Assert.NotEqual(rawToken, reader.GetString(1));
        }
        await using var expire = new NpgsqlCommand("UPDATE user_sessions SET expires_at = now() - interval '1 second' WHERE user_id = (SELECT id FROM user_accounts WHERE email = $1)", db);
        expire.Parameters.AddWithValue(email);
        await expire.ExecuteNonQueryAsync();
        client.DefaultRequestHeaders.Add("Cookie", cookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/auth/session")).StatusCode);
    }

    [Fact]
    public async Task Validation_unknown_credentials_and_missing_origin_are_rejected()
    {
        using var client = fixture.Client();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/auth/register", new { email = "invalid", displayName = "", password = "short" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/auth/login", new { email = "missing@example.test", password = "Wrong-password-123" })).StatusCode);
        using var untrusted = fixture.Client(false);
        Assert.Equal(HttpStatusCode.Forbidden, (await untrusted.PostAsJsonAsync("/auth/register", new { email = "blocked@example.test", displayName = "Blocked", password = "Local-test-password" })).StatusCode);
    }

    [Fact]
    public async Task Login_is_rate_limited_per_client_address()
    {
        await using var limitedFactory = fixture.Factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Auth:PermitLimit"] = "1" })));
        using var client = limitedFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:3000");
        client.DefaultRequestHeaders.Add("X-Requested-With", "marketplace");

        var payload = new { email = "missing@example.test", password = "Wrong-password-123" };
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/auth/login", payload)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/auth/login", payload)).StatusCode);
    }
}
