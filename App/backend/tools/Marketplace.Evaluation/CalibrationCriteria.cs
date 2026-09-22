using Marketplace.BLL;

namespace Marketplace.Evaluation;

public sealed record CalibrationCheck(string Scope, int? Seed, string Rule, string Origin,
    double? Baseline, double? Candidate, double? Limit, bool? Passed);
public sealed record CalibrationAssessment(string MainStatus, string GuardrailStatus, string Status,
    IReadOnlyList<CalibrationCheck> Checks);
public static class CalibrationCriteria
{
    public const string Version = "business-calibration-criteria-v1";
    public static CalibrationAssessment Assess(IReadOnlyList<HumanRankingRun> runs, RankingVariant candidate)
    {
        var checks = new List<CalibrationCheck>();
        var seeds = ReviewCases.Seeds("calibration");
        string[] strata = ["magazine/0", "magazine/1", "magazine/2", "search/0", "search/1", "search/2"];
        string[] scopes = ["all", "magazine", "search", .. strata];
        void Data(string scope, int? seed, string rule, bool pass) => checks.Add(new(scope, seed, rule, "data", null, null, null, pass));
        CalibrationAssessment Pending() => new("inconclusive", "inconclusive", "inconclusive", checks);
        Data("all", null, "paired-calibration-seeds", runs.Count == 6 &&
            candidate is RankingVariant.EquityOnly or RankingVariant.EquityNewcomer or RankingVariant.EquityRotation or RankingVariant.Equity &&
            seeds.All(seed => runs.Count(x => x.Seed == seed && x.Variant == RankingVariant.Proximity) == 1 &&
                runs.Count(x => x.Seed == seed && x.Variant == candidate) == 1));
        if (checks.Any(x => x.Passed != true)) return Pending();
        foreach (var run in runs)
        {
            Data("all", run.Seed, $"{run.Variant}:required-scopes", run.Scopes.Count == 9 && scopes.All(run.Scopes.ContainsKey));
            foreach (var (scope, m) in run.Scopes)
            {
                var values = Values(m.Metrics);
                Data(scope, run.Seed, $"{run.Variant}:complete-denominators", m.MissingJudgmentPairs == 0 && m.MissingJudgmentQueries == 0 &&
                    m.SelectedQueries > 0 && m.UnselectedQueries >= 0 && m.ZeroIdealQueries >= 0 && m.EmptySelectedQueries >= 0 &&
                    m.Metrics.JudgedQueries > 0 && m.Metrics.EmptyQueries >= m.EmptySelectedQueries && m.Metrics.EmptyQueries <= m.Metrics.Queries &&
                    m.SelectedQueries + m.UnselectedQueries == m.Metrics.Queries &&
                    m.Metrics.JudgedQueries + m.ZeroIdealQueries + m.EmptySelectedQueries == m.SelectedQueries &&
                    m.Metrics.Distribution.Eligible > 0 && m.Metrics.NewcomersEligible > 0 &&
                    m.Metrics.NewcomersEligible <= m.Metrics.Distribution.Eligible &&
                    values.Values.All(v => v is { } number && double.IsFinite(number) && number is >= 0 and <= 1));
            }
        }
        if (checks.Any(x => x.Passed != true)) return Pending();
        foreach (var seed in seeds)
        {
            var baseline = runs.Single(x => x.Seed == seed && x.Variant == RankingVariant.Proximity);
            var proposed = runs.Single(x => x.Seed == seed && x.Variant == candidate);
            foreach (var scope in scopes)
            {
                var b = baseline.Scopes[scope]; var p = proposed.Scopes[scope];
                Data(scope, seed, "same-sample-and-universe", b.SelectedQueries == p.SelectedQueries &&
                    b.Metrics.Queries == p.Metrics.Queries && b.Metrics.JudgedQueries == p.Metrics.JudgedQueries &&
                    b.ZeroIdealQueries == p.ZeroIdealQueries && b.EmptySelectedQueries == p.EmptySelectedQueries &&
                    b.Metrics.Distribution.Eligible == p.Metrics.Distribution.Eligible && b.Metrics.NewcomersEligible == p.Metrics.NewcomersEligible);
            }
            foreach (var run in new[] { baseline, proposed })
            {
                Data("all", seed, $"{run.Variant}:full-history", run.Scopes["all"].Metrics.Queries == 5000);
                foreach (var parent in new[] { "all", "magazine", "search" })
                {
                    var children = parent == "all" ? new[] { "magazine", "search" } : strata.Where(x => x.StartsWith(parent + "/")).ToArray();
                    var total = run.Scopes[parent];
                    Data(parent, seed, $"{run.Variant}:consistent-counts", children.Sum(s => run.Scopes[s].SelectedQueries) == total.SelectedQueries &&
                        children.Sum(s => run.Scopes[s].Metrics.JudgedQueries) == total.Metrics.JudgedQueries &&
                        children.Sum(s => run.Scopes[s].Metrics.Queries) == total.Metrics.Queries);
                }
            }
        }
        foreach (var variant in new[] { RankingVariant.Proximity, candidate })
            foreach (var stratum in strata)
            {
                var cells = runs.Where(x => x.Variant == variant).Select(x => x.Scopes[stratum]).ToArray();
                Data(stratum, null, $"{variant}:five-selected-four-useful", cells.Sum(x => x.SelectedQueries) == 5 && cells.Sum(x => x.Metrics.JudgedQueries) >= 4);
            }
        if (checks.Any(x => x.Passed != true)) return Pending();
        void Compare(string scope, int? seed, string metric, double b, double p, string origin, string mode, double tolerance)
        {
            var limit = mode == "relative" ? b * (1 - tolerance) : mode == "loss" ? b - tolerance : b + tolerance;
            checks.Add(new(scope, seed, $"{metric}-{mode}", origin, b, p, limit, mode == "loss" ? p >= limit : p <= limit));
        }
        foreach (var scope in scopes)
        {
            var local = scope.Contains('/');
            double Mean(RankingVariant variant, string metric) => HumanComparison.Mean(runs.Where(x => x.Variant == variant).OrderBy(x => x.Seed)
                .Select(x => Values(x.Scopes[scope].Metrics)[metric]!.Value).ToArray());
            foreach (var metric in new[] { "ndcg", "gini", "meritDeviation", "coverage", "newcomerCoverage" })
            {
                var distribution = metric is "gini" or "meritDeviation";
                Compare(scope, null, metric, Mean(RankingVariant.Proximity, metric), Mean(candidate, metric),
                    local && distribution ? "draft-guardrail" : "agreed", distribution ? local ? "increase" : "relative" : "loss",
                    metric == "ndcg" ? local ? .03 : .02 : distribution && !local ? metric == "gini" ? .1 : .05 : .01);
            }
        }
        foreach (var seed in seeds)
        {
            var b = Values(runs.Single(x => x.Seed == seed && x.Variant == RankingVariant.Proximity).Scopes["all"].Metrics);
            var p = Values(runs.Single(x => x.Seed == seed && x.Variant == candidate).Scopes["all"].Metrics);
            foreach (var metric in new[] { "ndcg", "gini", "meritDeviation", "coverage", "newcomerCoverage" })
                Compare("all", seed, metric, b[metric]!.Value, p[metric]!.Value, "draft-guardrail",
                    metric is "gini" or "meritDeviation" ? "increase" : "loss", metric == "ndcg" ? .03 : metric is "gini" or "meritDeviation" ? 0 : .01);
        }
        string Status(IEnumerable<CalibrationCheck> items) => items.Any(x => x.Passed != true) ? "does-not-meet" : "meets-in-this-collection";
        return new(Status(checks.Where(x => x.Origin == "agreed")), Status(checks.Where(x => x.Origin == "draft-guardrail")), Status(checks), checks);
    }
    private static Dictionary<string, double?> Values(PilotMetrics m) => new() { ["ndcg"] = m.Ndcg,
        ["gini"] = m.Distribution.Gini, ["meritDeviation"] = m.Distribution.MeritDeviation, ["coverage"] = m.Distribution.Coverage,
        ["newcomerCoverage"] = m.NewcomerCoverage, ["topTenShare"] = m.Distribution.TopTenShare };
}
