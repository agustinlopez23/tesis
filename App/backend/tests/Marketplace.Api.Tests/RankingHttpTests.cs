using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Marketplace.Api;
using Marketplace.BLL;
using Marketplace.DAL;
using Marketplace.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.Api.Tests;

public sealed partial class RankingHttpTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    private const string Route = "/public/ranking-snapshots";

    [Fact]
    public async Task Http_pages_retries_and_current_data_preserve_one_opportunity()
    {
        await using var factory = Preview();
        using var client = Client(factory);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        var owner = new UserAccount { Email = $"http-{Guid.NewGuid():N}@example.test", DisplayName = "Dueño", PasswordHash = "unused" };
        db.UserAccounts.Add(owner);
        var businesses = Enumerable.Range(0, 45).Select(i => new BusinessProfile {
            OwnerId = owner.Id, CategoryId = 1, Name = $"HTTP {i}", Description = "", Address = "Córdoba",
            WhatsApp = "+5493515551234", Status = "published", Location = new(-31.416281, -64.184022)
        }).ToArray();
        db.Businesses.AddRange(businesses);
        await db.SaveChangesAsync();
        var request = Body();
        var created = await client.PostAsJsonAsync(Route, request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        AssertNoStore(created);
        var first = await created.Content.ReadFromJsonAsync<JsonElement>();
        var firstCursor = first.GetProperty("cursor").GetString();
        var reread = await client.GetAsync(Route + "/page?cursor=" + Uri.EscapeDataString(firstCursor!));
        Assert.Equal(Ids(first), Ids(await reread.Content.ReadFromJsonAsync<JsonElement>()));
        var measuredId = Ids(first)[0];
        var visitId = Guid.NewGuid();
        var token = first.GetProperty("measurement").GetProperty(measuredId.ToString()).GetString();
        foreach (var (kind, surface) in new[] { ("impression", "directory"), ("profile_view", "profile"), ("contact", "profile") })
        {
            var activity = new { token, visitId, kind, surface };
            Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/public/events", activity)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/public/events", activity)).StatusCode);
        }
        var measured = await db.BusinessActivity.Where(x => x.VisitId == visitId).ToArrayAsync();
        Assert.Equal(3, measured.Length);
        Assert.All(measured, item => {
            Assert.Equal(first.GetProperty("rankingVersion").GetString(), item.RankingVersion);
            Assert.Equal(1, item.Position);
            Assert.Equal("directory", item.Source);
        });
        var retry = await client.PostAsJsonAsync(Route, request);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        var replay = await retry.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(first.GetProperty("expiresAt").GetString(), replay.GetProperty("expiresAt").GetString());
        Assert.Equal(Ids(first), Ids(replay));
        Assert.Equal(first.GetProperty("rankingVersion").GetString(), replay.GetProperty("rankingVersion").GetString());
        var second = await ReadPage(client, first);
        var currentSecond = await client.GetAsync(Route + "/page?cursor=" + Uri.EscapeDataString(second.GetProperty("cursor").GetString()!));
        Assert.Equal(Ids(second), Ids(await currentSecond.Content.ReadFromJsonAsync<JsonElement>()));
        var third = await ReadPage(client, second);
        Assert.Equal(new[] { 20, 20, 5 }, new[] { Ids(first).Length, Ids(second).Length, Ids(third).Length });
        Assert.False(third.GetProperty("hasMore").GetBoolean());
        Assert.Equal(JsonValueKind.Null, third.GetProperty("nextCursor").ValueKind);
        Assert.Equal(45, Ids(first).Concat(Ids(second)).Concat(Ids(third)).Distinct().Count());
        var removed = businesses.Single(x => x.Id == Ids(second)[0]);
        removed.Status = "draft";
        businesses.Single(x => x.Id == Ids(second)[1]).Name = "Actualizado";
        await db.SaveChangesAsync();
        var current = await ReadPage(client, first);
        Assert.Equal(Ids(second).Skip(1), Ids(current));
        Assert.Equal("Actualizado", current.GetProperty("items")[0].GetProperty("name").GetString());
        var measurement = current.GetProperty("measurement").GetProperty(Ids(current)[0].ToString()).GetString();
        var context = scope.ServiceProvider.GetRequiredService<ActivityTokens>().Read(measurement)!;
        Assert.Equal(22, context.Position);
        Assert.Equal(first.GetProperty("rankingVersion").GetString(), context.RankingVersion);
        Assert.Equal(1, await db.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM ranking_snapshots WHERE request_id = {request.RequestId}").SingleAsync());
        var snapshotId = scope.ServiceProvider.GetRequiredService<RankingCursorTokens>().Read(first.GetProperty("nextCursor").GetString()).Cursor!.SnapshotId;
        Assert.Equal(45, await db.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM ranking_contributions WHERE opportunity_id = {snapshotId}").SingleAsync());
        Assert.DoesNotContain(owner.Id.ToString(), first.GetRawText());
        Assert.DoesNotContain("components", first.GetRawText(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(Route, request with { ZoneId = "alberdi" })).StatusCode);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE ranking_snapshots SET expires_at = now() WHERE id = {snapshotId}");
        Assert.Equal(HttpStatusCode.Gone, (await client.PostAsJsonAsync(Route, request)).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync(PageUrl(first))).StatusCode);
    }

    [Theory]
    [InlineData("Testing", false)]
    [InlineData("Production", true)]
    public async Task Creation_is_closed_by_default_and_in_production(string environment, bool enabled)
    {
        await using var factory = Preview(enabled, environment);
        using var client = Client(factory);
        var response = await client.PostAsJsonAsync(Route, Body());
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertNoStore(response);
    }

    private WebApplicationFactory<Program> Preview(bool enabled = true, string environment = "Testing") =>
        fixture.Factory.WithWebHostBuilder(builder => builder.UseEnvironment(environment)
            .ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> {
                ["Ranking:PreviewEnabled"] = enabled.ToString()
            })));

    private static HttpClient Client(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:3000");
        client.DefaultRequestHeaders.Add("X-Requested-With", "marketplace");
        return client;
    }

    private sealed record SnapshotRequest(Guid RequestId, string ZoneId, short CategoryId, double RadiusKm);
    private static SnapshotRequest Body() => new(Guid.NewGuid(), "centro", 1, 4);
    private static Guid[] Ids(JsonElement page) => page.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ToArray();
    private static string PageUrl(JsonElement page) => Route + "/page?cursor=" + Uri.EscapeDataString(page.GetProperty("nextCursor").GetString()!);
    private static void AssertNoStore(HttpResponseMessage response) => Assert.True(response.Headers.CacheControl?.NoStore);
    private static async Task<JsonElement> ReadPage(HttpClient client, JsonElement page)
    {
        var response = await client.GetAsync(PageUrl(page));
        response.EnsureSuccessStatusCode();
        AssertNoStore(response);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
