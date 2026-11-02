namespace Marketplace.Models;

// Append-only audit trail for sensitive administrator operations.
public sealed class AdminAction
{
    public long Id { get; set; }
    public Guid ActorId { get; set; }
    public required string TargetType { get; set; }
    public required string TargetId { get; set; }
    public required string TargetLabel { get; set; }
    public required string Action { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
