namespace Marketplace.DTOs;

public sealed record OrderLineRequest(Guid ProductId, int Quantity, int ExpectedVersion);
public sealed record CreateOrderRequest(Guid RequestId, string? AccessKey, string? CustomerName, OrderLineRequest[]? Lines, string? CampaignToken = null);
public sealed record UpdateOrderRequest(int Version, string? Status, bool ContactReceived);
public sealed record OrderLineResponse(Guid ProductId, string Name, decimal UnitPrice, string Currency, int Quantity);
public sealed record OrderResponse(Guid Id, Guid BusinessId, string Reference, string CustomerName,
    string BusinessName, string WhatsApp, DateTimeOffset CreatedAt, DateTimeOffset AccessExpiresAt,
    string Status, int Version, DateTimeOffset? ContactReceivedAt, bool CanContact,
    IReadOnlyList<OrderLineResponse> Lines, IReadOnlyDictionary<string, decimal> Totals);
public sealed record OrderSummaryResponse(Guid Id, string Reference, string CustomerName, DateTimeOffset CreatedAt,
    string Status, DateTimeOffset? ContactReceivedAt);
public sealed record OrderPageResponse(IReadOnlyList<OrderSummaryResponse> Items, int Page, bool HasNextPage);
