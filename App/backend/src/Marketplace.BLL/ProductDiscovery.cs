using Marketplace.DTOs;
using Marketplace.Models;

namespace Marketplace.BLL;

// A business that owns at least one product matching the current search.
public sealed record ProductSearchBusiness(Guid Id, short CategoryId, double DistanceKm, DateTimeOffset? FirstPublishedAt);

public interface IProductDiscoveryRepository
{
    // Returns null when more businesses match than the ranking capacity allows.
    Task<IReadOnlyList<ProductSearchBusiness>?> MatchingBusinessesAsync(string query, string category, DiscoveryArea area, int limit, CancellationToken ct);
    Task<ProductDiscoveryResponse> Read(int page, string query, string category, DiscoveryArea? area,
        IReadOnlyList<Guid>? businessOrder, CancellationToken ct);
}

public sealed class ProductDiscoveryService(IProductDiscoveryRepository repository, ProductSearchRanking ranking, RankingReadLimits limits)
{
    public async Task<ProductDiscoveryResponse> Read(int page, string? query, string? category, DiscoveryArea? area,
        string? zone, bool rankingEnabled, CancellationToken ct)
    {
        var text = (query ?? "").Trim(); var rubro = (category ?? "").Trim();
        IReadOnlyList<Guid>? order = null;
        if (rankingEnabled && zone is not null && area is not null)
        {
            var businesses = await repository.MatchingBusinessesAsync(text, rubro, area, limits.MaxCandidates, ct);
            // Over capacity, the search keeps the distance order instead of ranking a partial pool.
            if (businesses is not null)
                try { order = await ranking.OrderAsync(zone, area.RadiusKm, businesses, RankingPolicies.EquityV1, ct); }
                catch (RankingCapacityException) { order = null; }
        }
        var result = await repository.Read(page, text, rubro, area, order, ct);
        return order is null ? result : result with { Policy = RankingPolicies.EquityV1.Label };
    }
}

// Orders the businesses of a product search with the same ranker used by the magazine.
// Stateless: it reads each business's exposure history from its own zone/category ledger and never records exposure.
public sealed class ProductSearchRanking(IRankingSnapshotStore store, TimeProvider clock)
{
    public async Task<IReadOnlyList<Guid>> OrderAsync(string zoneId, double radiusKm,
        IReadOnlyList<ProductSearchBusiness> businesses, RankingPolicy policy, CancellationToken ct)
    {
        var at = clock.GetUtcNow();
        var ids = businesses.Select(x => x.Id).ToHashSet();
        var history = new Dictionary<Guid, ExposureBalance>();
        foreach (var category in businesses.Select(x => x.CategoryId).Distinct().Order())
        {
            var (key, _) = MagazineRankingCandidates.ResolveScope(zoneId, category);
            foreach (var (id, balance) in await store.ReadBalancesAsync(key, at, ct))
                if (ids.Contains(id)) history[id] = balance;
        }
        var context = new RankingContext($"products/{zoneId}", at, radiusKm, policy.Seed, 0);
        var candidates = businesses.Select(x => new RankingCandidate(x.Id, 1, x.DistanceKm, x.FirstPublishedAt));
        return new BusinessRanker(policy.Configuration).Rank(candidates, context, policy.Variant, history)
            .Select(x => x.Id).ToArray();
    }
}
