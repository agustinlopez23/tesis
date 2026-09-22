using Marketplace.BLL;
using Marketplace.Evaluation;

namespace Marketplace.BLL.Tests;

public sealed class EvaluationMetricsTests
{
    private static Guid Id(int i) => new(i, 0, 0, new byte[8]);

    [Fact]
    public void Ndcg_uses_independent_grades_and_ideal_over_all_eligible_candidates()
    {
        var judgments = new Dictionary<Guid, int> { [Id(1)] = 3, [Id(2)] = 1, [Id(3)] = 0 };
        var expected = (1 + 7 / Math.Log2(3)) / (7 + 1 / Math.Log2(3));
        Assert.Equal(expected, EvaluationMetrics.Ndcg(judgments, [Id(2), Id(1)])!.Value, 10);
        Assert.Equal(1, EvaluationMetrics.Ndcg(judgments, [Id(1), Id(2)]));
        Assert.Equal(0, EvaluationMetrics.Ndcg(judgments, [Id(3)]));
        Assert.Null(EvaluationMetrics.Ndcg(new Dictionary<Guid, int> { [Id(1)] = 0 }, [Id(1)]));
        Assert.Throws<ArgumentException>(() => EvaluationMetrics.Ndcg(judgments, [Id(1), Id(1)]));
        Assert.Throws<ArgumentException>(() => EvaluationMetrics.Ndcg(judgments, [Id(4)]));
    }

    [Fact]
    public void Distribution_counts_zero_exposure_and_uses_businesses_for_top_ten_share()
    {
        var exposure = new Dictionary<Guid, ExposureBalance> { [Id(1)] = new(.5, 1), [Id(2)] = new(.5, 0) };
        var metrics = EvaluationMetrics.Distribution(exposure);
        Assert.Equal(.5, metrics.Gini);
        Assert.Equal(.5, metrics.Coverage);
        Assert.Equal(.5, metrics.MeritDeviation);
        Assert.Equal(1, metrics.TopTenShare);
        var uniform = EvaluationMetrics.Distribution(Enumerable.Range(1, 20).ToDictionary(Id, _ => new ExposureBalance(1, 1)));
        Assert.Equal(0, uniform.Gini);
        Assert.Equal(1, uniform.Coverage);
        Assert.Equal(.5, uniform.TopTenShare);
        Assert.Equal(0, uniform.MeritDeviation);
    }

    [Fact]
    public void Empty_and_invalid_distributions_never_report_artificial_success()
    {
        Assert.Null(EvaluationMetrics.Distribution(new Dictionary<Guid, ExposureBalance>()).Gini);
        Assert.Null(EvaluationMetrics.Distribution(new Dictionary<Guid, ExposureBalance> { [Id(1)] = new(0, 0) }).Coverage);
        Assert.Throws<ArgumentException>(() => EvaluationMetrics.Distribution(new Dictionary<Guid, ExposureBalance> { [Id(1)] = new(1, double.NaN) }));
    }
}
