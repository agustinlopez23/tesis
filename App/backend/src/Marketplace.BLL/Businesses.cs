using System.Text.RegularExpressions;
using Marketplace.DTOs;
using Marketplace.Models;

namespace Marketplace.BLL;

public enum BusinessSaveStatus { Saved, Invalid, NotFound }
public sealed record BusinessSaveResult(BusinessSaveStatus Status, BusinessResponse? Business = null);

public interface IBusinessRepository
{
    Task<IReadOnlyList<PublicBusinessResponse>> GetPublishedAsync(int skip, int take, string? query, string? category, CancellationToken cancellationToken);
    Task<IReadOnlyList<BusinessCategory>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<bool> CategoryExistsAsync(short categoryId, CancellationToken cancellationToken);
    Task<IReadOnlyList<BusinessProfile>> GetOwnedAsync(Guid ownerId, CancellationToken cancellationToken);
    Task<BusinessProfile?> GetOwnedAsync(Guid ownerId, Guid id, CancellationToken cancellationToken);
    Task AddAsync(BusinessProfile business, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
}

public sealed partial class BusinessService(IBusinessRepository repository)
{
    public async Task<PublicBusinessPage> GetPublishedAsync(int page, string? query, string? category, CancellationToken cancellationToken)
    {
        var items = await repository.GetPublishedAsync((page - 1) * 20, 21, query?.Trim(), category?.Trim(), cancellationToken);
        return new(items.Take(20).ToArray(), items.Count > 20);
    }

    public async Task<IReadOnlyList<BusinessCategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken) =>
        (await repository.GetCategoriesAsync(cancellationToken))
            .Select(x => new BusinessCategoryResponse(x.Id, x.Name, x.Slug)).ToArray();

    public async Task<IReadOnlyList<BusinessResponse>> GetOwnedAsync(Guid ownerId, CancellationToken cancellationToken) =>
        (await repository.GetOwnedAsync(ownerId, cancellationToken)).Select(ToResponse).ToArray();

    public async Task<BusinessResponse?> GetOwnedAsync(Guid ownerId, Guid id, CancellationToken cancellationToken)
    {
        var business = await repository.GetOwnedAsync(ownerId, id, cancellationToken);
        return business is null ? null : ToResponse(business);
    }

    public async Task<BusinessSaveResult> CreateAsync(
        Guid ownerId,
        SaveBusinessRequest request,
        CancellationToken cancellationToken)
    {
        var business = Build(ownerId, request);
        if (business is null || !await repository.CategoryExistsAsync(request.CategoryId, cancellationToken))
            return new(BusinessSaveStatus.Invalid);
        await repository.AddAsync(business, cancellationToken);
        return new(BusinessSaveStatus.Saved, ToResponse(business));
    }

    public async Task<BusinessSaveResult> UpdateAsync(
        Guid ownerId,
        Guid id,
        SaveBusinessRequest request,
        CancellationToken cancellationToken)
    {
        var values = Build(ownerId, request);
        if (values is null || !await repository.CategoryExistsAsync(request.CategoryId, cancellationToken))
            return new(BusinessSaveStatus.Invalid);
        var business = await repository.GetOwnedAsync(ownerId, id, cancellationToken);
        if (business is null) return new(BusinessSaveStatus.NotFound);

        business.Name = values.Name;
        business.Description = values.Description;
        business.Address = values.Address;
        business.CategoryId = values.CategoryId;
        business.WhatsApp = values.WhatsApp;
        business.Location = values.Location;
        business.Status = values.Status;
        business.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.SaveAsync(cancellationToken);
        return new(BusinessSaveStatus.Saved, ToResponse(business));
    }

    private static BusinessProfile? Build(Guid ownerId, SaveBusinessRequest request)
    {
        var name = request.Name?.Trim() ?? "";
        var description = request.Description?.Trim() ?? "";
        var address = request.Address?.Trim() ?? "";
        var whatsapp = request.WhatsApp?.Trim() ?? "";
        var status = request.Status?.Trim().ToLowerInvariant() ?? "";
        if (name.Length is < 1 or > 160 || description.Length > 2000 || address.Length > 250 ||
            !WhatsAppPattern().IsMatch(whatsapp) || !double.IsFinite(request.Latitude) ||
            !double.IsFinite(request.Longitude) || request.Latitude is < -90 or > 90 ||
            request.Longitude is < -180 or > 180 || status is not ("draft" or "published"))
            return null;

        return new BusinessProfile
        {
            OwnerId = ownerId,
            Name = name,
            Description = description,
            Address = address,
            CategoryId = request.CategoryId,
            WhatsApp = whatsapp,
            Location = new GeoLocation(request.Latitude, request.Longitude),
            Status = status
        };
    }

    private static BusinessResponse ToResponse(BusinessProfile business) => new(
        business.Id, business.Name, business.Description, business.Address, business.CategoryId,
        business.WhatsApp, business.Location.Latitude, business.Location.Longitude, business.Status);

    [GeneratedRegex("^\\+[0-9]{8,15}$", RegexOptions.CultureInvariant)]
    private static partial Regex WhatsAppPattern();
}
