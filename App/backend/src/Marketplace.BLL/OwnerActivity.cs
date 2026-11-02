namespace Marketplace.BLL;

public static class OwnerActivityKinds
{
    public const string ProfileUpdate = "profile_update";
    public const string CatalogUpdate = "catalog_update";
    public const string PromotionUpdate = "promotion_update";
}

// Substantive owner updates count once per business and kind inside DedupWindow.
// Signal() bounds the ranking input to [0, 1]: RankingCap updates in the 30-day window saturate it.
public sealed record OwnerActivitySettings
{
    public const int WindowDays = 30;
    public TimeSpan DedupWindow { get; }
    public int RankingCap { get; }

    public OwnerActivitySettings(TimeSpan dedupWindow, int rankingCap)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(dedupWindow, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rankingCap);
        DedupWindow = dedupWindow;
        RankingCap = rankingCap;
    }

    public static OwnerActivitySettings Default { get; } = new(TimeSpan.FromHours(1), 10);

    public double Signal(int recentUpdates) => Math.Min(Math.Max(recentUpdates, 0), RankingCap) / (double)RankingCap;
}

public interface IOwnerActivityRepository
{
    Task<bool> RecordAsync(Guid businessId, string kind, DateTimeOffset at, TimeSpan dedupWindow, CancellationToken ct);
}

public sealed class OwnerActivityService(IOwnerActivityRepository repository, OwnerActivitySettings settings, TimeProvider clock)
{
    public Task<bool> RecordAsync(Guid businessId, string kind, CancellationToken ct)
    {
        if (businessId == Guid.Empty || kind is not (OwnerActivityKinds.ProfileUpdate or OwnerActivityKinds.CatalogUpdate or OwnerActivityKinds.PromotionUpdate))
            throw new ArgumentException("Invalid owner activity.");
        return repository.RecordAsync(businessId, kind, clock.GetUtcNow(), settings.DedupWindow, ct);
    }
}
