using Marketplace.BLL;

namespace Marketplace.Evaluation;

public sealed record HumanRankingMetrics(PilotMetrics Metrics, int SelectedQueries, int UnselectedQueries,
    int MissingJudgmentQueries, int MissingJudgmentPairs, int ZeroIdealQueries, int EmptySelectedQueries);
public sealed record HumanRankingRun(int Seed, RankingVariant Variant, IReadOnlyDictionary<string, HumanRankingMetrics> Scopes);
public sealed record HumanRankingTrace(int Seed, RankingVariant Variant, int Sequence, string Context, string JudgmentStatus,
    IReadOnlyDictionary<Guid, int> Judgments, IReadOnlyDictionary<Guid, double> Expected,
    IReadOnlyList<RankedBusiness> TopTen, double? Ndcg);

public static class HumanRankingRunner
{
    public static HumanRankingRun Run(PilotDataset dataset, RankingConfiguration configuration, RankingVariant variant,
        IReadOnlySet<int> selectedQueries, IReadOnlyDictionary<int, IReadOnlyDictionary<Guid, int>> judgments,
        Action<HumanRankingTrace>? trace = null)
    {
        var sequences = dataset.Queries.Select(x => x.Sequence).ToHashSet();
        if (dataset.Queries.Count == 0 || sequences.Count != dataset.Queries.Count ||
            dataset.Businesses.Select(x => x.Id).Distinct().Count() != dataset.Businesses.Count ||
            !dataset.Queries.Select(x => x.At).SequenceEqual(dataset.Queries.Select(x => x.At).Order()) ||
            dataset.Queries.Any(x => x.Surface is not ("search" or "magazine") || x.Zone is < 0 or > 2) ||
            selectedQueries.Any(x => !sequences.Contains(x)) || judgments.Keys.Any(x => !selectedQueries.Contains(x)) ||
            judgments.Values.Any(x => x.Values.Any(grade => grade is < 0 or > 3)))
            throw new ArgumentException("Invalid chronological dataset, sample or human judgments.");
        var ranker = new BusinessRanker(configuration);
        var ledger = new ExposureLedger();
        var scopes = new Dictionary<string, Accumulator>(StringComparer.Ordinal);
        var newcomers = dataset.Businesses.Where(x => x.PublishedAt > dataset.Queries[0].At).Select(x => x.Id).ToHashSet();
        foreach (var query in dataset.Queries)
        {
            var context = new RankingContext(query.Context, query.At, query.RadiusKm, dataset.Seed, query.Sequence);
            var ranked = ranker.Rank(PilotGenerator.Candidates(dataset, query), context, variant, ledger.Snapshot(context.Key, query.At));
            var expected = ExposureLedger.Targets(ranked.OrderBy(x => x.Id).ToDictionary(x => x.Id, x => x.Merit));
            var grades = judgments.GetValueOrDefault(query.Sequence) ?? new Dictionary<Guid, int>();
            if (grades.Keys.Any(x => !expected.ContainsKey(x))) throw new ArgumentException("Human judgment refers to an ineligible candidate.");
            var top = ranked.Take(configuration.Cutoff).ToArray();
            var ids = top.Select(x => x.Id).ToArray();
            var selected = selectedQueries.Contains(query.Sequence);
            var missing = selected ? expected.Keys.Count(x => !grades.ContainsKey(x)) : 0;
            double? ndcg = selected && missing == 0 ? EvaluationMetrics.Ndcg(grades, ids) : null;
            var status = !selected ? "unselected" : expected.Count == 0 ? "empty" : missing > 0 ? "missing" :
                ndcg.HasValue ? "judged" : "zero-ideal";
            ledger.Observe(context.Key, query.At, expected, ids);
            foreach (var scope in new[] { "all", query.Surface, query.Stratum })
            {
                if (!scopes.TryGetValue(scope, out var accumulator)) scopes[scope] = accumulator = new();
                accumulator.Add(expected, ids, ndcg, status, missing);
            }
            trace?.Invoke(new(dataset.Seed, variant, query.Sequence, query.Context, status, grades, expected, top, ndcg));
        }
        return new(dataset.Seed, variant, scopes.OrderBy(x => x.Key, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Value.Summarize(newcomers)));
    }

    private sealed class Accumulator
    {
        private readonly PilotRunner.Accumulator metrics = new();
        private readonly Dictionary<string, int> statuses = new();
        private int missingPairs;
        public void Add(IReadOnlyDictionary<Guid, double> expected, IReadOnlyList<Guid> ids, double? ndcg, string status, int missing)
        {
            metrics.Add(expected, ids, ndcg);
            statuses[status] = statuses.GetValueOrDefault(status) + 1;
            missingPairs += missing;
        }
        public HumanRankingMetrics Summarize(HashSet<Guid> newcomers) => new(metrics.Summarize(newcomers),
            statuses.Where(x => x.Key != "unselected").Sum(x => x.Value), statuses.GetValueOrDefault("unselected"),
            statuses.GetValueOrDefault("missing"), missingPairs, statuses.GetValueOrDefault("zero-ideal"), statuses.GetValueOrDefault("empty"));
    }
}
