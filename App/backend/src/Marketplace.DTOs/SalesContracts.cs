namespace Marketplace.DTOs;

public sealed record AdjustStockRequest(Guid ProductId, int Delta, int Version, string? Reason);
public sealed record SaleLineRequest(Guid ProductId, int Quantity, decimal UnitPrice, string? Currency, string? Origin);
public sealed record CreateSaleRequest(Guid? OrderId, int? OrderVersion, SaleLineRequest[]? Lines);
public sealed record ReturnLineRequest(Guid ProductId, int Quantity);
public sealed record VoidSaleRequest(Guid OperationId, string? Reason, ReturnLineRequest[]? Returns);
public sealed record InventoryItem(Guid ProductId, string Name, int? Quantity, int Version);
public sealed record MovementResponse(Guid OperationId, Guid ProductId, string ProductName, int Delta, int Balance,
    string Kind, string Reason, Guid ActorId, Guid? SaleId, DateTimeOffset CreatedAt);
public sealed record SaleLineResponse(Guid ProductId, string Name, int Quantity, decimal UnitPrice, string Currency, string Origin, int ReturnedQuantity);
public sealed record SaleResponse(Guid Id, Guid? OrderId, string? OrderReference, Guid ActorId, DateTimeOffset CreatedAt,
    DateTimeOffset? VoidedAt, string? VoidReason, Guid? VoidedBy, IReadOnlyList<SaleLineResponse> Lines,
    IReadOnlyDictionary<string, Dictionary<string, decimal>> Totals);
public sealed record SalesPage<T>(IReadOnlyList<T> Items, int Page, bool HasNextPage);
