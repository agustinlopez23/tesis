namespace Marketplace.Models;

public sealed class BusinessSubscription
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public Guid GrantedBy { get; set; }
    public DateTimeOffset GrantedAt { get; set; }
    public required string Reason { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid? RevokedBy { get; set; }
    public string? RevocationReason { get; set; }
}

public sealed class CatalogProduct
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Category { get; set; }
    public decimal Price { get; set; }
    public required string Currency { get; set; }
    public bool Available { get; set; }
    public required string Status { get; set; }
    public bool IsSuspended { get; set; }
    public int Version { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ProductPhoto? Photo { get; set; }
}

public sealed class ProductPhoto
{
    public Guid ProductId { get; set; }
    public required byte[] Content { get; set; }
}
