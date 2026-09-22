using Marketplace.BLL;
using Marketplace.Evaluation;

namespace Marketplace.BLL.Tests;

public sealed class CalibrationGridTests
{
    [Fact]
    public void Grid_has_exactly_eighteen_unique_policies_and_excludes_activity()
    {
        var grid = CalibrationGrid.Policies();
        Assert.Equal(18, grid.Count); Assert.Equal(18, grid.Select(x => x.Id).Distinct().Count());
        Assert.Equal(18, grid.Select(x => (x.Configuration.EquityWeight, x.Configuration.NewcomerWeight, x.Rotation)).Distinct().Count());
        Assert.Equal(new[] { .025, .05, .1 }, grid.Select(x => x.Configuration.EquityWeight).Distinct());
        Assert.Equal(new[] { 0, .025, .05 }, grid.Select(x => x.Configuration.NewcomerWeight).Distinct());
        foreach (var policy in grid)
        {
            Assert.Equal(new RankingConfiguration(), policy.Configuration with { EquityWeight = .1, NewcomerWeight = .05 });
            Assert.Equal(policy.Rotation, policy.Variant is RankingVariant.Equity or RankingVariant.EquityRotation);
            Assert.Equal(policy.Configuration.NewcomerWeight > 0, policy.Variant is RankingVariant.Equity or RankingVariant.EquityNewcomer);
        }
    }
    [Fact]
    public void Selection_is_order_independent_and_uses_merit_then_ndcg_then_weights_and_rotation()
    {
        var grid = CalibrationGrid.Policies();
        List<CalibrationCandidate> Results(Func<CalibrationPolicy, (double Merit, double Ndcg)> metrics) => grid.Select(policy =>
        {
            var (merit, ndcg) = metrics(policy);
            var runs = CalibrationCriteriaTests.Good(policy.Variant).Select(run => run.Variant == RankingVariant.Proximity ? run : run with {
                Scopes = run.Scopes.ToDictionary(x => x.Key, x => x.Value with { Metrics = x.Value.Metrics with {
                    Ndcg = ndcg, Distribution = x.Value.Metrics.Distribution with { MeritDeviation = merit } } }) }).ToArray();
            return CalibrationGrid.Evaluate(policy, runs);
        }).ToList();
        Assert.Equal(grid[0].Id, CalibrationGrid.Select(Results(_ => (.4, .49))).PolicyId);
        Assert.Equal(grid[0].Id, CalibrationGrid.Select(Results(_ => (.4, .49)).AsEnumerable().Reverse().ToArray()).PolicyId);
        Assert.Equal(grid[^1].Id, CalibrationGrid.Select(Results(p => (p.Id == grid[^1].Id ? .39 : .4, .49))).PolicyId);
        Assert.Equal(grid[^1].Id, CalibrationGrid.Select(Results(p => (.4, p.Id == grid[^1].Id ? .5 : .49))).PolicyId);
        Assert.True(CalibrationGrid.Select(Results(_ => (.4, .49))).Provisional);
    }
    [Fact]
    public void Missing_or_inconclusive_cell_prevents_selection_and_failed_cells_never_win()
    {
        var candidates = CalibrationGrid.Policies().Select(p => CalibrationGrid.Evaluate(p, CalibrationCriteriaTests.Good(p.Variant))).ToArray();
        Assert.Equal("inconclusive", CalibrationGrid.Select(candidates.Take(17).ToArray()).Status);
        candidates[0] = CalibrationGrid.Evaluate(candidates[0].Policy, []);
        Assert.Null(CalibrationGrid.Select(candidates).PolicyId);
        var failed = CalibrationGrid.Policies().Select(p => CalibrationGrid.Evaluate(p,
            CalibrationCriteriaTests.Good(p.Variant).Select(run => run.Variant == RankingVariant.Proximity ? run : run with {
                Scopes = run.Scopes.ToDictionary(x => x.Key, x => x.Value with { Metrics = x.Value.Metrics with { Ndcg = .1 } }) }).ToArray())).ToArray();
        Assert.Equal("no-eligible-policy", CalibrationGrid.Select(failed).Status);
        Assert.Null(CalibrationGrid.Select(failed).PolicyId);
    }
}
