namespace Marketplace.Models;

public sealed class Campaign
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid? ProductId { get; set; }
    public int? PhotoSlot { get; set; }
    public byte[]? Image { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Conditions { get; set; }
    public required string ZoneId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Status { get; set; } = "draft";
    public int Version { get; set; }
    public required string SaveHash { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public string? PaymentReference { get; set; }
}
public sealed class CampaignChange
{
    public Guid OperationId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid ActorId { get; set; }
    public int Version { get; set; }
    public required string Action { get; set; }
    public required string Reason { get; set; }
    public required string RequestHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
public sealed class CampaignEvent
{
    public Guid CampaignId { get; set; }
    public Guid ContextId { get; set; }
    public required string Kind { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
