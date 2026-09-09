namespace Marketplace.Models;

public sealed record GeoLocation(double Latitude, double Longitude);

public sealed class BusinessCategory
{
    public short Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
}

public sealed class BusinessProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public short CategoryId { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Address { get; set; }
    public required string WhatsApp { get; set; }
    public required GeoLocation Location { get; set; }
    public required string Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
