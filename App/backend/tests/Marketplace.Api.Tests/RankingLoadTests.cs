using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Marketplace.DAL;
using Marketplace.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Marketplace.Api.Tests;

public sealed class RankingLoadTests(OnboardingFixture fixture, ITestOutputHelper output) : IClassFixture<OnboardingFixture>
{
    [Fact]
    public async Task Thousand_candidates_with_concurrent_creates_and_pages_preserve_complete_snapshots()
    {
        const string route = "/public/ranking-snapshots";
        await using var factory = fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["Ranking:PreviewEnabled"] = "true" })));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:3000");
        client.DefaultRequestHeaders.Add("X-Requested-With", "marketplace");
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        var owner = new UserAccount { Email = $"load-{Guid.NewGuid():N}@example.test", DisplayName = "Carga", PasswordHash = "unused" };
        db.UserAccounts.Add(owner);
        var businesses = Enumerable.Range(0, 1000).Select(i => new BusinessProfile {
            OwnerId = owner.Id, CategoryId = 1, Name = $"Carga {i}", Description = "", Address = "Córdoba",
            WhatsApp = "+5493515551234", Status = "published", Location = new(-31.416281, -64.184022)
        }).ToArray();
        db.Businesses.AddRange(businesses);
        await db.SaveChangesAsync();
        var allocatedBefore = GC.GetTotalAllocatedBytes();
        var elapsed = Stopwatch.StartNew();
        var firstId = Guid.NewGuid();
        object Body(Guid id) => new { requestId = id, zoneId = "centro", categoryId = 1, radiusKm = 4 };
        using var first = await client.PostAsJsonAsync(route, Body(firstId));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var page = await first.Content.ReadFromJsonAsync<JsonElement>();
        var cursor = page.GetProperty("nextCursor").GetString()!;
        var pageUrl = route + "/page?cursor=" + Uri.EscapeDataString(cursor);
        var postTimes = new List<double>();
        var getTimes = new List<double>();
        async Task<double> Timed(bool post)
        {
            var timer = Stopwatch.StartNew();
            using var response = post ? await client.PostAsJsonAsync(route, Body(Guid.NewGuid())) : await client.GetAsync(pageUrl);
            Assert.Equal(post ? HttpStatusCode.Created : HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.CacheControl?.NoStore);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(20, result.GetProperty("items").GetArrayLength());
            return timer.Elapsed.TotalMilliseconds;
        }
        for (var wave = 0; wave < 4; wave++)
        {
            // Real SQL and HTTP at the configured 4/16 parallelism, with no artificial delay.
            var creates = Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Timed(true)));
            var reads = Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Timed(false)));
            await Task.WhenAll(creates, reads);
            postTimes.AddRange(await creates);
            getTimes.AddRange(await reads);
        }
        // The other 16 snapshots are intentionally abandoned after their first page.
        var seen = new List<Guid>();
        var pages = 0;
        while (true)
        {
            seen.AddRange(page.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetGuid()));
            pages++;
            if (!page.GetProperty("hasMore").GetBoolean()) break;
            using var response = await client.GetAsync(route + "/page?cursor=" + Uri.EscapeDataString(page.GetProperty("nextCursor").GetString()!));
            response.EnsureSuccessStatusCode();
            page = await response.Content.ReadFromJsonAsync<JsonElement>();
        }
        Assert.Equal(50, pages);
        Assert.Equal(businesses.Select(x => x.Id).Order(), seen.Order());
        using var replay = await client.PostAsJsonAsync(route, Body(firstId));
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        const string key = "magazine/centro/category/1/radius/4/eligibility/v1";
        Assert.Equal(17, await db.Database.SqlQuery<long>($"SELECT sequence AS \"Value\" FROM ranking_contexts WHERE context_key = {key}").SingleAsync());
        Assert.Equal(17, await db.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM ranking_snapshots WHERE context_key = {key}").SingleAsync());
        Assert.Equal(17, await db.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM ranking_opportunities WHERE context_key = {key}").SingleAsync());
        Assert.Equal(17000, await db.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM ranking_contributions c JOIN ranking_opportunities o ON o.id = c.opportunity_id WHERE o.context_key = {key}").SingleAsync());
        var allocated = GC.GetTotalAllocatedBytes() - allocatedBefore;
        double Percentile(List<double> values, double fraction) => values.Order().ElementAt((int)Math.Ceiling(values.Count * fraction) - 1);
        output.WriteLine(JsonSerializer.Serialize(new {
            candidates = 1000, snapshots = 17, abandoned = 16, parallelPost = 4, parallelGet = 16,
            measuredPost = postTimes.Count, measuredGet = getTimes.Count, pages,
            postP50Ms = Percentile(postTimes, .50), postP95Ms = Percentile(postTimes, .95),
            getP50Ms = Percentile(getTimes, .50), getP95Ms = Percentile(getTimes, .95),
            elapsedMs = elapsed.Elapsed.TotalMilliseconds, allocatedBytesProcessWide = allocated
        }));
    }
}
