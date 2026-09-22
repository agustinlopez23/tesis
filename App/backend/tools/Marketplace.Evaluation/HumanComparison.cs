using Marketplace.BLL;

namespace Marketplace.Evaluation;

public sealed record HumanComparisonRow(string Scope, string Metric, int SeedCount, int AvailablePairs,
    double? BaselineMean, double? CandidateMean, double? DeltaMean, double? DeltaStandardDeviation);
public static class HumanComparison
{
    public static IReadOnlyList<HumanComparisonRow> Summarize(IReadOnlyList<HumanRankingRun> runs, RankingVariant candidate)
    {
        var seeds = runs.Select(x => x.Seed).Distinct().Order().ToArray();
        var scopes = runs.SelectMany(x => x.Scopes.Keys).Distinct().Order(StringComparer.Ordinal).ToArray();
        if (seeds.Length == 0 || scopes.Length == 0 || candidate == RankingVariant.Proximity ||
            runs.Any(x => x.Variant != RankingVariant.Proximity && x.Variant != candidate) ||
            seeds.Any(seed => runs.Count(x => x.Seed == seed && x.Variant == RankingVariant.Proximity) != 1 ||
                runs.Count(x => x.Seed == seed && x.Variant == candidate) != 1) ||
            runs.Any(x => scopes.Any(scope => !x.Scopes.ContainsKey(scope))))
            throw new ArgumentException("Comparison requires one baseline and candidate per seed with identical scopes.");
        var metrics = new Dictionary<string, Func<PilotMetrics, double?>> {
            ["ndcg"] = x => x.Ndcg, ["gini"] = x => x.Distribution.Gini,
            ["topTenShare"] = x => x.Distribution.TopTenShare, ["coverage"] = x => x.Distribution.Coverage,
            ["meritDeviation"] = x => x.Distribution.MeritDeviation, ["newcomerCoverage"] = x => x.NewcomerCoverage };
        var rows = new List<HumanComparisonRow>();
        foreach (var scope in scopes)
            foreach (var (metric, read) in metrics)
            {
                double?[] Values(RankingVariant variant) => seeds.Select(seed =>
                    read(runs.Single(x => x.Seed == seed && x.Variant == variant).Scopes[scope].Metrics)).ToArray();
                var baseline = Values(RankingVariant.Proximity); var proposed = Values(candidate);
                var pairs = baseline.Zip(proposed).Where(x => x.First.HasValue && x.Second.HasValue).ToArray();
                var deltas = pairs.Select(x => x.Second!.Value - x.First!.Value).ToArray();
                var complete = deltas.Length == seeds.Length;
                double? mean = complete ? Mean(deltas) : null;
                double? deviation = complete && deltas.Length > 1
                    ? Math.Sqrt(deltas.Sum(x => Math.Pow(x - mean!.Value, 2)) / (deltas.Length - 1)) : null;
                rows.Add(new(scope, metric, seeds.Length, deltas.Length,
                    baseline.All(x => x.HasValue) ? Mean(baseline.Select(x => x!.Value).ToArray()) : null,
                    proposed.All(x => x.HasValue) ? Mean(proposed.Select(x => x!.Value).ToArray()) : null, mean, deviation));
            }
        return rows;
    }
    // Center before summing: identical values keep their exact representation at inclusive boundaries.
    internal static double Mean(IReadOnlyList<double> values) => values[0] + values.Average(x => x - values[0]);
}
