using Marketplace.BLL;

namespace Marketplace.BLL.Tests;

public sealed class BusinessRankerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-01T12:00:00Z");
    private static readonly RankingContext Context = new("search/centro/cafe", Now, 5, 17, 1);
    private static RankingCandidate Candidate(int id, double relevance = .8, double? distance = 1) =>
        new(new Guid(id, 0, 0, new byte[8]), relevance, distance, Now.AddDays(-30));
    private static IReadOnlyList<RankedBusiness> Rank(IEnumerable<RankingCandidate> items,
        RankingVariant variant = RankingVariant.Equity, RankingContext? context = null,
        IReadOnlyDictionary<Guid, ExposureBalance>? history = null) =>
        new BusinessRanker(new()).Rank(items, context ?? Context, variant, history ?? new Dictionary<Guid, ExposureBalance>());

    [Fact]
    public void Eligibility_precedes_every_bonus()
    {
        var candidates = new[] { Candidate(1), Candidate(2) with { Published = false },
            Candidate(3) with { Suspended = true }, Candidate(4, .19), Candidate(5, 1, 5.01),
            Candidate(6) with { PublishedAt = Now.AddSeconds(1) }, Candidate(7, 1, null) };
        Assert.Equal(Candidate(1).Id, Assert.Single(Rank(candidates)).Id);
    }

    [Fact]
    public void Baselines_separate_relevance_from_distance_and_missing_location()
    {
        var items = new[] { Candidate(1, 1, 5), Candidate(2, .9, 0) };
        Assert.Equal(items[0].Id, Rank(items, RankingVariant.Relevance)[0].Id);
        Assert.Equal(items[1].Id, Rank(items, RankingVariant.Proximity)[0].Id);
        Assert.Equal(1, Rank(items, RankingVariant.Proximity, Context with { RadiusKm = null })[0].Merit);
        Assert.Equal(Math.Exp(-.5), Rank([Candidate(1)], RankingVariant.Proximity)[0].Proximity, 10);
    }

    [Fact]
    public void Underexposure_breaks_equal_merit_and_newcomer_bonus_decays()
    {
        var a = Candidate(1); var b = Candidate(2);
        var history = new Dictionary<Guid, ExposureBalance> { [a.Id] = new(10, 10), [b.Id] = new(10, 0) };
        var result = Rank([a, b], history: history);
        Assert.Equal(b.Id, result[0].Id);
        Assert.Equal(1, result[0].Deficit);
        Assert.Equal(0, result[1].Deficit);
        Assert.Equal(1, Rank([a with { PublishedAt = Now }])[0].Newcomer);
        Assert.Equal(.5, Rank([a with { PublishedAt = Now.AddDays(-7) }])[0].Newcomer);
        Assert.Equal(0, Rank([a with { PublishedAt = Now.AddDays(-14) }])[0].Newcomer);
    }

    [Fact]
    public void Unknown_publication_history_remains_eligible_without_newcomer_bonus()
    {
        var unknown = Candidate(1) with { PublishedAt = null };
        var result = Assert.Single(Rank([unknown], RankingVariant.EquityNewcomer));
        Assert.Equal(0, result.Newcomer);
        Assert.Equal(Rank([Candidate(1)], RankingVariant.EquityNewcomer)[0].Score, result.Score);
    }

    [Fact]
    public void Activity_is_an_explicit_bounded_ablation()
    {
        var a = Candidate(1);
        Assert.Equal(Rank([a])[0].Score, Rank([a with { Activity = 1 }])[0].Score);
        var without = Rank([a], RankingVariant.EquityActivity)[0].Score;
        Assert.Equal(.05, Rank([a with { Activity = 1 }], RankingVariant.EquityActivity)[0].Score - without, 10);
    }

    [Fact]
    public void Rotation_is_reproducible_independent_of_input_order_and_changes_across_queries()
    {
        var items = Enumerable.Range(1, 30).Select(i => Candidate(i)).ToArray();
        var first = Rank(items).Select(x => x.Id).ToArray();
        Assert.Equal(first, Rank(items.Reverse()).Select(x => x.Id));
        Assert.False(first.SequenceEqual(Rank(items, context: Context with { Sequence = 2 }).Select(x => x.Id)));
    }

    [Fact]
    public void Invalid_candidates_history_and_configuration_fail_explicitly()
    {
        var a = Candidate(1);
        Assert.Throws<ArgumentException>(() => Rank([a, a]));
        foreach (var invalid in new[] { a with { Relevance = double.NaN }, a with { Relevance = 1.1 },
            a with { DistanceKm = -1 }, a with { DistanceKm = double.PositiveInfinity }, a with { Activity = -1 } })
            Assert.Throws<ArgumentException>(() => Rank([invalid]));
        Assert.Throws<ArgumentException>(() => Rank([a], context: Context with { RadiusKm = double.NaN }));
        Assert.Throws<ArgumentException>(() => Rank([a], history: new Dictionary<Guid, ExposureBalance> { [a.Id] = new(-1, 0) }));
        Assert.Throws<ArgumentException>(() => new BusinessRanker(new() { DistanceDecayKm = 0 }));
        Assert.Throws<ArgumentException>(() => new BusinessRanker(new() { EquityWeight = .95 }));
    }
}
