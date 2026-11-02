namespace Marketplace.BLL;

// A named, versioned ranking policy. The label is public; changing any value requires a new label.
public sealed record RankingPolicy(string Label, RankingVariant Variant, RankingConfiguration Configuration, int Seed);

public static class RankingPolicies
{
    public static RankingPolicy EquityV1 { get; } = new("equity-v1", RankingVariant.EquityOnly, new(), 17);
}
