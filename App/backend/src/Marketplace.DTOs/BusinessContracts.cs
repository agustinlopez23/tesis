namespace Marketplace.DTOs;

public sealed record BusinessCategoryResponse(short Id, string Name, string Slug);

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
