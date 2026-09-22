using Marketplace.BLL;

namespace Marketplace.Evaluation;

public sealed record DistributionMetrics(int Eligible, double? Gini, double? TopTenShare,
    double? Coverage, double? MeritDeviation);

public static class EvaluationMetrics
{
    public static double? Ndcg(IReadOnlyDictionary<Guid, int> judgments, IReadOnlyList<Guid> ranking)
    {
        if (judgments.Values.Any(x => x is < 0 or > 3) || ranking.Distinct().Count() != ranking.Count ||
            ranking.Any(x => !judgments.ContainsKey(x)))
            throw new ArgumentException("Invalid relevance judgments or ranking.");
        var ideal = Gain(judgments.Values.OrderDescending());
        return ideal == 0 ? null : Gain(ranking.Select(x => judgments[x])) / ideal;
    }

    private static double Gain(IEnumerable<int> grades) => grades.Take(10)
        .Select((grade, i) => (Math.Pow(2, grade) - 1) * ExposureLedger.Attention(i + 1)).Sum();

    public static DistributionMetrics Distribution(IReadOnlyDictionary<Guid, ExposureBalance> exposure)
    {
        if (exposure.Values.Any(x => !double.IsFinite(x.Expected) || x.Expected < 0 ||
            !double.IsFinite(x.Received) || x.Received < 0))
            throw new ArgumentException("Invalid exposure totals.");
        var values = exposure.Values.Select(x => x.Received).Order().ToArray();
        var total = values.Sum();
        var expected = exposure.Values.Sum(x => x.Expected);
        if (total == 0) return new(values.Length, null, null, null, null);
        var n = values.Length;
        var gini = values.Select((value, i) => (2.0 * (i + 1) - n - 1) * value).Sum() / (n * total);
        return new(n, Math.Clamp(gini, 0, 1), values.TakeLast(10).Sum() / total,
            (double)values.Count(x => x > 0) / n,
            expected == 0 ? null : .5 * exposure.Values.Sum(x => Math.Abs(x.Received / total - x.Expected / expected)));
    }
}
