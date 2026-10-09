using System.Globalization;
using Marketplace.DTOs;
using Marketplace.Models;

namespace Marketplace.BLL;

public sealed record PositionedRankingBusiness(PublicBusinessResponse Business, int Position);
public sealed record RankingSnapshotPage(Guid SnapshotId, DateTimeOffset ExpiresAt, string PolicyHash,
    IReadOnlyList<PositionedRankingBusiness> Items, int? NextOffset);

public interface IRankingPageRepository
{
    Task<IReadOnlyList<PublicBusinessResponse>> ReadAsync(Guid[] ids, short categoryId, DiscoveryArea area, CancellationToken ct);
}

public sealed class RankingSnapshotPages(IRankingSnapshotStore store, IRankingPageRepository repository, TimeProvider clock)
{
    public async Task<RankingSnapshotPage?> ReadAsync(Guid snapshotId, int offset, CancellationToken ct)
    {
        if (snapshotId == Guid.Empty || offset < 0 || offset % 20 != 0)
            throw new ArgumentException("Invalid snapshot page.");
        var snapshot = await store.FindAsync(snapshotId, ct);
        if (snapshot is null) return null;
        if (offset != 0 && offset >= snapshot.BusinessIds.Count)
            throw new ArgumentOutOfRangeException(nameof(offset));
        var parts = snapshot.ContextKey.Split('/');
        if (parts.Length != 8 || !short.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out var category))
            throw new InvalidOperationException("Invalid stored magazine context.");
        var (key, area) = MagazineRankingCandidates.ResolveScope(parts[1], category);
        if (key != snapshot.ContextKey) throw new InvalidOperationException("Unsupported stored magazine context.");
        var ids = snapshot.BusinessIds.Skip(offset).Take(20).ToArray();
        var current = (await repository.ReadAsync(ids, category, area, ct)).ToDictionary(x => x.Id);
        if (snapshot.ExpiresAt <= clock.GetUtcNow()) return null;
        var items = ids.Select((id, index) => (id, position: offset + index + 1))
            .Where(x => current.ContainsKey(x.id))
            .Select(x => new PositionedRankingBusiness(current[x.id], x.position)).ToArray();
        var more = snapshot.BusinessIds.Count - offset > 20;
        return new(snapshot.Id, snapshot.ExpiresAt, snapshot.PolicyHash, Array.AsReadOnly(items), more ? offset + 20 : null);
    }
}
