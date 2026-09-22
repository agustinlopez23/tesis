using Marketplace.BLL;

namespace Marketplace.Evaluation;

public sealed record PilotMetrics(int Queries, int EmptyQueries, int JudgedQueries, double? Ndcg,
    DistributionMetrics Distribution, int NewcomersEligible, double? NewcomerCoverage);
public sealed record PilotRun(int Seed, RankingVariant Variant, IReadOnlyDictionary<string, PilotMetrics> Strata);
public sealed record PilotTrace(int Seed, RankingVariant Variant, int Sequence, string Context,
    IReadOnlyDictionary<Guid, int> Judgments, IReadOnlyDictionary<Guid, double> Expected,
    IReadOnlyList<RankedBusiness> TopTen, double? Ndcg);

public static class PilotRunner
{
    public static PilotRun Run(PilotDataset dataset, RankingConfiguration configuration, RankingVariant variant,
        Action<PilotTrace>? trace = null)
    {
        if (dataset.Queries.Count == 0 || dataset.Businesses.Select(x => x.Id).Distinct().Count() != dataset.Businesses.Count ||
            dataset.Queries.Select(x => x.Sequence).Distinct().Count() != dataset.Queries.Count ||
            !dataset.Queries.Select(x => x.At).SequenceEqual(dataset.Queries.Select(x => x.At).Order()))
            throw new ArgumentException("Dataset requires unique identifiers and chronological queries.");
        var ranker = new BusinessRanker(configuration);
        var ledger = new ExposureLedger();
        var strata = new Dictionary<string, Accumulator>(StringComparer.Ordinal) { ["all"] = new() };
        var newcomers = dataset.Businesses.Where(x => x.PublishedAt > dataset.Queries[0].At).Select(x => x.Id).ToHashSet();
        foreach (var query in dataset.Queries)
        {
            var context = new RankingContext(query.Context, query.At, query.RadiusKm, dataset.Seed, query.Sequence);
            var ranked = ranker.Rank(PilotGenerator.Candidates(dataset, query), context, variant, ledger.Snapshot(context.Key, query.At));
            // Stable ID order keeps target arithmetic identical across ranking variants.
            var expected = ExposureLedger.Targets(ranked.OrderBy(x => x.Id).ToDictionary(x => x.Id, x => x.Merit));
            var allJudgments = PilotGenerator.Judge(dataset, query);
            var judgments = ranked.OrderBy(x => x.Id).ToDictionary(x => x.Id, x => allJudgments[x.Id]);
            var top = ranked.Take(configuration.Cutoff).ToArray();
            var ids = top.Select(x => x.Id).ToArray();
            var ndcg = EvaluationMetrics.Ndcg(judgments, ids);
            ledger.Observe(context.Key, query.At, expected, ids);
            if (!strata.TryGetValue(query.Stratum, out var stratum)) strata[query.Stratum] = stratum = new();
            strata["all"].Add(expected, ids, ndcg);
            stratum.Add(expected, ids, ndcg);
            trace?.Invoke(new(dataset.Seed, variant, query.Sequence, query.Context, judgments, expected, top, ndcg));
        }
        return new(dataset.Seed, variant, strata.OrderBy(x => x.Key, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.Value.Summarize(newcomers)));
    }

    internal sealed class Accumulator
    {
        private int queries, empty, judged;
        private double ndcgTotal;
        private readonly Dictionary<Guid, ExposureBalance> exposure = [];

        public void Add(IReadOnlyDictionary<Guid, double> expected, IReadOnlyList<Guid> displayed, double? ndcg)
        {
            queries++;
            if (expected.Count == 0) empty++;
            if (ndcg is { } value) { judged++; ndcgTotal += value; }
            var received = displayed.Select((id, i) => (id, amount: ExposureLedger.Attention(i + 1)))
                .ToDictionary(x => x.id, x => x.amount);
            foreach (var (id, amount) in expected)
            {
                var old = exposure.GetValueOrDefault(id, new(0, 0));
                exposure[id] = new(old.Expected + amount, old.Received + received.GetValueOrDefault(id));
            }
        }

        public PilotMetrics Summarize(HashSet<Guid> newcomers)
        {
            var newExposure = exposure.Where(x => newcomers.Contains(x.Key)).Select(x => x.Value.Received).ToArray();
            return new(queries, empty, judged, judged == 0 ? null : ndcgTotal / judged,
                EvaluationMetrics.Distribution(exposure), newExposure.Length,
                newExposure.Length == 0 ? null : (double)newExposure.Count(x => x > 0) / newExposure.Length);
        }
    }
}
