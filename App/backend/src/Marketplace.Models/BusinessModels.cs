namespace Marketplace.Models;

public sealed record GeoLocation(double Latitude, double Longitude);

public sealed record DiscoveryArea(double Latitude, double Longitude, double RadiusKm)
{
    public bool IsValid => double.IsFinite(Latitude) && double.IsFinite(Longitude) &&
        double.IsFinite(RadiusKm) && Latitude is >= -90 and <= 90 &&
        Longitude is >= -180 and <= 180 && RadiusKm is >= 0.1 and <= 50;
}

public sealed class BusinessCategory
{
    public short Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public bool IsActive { get; set; } = true;
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
    public bool IsSuspended { get; set; }
    // Database-maintained history: null/false = never published, null/true = unknown.
    public DateTimeOffset? FirstPublishedAt { get; private set; }
    public bool PublicationHistoryUnknown { get; private set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
