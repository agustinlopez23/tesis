using System.Net;
using System.Net.Http.Json;
using Marketplace.BLL;
using Marketplace.DAL;
using Marketplace.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.Api.Tests;

public sealed partial class RankingHttpTests
{
    [Fact]
    public async Task Historical_capacity_includes_ineligible_businesses_and_rolls_back_before_retry()
    {
        await using var seedFactory = Preview();
        using var seedClient = Client(seedFactory);
        using var scope = seedFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        db.BusinessCategories.Add(new BusinessCategory { Id = 5, Name = "Historial", Slug = "historial" });
        var owner = new UserAccount { Email = $"history-{Guid.NewGuid():N}@example.test", DisplayName = "Dueño", PasswordHash = "unused" };
        db.UserAccounts.Add(owner);
        var businesses = Enumerable.Range(0, 3).Select(i => new BusinessProfile {
            OwnerId = owner.Id, CategoryId = 5, Name = $"Historial {i}", Description = "", Address = "Córdoba",
            WhatsApp = "+5493515551234", Status = "published", Location = new(-31.416281, -64.184022)
        }).ToArray();
        db.Businesses.AddRange(businesses);
        await db.SaveChangesAsync();
        var body = Body() with { CategoryId = 5 };
        Assert.Equal(HttpStatusCode.Created, (await seedClient.PostAsJsonAsync(Route, body)).StatusCode);
        businesses[2].Status = "draft";
        await db.SaveChangesAsync();
        await using var limitedFactory = Preview().WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["Ranking:MaxBalanceRows"] = "2" })));
        using var client = Client(limitedFactory);
        // Idempotent replay does not rebuild history even when it now exceeds capacity.
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync(Route, body)).StatusCode);
        body = body with { RequestId = Guid.NewGuid() };
        var rejected = await client.PostAsJsonAsync(Route, body);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, rejected.StatusCode);
        AssertNoStore(rejected);
        var key = "magazine/centro/category/5/radius/4/eligibility/v1";
        Assert.Equal(0, await db.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM ranking_snapshots WHERE request_id = {body.RequestId}").SingleAsync());
        Assert.Equal(1, await db.Database.SqlQuery<long>($"SELECT sequence AS \"Value\" FROM ranking_contexts WHERE context_key = {key}").SingleAsync());
        Assert.Equal(1, await db.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM ranking_opportunities WHERE context_key = {key}").SingleAsync());
        var balances = await scope.ServiceProvider.GetRequiredService<IRankingSnapshotStore>().ReadBalancesAsync(key, DateTimeOffset.UtcNow, default);
        Assert.Equal(3, balances.Count);
        // Simulate historical retention reaching the exact configured boundary.
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM ranking_contributions WHERE business_id = {businesses[2].Id}");
        var retry = await client.PostAsJsonAsync(Route, body);
        Assert.Equal(HttpStatusCode.Created, retry.StatusCode);
        Assert.Equal(businesses.Take(2).Select(x => x.Id).Order(), Ids(await retry.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).Order());
        Assert.Equal(2, await db.Database.SqlQuery<long>($"SELECT sequence AS \"Value\" FROM ranking_contexts WHERE context_key = {key}").SingleAsync());
    }

    [Fact]
    public async Task Candidate_capacity_rejects_whole_list_and_retry_at_boundary_preserves_every_candidate()
    {
        await using var factory = Preview().WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?> { ["Ranking:MaxCandidates"] = "2" })));
        using var client = Client(factory);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        db.BusinessCategories.Add(new BusinessCategory { Id = 4, Name = "Capacidad", Slug = "capacidad" });
        var owner = new UserAccount { Email = $"capacity-{Guid.NewGuid():N}@example.test", DisplayName = "Dueño", PasswordHash = "unused" };
        db.UserAccounts.Add(owner);
        var businesses = Enumerable.Range(0, 3).Select(i => new BusinessProfile {
            OwnerId = owner.Id, CategoryId = 4, Name = $"Capacidad {i}", Description = "", Address = "Córdoba",
            WhatsApp = "+5493515551234", Status = "published", Location = new(-31.416281, -64.184022)
        }).ToArray();
        db.Businesses.AddRange(businesses);
        await db.SaveChangesAsync();
        var body = Body() with { CategoryId = 4 };
        var response = await client.PostAsJsonAsync(Route, body);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        AssertNoStore(response);
        Assert.Equal(0, await db.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM ranking_snapshots WHERE request_id = {body.RequestId}").SingleAsync());
        businesses[2].Status = "draft";
        await db.SaveChangesAsync();
        response = await client.PostAsJsonAsync(Route, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(businesses.Take(2).Select(x => x.Id).Order(), Ids(page).Order());
        var key = "magazine/centro/category/4/radius/4/eligibility/v1";
        Assert.Equal(1, await db.Database.SqlQuery<long>($"SELECT sequence AS \"Value\" FROM ranking_contexts WHERE context_key = {key}").SingleAsync());
        // An existing request bypasses candidate rebuilding, even if the current pool grows.
        businesses[2].Status = "published";
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync(Route, body)).StatusCode);
    }
}
