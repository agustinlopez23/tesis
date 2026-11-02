using System.Security.Cryptography;
using System.Text.Json;

namespace Marketplace.BLL;

public sealed record RankingTrace(int Position, RankingCandidate Candidate, RankedBusiness Components,
    ExposureBalance Previous, double Expected, double Received);

// Private audit data: never serialize this record into a public response.
// PolicyLabel names the published policy (for example equity-v1); null for unnamed preview variants.
public sealed record RankingOpportunity(Guid Id, RankingContext Context, RankingVariant Variant,
    RankingConfiguration Configuration, string PolicyHash, string Reason, IReadOnlyList<RankingTrace> Candidates,
    string? PolicyLabel = null);

public sealed record RankingSnapshot(Guid Id, string ContextKey, DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt, string PolicyHash, IReadOnlyList<Guid> BusinessIds, string? PolicyLabel = null);

public enum SnapshotStatus { Created, Existing, Conflict, Expired }
public sealed record SnapshotResult(SnapshotStatus Status, RankingSnapshot? Snapshot = null);

public interface IRankingSnapshotStore
{
    Task<SnapshotResult> CreateAsync(Guid requestId, string contextKey,
        Func<long, CancellationToken, Task<RankingOpportunity>> build, CancellationToken ct);
    Task<IReadOnlyDictionary<Guid, ExposureBalance>> ReadBalancesAsync(string contextKey, DateTimeOffset at, CancellationToken ct);
    Task<RankingSnapshot?> FindAsync(Guid id, CancellationToken ct);
    Task PurgeAsync(CancellationToken ct);
}

public static class RankingOpportunityBuilder
{
    public static RankingOpportunity Build(MagazineRankingInput input, RankingVariant variant,
        RankingConfiguration configuration, IReadOnlyDictionary<Guid, ExposureBalance> history, Guid? actorId)
    {
        if (input.OwnerIds.Count != input.Candidates.Count || input.Candidates.Any(x =>
            !input.OwnerIds.TryGetValue(x.Id, out var owner) || owner == Guid.Empty))
            throw new ArgumentException("Every candidate needs an internal owner.");
        var ranked = new BusinessRanker(configuration).Rank(input.Candidates, input.Context, variant, history);
        if (ranked.Count != input.Candidates.Count)
            throw new ArgumentException("An opportunity must preserve every eligible candidate.");
        var ownPreview = actorId is { } actor && input.OwnerIds.Values.Contains(actor);
        var expected = ExposureLedger.Targets(ranked.ToDictionary(x => x.Id, x => x.Merit));
        var candidates = input.Candidates.ToDictionary(x => x.Id);
        var trace = ranked.Select((item, index) => new RankingTrace(index + 1, candidates[item.Id], item,
            history.GetValueOrDefault(item.Id, new(0, 0)), ownPreview ? 0 : expected[item.Id],
            !ownPreview && index < 10 ? ExposureLedger.Attention(index + 1) : 0)).ToArray();
        var policy = JsonSerializer.SerializeToUtf8Bytes(new
            { Schema = "magazine-assignment-v1", Configuration = configuration, Variant = variant, input.Context.Seed });
        var hash = Convert.ToHexString(SHA256.HashData(policy)).ToLowerInvariant();
        return new(Guid.NewGuid(), input.Context, variant, configuration, hash,
            ownPreview ? "own-business-preview" : "assigned", Array.AsReadOnly(trace));
    }
}
