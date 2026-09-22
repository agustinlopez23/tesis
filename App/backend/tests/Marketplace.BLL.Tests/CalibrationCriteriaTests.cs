using Marketplace.BLL;
using Marketplace.Evaluation;

namespace Marketplace.BLL.Tests;

public sealed class CalibrationCriteriaTests
{
    internal static List<HumanRankingRun> Good(RankingVariant variant = RankingVariant.EquityOnly)
    {
        var result = new List<HumanRankingRun>();
        foreach (var seed in new[] { 101, 103, 107 })
            foreach (var policy in new[] { RankingVariant.Proximity, variant })
            {
                var selected = seed == 101 ? 1 : 2;
                HumanRankingMetrics M(int queries, int count) => new(new(queries, 0, count,
                    policy == RankingVariant.Proximity ? .5 : .49,
                    new(200, policy == RankingVariant.Proximity ? .5 : .4, .2, .8,
                        policy == RankingVariant.Proximity ? .5 : .4), 20, .8), count, queries - count, 0, 0, 0, 0);
                var scopes = new Dictionary<string, HumanRankingMetrics> { ["all"] = M(5000, 6 * selected) };
                foreach (var surface in new[] { "search", "magazine" })
                {
                    scopes[surface] = M(2500, 3 * selected);
                    for (var zone = 0; zone < 3; zone++) scopes[$"{surface}/{zone}"] = M(zone == 0 ? 834 : 833, selected);
                }
                result.Add(new(seed, policy, scopes));
            }
        return result;
    }
    private static List<HumanRankingRun> Change(List<HumanRankingRun> runs, string scope,
        Func<HumanRankingMetrics, HumanRankingMetrics> change, int? seed = null, bool both = false) => runs.Select(run =>
        (both || run.Variant != RankingVariant.Proximity) && (seed is null || run.Seed == seed)
        ? run with { Scopes = run.Scopes.ToDictionary(x => x.Key, x => x.Key == scope ? change(x.Value) : x.Value) } : run).ToList();

    [Fact]
    public void Complete_collection_passes_and_reports_all_rules_with_provenance()
    {
        var result = CalibrationCriteria.Assess(Good(), RankingVariant.EquityOnly);
        Assert.Equal("meets-in-this-collection", result.MainStatus);
        Assert.Equal(result.MainStatus, result.Status);
        Assert.Contains(result.Checks, x => x.Origin == "agreed" && x.Rule == "gini-relative");
        Assert.Contains(result.Checks, x => x.Origin == "draft-guardrail" && x.Seed == 101);
    }
    [Fact]
    public void Inclusive_boundaries_pass_but_small_excess_fails_without_rounding()
    {
        var runs = Change(Good(), "all", m => m with { Metrics = m.Metrics with {
            Ndcg = .5 - .02, NewcomerCoverage = .8 - .01,
            Distribution = m.Metrics.Distribution with { Gini = .5 * (1 - .1), MeritDeviation = .5 * (1 - .05), Coverage = .8 - .01 } } });
        Assert.Equal("meets-in-this-collection", CalibrationCriteria.Assess(runs, RankingVariant.EquityOnly).Status);
        runs = Change(runs, "all", m => m with { Metrics = m.Metrics with { Ndcg = .5 - .020000001 } });
        Assert.Equal("does-not-meet", CalibrationCriteria.Assess(runs, RankingVariant.EquityOnly).MainStatus);
    }
    [Fact]
    public void Local_and_seed_failures_cannot_be_hidden_by_global_mean()
    {
        var local = Change(Good(), "search/1", m => m with { Metrics = m.Metrics with { Ndcg = .46 } });
        Assert.Equal("does-not-meet", CalibrationCriteria.Assess(local, RankingVariant.EquityOnly).MainStatus);
        var guardrail = Change(Good(), "all", m => m with { Metrics = m.Metrics with { Ndcg = .46 } }, 101);
        var result = CalibrationCriteria.Assess(guardrail, RankingVariant.EquityOnly);
        Assert.Equal("meets-in-this-collection", result.MainStatus);
        Assert.Equal("does-not-meet", result.GuardrailStatus);
        Assert.Equal("does-not-meet", result.Status);
    }
    [Fact]
    public void Missing_seed_scope_grade_denominator_or_useful_query_is_inconclusive()
    {
        var cases = new List<List<HumanRankingRun>> {
            Good().Where(x => x.Seed != 107).ToList(),
            Good().Select(x => x with { Scopes = x.Scopes.Where(s => s.Key != "search").ToDictionary() }).ToList(),
            Change(Good(), "search/1", m => m with { MissingJudgmentPairs = 1 }),
            Change(Good(), "search/1", m => m with { Metrics = m.Metrics with { NewcomersEligible = 0, NewcomerCoverage = null } }),
            Change(Good(), "search/1", m => m with { Metrics = m.Metrics with { JudgedQueries = 0, Ndcg = null }, ZeroIdealQueries = m.SelectedQueries }, 101, true),
            Change(Good(), "search/1", m => m with { Metrics = m.Metrics with { Ndcg = double.NaN } }),
            Change(Good(), "search/1", m => m with { SelectedQueries = 999 }) };
        foreach (var runs in cases)
            Assert.Equal("inconclusive", CalibrationCriteria.Assess(runs, RankingVariant.EquityOnly).Status);
        // Three useful queries out of five: each seed has one, yet pooled coverage is insufficient.
        var sparse = Change(Good(), "search/1", m => m with { Metrics = m.Metrics with { JudgedQueries = 1 }, ZeroIdealQueries = m.SelectedQueries - 1 }, both: true);
        Assert.Equal("inconclusive", CalibrationCriteria.Assess(sparse, RankingVariant.EquityOnly).Status);
    }
    [Fact]
    public void Zero_baseline_requires_zero_candidate_and_missing_data_takes_precedence()
    {
        var zero = Change(Good(), "all", m => m with { Metrics = m.Metrics with { Distribution = m.Metrics.Distribution with { Gini = 0, MeritDeviation = 0 } } }, both: true);
        Assert.Equal("meets-in-this-collection", CalibrationCriteria.Assess(zero, RankingVariant.EquityOnly).Status);
        var positive = Change(zero, "all", m => m with { Metrics = m.Metrics with { Distribution = m.Metrics.Distribution with { Gini = .001 } } });
        Assert.Equal("does-not-meet", CalibrationCriteria.Assess(positive, RankingVariant.EquityOnly).Status);
        positive = Change(positive, "search/0", m => m with { MissingJudgmentPairs = 1 });
        Assert.Equal("inconclusive", CalibrationCriteria.Assess(positive, RankingVariant.EquityOnly).Status);
    }
}
