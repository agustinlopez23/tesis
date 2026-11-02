using Marketplace.BLL;
using Marketplace.DTOs;
using Marketplace.Models;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.DAL;

public sealed class ActivityRepository(MarketplaceDbContext database) : IActivityRepository
{
    public async Task RecordAsync(BusinessActivity entry, Guid? actorId, CancellationToken ct)
    {
        await database.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO business_activity (visit_id, business_id, kind, surface, source, position, ranking_version, occurred_at)
            SELECT {entry.VisitId}, b.id, {entry.Kind}, {entry.Surface}, {entry.Source},
                {entry.Position}, {entry.RankingVersion}, {entry.OccurredAt}
            FROM businesses b JOIN user_accounts u ON u.id = b.owner_id
            WHERE b.id = {entry.BusinessId} AND b.status = 'published' AND NOT u.is_suspended AND NOT b.is_suspended
                AND b.owner_id IS DISTINCT FROM {actorId}
            ON CONFLICT (visit_id, business_id, kind, surface) DO NOTHING
            """, ct);
    }

    public async Task<IReadOnlyList<DailyActivityResponse>> ReadDailyAsync(Guid id, DateTimeOffset from, DateTimeOffset until, CancellationToken ct) =>
        await database.Database.SqlQuery<DailyActivityResponse>($"""
            SELECT date(timezone('America/Argentina/Cordoba', occurred_at)) AS "Date",
                count(*) FILTER (WHERE kind = 'impression')::integer AS "Impressions",
                count(*) FILTER (WHERE kind = 'profile_view')::integer AS "ProfileViews",
                count(*) FILTER (WHERE kind = 'contact')::integer AS "Contacts"
            FROM business_activity
            WHERE business_id = {id} AND occurred_at >= {from} AND occurred_at <= {until}
                AND occurred_at >= {until.AddDays(-30)}
            GROUP BY date(timezone('America/Argentina/Cordoba', occurred_at))
            """).ToArrayAsync(ct);

    public Task<int> PurgeAsync(DateTimeOffset before, CancellationToken ct) =>
        database.BusinessActivity.Where(x => x.OccurredAt < before).ExecuteDeleteAsync(ct);
}
