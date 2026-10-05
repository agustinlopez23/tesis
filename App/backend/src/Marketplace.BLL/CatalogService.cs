using Marketplace.DTOs;

namespace Marketplace.BLL;

public enum CatalogStatus { Saved, NotFound, PlanRequired, Conflict, Invalid, LimitReached }
public sealed record CatalogResult(CatalogStatus Status, ProductResponse? Product = null);
public interface ICatalogRepository
{
    Task<BusinessPlanResponse?> PlanAsync(Guid businessId, Guid? ownerId, CancellationToken ct);
    Task<CatalogStatus> GrantAsync(Guid businessId, Guid id, Guid admin, GrantSubscriptionRequest request, CancellationToken ct);
    Task<CatalogStatus> RevokeAsync(Guid businessId, Guid id, Guid admin, string reason, CancellationToken ct);
    Task<IReadOnlyList<AdminBusinessResponse>> AdminBusinessesAsync(string query, int page, CancellationToken ct);
    Task<IReadOnlyList<ProductResponse>?> OwnedAsync(Guid businessId, Guid ownerId, CancellationToken ct);
    Task<PublicCatalogResponse?> PublicAsync(Guid businessId, int page, string query, CancellationToken ct);
    Task<CatalogResult> SaveAsync(Guid businessId, Guid productId, Guid ownerId, SaveProductRequest request, CancellationToken ct);
    Task<CatalogResult> PhotoAsync(Guid businessId, Guid productId, Guid ownerId, int version, byte[]? content, CancellationToken ct);
    Task<byte[]?> ReadPhotoAsync(Guid businessId, Guid productId, Guid? ownerId, CancellationToken ct);
}

public sealed class CatalogService(ICatalogRepository repository, IPhotoProcessor photos)
{
    public Task<BusinessPlanResponse?> PlanAsync(Guid id, Guid? owner, CancellationToken ct) => repository.PlanAsync(id, owner, ct);
    public Task<IReadOnlyList<AdminBusinessResponse>> AdminBusinessesAsync(string query, int page, CancellationToken ct) => repository.AdminBusinessesAsync(query, page, ct);
    public Task<CatalogStatus> GrantAsync(Guid business, Guid id, Guid admin, GrantSubscriptionRequest request, CancellationToken ct)
    {
        var reason = request.Reason?.Trim() ?? "";
        // PostgreSQL stores microseconds; normalize before comparing retries and intervals.
        static DateTimeOffset Microseconds(DateTimeOffset value) => new(value.UtcTicks - value.UtcTicks % 10, TimeSpan.Zero);
        request = request with { StartsAt = Microseconds(request.StartsAt), EndsAt = Microseconds(request.EndsAt), Reason = reason };
        if (id == Guid.Empty || reason.Length is < 3 or > 500 || request.EndsAt <= request.StartsAt ||
            request.StartsAt.Year < 2020 || request.EndsAt.Year > 2100)
            return Task.FromResult(CatalogStatus.Invalid);
        return repository.GrantAsync(business, id, admin, request, ct);
    }
    public Task<CatalogStatus> RevokeAsync(Guid business, Guid id, Guid admin, string? reason, CancellationToken ct) =>
        reason?.Trim().Length is >= 3 and <= 500
            ? repository.RevokeAsync(business, id, admin, reason.Trim(), ct) : Task.FromResult(CatalogStatus.Invalid);
    public Task<IReadOnlyList<ProductResponse>?> OwnedAsync(Guid business, Guid owner, CancellationToken ct) => repository.OwnedAsync(business, owner, ct);
    public Task<PublicCatalogResponse?> PublicAsync(Guid business, int page, string query, CancellationToken ct) => repository.PublicAsync(business, page, query, ct);
    public Task<byte[]?> ReadPhotoAsync(Guid business, Guid product, Guid? owner, CancellationToken ct) => repository.ReadPhotoAsync(business, product, owner, ct);
    public Task<CatalogResult> SaveAsync(Guid business, Guid product, Guid owner, SaveProductRequest request, CancellationToken ct)
    {
        var normalized = request with { Name = request.Name?.Trim(), Description = request.Description?.Trim() ?? "", Category = request.Category?.Trim() };
        if (product == Guid.Empty || normalized.Name?.Length is not (>= 1 and <= 160) || normalized.Description.Length > 2000 ||
            normalized.Category?.Length is not (>= 1 and <= 80) || request.Price is < 0 or > 9999999999.99m ||
            decimal.Round(request.Price, 2) != request.Price || request.Currency is not ("ARS" or "USD") ||
            request.Status is not ("draft" or "published" or "archived") || request.Version < 0)
            return Task.FromResult(new CatalogResult(CatalogStatus.Invalid));
        return repository.SaveAsync(business, product, owner, normalized, ct);
    }
    public Task<CatalogResult> PhotoAsync(Guid business, Guid product, Guid owner, int version, byte[]? input, CancellationToken ct)
    {
        if (version < 1 || input?.Length > BusinessProfileService.MaxBytes) return Task.FromResult(new CatalogResult(CatalogStatus.Invalid));
        var processed = input is null ? null : photos.Process(input);
        return input is not null && processed is null ? Task.FromResult(new CatalogResult(CatalogStatus.Invalid))
            : repository.PhotoAsync(business, product, owner, version, processed?.Content, ct);
    }
}
