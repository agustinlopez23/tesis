namespace Marketplace.BLL;

public sealed class RankingSnapshotService(MagazineRankingCandidates candidates, IRankingSnapshotStore store)
{
    public Task<SnapshotResult> CreateAsync(Guid requestId, string zoneId, short categoryId,
        RankingPolicy policy, Guid? actorId, CancellationToken ct) =>
        CreateAsync(requestId, zoneId, categoryId, policy.Variant, policy.Configuration, policy.Seed, actorId, ct, policy.Label);

    public Task<SnapshotResult> CreateAsync(Guid requestId, string zoneId, short categoryId,
        RankingVariant variant, RankingConfiguration configuration, int seed, Guid? actorId, CancellationToken ct,
        string? policyLabel = null)
    {
        var (key, _) = MagazineRankingCandidates.ResolveScope(zoneId, categoryId);
        return store.CreateAsync(requestId, key, async (sequence, token) =>
        {
            var input = await candidates.PrepareAsync(zoneId, categoryId, seed, sequence, token);
            var history = await store.ReadBalancesAsync(key, input.Context.At, token);
            return RankingOpportunityBuilder.Build(input, variant, configuration, history, actorId) with { PolicyLabel = policyLabel };
        }, ct);
    }
}
