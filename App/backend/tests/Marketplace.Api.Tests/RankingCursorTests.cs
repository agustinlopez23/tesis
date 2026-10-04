using System.Text.Json;
using Marketplace.Api;
using Microsoft.AspNetCore.DataProtection;

namespace Marketplace.Api.Tests;

public sealed class RankingCursorTests
{
    private readonly EphemeralDataProtectionProvider provider = new();
    private readonly CursorClock clock = new();

    [Fact]
    public void Roundtrip_preserves_absolute_expiry_and_expires_exactly_without_renewing()
    {
        var tokens = new RankingCursorTokens(provider, clock);
        var cursor = new RankingCursor(Guid.NewGuid(), 20, clock.Now.AddMinutes(30));
        var token = tokens.Issue(cursor);
        Assert.Equal(cursor, tokens.Read(token).Cursor);
        clock.Now = cursor.ExpiresAt.AddTicks(-1);
        Assert.Equal(RankingCursorStatus.Valid, tokens.Read(token).Status);
        var next = tokens.Issue(cursor with { Offset = 40 });
        clock.Now = cursor.ExpiresAt;
        Assert.Equal(RankingCursorStatus.Expired, tokens.Read(token).Status);
        Assert.Equal(RankingCursorStatus.Expired, tokens.Read(next).Status);
    }

    [Fact]
    public void Rejects_tampering_other_purposes_and_oversized_input()
    {
        var tokens = new RankingCursorTokens(provider, clock);
        var token = tokens.Issue(new(Guid.NewGuid(), 20, clock.Now.AddMinutes(30)));
        var altered = token[..(token.Length / 2)] + "!" + token[(token.Length / 2 + 1)..];
        var unrelated = provider.CreateProtector("Cerca.Activity.v1").Protect("{}");
        foreach (var invalid in new[] { null, "", "bad", altered, unrelated, new string('a', 2049) })
            Assert.Equal(RankingCursorStatus.Invalid, tokens.Read(invalid).Status);
        Assert.Equal(RankingCursorStatus.Invalid,
            new RankingCursorTokens(new EphemeralDataProtectionProvider(), clock).Read(token).Status);
    }

    [Fact]
    public void Rejects_authenticated_payloads_with_invalid_schema()
    {
        var tokens = new RankingCursorTokens(provider, clock);
        var valid = new RankingCursor(Guid.NewGuid(), 20, clock.Now.AddMinutes(30));
        var protector = provider.CreateProtector("Cerca.RankingCursor.v1");
        foreach (var bad in new[] { valid with { SnapshotId = Guid.Empty }, valid with { Offset = -20 },
            valid with { Offset = 1 }, valid with { PageSize = 10 }, valid with { Version = 2 },
            valid with { ExpiresAt = default } })
        {
            Assert.Equal(RankingCursorStatus.Invalid, tokens.Read(protector.Protect(JsonSerializer.Serialize(bad))).Status);
            Assert.Throws<ArgumentException>(() => tokens.Issue(bad));
        }
        foreach (var malformed in new[] { "null", "{}", "[]", "not-json" })
            Assert.Equal(RankingCursorStatus.Invalid, tokens.Read(protector.Protect(malformed)).Status);
    }

    private sealed class CursorClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
