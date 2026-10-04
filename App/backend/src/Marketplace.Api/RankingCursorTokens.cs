using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Marketplace.Api;

public sealed record RankingCursor(Guid SnapshotId, int Offset, DateTimeOffset ExpiresAt, int PageSize = 20, int Version = 1);
public enum RankingCursorStatus { Valid, Invalid, Expired }
public sealed record RankingCursorResult(RankingCursorStatus Status, RankingCursor? Cursor = null);

public sealed class RankingCursorTokens(IDataProtectionProvider provider, TimeProvider clock)
{
    private readonly IDataProtector protector = provider.CreateProtector("Cerca.RankingCursor.v1");

    public string Issue(RankingCursor cursor)
    {
        if (!IsValid(cursor)) throw new ArgumentException("Invalid ranking cursor.", nameof(cursor));
        return protector.Protect(JsonSerializer.Serialize(cursor));
    }

    public RankingCursorResult Read(string? token)
    {
        if (string.IsNullOrEmpty(token) || token.Length > 2048) return new(RankingCursorStatus.Invalid);
        try
        {
            var cursor = JsonSerializer.Deserialize<RankingCursor>(protector.Unprotect(token));
            if (!IsValid(cursor)) return new(RankingCursorStatus.Invalid);
            // Authenticate first: an expired authentic token is Gone, a forgery is Bad Request.
            return cursor!.ExpiresAt <= clock.GetUtcNow()
                ? new(RankingCursorStatus.Expired) : new(RankingCursorStatus.Valid, cursor);
        }
        catch (CryptographicException) { return new(RankingCursorStatus.Invalid); }
        catch (JsonException) { return new(RankingCursorStatus.Invalid); }
    }

    private static bool IsValid(RankingCursor? cursor) => cursor is not null &&
        cursor.SnapshotId != Guid.Empty && cursor.Offset >= 0 && cursor.Offset % 20 == 0 &&
        cursor.PageSize == 20 && cursor.Version == 1 && cursor.ExpiresAt != default;
}
