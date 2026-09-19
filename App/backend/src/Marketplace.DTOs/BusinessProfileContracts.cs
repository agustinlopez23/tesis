namespace Marketplace.DTOs;

public sealed record BusinessPhotoResponse(int Slot, string Caption, int Width, int Height);
public sealed record PublicBusinessProfileResponse(Guid Id, string Name, string Description,
    string Address, string Category, string WhatsApp, IReadOnlyList<BusinessPhotoResponse> Photos);
