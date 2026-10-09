namespace Marketplace.BLL;

public enum RankingVariant { Relevance, Proximity, Equity, EquityActivity, EquityOnly, EquityNewcomer, EquityRotation }
// Null means publication history is unknown, not newly published.
public sealed record RankingCandidate(Guid Id, double Relevance, double? DistanceKm,
    DateTimeOffset? PublishedAt, bool Published = true, bool Suspended = false, double Activity = 0);
public sealed record RankingContext(string Key, DateTimeOffset At, double? RadiusKm, int Seed, long Sequence);
public sealed record ExposureBalance(double Expected, double Received);
public sealed record RankedBusiness(Guid Id, double Relevance, double Proximity, double Merit,
    double Deficit, double Newcomer, double Activity, double Score);
public sealed record RankingConfiguration
{
    public string Version => "business-pilot-v1";
    public double MinimumRelevance { get; init; } = .2;
    public double DistanceDecayKm { get; init; } = 2;
    public double RelevanceWeight { get; init; } = .7;
    public double EquityWeight { get; init; } = .1;
    public double NewcomerWeight { get; init; } = .05;
    public double ActivityWeight { get; init; } = .05;
    public double NewcomerDays { get; init; } = 14;
    public double TieBucketWidth { get; init; } = .01;
    public int WindowDays => 30;
    public int Cutoff => 10;
}
