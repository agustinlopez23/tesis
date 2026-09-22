using Marketplace.BLL;
using Marketplace.Models;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.DAL;

public sealed class RankingCandidateRepository(MarketplaceDbContext database) : IRankingCandidateRepository
{
    public async Task<IReadOnlyList<EligibleRankingBusiness>> GetEligibleAsync(
        short categoryId, DiscoveryArea area, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(categoryId);
        ArgumentNullException.ThrowIfNull(area);
        if (!area.IsValid) throw new ArgumentException("Invalid discovery area.", nameof(area));
        cancellationToken.ThrowIfCancellationRequested();

        // A single statement sees one database snapshot. No pagination or silent cap.
        var candidates = await database.Database.SqlQuery<EligibleRankingBusiness>($"""
            SELECT b.id AS "Id", b.owner_id AS "OwnerId",
                ST_Distance(b.location, ST_SetSRID(ST_MakePoint({area.Longitude}, {area.Latitude}), 4326)::geography) / 1000 AS "DistanceKm",
                b.first_published_at AS "FirstPublishedAt",
                b.publication_history_unknown AS "PublicationHistoryUnknown"
            FROM businesses b
            JOIN user_accounts u ON u.id = b.owner_id
            WHERE b.status = 'published' AND NOT u.is_suspended
              AND b.category_id = {categoryId}
              AND ST_DWithin(b.location, ST_SetSRID(ST_MakePoint({area.Longitude}, {area.Latitude}), 4326)::geography, {area.RadiusKm * 1000})
            """).OrderBy(x => x.Id).ToListAsync(cancellationToken);

        var ids = new HashSet<Guid>();
        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (candidate.Id == Guid.Empty || candidate.OwnerId == Guid.Empty || !ids.Add(candidate.Id) ||
                !double.IsFinite(candidate.DistanceKm) || candidate.DistanceKm < 0 ||
                (candidate.PublicationHistoryUnknown ? candidate.FirstPublishedAt is not null : candidate.FirstPublishedAt is null))
                throw new InvalidOperationException("Invalid ranking candidate data.");
        }
        return candidates.AsReadOnly();
    }
}
