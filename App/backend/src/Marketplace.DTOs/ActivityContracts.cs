namespace Marketplace.DTOs;

public sealed record RecordActivityRequest(string? Token, Guid VisitId, string? Kind, string? Surface);
public sealed record DailyActivityResponse(DateOnly Date, int Impressions, int ProfileViews, int Contacts);
public sealed record ActivityResponse(int Days, DateTimeOffset UpdatedAt, string TimeZone,
    int Impressions, int ProfileViews, int Contacts, IReadOnlyList<DailyActivityResponse> Daily);
