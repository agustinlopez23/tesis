using Marketplace.BLL;
using Marketplace.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Marketplace.Api.Tests;

public sealed class RankingSnapshotStoreTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-04T12:00:00Z");
    private static readonly Guid Business = Guid.NewGuid();
    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => Now; }
    private static RankingOpportunity Opportunity(string key, long sequence, ExposureBalance? previous = null)
    {
        var input = new MagazineRankingInput(new(key, Now, 4, 17, sequence),
            [new(Business, 1, 1, null)], new Dictionary<Guid, Guid> { [Business] = Guid.NewGuid() });
        return RankingOpportunityBuilder.Build(input, RankingVariant.EquityNewcomer, new(),
            new Dictionary<Guid, ExposureBalance> { [Business] = previous ?? new(0, 0) }, null);
    }

    [Fact]
    public async Task Same_request_concurrently_builds_once_and_different_scope_conflicts()
    {
        var key = Guid.NewGuid().ToString();
        var request = Guid.NewGuid();
        var builds = 0;
        async Task<SnapshotResult> Create()
        {
            using var scope = fixture.Factory.Services.CreateScope();
            var store = new RankingSnapshotStore(scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>(), new Clock(), new RankingReadLimits());
            return await store.CreateAsync(request, key, (seq, _) =>
            {
                Interlocked.Increment(ref builds);
                return Task.FromResult(Opportunity(key, seq));
            }, default);
        }
        var results = await Task.WhenAll(Create(), Create(), Create());
        Assert.Equal(1, builds);
        Assert.Single(results, x => x.Status == SnapshotStatus.Created);
        Assert.Equal(2, results.Count(x => x.Status == SnapshotStatus.Existing));
        Assert.Single(results.Select(x => x.Snapshot!.Id).Distinct());
        using var read = fixture.Factory.Services.CreateScope();
        var db = read.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        var repository = new RankingSnapshotStore(db, new Clock(), new RankingReadLimits());
        var conflict = await repository.CreateAsync(request, key + "-other", (_, _) => throw new Exception("must not build"), default);
        Assert.Equal(SnapshotStatus.Conflict, conflict.Status);
        var snapshot = await repository.FindAsync(results[0].Snapshot!.Id, default);
        Assert.Equal(Now.AddMinutes(30), snapshot!.ExpiresAt);
        Assert.Equal(new[] { Business }, snapshot.BusinessIds);
        Assert.Equal(new ExposureBalance(1, 1), (await repository.ReadBalancesAsync(key, Now, default))[Business]);
        Assert.Equal(1, await db.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM ranking_opportunities WHERE context_key = {key}").SingleAsync());
    }

    [Fact]
    public async Task Different_requests_serialize_context_sequence_and_observe_committed_balance()
    {
        var key = Guid.NewGuid().ToString();
        async Task<(long Sequence, double Previous)> Create()
        {
            using var scope = fixture.Factory.Services.CreateScope();
            var store = new RankingSnapshotStore(scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>(), new Clock(), new RankingReadLimits());
            (long, double) observed = default;
            await store.CreateAsync(Guid.NewGuid(), key, async (seq, ct) =>
            {
                var balance = (await store.ReadBalancesAsync(key, Now, ct)).GetValueOrDefault(Business, new(0, 0));
                observed = (seq, balance.Expected);
                return Opportunity(key, seq, balance);
            }, default);
            return observed;
        }
        var results = await Task.WhenAll(Create(), Create(), Create(), Create());
        Assert.Equal(new long[] { 1, 2, 3, 4 }, results.Select(x => x.Sequence).Order());
        Assert.All(results, x => Assert.Equal(x.Sequence - 1, x.Previous));
    }

    [Fact]
    public async Task Failure_after_snapshot_insert_rolls_back_every_write_and_sequence()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        var store = new RankingSnapshotStore(db, new Clock(), new RankingReadLimits());
        var key = Guid.NewGuid().ToString();
        var request = Guid.NewGuid();
        await Assert.ThrowsAsync<PostgresException>(() => store.CreateAsync(request, key, (seq, _) =>
        {
            var valid = Opportunity(key, seq);
            return Task.FromResult(valid with { Candidates = [valid.Candidates[0] with { Expected = 2 }] });
        }, default));
        Assert.Equal(0, await db.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM ranking_snapshots WHERE request_id = {request}").SingleAsync());
        Assert.Empty(await store.ReadBalancesAsync(key, Now, default));
        var retry = await store.CreateAsync(request, key, (seq, _) =>
        {
            Assert.Equal(1, seq);
            return Task.FromResult(Opportunity(key, seq));
        }, default);
        Assert.Equal(SnapshotStatus.Created, retry.Status);
    }

    [Fact]
    public async Task Invalid_input_and_cancellation_never_publish_a_partial_snapshot()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        var store = new RankingSnapshotStore(db, new Clock(), new RankingReadLimits());
        var key = Guid.NewGuid().ToString();
        var request = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.CreateAsync(request, key, (seq, _) =>
        {
            cancellation.Cancel();
            return Task.FromResult(Opportunity(key, seq));
        }, cancellation.Token));
        Assert.Empty(await store.ReadBalancesAsync(key, Now, default));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => store.CreateAsync(Guid.Empty, key, (_, _) => throw new Exception(), default));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => store.CreateAsync(request, "", (_, _) => throw new Exception(), default));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => store.CreateAsync(request, key,
            (seq, _) => Task.FromResult(Opportunity("wrong-context", seq)), default));
    }
}
