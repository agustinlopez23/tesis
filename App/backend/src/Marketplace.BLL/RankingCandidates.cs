using Marketplace.Models;

namespace Marketplace.BLL;

// Internal retrieval contract, not a public response or an evaluated ranking policy.
public sealed record EligibleRankingBusiness(Guid Id, Guid OwnerId, double DistanceKm,
    DateTimeOffset? FirstPublishedAt, bool PublicationHistoryUnknown);

public interface IRankingCandidateRepository
{
    Task<IReadOnlyList<EligibleRankingBusiness>> GetEligibleAsync(
        short categoryId, DiscoveryArea area, CancellationToken cancellationToken);
}
