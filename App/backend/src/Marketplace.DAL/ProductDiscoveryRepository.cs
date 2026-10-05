using Marketplace.BLL;
using Marketplace.DTOs;
using Marketplace.Models;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.DAL;

public sealed class ProductDiscoveryRepository(MarketplaceDbContext db, TimeProvider clock) : IProductDiscoveryRepository
{
    public async Task<ProductDiscoveryResponse> Read(int page, string query, string category, DiscoveryArea? area, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var folded = query.ToLowerInvariant().Replace('á', 'a').Replace('é', 'e').Replace('í', 'i').Replace('ó', 'o').Replace('ú', 'u').Replace('ü', 'u');
        var pattern = "%" + folded.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
        var located = area != null; var lat = area?.Latitude ?? 0; var lon = area?.Longitude ?? 0; var radius = (area?.RadiusKm ?? 1) * 1000;
        var rows = db.Database.SqlQuery<ProductRow>($"""
            SELECT p.id AS "Id", p.business_id AS "BusinessId", b.name AS "BusinessName", c.name AS "BusinessCategory",
                p.name AS "Name", p.description AS "Description", p.category AS "Category", p.price AS "Price",
                p.currency AS "Currency", p.version AS "Version", EXISTS (SELECT 1 FROM product_photos f WHERE f.product_id=p.id) AS "HasPhoto",
                CASE WHEN {located} THEN ST_Distance(b.location,ST_SetSRID(ST_MakePoint({lon},{lat}),4326)::geography)/1000 ELSE NULL END AS "DistanceKm"
            FROM catalog_products p JOIN businesses b ON b.id=p.business_id
            JOIN user_accounts u ON u.id=b.owner_id JOIN business_categories c ON c.id=b.category_id
            WHERE p.status='published' AND p.available AND b.status='published' AND NOT u.is_suspended
                AND EXISTS (SELECT 1 FROM business_subscriptions s WHERE s.business_id=b.id AND s.revoked_at IS NULL AND s.starts_at <= {now} AND s.ends_at > {now})
                AND NOT EXISTS (SELECT 1 FROM inventory_balances i WHERE i.product_id=p.id AND i.quantity=0)
                AND ({category}='' OR c.name={category})
                AND translate(lower(p.name || ' ' || p.description || ' ' || p.category || ' ' || b.name),'áéíóúü','aeiouu') LIKE {pattern} ESCAPE '\'
                AND (NOT {located} OR ST_DWithin(b.location,ST_SetSRID(ST_MakePoint({lon},{lat}),4326)::geography,{radius}))
            """);
        var ordered = located ? rows.OrderBy(p => p.DistanceKm).ThenBy(p => p.Name).ThenBy(p => p.Id) : rows.OrderBy(p => p.Name).ThenBy(p => p.Id);
        var items = await ordered.Skip((page - 1) * 20).Take(21).ToListAsync(ct);
        return new(items.Take(20).Select(p => new DiscoveredProduct(p.Id, p.BusinessId, p.BusinessName, p.BusinessCategory, p.Name, p.Description,
            p.Category, p.Price, p.Currency, p.Version, p.HasPhoto, p.DistanceKm)).ToArray(), page, items.Count > 20, located ? "distance" : "name");
    }
    private sealed class ProductRow
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public required string BusinessName { get; set; }
        public required string BusinessCategory { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string Category { get; set; }
        public decimal Price { get; set; }
        public required string Currency { get; set; }
        public int Version { get; set; }
        public bool HasPhoto { get; set; }
        public double? DistanceKm { get; set; }
    }
}
