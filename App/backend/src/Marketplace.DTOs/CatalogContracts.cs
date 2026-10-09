namespace Marketplace.DTOs;

public sealed record GrantSubscriptionRequest(DateTimeOffset StartsAt, DateTimeOffset EndsAt, string? Reason);
public sealed record RevokeSubscriptionRequest(string? Reason);
public sealed record SubscriptionResponse(Guid Id, DateTimeOffset StartsAt, DateTimeOffset EndsAt,
    DateTimeOffset GrantedAt, string Reason, DateTimeOffset? RevokedAt, string? RevocationReason);
public sealed record BusinessPlanResponse(bool CatalogEnabled, string Plan, IReadOnlyList<SubscriptionResponse> Periods);
public sealed record SaveProductRequest(string? Name, string? Description, string? Category,
    decimal Price, string? Currency, bool Available, string? Status, int Version);
public sealed record ProductResponse(Guid Id, string Name, string Description, string Category,
    decimal Price, string Currency, bool Available, string Status, int Version, bool HasPhoto);
public sealed record PublicCatalogResponse(IReadOnlyList<ProductResponse> Items, int Page, bool HasNextPage);
public sealed record AdminBusinessResponse(Guid Id, string Name, string OwnerEmail);
