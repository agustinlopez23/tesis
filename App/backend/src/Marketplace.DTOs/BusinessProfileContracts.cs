namespace Marketplace.DTOs;

public sealed record BusinessPhotoResponse(int Slot, string Caption, int Width, int Height);
public sealed record PublicBusinessProfileResponse(Guid Id, string Name, string Description,
    string Address, string Category, string WhatsApp, IReadOnlyList<BusinessPhotoResponse> Photos)
{
    public string? MeasurementToken { get; init; }
    // True while an active Impulso plan exposes the public catalog; lets clients skip a request that would 404.
    public bool HasPublicCatalog { get; init; }
    public IReadOnlyList<PublicCampaign> Promotions { get; init; } = [];
}
