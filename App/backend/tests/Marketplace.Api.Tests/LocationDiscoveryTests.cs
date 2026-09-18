using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Marketplace.DAL;
using Marketplace.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.Api.Tests;

public sealed class LocationDiscoveryTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    [Fact]
    public async Task Radius_filters_before_pagination_and_orders_by_distance_with_private_data_hidden()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        var owner = new UserAccount { Email = "geo@example.test", DisplayName = "Cuenta privada", PasswordHash = "unused" };
        var suspended = new UserAccount { Email = "suspended@example.test", DisplayName = "Suspendido", PasswordHash = "unused", IsSuspended = true };
        db.UserAccounts.AddRange(owner, suspended);
        var center = Business(owner.Id, "Zeta centro", -31.4167);
        var nearby = Business(owner.Id, "Alfa a un kilómetro y medio", -31.43);
        db.Businesses.AddRange(center, nearby, Business(owner.Id, "Borrador", -31.4167, "draft"),
            Business(suspended.Id, "Suspendido", -31.4167));
        for (var n = 0; n < 21; n++) db.Businesses.Add(Business(owner.Id, "Afuera " + n, -31.6));
        await db.SaveChangesAsync();
        using var visitor = fixture.Client(false);
        const string area = "lat=-31.4167&lon=-64.1833&radiusKm=2";
        var response = await visitor.GetAsync("/public/businesses?" + area);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = page.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal(2, items.Length);
        Assert.Equal(center.Id, items[0].GetProperty("id").GetGuid());
        Assert.InRange(items[0].GetProperty("distanceKm").GetDouble(), 0, 0.001);
        Assert.InRange(items[1].GetProperty("distanceKm").GetDouble(), 1.4, 1.6);
        Assert.False(items[0].TryGetProperty("ownerId", out _));
        Assert.False(items[0].TryGetProperty("latitude", out _));
        Assert.False(page.GetProperty("hasMore").GetBoolean());
        var smaller = await visitor.GetFromJsonAsync<JsonElement>("/public/businesses?lat=-31.4167&lon=-64.1833&radiusKm=1");
        Assert.Single(smaller.GetProperty("items").EnumerateArray());
        var filtered = await visitor.GetFromJsonAsync<JsonElement>("/public/businesses?" + area + "&q=ZETA&category=Almacenes");
        Assert.Single(filtered.GetProperty("items").EnumerateArray());
        Assert.Empty((await visitor.GetFromJsonAsync<JsonElement>("/public/businesses?" + area + "&category=Indumentaria")).GetProperty("items").EnumerateArray());
        var general = await visitor.GetFromJsonAsync<JsonElement>("/public/businesses?q=Zeta");
        Assert.False(Assert.Single(general.GetProperty("items").EnumerateArray()).TryGetProperty("distanceKm", out _));

        for (var n = 0; n < 20; n++) db.Businesses.Add(Business(owner.Id, "Zeta centro", -31.4167));
        await db.SaveChangesAsync();
        var first = await visitor.GetFromJsonAsync<JsonElement>("/public/businesses?" + area);
        var second = await visitor.GetFromJsonAsync<JsonElement>("/public/businesses?" + area + "&page=2");
        Assert.Equal(20, first.GetProperty("items").GetArrayLength());
        Assert.True(first.GetProperty("hasMore").GetBoolean());
        Assert.Equal(2, second.GetProperty("items").GetArrayLength());
        Assert.False(second.GetProperty("hasMore").GetBoolean());
        var all = first.GetProperty("items").EnumerateArray().Concat(second.GetProperty("items").EnumerateArray()).ToArray();
        Assert.Equal(22, all.Select(x => x.GetProperty("id").GetGuid()).Distinct().Count());
        Assert.Equal(nearby.Id, all[^1].GetProperty("id").GetGuid());
        var tiedIds = all.Take(21).Select(x => x.GetProperty("id").GetGuid()).ToArray();
        Assert.Equal(tiedIds.Order().ToArray(), tiedIds);
        var indexCount = await db.Database.SqlQuery<int>($"""
            SELECT count(*)::int AS "Value" FROM pg_indexes
            WHERE tablename = 'businesses' AND indexdef LIKE '%USING gist (location)%'
            """).SingleAsync();
        Assert.Equal(1, indexCount);
    }

    [Theory]
    [InlineData("lat=-31.4")]
    [InlineData("radiusKm=2")]
    [InlineData("lat=-31.4&lon=-64.18")]
    [InlineData("lat=91&lon=0&radiusKm=2")]
    [InlineData("lat=0&lon=181&radiusKm=2")]
    [InlineData("lat=NaN&lon=0&radiusKm=2")]
    [InlineData("lat=0&lon=Infinity&radiusKm=2")]
    [InlineData("lat=0&lon=0&radiusKm=0")]
    [InlineData("lat=0&lon=0&radiusKm=51")]
    [InlineData("lat=0&lon=0&radiusKm=NaN")]
    public async Task Invalid_or_partial_area_is_rejected(string query)
    {
        using var visitor = fixture.Client(false);
        Assert.Equal(HttpStatusCode.BadRequest, (await visitor.GetAsync("/public/businesses?" + query)).StatusCode);
    }

    private static BusinessProfile Business(Guid owner, string name, double latitude, string status = "published") => new()
    {
        OwnerId = owner, CategoryId = 1, Name = name, Description = "Oferta local", Address = "Córdoba",
        WhatsApp = "+5493515550101", Location = new GeoLocation(latitude, -64.1833), Status = status,
    };
}
