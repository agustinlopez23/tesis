using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Marketplace.Api.Tests;

public sealed class BusinessManagementTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    [Fact]
    public async Task Owner_creates_and_reads_business_with_geography_location()
    {
        using var client = await OwnerClientAsync();
        var categories = await client.GetFromJsonAsync<JsonElement>("/business-categories");
        var categoryId = categories.EnumerateArray().First().GetProperty("id").GetInt16();

        var response = await client.PostAsJsonAsync("/businesses", new
        {
            name = "Almacén Córdoba",
            description = "Productos regionales",
            address = "Av. Colón 1234",
            categoryId,
            whatsapp = "+5493515551234",
            latitude = -31.4167,
            longitude = -64.1833,
            status = "draft"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();
        var fetched = await client.GetFromJsonAsync<JsonElement>($"/businesses/{id}");
        Assert.Equal("Almacén Córdoba", fetched.GetProperty("name").GetString());
        Assert.Equal(-31.4167, fetched.GetProperty("latitude").GetDouble(), 4);
        Assert.Equal(-64.1833, fetched.GetProperty("longitude").GetDouble(), 4);

        await using var db = new NpgsqlConnection(fixture.ConnectionString);
        await db.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT ST_SRID(location::geometry), ST_Y(location::geometry), ST_X(location::geometry) FROM businesses WHERE id = $1", db);
        command.Parameters.AddWithValue(id);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(4326, reader.GetInt32(0));
        Assert.Equal(-31.4167, reader.GetDouble(1), 4);
        Assert.Equal(-64.1833, reader.GetDouble(2), 4);
    }

    [Fact]
    public async Task Missing_session_and_other_owner_cannot_read_or_edit_business()
    {
        using var owner = await OwnerClientAsync();
        var categoryId = (await owner.GetFromJsonAsync<JsonElement>("/business-categories"))
            .EnumerateArray().First().GetProperty("id").GetInt16();
        var created = await owner.PostAsJsonAsync("/businesses", ValidBusiness(categoryId, "Privado"));
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var anonymous = fixture.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/businesses")).StatusCode);
        using var other = await OwnerClientAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/businesses/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await other.PutAsJsonAsync($"/businesses/{id}", ValidBusiness(categoryId, "Intento"))).StatusCode);
    }

    [Fact]
    public async Task Owner_edits_business_and_invalid_change_is_atomic()
    {
        using var client = await OwnerClientAsync();
        var categoryId = (await client.GetFromJsonAsync<JsonElement>("/business-categories"))
            .EnumerateArray().First().GetProperty("id").GetInt16();
        var created = await client.PostAsJsonAsync("/businesses", ValidBusiness(categoryId, "Original"));
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var updated = await client.PutAsJsonAsync($"/businesses/{id}", ValidBusiness(categoryId, "Actualizado"));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var invalid = await client.PutAsJsonAsync($"/businesses/{id}", new
        {
            name = "No debe persistir", description = "", address = "Córdoba", categoryId,
            whatsapp = "+5493515551234", latitude = 91, longitude = -64.18, status = "published"
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var fetched = await client.GetFromJsonAsync<JsonElement>($"/businesses/{id}");
        Assert.Equal("Actualizado", fetched.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Database_failure_is_controlled_without_breaking_liveness()
    {
        await using var brokenFactory = fixture.Factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Marketplace"] = "Host=127.0.0.1;Port=1;Database=missing;Username=missing;Password=missing;Timeout=1"
                })));
        using var client = brokenFactory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/business-categories")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
    }

    private async Task<HttpClient> OwnerClientAsync()
    {
        var client = fixture.Client();
        var email = $"owner-{Guid.NewGuid():N}@example.test";
        var password = $"Local-test-{Guid.NewGuid():N}";
        Assert.Equal(HttpStatusCode.Created,
            (await client.PostAsJsonAsync("/auth/register", new { email, password, displayName = "Responsable" })).StatusCode);
        var login = await client.PostAsJsonAsync("/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Add("Cookie", Assert.Single(login.Headers.GetValues("Set-Cookie")).Split(';')[0]);
        return client;
    }

    private static object ValidBusiness(short categoryId, string name) => new
    {
        name, description = "Descripción", address = "Córdoba", categoryId,
        whatsapp = "+5493515551234", latitude = -31.4167, longitude = -64.1833, status = "draft"
    };
}
