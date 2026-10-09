using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Marketplace.DTOs;

namespace Marketplace.BLL;

public sealed record SalesResult(int Status, object? Value = null);
public interface ISalesRepository
{
    Task<object?> Inventory(Guid business, Guid owner, CancellationToken ct);
    Task<object?> Movements(Guid business, Guid owner, Guid? product, int page, CancellationToken ct);
    Task<object?> List(Guid business, Guid owner, Guid? order, int page, CancellationToken ct);
    Task<SaleResponse?> Read(Guid business, Guid owner, Guid id, CancellationToken ct);
    Task<SalesResult> Adjust(Guid business, Guid owner, Guid id, AdjustStockRequest body, string hash, CancellationToken ct);
    Task<SalesResult> Create(Guid business, Guid owner, Guid id, CreateSaleRequest body, string hash, CancellationToken ct);
    Task<SalesResult> Void(Guid business, Guid owner, Guid id, VoidSaleRequest body, string hash, CancellationToken ct);
}
public sealed class SalesService(ISalesRepository repository)
{
    private static string Hash(object value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));
    private static bool Reason(string? reason) => reason?.Trim().Length is >= 3 and <= 300 && !reason.Any(char.IsControl);
    public Task<object?> Inventory(Guid b, Guid o, CancellationToken ct) => repository.Inventory(b, o, ct);
    public Task<object?> Movements(Guid b, Guid o, Guid? p, int page, CancellationToken ct) => repository.Movements(b, o, p, page, ct);
    public Task<object?> List(Guid b, Guid o, Guid? order, int page, CancellationToken ct) => repository.List(b, o, order, page, ct);
    public Task<SaleResponse?> Read(Guid b, Guid o, Guid id, CancellationToken ct) => repository.Read(b, o, id, ct);
    public Task<SalesResult> Adjust(Guid b, Guid o, Guid id, AdjustStockRequest body, CancellationToken ct)
    {
        if (id == Guid.Empty || body.ProductId == Guid.Empty || body.Version < 0 || body.Delta is < -1000000 or > 1000000 || !Reason(body.Reason)) return Task.FromResult(new SalesResult(400));
        body = body with { Reason = body.Reason!.Trim() };
        return repository.Adjust(b, o, id, body, Hash(new { b, o, body }), ct);
    }
    public Task<SalesResult> Create(Guid b, Guid o, Guid id, CreateSaleRequest body, CancellationToken ct)
    {
        if (id == Guid.Empty || body.OrderId == Guid.Empty || (body.OrderId != null && body.OrderVersion is not > 0) ||
            (body.OrderId == null && body.OrderVersion != null) || body.Lines is not { Length: >= 1 and <= 20 } ||
            body.Lines.Any(x => x is null || x.ProductId == Guid.Empty || x.Quantity is < 1 or > 99 || x.UnitPrice is < 0 or > 9999999999.99m || decimal.Round(x.UnitPrice, 2) != x.UnitPrice ||
                x.Currency is not ("ARS" or "USD") || (body.OrderId == null ? x.Origin != "direct" : x.Origin is not ("requested" or "conversation"))) ||
            body.Lines.Select(x => x.ProductId).Distinct().Count() != body.Lines.Length) return Task.FromResult(new SalesResult(400));
        body = body with { Lines = body.Lines.OrderBy(x => x.ProductId).ToArray() };
        return repository.Create(b, o, id, body, Hash(new { b, o, body }), ct);
    }
    public Task<SalesResult> Void(Guid b, Guid o, Guid id, VoidSaleRequest body, CancellationToken ct)
    {
        if (body.OperationId == Guid.Empty || !Reason(body.Reason) || body.Returns is not { Length: <= 20 } ||
            body.Returns.Any(x => x is null || x.ProductId == Guid.Empty || x.Quantity is < 1 or > 99) ||
            body.Returns.Select(x => x.ProductId).Distinct().Count() != body.Returns.Length) return Task.FromResult(new SalesResult(400));
        body = body with { Reason = body.Reason!.Trim(), Returns = body.Returns.OrderBy(x => x.ProductId).ToArray() };
        return repository.Void(b, o, id, body, Hash(new { b, o, id, body }), ct);
    }
}
