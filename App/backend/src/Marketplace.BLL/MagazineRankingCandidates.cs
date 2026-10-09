using System.Collections.ObjectModel;
using System.Globalization;
using Marketplace.Models;

namespace Marketplace.BLL;

public sealed record MagazineRankingInput(RankingContext Context, IReadOnlyList<RankingCandidate> Candidates,
    IReadOnlyDictionary<Guid, Guid> OwnerIds);

public sealed class MagazineRankingCandidates(IRankingCandidateRepository repository, TimeProvider clock)
{
    public async Task<MagazineRankingInput> PrepareAsync(string zoneId, short categoryId, int seed, long sequence,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sequence);
        var (key, area) = ResolveScope(zoneId, categoryId);
        cancellationToken.ThrowIfCancellationRequested();
        var rows = await repository.GetEligibleAsync(categoryId, area, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var at = clock.GetUtcNow();
        var candidates = new List<RankingCandidate>(rows.Count);
        var owners = new Dictionary<Guid, Guid>();
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (row.Id == Guid.Empty || row.OwnerId == Guid.Empty || !owners.TryAdd(row.Id, row.OwnerId) ||
                !double.IsFinite(row.DistanceKm) || row.DistanceKm < 0 || row.DistanceKm > area.RadiusKm ||
                (row.PublicationHistoryUnknown ? row.FirstPublishedAt is not null : row.FirstPublishedAt is null) ||
                row.FirstPublishedAt > at)
                throw new InvalidOperationException("Invalid magazine ranking candidate data.");
            candidates.Add(new(row.Id, 1, row.DistanceKm, row.FirstPublishedAt));
        }
        return new(new(key, at, area.RadiusKm, seed, sequence), candidates.AsReadOnly(),
            new ReadOnlyDictionary<Guid, Guid>(owners));
    }

    public static (string Key, DiscoveryArea Area) ResolveScope(string zoneId, short categoryId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(categoryId);
        // Public reference points match discovery-location.ts; changing them requires a new eligibility version.
        DiscoveryArea area = zoneId switch
        {
            "centro" => new(-31.416281, -64.184022, 4),
            "nueva-cordoba" => new(-31.423889, -64.186389, 4),
            "alberdi" => new(-31.4093, -64.19555, 4),
            _ => throw new ArgumentException("Unknown magazine zone.", nameof(zoneId))
        };
        return (string.Create(CultureInfo.InvariantCulture,
            $"magazine/{zoneId}/category/{categoryId}/radius/4/eligibility/v1"), area);
    }
}
