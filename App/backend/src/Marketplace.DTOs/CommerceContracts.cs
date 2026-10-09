namespace Marketplace.DTOs;

public sealed record CommerceResponse(int Days, DateTimeOffset From, DateTimeOffset AsOf, string TimeZone,
    int Orders, int ContactsReceived, int Converted, decimal? ConversionRate, int ActiveSales, int VoidedSales,
    IReadOnlyDictionary<string, Dictionary<string, decimal>> Amounts);
