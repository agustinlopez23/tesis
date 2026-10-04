using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Marketplace.BLL;

public sealed class BusinessRanker
{
    private readonly RankingConfiguration configuration;

    public BusinessRanker(RankingConfiguration configuration)
    {
        var weights = new[] { configuration.MinimumRelevance, configuration.RelevanceWeight,
            configuration.EquityWeight, configuration.NewcomerWeight, configuration.ActivityWeight };
        if (weights.Any(x => !Unit(x)) || configuration.MinimumRelevance <= 0 ||
            configuration.EquityWeight + configuration.NewcomerWeight + configuration.ActivityWeight >= 1 ||
            !Positive(configuration.DistanceDecayKm) || !Positive(configuration.NewcomerDays) ||
            !Positive(configuration.TieBucketWidth) || configuration.TieBucketWidth > 1)
            throw new ArgumentException("Invalid ranking configuration.");
        this.configuration = configuration;
    }

    public IReadOnlyList<RankedBusiness> Rank(IEnumerable<RankingCandidate> candidates,
        RankingContext context, RankingVariant variant, IReadOnlyDictionary<Guid, ExposureBalance> history)
    {
        if (string.IsNullOrWhiteSpace(context.Key) || context.Sequence < 0 || !Enum.IsDefined(variant) ||
            context.RadiusKm is { } radius && !Positive(radius))
            throw new ArgumentException("Invalid ranking context.");
        var items = candidates.ToArray();
        if (items.Select(x => x.Id).Distinct().Count() != items.Length || items.Any(x =>
            x.Id == Guid.Empty || !Unit(x.Relevance) || !Unit(x.Activity) ||
            x.DistanceKm is { } d && (!double.IsFinite(d) || d < 0)) ||
            history.Values.Any(x => !double.IsFinite(x.Expected) || x.Expected < 0 ||
                !double.IsFinite(x.Received) || x.Received < 0))
            throw new ArgumentException("Invalid candidates or exposure history.");

        var scored = items.Where(x => x.Published && !x.Suspended &&
            (x.PublishedAt is null || x.PublishedAt <= context.At) &&
            x.Relevance >= configuration.MinimumRelevance &&
            (context.RadiusKm is null || x.DistanceKm is { } d && d <= context.RadiusKm))
            .Select(x => Score(x, context, variant, history)).ToArray();
        if (variant is RankingVariant.Relevance or RankingVariant.Proximity or
            RankingVariant.EquityOnly or RankingVariant.EquityNewcomer)
            return scored.OrderByDescending(x => x.Score).ThenBy(x => x.Id).ToArray();
        return scored.OrderByDescending(x => Math.Floor(x.Score / configuration.TieBucketWidth))
            .ThenBy(x => RotationKey(x.Id, context), StringComparer.Ordinal).ThenBy(x => x.Id).ToArray();
    }

    private RankedBusiness Score(RankingCandidate item, RankingContext context, RankingVariant variant,
        IReadOnlyDictionary<Guid, ExposureBalance> history)
    {
        var proximity = context.RadiusKm is null ? 0 : Math.Exp(-item.DistanceKm!.Value / configuration.DistanceDecayKm);
        var merit = context.RadiusKm is null ? item.Relevance :
            configuration.RelevanceWeight * item.Relevance + (1 - configuration.RelevanceWeight) * proximity;
        var balance = history.GetValueOrDefault(item.Id);
        var deficit = balance is null ? 0 : Math.Clamp(
            (balance.Expected - balance.Received) / Math.Max(balance.Expected, 1), 0, 1);
        var newcomer = item.PublishedAt is { } publishedAt
            ? Math.Max(0, 1 - (context.At - publishedAt).TotalDays / configuration.NewcomerDays) : 0;
        var activityWeight = variant == RankingVariant.EquityActivity ? configuration.ActivityWeight : 0;
        var newcomerWeight = variant is RankingVariant.EquityOnly or RankingVariant.EquityRotation
            ? 0 : configuration.NewcomerWeight;
        var score = variant switch
        {
            RankingVariant.Relevance => item.Relevance,
            RankingVariant.Proximity => merit,
            _ => (1 - configuration.EquityWeight - newcomerWeight - activityWeight) * merit +
                configuration.EquityWeight * deficit + newcomerWeight * newcomer + activityWeight * item.Activity
        };
        return new(item.Id, item.Relevance, proximity, merit, deficit, newcomer, item.Activity, score);
    }

    private static string RotationKey(Guid id, RankingContext context) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(string.Create(CultureInfo.InvariantCulture,
            $"{context.Seed}|{context.Key.Length}:{context.Key}|{context.Sequence}|{id:D}"))));
    private static bool Unit(double value) => double.IsFinite(value) && value is >= 0 and <= 1;
    private static bool Positive(double value) => double.IsFinite(value) && value > 0;
}
