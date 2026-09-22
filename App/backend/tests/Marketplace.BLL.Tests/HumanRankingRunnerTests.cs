using Marketplace.BLL;
using Marketplace.Evaluation;

namespace Marketplace.BLL.Tests;

public sealed class HumanRankingRunnerTests
{
    [Fact]
    public void Human_sample_replays_every_query_and_preserves_history_without_oracle_fallback()
    {
        var dataset = PilotGenerator.Generate(101, 120);
        var pilot = new List<PilotTrace>();
        PilotRunner.Run(dataset, new(), RankingVariant.Equity, pilot.Add);
        IReadOnlyDictionary<Guid, int> grades = pilot[^1].Expected.Keys.ToDictionary(id => id, _ => 3);
        var human = new List<HumanRankingTrace>();
        var result = HumanRankingRunner.Run(dataset, new(), RankingVariant.Equity, new HashSet<int> { 119 },
            new Dictionary<int, IReadOnlyDictionary<Guid, int>> { [119] = grades }, human.Add);
        Assert.Equal(120, human.Count);
        for (var i = 0; i < pilot.Count; i++)
        {
            Assert.Equal(pilot[i].TopTen, human[i].TopTen);
            Assert.Equal(pilot[i].Expected.OrderBy(x => x.Key), human[i].Expected.OrderBy(x => x.Key));
        }
        Assert.All(human.Take(119), row => { Assert.Null(row.Ndcg); Assert.Empty(row.Judgments); Assert.Equal("unselected", row.JudgmentStatus); });
        Assert.Equal(1, result.Scopes["all"].Metrics.JudgedQueries);
        Assert.Equal(1, result.Scopes["all"].Metrics.Ndcg);
        Assert.Equal(119, result.Scopes["all"].UnselectedQueries);
        Assert.Equal(9, result.Scopes.Count);
        Assert.Equal(60, result.Scopes["search"].Metrics.Queries);
        Assert.Equal(60, result.Scopes["magazine"].Metrics.Queries);
    }

    [Fact]
    public void Missing_partial_zero_gain_and_empty_selected_queries_are_distinct()
    {
        var dataset = PilotGenerator.Generate(101, 30);
        var traces = new List<PilotTrace>();
        PilotRunner.Run(dataset, new(), RankingVariant.Proximity, traces.Add);
        var selected = new HashSet<int> { 0, 1, 2 };
        var partial = traces[1].Expected.Keys.Take(1).ToDictionary(id => id, _ => 2);
        var zeros = traces[2].Expected.Keys.ToDictionary(id => id, _ => 0);
        var result = HumanRankingRunner.Run(dataset, new(), RankingVariant.Proximity, selected,
            new Dictionary<int, IReadOnlyDictionary<Guid, int>> { [1] = partial, [2] = zeros }).Scopes["all"];
        Assert.Equal(2, result.MissingJudgmentQueries);
        Assert.Equal(traces[0].Expected.Count + traces[1].Expected.Count - 1, result.MissingJudgmentPairs);
        Assert.Equal(1, result.ZeroIdealQueries);
        Assert.Null(result.Metrics.Ndcg);
        Assert.Equal(0, result.Metrics.JudgedQueries);
        dataset = dataset with { Businesses = dataset.Businesses.Select(x => x with { Published = false }).ToArray() };
        var empty = HumanRankingRunner.Run(dataset, new(), RankingVariant.Proximity, selected,
            new Dictionary<int, IReadOnlyDictionary<Guid, int>>()).Scopes["all"];
        Assert.Equal(3, empty.EmptySelectedQueries);
        Assert.Equal(0, empty.MissingJudgmentQueries);
        Assert.Equal(30, empty.Metrics.EmptyQueries);
        Assert.Null(empty.Metrics.Distribution.Gini);
    }

    [Fact]
    public void Surface_gini_uses_combined_exposures_not_average_of_zone_ginis()
    {
        var dataset = PilotGenerator.Generate(101, 120);
        var trace = new List<HumanRankingTrace>();
        var result = HumanRankingRunner.Run(dataset, new(), RankingVariant.Equity, new HashSet<int>(),
            new Dictionary<int, IReadOnlyDictionary<Guid, int>>(), trace.Add);
        var balances = new Dictionary<Guid, ExposureBalance>();
        foreach (var row in trace.Where(x => dataset.Queries.Single(q => q.Sequence == x.Sequence).Surface == "search"))
            foreach (var (id, expected) in row.Expected)
            {
                var previous = balances.GetValueOrDefault(id, new(0, 0));
                var position = row.TopTen.Select(x => x.Id).ToList().IndexOf(id);
                balances[id] = new(previous.Expected + expected, previous.Received + (position < 0 ? 0 : ExposureLedger.Attention(position + 1)));
            }
        Assert.Equal(EvaluationMetrics.Distribution(balances), result.Scopes["search"].Metrics.Distribution);
        Assert.NotEqual(result.Scopes.Where(x => x.Key.StartsWith("search/")).Average(x => x.Value.Metrics.Distribution.Gini),
            result.Scopes["search"].Metrics.Distribution.Gini);
    }

    [Fact]
    public void Foreign_queries_candidates_and_grades_are_rejected()
    {
        var data = PilotGenerator.Generate(101, 10);
        Assert.Throws<ArgumentException>(() => HumanRankingRunner.Run(data, new(), RankingVariant.Equity,
            new HashSet<int> { 100 }, new Dictionary<int, IReadOnlyDictionary<Guid, int>>()));
        Assert.Throws<ArgumentException>(() => HumanRankingRunner.Run(data, new(), RankingVariant.Equity,
            new HashSet<int>(), new Dictionary<int, IReadOnlyDictionary<Guid, int>> { [0] = new Dictionary<Guid, int>() }));
        Assert.Throws<ArgumentException>(() => HumanRankingRunner.Run(data, new(), RankingVariant.Equity,
            new HashSet<int> { 0 }, new Dictionary<int, IReadOnlyDictionary<Guid, int>> { [0] = new Dictionary<Guid, int> { [Guid.NewGuid()] = 4 } }));
        Assert.Throws<ArgumentException>(() => HumanRankingRunner.Run(data, new(), RankingVariant.Equity,
            new HashSet<int> { 0 }, new Dictionary<int, IReadOnlyDictionary<Guid, int>> { [0] = new Dictionary<Guid, int> { [Guid.NewGuid()] = 2 } }));
    }
}
