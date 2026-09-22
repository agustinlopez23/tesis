using System.Globalization;
using Marketplace.BLL;

namespace Marketplace.Evaluation;

public sealed record CalibrationPolicy(string Id, RankingVariant Variant, RankingConfiguration Configuration, bool Rotation);
public sealed record CalibrationCandidate(CalibrationPolicy Policy, IReadOnlyList<HumanRankingRun> Runs,
    IReadOnlyList<HumanComparisonRow> Comparisons, CalibrationAssessment Assessment);
public sealed record CalibrationSelection(string Status, string? PolicyId, string Reason, bool Provisional = true);
public static class CalibrationGrid
{
    public static IReadOnlyList<CalibrationPolicy> Policies() => (from eq in new[] { .025, .05, .1 }
        from novelty in new[] { 0, .025, .05 } from rotation in new[] { false, true }
        select new CalibrationPolicy(string.Create(CultureInfo.InvariantCulture, $"eq{eq * 1000:000}-n{novelty * 1000:000}-r{(rotation ? 1 : 0)}"),
            novelty == 0 ? rotation ? RankingVariant.EquityRotation : RankingVariant.EquityOnly
                : rotation ? RankingVariant.Equity : RankingVariant.EquityNewcomer,
            new() { EquityWeight = eq, NewcomerWeight = novelty }, rotation)).ToArray();
    public static CalibrationCandidate Evaluate(CalibrationPolicy policy, IReadOnlyList<HumanRankingRun> runs)
    {
        if (!Policies().Contains(policy)) throw new ArgumentException("Policy is outside the fixed calibration grid.");
        var assessment = CalibrationCriteria.Assess(runs, policy.Variant);
        return new(policy, runs, assessment.Status == "inconclusive" ? [] : HumanComparison.Summarize(runs, policy.Variant), assessment);
    }
    public static CalibrationSelection Select(IReadOnlyList<CalibrationCandidate> candidates)
    {
        var grid = Policies();
        if (candidates.Count != grid.Count || grid.Any(p => candidates.Count(c => c.Policy == p) != 1))
            return new("inconclusive", null, "incomplete-or-invalid-grid");
        if (candidates.Any(x => x.Assessment.Status == "inconclusive"))
            return new("inconclusive", null, "insufficient-evidence");
        double Global(CalibrationCandidate c, string metric) => c.Comparisons.Single(x => x.Scope == "all" && x.Metric == metric).CandidateMean!.Value;
        var best = candidates.Where(x => x.Assessment.Status == "meets-in-this-collection")
            .OrderBy(x => Global(x, "meritDeviation")).ThenByDescending(x => Global(x, "ndcg"))
            .ThenBy(x => x.Policy.Configuration.EquityWeight).ThenBy(x => x.Policy.Configuration.NewcomerWeight)
            .ThenBy(x => x.Policy.Rotation).FirstOrDefault();
        return best is null ? new("no-eligible-policy", null, "no-policy-meets-draft-design")
            : new("provisional-candidate", best.Policy.Id, "merit-ndcg-equity-novelty-rotation");
    }
}
