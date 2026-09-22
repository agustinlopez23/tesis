using System.Text.Json;
using Marketplace.BLL;
using Marketplace.Evaluation;

namespace Marketplace.BLL.Tests;

public sealed class PilotRunnerTests
{
    [Theory]
    [InlineData(RankingVariant.Equity)]
    [InlineData(RankingVariant.EquityOnly)]
    [InlineData(RankingVariant.EquityNewcomer)]
    [InlineData(RankingVariant.EquityRotation)]
    public void Variants_share_candidates_and_targets_but_never_share_history(RankingVariant variant)
    {
        var dataset = PilotGenerator.Generate(17, 100);
        var baseline = new List<PilotTrace>(); var equity = new List<PilotTrace>();
        var a = PilotRunner.Run(dataset, new(), RankingVariant.Proximity, baseline.Add);
        var b = PilotRunner.Run(dataset, new(), variant, equity.Add);
        Assert.Equal(100, baseline.Count);
        Assert.Equal(100, b.Strata["all"].Queries);
        for (var i = 0; i < baseline.Count; i++)
        {
            Assert.Equal(baseline[i].Expected.OrderBy(x => x.Key), equity[i].Expected.OrderBy(x => x.Key));
            Assert.Equal(baseline[i].Judgments.OrderBy(x => x.Key), equity[i].Judgments.OrderBy(x => x.Key));
            Assert.Equal(baseline[i].TopTen.Count, equity[i].TopTen.Count);
            Assert.Equal(equity[i].TopTen.Count, equity[i].TopTen.Select(x => x.Id).Distinct().Count());
        }
        Assert.Equal(JsonSerializer.Serialize(a), JsonSerializer.Serialize(PilotRunner.Run(dataset, new(), RankingVariant.Proximity)));
        Assert.Equal(JsonSerializer.Serialize(b), JsonSerializer.Serialize(PilotRunner.Run(dataset, new(), variant)));
        Assert.Equal(7, b.Strata.Count);
    }

    [Fact]
    public void Empty_queries_and_future_businesses_do_not_inflate_coverage()
    {
        var dataset = PilotGenerator.Generate(17, 20);
        dataset = dataset with { Businesses = dataset.Businesses.Select(x => x with { Published = false }).ToArray() };
        var result = PilotRunner.Run(dataset, new(), RankingVariant.Equity).Strata["all"];
        Assert.Equal(20, result.EmptyQueries);
        Assert.Equal(0, result.JudgedQueries);
        Assert.Equal(0, result.Distribution.Eligible);
        Assert.Null(result.Ndcg);
        Assert.Null(result.Distribution.Coverage);
    }
}
