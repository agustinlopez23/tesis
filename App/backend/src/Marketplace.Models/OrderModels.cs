namespace Marketplace.Models;

public sealed class CustomerOrder
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid? CampaignId { get; set; }
    public required string Reference { get; set; }
    public required string CustomerName { get; set; }
    public required string BusinessName { get; set; }
    public required string WhatsApp { get; set; }
    public required string AccessKeyHash { get; set; }
    public required string RequestHash { get; set; }
    public DateTimeOffset AccessExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ContactReceivedAt { get; set; }
    public string Status { get; set; } = "pending";
    public string Source { get; set; } = "catalog";
    public int Version { get; set; } = 1;
    public List<OrderLine> Lines { get; set; } = [];
}

public sealed class OrderLine
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public required string Name { get; set; }
    public decimal UnitPrice { get; set; }
    public required string Currency { get; set; }
    public int Quantity { get; set; }
    public int ProductVersion { get; set; }
}

public sealed class OrderChange
{
    public Guid OrderId { get; set; }
    public int Version { get; set; }
    public Guid ActorId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public required string FromStatus { get; set; }
    public required string ToStatus { get; set; }
    public bool ContactReceived { get; set; }
}
