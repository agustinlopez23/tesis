namespace Marketplace.Models;

public sealed class InventoryBalance
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public int Version { get; set; }
}
public sealed class InventoryMovement
{
    public Guid OperationId { get; set; }
    public Guid ProductId { get; set; }
    public Guid BusinessId { get; set; }
    public Guid ActorId { get; set; }
    public Guid? SaleId { get; set; }
    public int Delta { get; set; }
    public int Balance { get; set; }
    public required string Kind { get; set; }
    public required string Reason { get; set; }
    public required string RequestHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
public sealed class Sale
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid ActorId { get; set; }
    public required string RequestHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? VoidOperationId { get; set; }
    public string? VoidHash { get; set; }
    public string? VoidReason { get; set; }
    public Guid? VoidedBy { get; set; }
    public DateTimeOffset? VoidedAt { get; set; }
    public List<SaleLine> Lines { get; set; } = [];
}
public sealed class SaleLine
{
    public Guid SaleId { get; set; }
    public Guid ProductId { get; set; }
    public required string Name { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public required string Currency { get; set; }
    public required string Origin { get; set; }
    public int ReturnedQuantity { get; set; }
}
