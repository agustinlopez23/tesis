using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Marketplace.Api.Tests;

public class LivenessTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public LivenessTests(WebApplicationFactory<Program> factory) => this.factory = factory;

    [Fact]
    public async Task GetLiveness_ReturnsExactContract_WithoutDatabaseConfiguration()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Object, body.RootElement.ValueKind);
        Assert.Single(body.RootElement.EnumerateObject());
        Assert.Equal("ok", body.RootElement.GetProperty("status").GetString());
    }
}
