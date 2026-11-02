using Marketplace.BLL;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.DAL;

public sealed class OwnerActivityRepository(MarketplaceDbContext database) : IOwnerActivityRepository
{
    public async Task<bool> RecordAsync(Guid businessId, string kind, DateTimeOffset at, TimeSpan dedupWindow, CancellationToken ct)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(ct);
        // Serializes concurrent saves of the same business and kind so the window holds one row.
        var lockKey = "owner-activity:" + businessId.ToString("D") + ":" + kind;
        await database.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", ct);
        var inserted = await database.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO owner_activity (business_id, kind, occurred_at)
            SELECT {businessId}, {kind}, {at}
            WHERE NOT EXISTS (SELECT 1 FROM owner_activity
                WHERE business_id = {businessId} AND kind = {kind} AND occurred_at > {at - dedupWindow})
            """, ct);
        await transaction.CommitAsync(ct);
        return inserted == 1;
    }
}
