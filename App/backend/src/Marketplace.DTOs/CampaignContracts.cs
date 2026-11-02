namespace Marketplace.DTOs;

public sealed record SaveCampaignRequest(int Version, Guid? ProductId, int? PhotoSlot, string? Title, string? Description,
    string? Conditions, string? ZoneId, DateTimeOffset StartsAt, DateTimeOffset EndsAt, string? Status);
public sealed record CampaignActionRequest(Guid OperationId, int Version, string? Action, string? Reason, string? PaymentReference);
public sealed record CampaignResponse(Guid Id, Guid BusinessId, string BusinessName, Guid? ProductId, int? PhotoSlot, bool HasImage,
    string Title, string Description, string Conditions, string ZoneId, DateTimeOffset StartsAt, DateTimeOffset EndsAt,
    string Status, string DeliveryState, int Version, string? PaymentReference, string? RejectionReason = null);
public sealed record PublicCampaign(Guid Id, Guid BusinessId, string BusinessName, Guid? ProductId, bool HasImage, string Title,
    string Description, string Conditions, string ZoneId, DateTimeOffset EndsAt, string WhatsApp, string? Token = null);
public sealed record CampaignEventRequest(string? Token, string? Kind);
public sealed record CampaignMetrics(int EventDays, int Impressions, int Clicks, int Orders, int ActiveSales,
    IReadOnlyDictionary<string, Dictionary<string, decimal>> Amounts, IReadOnlyList<CampaignAuditResponse> Changes);
public sealed record CampaignAuditResponse(int Version, Guid ActorId, string Action, string Reason, DateTimeOffset CreatedAt);
