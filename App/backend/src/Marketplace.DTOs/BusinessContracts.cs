namespace Marketplace.DTOs;

public sealed record BusinessCategoryResponse(short Id, string Name, string Slug);

public sealed record PublicBusinessResponse(Guid Id, string Name, string Description,
    string Address, string Category, string WhatsApp);
public sealed record PublicBusinessPage(IReadOnlyList<PublicBusinessResponse> Items, bool HasMore);

public sealed record SaveBusinessRequest(
    string? Name,
    string? Description,
    string? Address,
    short CategoryId,
    string? WhatsApp,
    double Latitude,
    double Longitude,
    string? Status);

public sealed record BusinessResponse(
    Guid Id,
    string Name,
    string Description,
    string Address,
    short CategoryId,
    string WhatsApp,
    double Latitude,
    double Longitude,
    string Status);
