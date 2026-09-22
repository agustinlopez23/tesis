using System.Text.Json;
using Marketplace.BLL;
using Marketplace.Evaluation;

namespace Marketplace.BLL.Tests;

public sealed class RankingAblationTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly RankingContext Context = new("search/centro", Now, null, 17, 1);
    private static RankingCandidate Candidate(int id, double relevance = .8) =>
        new(new Guid(id, 0, 0, new byte[8]), relevance, null, Now);

    [Theory]
    [InlineData(RankingVariant.EquityOnly, .82)]
    [InlineData(RankingVariant.EquityRotation, .82)]
    [InlineData(RankingVariant.EquityNewcomer, .83)]
    [InlineData(RankingVariant.Equity, .83)]
    public void Factorial_cells_reallocate_removed_novelty_weight_to_merit(RankingVariant variant, double expected)
    {
        var item = Candidate(1);
        var ranker = new BusinessRanker(new());
        var history = new Dictionary<Guid, ExposureBalance> { [item.Id] = new(10, 0) };
        var result = Assert.Single(ranker.Rank([item], Context, variant, history));
        Assert.Equal(expected, result.Score, 12);
        Assert.Equal(result.Score, Assert.Single(ranker.Rank([item with { Activity = 1 }], Context, variant, history)).Score);
    }

    [Theory]
    [InlineData(RankingVariant.EquityOnly)]
    [InlineData(RankingVariant.EquityNewcomer)]
    public void Without_rotation_exact_scores_then_ids_are_stable_across_sequences(RankingVariant variant)
    {
        var ranker = new BusinessRanker(new());
        var items = Enumerable.Range(1, 30).Select(i => Candidate(i, .8 + i * .00001)).ToArray();
        var expected = items.Reverse().Select(x => x.Id).ToArray();
        foreach (var sequence in new[] { 1, 2, 100 })
            Assert.Equal(expected, ranker.Rank(items, Context with { Sequence = sequence }, variant,
                new Dictionary<Guid, ExposureBalance>()).Select(x => x.Id));
        var tied = items.Select(x => x with { Relevance = .8 }).Reverse();
        Assert.Equal(items.Select(x => x.Id), ranker.Rank(tied, Context, variant,
            new Dictionary<Guid, ExposureBalance>()).Select(x => x.Id));
    }

    [Fact]
    public void Rotation_without_novelty_is_reproducible_and_ignores_publication_age()
    {
        var ranker = new BusinessRanker(new());
        var items = Enumerable.Range(1, 30).Select(i => Candidate(i)).ToArray();
        IReadOnlyList<RankedBusiness> Rank(IEnumerable<RankingCandidate> candidates, int sequence = 1) =>
            ranker.Rank(candidates, Context with { Sequence = sequence }, RankingVariant.EquityRotation,
                new Dictionary<Guid, ExposureBalance>());
        var first = Rank(items);
        Assert.Equal(first, Rank(items.Reverse()));
        Assert.Equal(first.Select(x => (x.Id, x.Score)), Rank(items.Select(x => x with { PublishedAt = Now.AddDays(-30) }))
            .Select(x => (x.Id, x.Score)));
        Assert.False(first.Select(x => x.Id).SequenceEqual(Rank(items, 2).Select(x => x.Id)));
    }

    [Fact]
    public void Ablation_export_covers_five_cells_and_reproduces_inputs_and_common_variants()
    {
        var root = Path.Combine(Path.GetTempPath(), "ranking-ablation-" + Guid.NewGuid().ToString("N"));
        try
        {
            var pilot = Path.Combine(root, "pilot"); var ablation = Path.Combine(root, "ablation");
            PilotExport.Write(pilot, [17], 20, new());
            PilotExport.Write(ablation, [17], 20, new(), ablations: true);
            Assert.Equal(File.ReadAllBytes(Path.Combine(pilot, "inputs-17.json")), File.ReadAllBytes(Path.Combine(ablation, "inputs-17.json")));
            using var a = JsonDocument.Parse(File.ReadAllText(Path.Combine(pilot, "summary.json")));
            using var b = JsonDocument.Parse(File.ReadAllText(Path.Combine(ablation, "summary.json")));
            var runs = b.RootElement.GetProperty("runs").EnumerateArray().ToArray();
            Assert.Equal(new[] { "Proximity", "EquityOnly", "EquityNewcomer", "EquityRotation", "Equity" },
                runs.Select(x => x.GetProperty("variant").GetString()));
            foreach (var variant in new[] { "Proximity", "Equity" })
                Assert.Equal(a.RootElement.GetProperty("runs").EnumerateArray().Single(x => x.GetProperty("variant").GetString() == variant).GetRawText(),
                    runs.Single(x => x.GetProperty("variant").GetString() == variant).GetRawText());
            Assert.Equal("ranking-ablation-report-v1", b.RootElement.GetProperty("schema").GetString());
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
