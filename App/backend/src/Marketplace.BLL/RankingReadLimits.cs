namespace Marketplace.BLL;

// Operational limits reject a whole opportunity; they never select a ranking prefix.
public sealed class RankingReadLimits
{
    public int MaxCandidates { get; }
    public int MaxBalanceRows { get; }

    public RankingReadLimits(int maxCandidates = 1000, int maxBalanceRows = 10000)
    {
        if (maxCandidates is < 1 or > 10000)
            throw new ArgumentOutOfRangeException(nameof(maxCandidates));
        MaxCandidates = maxCandidates;
        if (maxBalanceRows is < 1 or > 100000)
            throw new ArgumentOutOfRangeException(nameof(maxBalanceRows));
        MaxBalanceRows = maxBalanceRows;
    }
}

public sealed class RankingCapacityException() : Exception("Ranking capacity exceeded.");
