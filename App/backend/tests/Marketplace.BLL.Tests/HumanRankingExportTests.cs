using System.Text.Json;
using Marketplace.BLL;
using Marketplace.Evaluation;

namespace Marketplace.BLL.Tests;

public sealed class HumanRankingExportTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "human-ranking-export-" + Guid.NewGuid().ToString("N"));
    private static RankingConfiguration Config => new() { EquityWeight = .025, NewcomerWeight = 0 };
    [Fact]
    public void Empty_reviews_stop_before_simulation_and_cannot_be_overwritten()
    {
        var package = Path.Combine(root, "package"); var output = Path.Combine(root, "pending");
        var digest = ReviewPackage.Write("calibration", package);
        Assert.False(HumanRankingExport.Write(package, digest, output, Config, RankingVariant.EquityOnly));
        Assert.True(File.Exists(Path.Combine(output, "review/review-status.json")));
        Assert.False(File.Exists(Path.Combine(output, "summary.json")));
        Assert.Empty(Directory.GetFiles(output, "*.gz"));
        Assert.Throws<IOException>(() => HumanRankingExport.Write(package, digest, output, Config, RankingVariant.EquityOnly));
    }
    [Fact]
    public void Training_and_changes_to_frozen_merit_or_eligibility_are_rejected()
    {
        var package = Path.Combine(root, "package"); var output = Path.Combine(root, "invalid");
        var digest = ReviewPackage.Write("training", package);
        Assert.Throws<ArgumentException>(() => HumanRankingExport.Write(package, digest, output, Config, RankingVariant.EquityOnly));
        Assert.False(Directory.Exists(output));
        foreach (var config in new[] { Config with { MinimumRelevance = .9 }, Config with { RelevanceWeight = .9 },
            Config with { DistanceDecayKm = 3 }, Config with { EquityWeight = .075 }, Config with { NewcomerWeight = .025 } })
            Assert.Throws<ArgumentException>(() => HumanRankingExport.Write(package, digest, output, config, RankingVariant.EquityOnly));
    }
    [Fact]
    public void Complete_software_fixture_replays_three_seeds_and_preserves_human_sample_denominators()
    {
        var package = Path.Combine(root, "fixture-not-human-evidence"); var output = Path.Combine(root, "comparison");
        var digest = ReviewPackage.Write("calibration", package);
        foreach (var assessor in new[] { "E1", "E2" })
        {
            var file = Path.Combine(package, "reviewers", assessor + ".csv");
            File.WriteAllText(file, File.ReadAllText(file).Replace(",\"\",\"\"\n", ",\"2\",\"software-fixture-only\"\n"));
        }
        Assert.True(HumanRankingExport.Write(package, digest, output, Config, RankingVariant.EquityOnly));
        using var summary = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "summary.json")));
        Assert.Equal(digest, summary.RootElement.GetProperty("packageSha256").GetString());
        Assert.False(summary.RootElement.GetProperty("acceptanceEvaluated").GetBoolean());
        var runs = summary.RootElement.GetProperty("runs").EnumerateArray().ToArray();
        Assert.Equal(6, runs.Length);
        Assert.Equal(30, runs.Where(x => x.GetProperty("variant").GetString() == "EquityOnly")
            .Sum(x => x.GetProperty("scopes").GetProperty("all").GetProperty("selectedQueries").GetInt32()));
        Assert.All(runs, run =>
        {
            var scopes = run.GetProperty("scopes");
            Assert.Equal(9, scopes.EnumerateObject().Count());
            Assert.Equal(5000, scopes.GetProperty("all").GetProperty("metrics").GetProperty("queries").GetInt32());
            Assert.Equal(1, scopes.GetProperty("all").GetProperty("metrics").GetProperty("ndcg").GetDouble());
        });
        Assert.Equal(3, Directory.GetFiles(output, "traces-*.jsonl.gz").Length);
        Assert.True(File.Exists(Path.Combine(output, "sha256.json")));
    }
    [Fact]
    public void Comparisons_weight_seeds_equally_and_do_not_hide_missing_seed_metrics()
    {
        HumanRankingRun Run(int seed, RankingVariant variant, int queries, double? ndcg) => new(seed, variant,
            new Dictionary<string, HumanRankingMetrics> { ["all"] = new(new(queries, 0, queries, ndcg,
                new(2, .3, 1, 1, .2), 1, 1), queries, 0, 0, 0, 0, 0) });
        var rows = HumanComparison.Summarize([Run(1, RankingVariant.Proximity, 1, .2), Run(2, RankingVariant.Proximity, 9, .8),
            Run(1, RankingVariant.EquityOnly, 1, .1), Run(2, RankingVariant.EquityOnly, 9, .7)], RankingVariant.EquityOnly);
        var ndcg = rows.Single(x => x.Metric == "ndcg");
        Assert.Equal(.5, ndcg.BaselineMean); Assert.Equal(.4, ndcg.CandidateMean!.Value, precision: 10);
        Assert.Equal(-.1, ndcg.DeltaMean!.Value, precision: 10);
        var missing = HumanComparison.Summarize([Run(1, RankingVariant.Proximity, 1, .2), Run(2, RankingVariant.Proximity, 9, null),
            Run(1, RankingVariant.EquityOnly, 1, .1), Run(2, RankingVariant.EquityOnly, 9, null)], RankingVariant.EquityOnly).Single(x => x.Metric == "ndcg");
        Assert.Null(missing.DeltaMean); Assert.Equal(1, missing.AvailablePairs); Assert.Equal(2, missing.SeedCount);
    }
    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
