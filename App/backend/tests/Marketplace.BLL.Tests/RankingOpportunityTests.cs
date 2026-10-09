namespace Marketplace.BLL.Tests;

public sealed class RankingOpportunityTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-04T12:00:00Z");
    private static MagazineRankingInput Input(int count = 45)
    {
        var candidates = Enumerable.Range(1, count).Select(i => new RankingCandidate(
            new Guid(i, 0, 0, new byte[8]), 1, i / 20d, i == 1 ? null : Now.AddDays(-30))).ToArray();
        return new(new("magazine/centro/category/1/radius/4/eligibility/v1", Now, 4, 17, 1),
            candidates, candidates.ToDictionary(x => x.Id, _ => Owner));
    }

    [Fact]
    public void Every_candidate_has_a_trace_and_top_ten_budget_is_conserved()
    {
        var input = Input();
        var history = new Dictionary<Guid, ExposureBalance> { [input.Candidates[0].Id] = new(4, 1) };
        var result = RankingOpportunityBuilder.Build(input, RankingVariant.EquityNewcomer, new(), history, null);
        Assert.Equal(45, result.Candidates.Count);
        Assert.Equal(Enumerable.Range(1, 45), result.Candidates.Select(x => x.Position));
        Assert.Equal(10, result.Candidates.Count(x => x.Received > 0));
        Assert.All(result.Candidates.Skip(10), x => Assert.Equal(0, x.Received));
        Assert.All(result.Candidates, x => Assert.InRange(x.Expected, double.Epsilon, 1));
        var budget = Enumerable.Range(1, 10).Sum(ExposureLedger.Attention);
        Assert.Equal(budget, result.Candidates.Sum(x => x.Expected), 10);
        Assert.Equal(budget, result.Candidates.Sum(x => x.Received), 10);
        var unknown = Assert.Single(result.Candidates, x => x.Candidate.PublishedAt is null);
        Assert.Equal(new ExposureBalance(4, 1), unknown.Previous);
        Assert.Equal(0, unknown.Components.Newcomer);
        Assert.Equal("assigned", result.Reason);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public void Own_business_preview_zeros_entire_opportunity_but_keeps_order_and_trace()
    {
        var input = Input();
        var normal = RankingOpportunityBuilder.Build(input, RankingVariant.Equity, new(), new Dictionary<Guid, ExposureBalance>(), null);
        var preview = RankingOpportunityBuilder.Build(input, RankingVariant.Equity, new(), new Dictionary<Guid, ExposureBalance>(), Owner);
        Assert.Equal(normal.Candidates.Select(x => x.Candidate.Id), preview.Candidates.Select(x => x.Candidate.Id));
        Assert.All(preview.Candidates, x => { Assert.Equal(0, x.Expected); Assert.Equal(0, x.Received); });
        Assert.Equal("own-business-preview", preview.Reason);
        Assert.Equal(normal.PolicyHash, preview.PolicyHash);
    }

    [Fact]
    public void Policy_digest_tracks_parameters_variant_and_seed_but_not_opportunity()
    {
        var input = Input(0);
        RankingOpportunity Build(MagazineRankingInput value, RankingVariant variant, RankingConfiguration config) =>
            RankingOpportunityBuilder.Build(value, variant, config, new Dictionary<Guid, ExposureBalance>(), null);
        var result = Build(input, RankingVariant.EquityOnly, new());
        Assert.Empty(result.Candidates);
        Assert.Equal(64, result.PolicyHash.Length);
        Assert.Equal(result.PolicyHash, Build(input with { Context = input.Context with { Sequence = 2 } }, RankingVariant.EquityOnly, new()).PolicyHash);
        Assert.NotEqual(result.PolicyHash, Build(input, RankingVariant.Equity, new()).PolicyHash);
        Assert.NotEqual(result.PolicyHash, Build(input, RankingVariant.EquityOnly, new() { EquityWeight = .2 }).PolicyHash);
        Assert.NotEqual(result.PolicyHash, Build(input with { Context = input.Context with { Seed = 29 } }, RankingVariant.EquityOnly, new()).PolicyHash);
    }

    [Fact]
    public void Incomplete_owner_map_or_ineligible_candidate_aborts_instead_of_losing_trace()
    {
        var input = Input(1);
        Assert.Throws<ArgumentException>(() => RankingOpportunityBuilder.Build(input with { OwnerIds = new Dictionary<Guid, Guid>() },
            RankingVariant.Equity, new(), new Dictionary<Guid, ExposureBalance>(), null));
        Assert.Throws<ArgumentException>(() => RankingOpportunityBuilder.Build(input with
            { Candidates = [input.Candidates[0] with { Published = false }] },
            RankingVariant.Equity, new(), new Dictionary<Guid, ExposureBalance>(), null));
    }
}
