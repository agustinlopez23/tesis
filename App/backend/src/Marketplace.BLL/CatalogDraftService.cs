using Marketplace.DTOs;

namespace Marketplace.BLL;

/// <summary>Strategy port for catalog AI providers; adapters live in Marketplace.Integrations.</summary>
public interface ICatalogDraftProvider
{
    /// <summary>Provider name shown to the owner before consenting.</summary>
    string Name { get; }
    bool Enabled { get; }
    /// <param name="jpegPhoto">Optional re-encoded JPEG of the product; never the raw upload.</param>
    Task<CatalogDraft?> Generate(string facts, byte[]? jpegPhoto, CancellationToken ct);
}
public sealed record CatalogDraftResult(int Status, CatalogDraft? Draft = null, bool InvalidPhoto = false);
public sealed class CatalogDraftService(ICatalogRepository catalog, ICatalogDraftProvider provider, IPhotoProcessor photos)
{
    // Vision input is downscaled to keep the provider request small; it is never stored.
    public const int PhotoMaxSide = 1024;
    public async Task<CatalogDraftAvailability?> Availability(Guid business, Guid owner, CancellationToken ct)
    {
        var plan = await catalog.PlanAsync(business, owner, ct);
        return plan == null ? null : new(provider.Enabled && plan.CatalogEnabled, provider.Enabled ? provider.Name : null);
    }
    public async Task<CatalogDraftResult> Generate(Guid business, Guid owner, CatalogDraftRequest request, byte[]? photo, CancellationToken ct)
    {
        var plan = await catalog.PlanAsync(business, owner, ct);
        if (plan == null) return new(404);
        if (!plan.CatalogEnabled) return new(403);
        if (!request.Consent || request.Facts?.Trim().Length is not (>= 3 and <= 1500)) return new(400);
        byte[]? jpeg = null;
        if (photo != null)
        {
            if (photo.Length > BusinessProfileService.MaxBytes) return new(413);
            jpeg = photos.Process(photo, PhotoMaxSide)?.Content;
            if (jpeg == null) return new(400, InvalidPhoto: true);
        }
        if (!provider.Enabled) return new(503);
        var draft = await provider.Generate(request.Facts.Trim(), jpeg, ct);
        if (draft == null || !Valid(draft.Name, 160) || !Valid(draft.Description, 2000) || !Valid(draft.Category, 80)) return new(503);
        // A plan may expire during generation; publication always goes through normal catalog validation.
        if ((await catalog.PlanAsync(business, owner, ct))?.CatalogEnabled != true) return new(403);
        return new(200, draft with { Name = draft.Name.Trim(), Description = draft.Description.Trim(), Category = draft.Category.Trim() });
    }
    private static bool Valid(string? text, int max) => text?.Trim().Length >= 1 && text.Length <= max && !text.Any(c => char.IsControl(c) && c is not ('\n' or '\r' or '\t'));
}
