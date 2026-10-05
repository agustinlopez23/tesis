using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Marketplace.DTOs;

namespace Marketplace.BLL;

public enum OrderResultStatus { Created, Saved, Invalid, NotFound, Unavailable, Conflict, AttributionUnavailable }
public sealed record OrderResult(OrderResultStatus Status, OrderResponse? Order = null);
public interface IOrderRepository
{
    Task<OrderResult> CreateAsync(Guid business, CreateOrderRequest request, string keyHash, string requestHash, Guid? campaign, CancellationToken ct);
    Task<OrderResponse?> GuestAsync(Guid id, string keyHash, CancellationToken ct);
    Task<OrderResponse?> OwnedAsync(Guid business, Guid id, Guid owner, CancellationToken ct);
    Task<OrderPageResponse?> ListAsync(Guid business, Guid owner, int page, string reference, string? status, CancellationToken ct);
    Task<OrderResult> UpdateAsync(Guid business, Guid id, Guid owner, UpdateOrderRequest request, CancellationToken ct);
}

public sealed class OrderService(IOrderRepository repository, ICampaignTokens campaignTokens)
{
    public static bool ValidKey(string? key) => key?.Length == 64 && key.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
    private static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    public Task<OrderResult> CreateAsync(Guid business, CreateOrderRequest request, CancellationToken ct)
    {
        var name = request.CustomerName?.Trim() ?? "";
        if (request.RequestId == Guid.Empty || !ValidKey(request.AccessKey) || name.Length is < 1 or > 80 || name.Any(char.IsControl) || request.CampaignToken?.Length > 2048 ||
            request.Lines is not { Length: >= 1 and <= 20 } || request.Lines.Any(x => x is null || x.ProductId == Guid.Empty || x.Quantity is < 1 or > 99 || x.ExpectedVersion < 1) ||
            request.Lines.Select(x => x.ProductId).Distinct().Count() != request.Lines.Length)
            return Task.FromResult(new OrderResult(OrderResultStatus.Invalid));
        var normalized = request with { CustomerName = name, Lines = request.Lines.OrderBy(x => x.ProductId).ToArray() };
        // Preserve the pre-campaign fingerprint for existing catalog-only retries.
        object identity = new { BusinessId = business, CustomerName = name, normalized.Lines };
        if (request.CampaignToken != null) identity = new { BusinessId = business, CustomerName = name, normalized.Lines, request.CampaignToken };
        var fingerprint = Hash(JsonSerializer.Serialize(identity));
        return repository.CreateAsync(business, normalized, Hash(request.AccessKey!), fingerprint, campaignTokens.Read(request.CampaignToken)?.CampaignId, ct);
    }
    public Task<OrderResponse?> GuestAsync(Guid id, string? key, CancellationToken ct) => ValidKey(key)
        ? repository.GuestAsync(id, Hash(key!), ct) : Task.FromResult<OrderResponse?>(null);
    public Task<OrderResponse?> OwnedAsync(Guid business, Guid id, Guid owner, CancellationToken ct) => repository.OwnedAsync(business, id, owner, ct);
    public Task<OrderPageResponse?> ListAsync(Guid business, Guid owner, int page, string reference, string? status, CancellationToken ct) =>
        repository.ListAsync(business, owner, page, reference.Trim().ToUpperInvariant(), status, ct);
    public Task<OrderResult> UpdateAsync(Guid business, Guid id, Guid owner, UpdateOrderRequest request, CancellationToken ct) =>
        request.Version < 1 || request.Status is not ("pending" or "in_attention" or "cancelled")
            ? Task.FromResult(new OrderResult(OrderResultStatus.Invalid)) : repository.UpdateAsync(business, id, owner, request, ct);
}
