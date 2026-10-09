using Marketplace.Models;

namespace Marketplace.BLL.Tests;

public sealed class MagazineRankingCandidatesTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-04T12:00:00Z");
    private static EligibleRankingBusiness Business(int id = 1) =>
        new(new Guid(id, 0, 0, new byte[8]), Guid.NewGuid(), 2, Now.AddDays(-7), false);
    private static IReadOnlyList<RankedBusiness> Rank(MagazineRankingInput input) =>
        new BusinessRanker(new()).Rank(input.Candidates, input.Context, RankingVariant.EquityNewcomer,
            new Dictionary<Guid, ExposureBalance>());

    [Fact]
    public async Task All_candidates_reach_motor_with_real_history_distance_and_internal_owners()
    {
        var rows = Enumerable.Range(1, 45).Select(i => Business(i)).ToArray();
        rows[0] = rows[0] with { FirstPublishedAt = null, PublicationHistoryUnknown = true };
        rows[1] = rows[1] with { FirstPublishedAt = Now.AddDays(-14) };
        rows[2] = rows[2] with { FirstPublishedAt = Now };
        var repository = new Repository(rows);
        var input = await new MagazineRankingCandidates(repository, new Clock()).PrepareAsync("centro", 1, 17, 2, default);
        Assert.Equal(45, input.Candidates.Count);
        Assert.Equal(rows.Select(x => x.Id), input.Candidates.Select(x => x.Id));
        Assert.All(input.Candidates, x =>
        {
            Assert.Equal(1, x.Relevance);
            Assert.Equal(2, x.DistanceKm);
            Assert.Equal(0, x.Activity);
            Assert.Equal(rows.Single(r => r.Id == x.Id).OwnerId, input.OwnerIds[x.Id]);
        });
        Assert.Null(input.Candidates[0].PublishedAt);
        var ranked = Rank(input).ToDictionary(x => x.Id);
        Assert.Equal(45, ranked.Count);
        Assert.Equal(0, ranked[rows[0].Id].Newcomer);
        Assert.Equal(0, ranked[rows[1].Id].Newcomer);
        Assert.Equal(1, ranked[rows[2].Id].Newcomer);
        Assert.Equal(.5, ranked[rows[3].Id].Newcomer);
        Assert.Equal(Math.Exp(-1), ranked[rows[0].Id].Proximity, 12);
    }

    [Theory]
    [InlineData("centro", -31.416281, -64.184022)]
    [InlineData("nueva-cordoba", -31.423889, -64.186389)]
    [InlineData("alberdi", -31.4093, -64.19555)]
    public async Task Scope_uses_public_reference_and_canonical_context(string zone, double latitude, double longitude)
    {
        var repository = new Repository([]);
        var service = new MagazineRankingCandidates(repository, new Clock());
        using var cancellation = new CancellationTokenSource();
        var input = await service.PrepareAsync(zone, 2, 17, 3, cancellation.Token);
        Assert.Equal(new DiscoveryArea(latitude, longitude, 4), repository.Area);
        Assert.Equal(2, repository.Category);
        Assert.Equal(cancellation.Token, repository.Token);
        Assert.Equal(new RankingContext($"magazine/{zone}/category/2/radius/4/eligibility/v1", Now, 4, 17, 3), input.Context);
        Assert.Empty(input.Candidates);
        Assert.Empty(input.OwnerIds);
        var another = await service.PrepareAsync(zone, 3, 19, 4, default);
        Assert.NotEqual(input.Context.Key, another.Context.Key);
        var repeat = await service.PrepareAsync(zone, 2, 19, 4, default);
        Assert.Equal(input.Context.Key, repeat.Context.Key);
    }

    [Theory]
    [InlineData(null, 1, 0)]
    [InlineData("", 1, 0)]
    [InlineData("device", 1, 0)]
    [InlineData("Centro", 1, 0)]
    [InlineData("centro", 0, 0)]
    [InlineData("centro", -1, 0)]
    [InlineData("centro", 1, -1)]
    public async Task Invalid_scope_fails_before_retrieval(string? zone, short category, long sequence)
    {
        var repository = new Repository([]);
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            new MagazineRankingCandidates(repository, new Clock()).PrepareAsync(zone!, category, 17, sequence, default));
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task Corruption_aborts_whole_batch_without_silently_dropping_rows()
    {
        var valid = Business();
        var invalidRows = new[] { valid with { Id = Guid.Empty }, valid with { OwnerId = Guid.Empty },
            valid with { DistanceKm = double.NaN }, valid with { DistanceKm = double.PositiveInfinity },
            valid with { DistanceKm = -1 }, valid with { DistanceKm = 4.01 },
            valid with { FirstPublishedAt = null }, valid with { PublicationHistoryUnknown = true },
            valid with { FirstPublishedAt = Now.AddSeconds(1) } };
        foreach (var invalid in invalidRows)
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new MagazineRankingCandidates(new Repository([Business(2), invalid]), new Clock())
                    .PrepareAsync("centro", 1, 17, 0, default));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new MagazineRankingCandidates(new Repository([valid, valid]), new Clock())
                .PrepareAsync("centro", 1, 17, 0, default));
    }

    [Fact]
    public async Task Cancellation_before_and_during_read_and_repository_errors_propagate()
    {
        using var cancellation = new CancellationTokenSource();
        var repository = new Repository([Business()]) { OnRead = cancellation.Cancel };
        var service = new MagazineRankingCandidates(repository, new Clock());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.PrepareAsync("centro", 1, 17, 0, cancellation.Token));
        Assert.Equal(1, repository.Calls);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.PrepareAsync("centro", 1, 17, 0, cancellation.Token));
        Assert.Equal(1, repository.Calls);
        repository.OnRead = () => throw new InvalidOperationException("storage unavailable");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PrepareAsync("centro", 1, 17, 0, default));
    }

    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => Now; }
    private sealed class Repository(IReadOnlyList<EligibleRankingBusiness> rows) : IRankingCandidateRepository
    {
        public DiscoveryArea? Area { get; private set; }
        public short Category { get; private set; }
        public CancellationToken Token { get; private set; }
        public int Calls { get; private set; }
        public Action? OnRead { get; set; }
        public Task<IReadOnlyList<EligibleRankingBusiness>> GetEligibleAsync(short categoryId, DiscoveryArea area, CancellationToken cancellationToken)
        {
            Calls++;
            (Category, Area, Token) = (categoryId, area, cancellationToken);
            OnRead?.Invoke();
            return Task.FromResult(rows);
        }
    }
}
