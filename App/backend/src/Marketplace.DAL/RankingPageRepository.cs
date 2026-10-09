using Marketplace.BLL;
using Marketplace.DTOs;
using Marketplace.Models;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.DAL;

public sealed class RankingPageRepository(MarketplaceDbContext database) : IRankingPageRepository
{
    public async Task<IReadOnlyList<PublicBusinessResponse>> ReadAsync(Guid[] ids, short categoryId, DiscoveryArea area, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(ids);
        ArgumentNullException.ThrowIfNull(area);
        if (ids.Length > 20 || ids.Contains(Guid.Empty) || ids.Distinct().Count() != ids.Length ||
            categoryId <= 0 || !area.IsValid) throw new ArgumentException("Invalid ranking page scope.");
        // Revalidate these positions in one statement; never fill gaps from later positions.
        return await database.Database.SqlQuery<PublicBusinessResponse>($"""
            SELECT b.id AS "Id", b.name AS "Name", b.description AS "Description", b.address AS "Address",
                c.name AS "Category", b.whatsapp AS "WhatsApp",
                ST_Distance(b.location, ST_SetSRID(ST_MakePoint({area.Longitude}, {area.Latitude}), 4326)::geography) / 1000 AS "DistanceKm",
                (SELECT min(p.slot)::integer FROM business_photos p WHERE p.business_id = b.id) AS "CoverPhotoSlot"
            FROM businesses b
            JOIN user_accounts u ON u.id = b.owner_id
            JOIN business_categories c ON c.id = b.category_id
            WHERE b.id = ANY({ids}) AND b.status = 'published' AND NOT u.is_suspended
                AND b.category_id = {categoryId}
                AND ST_DWithin(b.location, ST_SetSRID(ST_MakePoint({area.Longitude}, {area.Latitude}), 4326)::geography, {area.RadiusKm * 1000})
            """).ToListAsync(ct);
    }
}
