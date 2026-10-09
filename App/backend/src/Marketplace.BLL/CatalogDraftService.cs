using Marketplace.DTOs;

namespace Marketplace.BLL;

public interface ICatalogDraftProvider
{
    bool Enabled { get; }
    Task<CatalogDraft?> Generate(string facts, CancellationToken ct);
}
public sealed class CatalogDraftService(ICatalogRepository catalog, ICatalogDraftProvider provider)
{
    public async Task<CatalogDraftAvailability?> Availability(Guid business, Guid owner, CancellationToken ct)
    {
        var plan = await catalog.PlanAsync(business, owner, ct);
        return plan == null ? null : new(provider.Enabled && plan.CatalogEnabled, provider.Enabled ? "OpenAI" : null);
    }
    public async Task<(int Status, CatalogDraft? Draft)> Generate(Guid business, Guid owner, CatalogDraftRequest request, CancellationToken ct)
    {
        var plan = await catalog.PlanAsync(business, owner, ct);
        if (plan == null) return (404, null);
        if (!plan.CatalogEnabled) return (403, null);
        if (!request.Consent || request.Facts?.Trim().Length is not (>= 3 and <= 1500)) return (400, null);
        if (!provider.Enabled) return (503, null);
        var draft = await provider.Generate(request.Facts.Trim(), ct);
        if (draft == null || !Valid(draft.Name, 160) || !Valid(draft.Description, 2000) || !Valid(draft.Category, 80)) return (503, null);
        // A plan may expire during generation; publication always goes through normal catalog validation.
        if ((await catalog.PlanAsync(business, owner, ct))?.CatalogEnabled != true) return (403, null);
        return (200, draft with { Name = draft.Name.Trim(), Description = draft.Description.Trim(), Category = draft.Category.Trim() });
    }
    private static bool Valid(string? text, int max) => text?.Trim().Length >= 1 && text.Length <= max && !text.Any(c => char.IsControl(c) && c is not ('\n' or '\r' or '\t'));
}
