namespace Marketplace.DTOs;

public sealed record AdminReasonRequest(string? Reason);
public sealed record SaveCategoryRequest(string? Name, bool? IsActive);
public sealed record AdminCategoryResponse(short Id, string Name, string Slug, bool IsActive, int BusinessCount);
public sealed record ModeratedAccountResponse(Guid Id, string Email, string DisplayName, bool IsSuspended, int BusinessCount);
public sealed record ModeratedBusinessResponse(Guid Id, string Name, string OwnerEmail, string Category, string Status, bool IsSuspended);
public sealed record ModeratedProductResponse(Guid Id, Guid BusinessId, string BusinessName, string Name, string Status, bool IsSuspended);
public sealed record AdminActionResponse(long Id, string ActorEmail, string TargetType, string TargetId, string TargetLabel,
    string Action, string? Reason, DateTimeOffset CreatedAt);
public sealed record AdminAuditPage(IReadOnlyList<AdminActionResponse> Items, int Page, bool HasNextPage);
