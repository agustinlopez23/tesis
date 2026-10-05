using Marketplace.BLL;
using Marketplace.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.Api.Tests;

public sealed class RankingRetentionTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.Parse("2026-10-04T12:00:00Z");
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public async Task Expiry_does_not_renew_and_purge_preserves_sequence_without_old_balance()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        var clock = new Clock();
        var store = new RankingSnapshotStore(db, clock, new RankingReadLimits());
        var key = "retention";
        var request = Guid.NewGuid();
        var business = Guid.NewGuid();
        var at = clock.Now;
        Task<RankingOpportunity> Build(long seq, CancellationToken _) => Task.FromResult(RankingOpportunityBuilder.Build(
            new(new(key, clock.Now, 4, 17, seq), [new(business, 1, 1, null)],
                new Dictionary<Guid, Guid> { [business] = Guid.NewGuid() }), RankingVariant.EquityOnly,
            new(), new Dictionary<Guid, ExposureBalance>(), null));
        var result = await store.CreateAsync(request, key, Build, default);
        clock.Now = at.AddMinutes(29);
        Assert.Equal(at.AddMinutes(30), (await store.CreateAsync(request, key, Build, default)).Snapshot!.ExpiresAt);
        clock.Now = at.AddMinutes(30);
        Assert.Null(await store.FindAsync(result.Snapshot!.Id, default));
        Assert.Equal(SnapshotStatus.Expired, (await store.CreateAsync(request, key, Build, default)).Status);
        Assert.Single(await store.ReadBalancesAsync(key, clock.Now, default));
        Assert.Empty(await store.ReadBalancesAsync("another-context", clock.Now, default));
        Assert.Empty(await store.ReadBalancesAsync(key, at.AddTicks(-1), default));
        clock.Now = at.AddDays(30);
        Assert.Empty(await store.ReadBalancesAsync(key, clock.Now, default));
        await store.PurgeAsync(default);
        Assert.Equal(0, await db.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM ranking_opportunities WHERE context_key = {key}").SingleAsync());
        Assert.Equal(0, await db.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM ranking_contributions WHERE business_id = {business}").SingleAsync());
        Assert.Equal(0, await db.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM ranking_snapshots WHERE request_id = {request}").SingleAsync());
        // Once the request mapping has been purged, deduplication is no longer promised.
        Assert.Equal(SnapshotStatus.Created, (await store.CreateAsync(Guid.NewGuid(), key, Build, default)).Status);
        Assert.Equal(2, await db.Database.SqlQuery<long>($"SELECT sequence AS \"Value\" FROM ranking_contexts WHERE context_key = {key}").SingleAsync());
    }
}
