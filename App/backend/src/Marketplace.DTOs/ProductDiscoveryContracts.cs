namespace Marketplace.DTOs;

public sealed record DiscoveredProduct(Guid Id, Guid BusinessId, string BusinessName, string BusinessCategory,
    string Name, string Description, string Category, decimal Price, string Currency, int Version, bool HasPhoto, double? DistanceKm);
public sealed record ProductDiscoveryResponse(IReadOnlyList<DiscoveredProduct> Items, int Page, bool HasNextPage, string Order);
