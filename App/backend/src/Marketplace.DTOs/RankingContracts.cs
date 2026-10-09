using System.Text.Json.Serialization;

namespace Marketplace.DTOs;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateRankingSnapshotRequest(Guid RequestId, string? ZoneId, short CategoryId, double RadiusKm);

public sealed record PublicRankingPage(IReadOnlyList<PublicBusinessResponse> Items, DateTimeOffset ExpiresAt,
    string RankingVersion, bool HasMore, string? NextCursor, IReadOnlyDictionary<Guid, string> Measurement,
    string Cursor);
