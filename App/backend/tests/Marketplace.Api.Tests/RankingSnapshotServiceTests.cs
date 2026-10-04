using System.Text.Json;
using Marketplace.BLL;
using Marketplace.DAL;
using Marketplace.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.Api.Tests;

public sealed class RankingSnapshotServiceTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    [Fact]
    public async Task Real_candidates_are_saved_completely_and_trace_replays_without_visitor_data()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        var owner = new UserAccount { Email = $"snapshot-{Guid.NewGuid():N}@example.test", DisplayName = "Dueño", PasswordHash = "unused" };
        db.UserAccounts.Add(owner);
        var businesses = Enumerable.Range(0, 45).Select(i => new BusinessProfile
        {
            OwnerId = owner.Id, CategoryId = 1, Name = $"Negocio {i}", Description = "", Address = "Córdoba",
            WhatsApp = "+5493515551234", Status = "published", Location = new(-31.416281 + i * .0001, -64.184022)
        }).ToArray();
        db.Businesses.AddRange(businesses);
        await db.SaveChangesAsync();
        var service = scope.ServiceProvider.GetRequiredService<RankingSnapshotService>();
        var store = scope.ServiceProvider.GetRequiredService<IRankingSnapshotStore>();
        var request = Guid.NewGuid();
        var result = await service.CreateAsync(request, "centro", 1, RankingVariant.EquityNewcomer, new(), 17, null, default);
        var snapshot = result.Snapshot!;
        Assert.Equal(SnapshotStatus.Created, result.Status);
        Assert.Equal(45, snapshot.BusinessIds.Count);
        Assert.Equal(businesses.Select(x => x.Id).Order(), snapshot.BusinessIds.Order());
        var json = await db.Database.SqlQuery<string>($"SELECT payload::text AS \"Value\" FROM ranking_opportunities WHERE id = {snapshot.Id}").SingleAsync();
        Assert.DoesNotContain(owner.Id.ToString(), json);
        Assert.DoesNotContain(request.ToString(), json);
        Assert.DoesNotContain(owner.Email, json);
        var audit = JsonSerializer.Deserialize<RankingOpportunity>(json)!;
        Assert.Equal(45, audit.Candidates.Count);
        Assert.Equal(snapshot.BusinessIds, audit.Candidates.Select(x => x.Candidate.Id));
        var replay = new BusinessRanker(audit.Configuration).Rank(audit.Candidates.Select(x => x.Candidate),
            audit.Context, audit.Variant, audit.Candidates.ToDictionary(x => x.Candidate.Id, x => x.Previous));
        Assert.Equal(audit.Candidates.Select(x => x.Components), replay);
        Assert.All(audit.Candidates.Skip(20), x => { Assert.True(x.Expected > 0); Assert.Equal(0, x.Received); });
        var before = await store.ReadBalancesAsync(snapshot.ContextKey, DateTimeOffset.UtcNow, default);
        // A new service scope simulates another request/process reading the persisted list.
        using var another = fixture.Factory.Services.CreateScope();
        var retry = await another.ServiceProvider.GetRequiredService<RankingSnapshotService>()
            .CreateAsync(request, "centro", 1, RankingVariant.EquityOnly, new(), 29, null, default);
        Assert.Equal(SnapshotStatus.Existing, retry.Status);
        Assert.Equal(snapshot.Id, retry.Snapshot!.Id);
        Assert.Equal(snapshot.PolicyHash, retry.Snapshot.PolicyHash);
        var preview = await service.CreateAsync(Guid.NewGuid(), "centro", 1,
            RankingVariant.EquityNewcomer, new(), 17, owner.Id, default);
        var after = await store.ReadBalancesAsync(snapshot.ContextKey, DateTimeOffset.UtcNow, default);
        Assert.Equal(before.OrderBy(x => x.Key), after.OrderBy(x => x.Key));
        var previewJson = await db.Database.SqlQuery<string>($"SELECT payload::text AS \"Value\" FROM ranking_opportunities WHERE id = {preview.Snapshot!.Id}").SingleAsync();
        Assert.Equal("own-business-preview", JsonSerializer.Deserialize<RankingOpportunity>(previewJson)!.Reason);
        Assert.Equal(45, await db.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM ranking_contributions WHERE opportunity_id = {preview.Snapshot.Id}").SingleAsync());
    }
}
