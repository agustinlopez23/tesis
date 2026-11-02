namespace Marketplace.Models;

public sealed class BusinessActivity
{
    public Guid VisitId { get; set; }
    public Guid BusinessId { get; set; }
    public string Kind { get; set; } = "";
    public string Surface { get; set; } = "";
    public string Source { get; set; } = "";
    public int? Position { get; set; }
    public string? RankingVersion { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

// A substantive owner change (profile, catalog or promotion), stored once per deduplication window.
public sealed class OwnerActivity
{
    public long Id { get; set; }
    public Guid BusinessId { get; set; }
    public string Kind { get; set; } = "";
    public DateTimeOffset OccurredAt { get; set; }
}
