using Marketplace.BLL;
using Marketplace.DTOs;
using Marketplace.Models;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.DAL;

public sealed class ProductDiscoveryRepository(MarketplaceDbContext db, TimeProvider clock, CatalogSettings settings) : IProductDiscoveryRepository
{
    // Typo tolerance: trigram word similarity (pg_trgm) plus a bounded edit distance (fuzzystrmatch) for one-word queries.
    private const double SimilarityThreshold = 0.5;

    public async Task<IReadOnlyList<ProductSearchBusiness>?> MatchingBusinessesAsync(string query, string category, DiscoveryArea area, int limit, CancellationToken ct)
    {
        var rows = await Rows(query, category, area, [])
            .Select(p => new { p.BusinessId, p.CategoryId, p.DistanceKm, p.FirstPublishedAt }).Distinct()
            .OrderBy(x => x.BusinessId).Take(limit + 1).ToListAsync(ct);
        if (rows.Count > limit) return null;
        return rows.Select(x => new ProductSearchBusiness(x.BusinessId, x.CategoryId, x.DistanceKm ?? 0, x.FirstPublishedAt)).ToArray();
    }

    public async Task<ProductDiscoveryResponse> Read(int page, string query, string category, DiscoveryArea? area,
        IReadOnlyList<Guid>? businessOrder, CancellationToken ct)
    {
        var rows = Rows(query, category, area, businessOrder?.ToArray() ?? []);
        var ordered = businessOrder is not null
            ? rows.OrderBy(p => p.RankPosition).ThenByDescending(p => p.Relevance).ThenBy(p => p.Name).ThenBy(p => p.Id)
            : area is not null ? rows.OrderBy(p => p.DistanceKm).ThenBy(p => p.Name).ThenBy(p => p.Id) : rows.OrderBy(p => p.Name).ThenBy(p => p.Id);
        var items = await ordered.Skip((page - 1) * 20).Take(21).ToListAsync(ct);
        return new(items.Take(20).Select(p => new DiscoveredProduct(p.Id, p.BusinessId, p.BusinessName, p.BusinessCategory, p.Name, p.Description,
            p.Category, p.Price, p.Currency, p.Version, p.HasPhoto, p.DistanceKm, p.LowStock)).ToArray(), page, items.Count > 20,
            businessOrder is not null ? "ranking" : area is not null ? "distance" : "name");
    }

    private IQueryable<ProductRow> Rows(string query, string category, DiscoveryArea? area, Guid[] businessOrder)
    {
        var now = clock.GetUtcNow();
        var folded = query.ToLowerInvariant().Replace('á', 'a').Replace('é', 'e').Replace('í', 'i').Replace('ó', 'o').Replace('ú', 'u').Replace('ü', 'u');
        var pattern = "%" + folded.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
        var fuzzy = folded.Length is >= 4 and <= 60;
        var oneWord = fuzzy && folded.Length <= 40 && folded.All(char.IsLetterOrDigit);
        var maxEdits = folded.Length >= 5 ? 2 : 1;
        var threshold = settings.LowStockThreshold;
        var located = area != null; var lat = area?.Latitude ?? 0; var lon = area?.Longitude ?? 0; var radius = (area?.RadiusKm ?? 1) * 1000;
        return db.Database.SqlQuery<ProductRow>($"""
            SELECT m.*, array_position({businessOrder}::uuid[], m."BusinessId") AS "RankPosition"
            FROM (
                SELECT p.id AS "Id", p.business_id AS "BusinessId", b.name AS "BusinessName", c.name AS "BusinessCategory",
                    b.category_id AS "CategoryId", b.first_published_at AS "FirstPublishedAt",
                    p.name AS "Name", p.description AS "Description", p.category AS "Category", p.price AS "Price",
                    p.currency AS "Currency", p.version AS "Version", EXISTS (SELECT 1 FROM product_photos f WHERE f.product_id=p.id) AS "HasPhoto",
                    EXISTS (SELECT 1 FROM inventory_balances i WHERE i.product_id=p.id AND i.quantity > 0 AND i.quantity < {threshold}) AS "LowStock",
                    CASE WHEN {located} THEN ST_Distance(b.location,ST_SetSRID(ST_MakePoint({lon},{lat}),4326)::geography)/1000 ELSE NULL END AS "DistanceKm",
                    CASE WHEN translate(lower(p.name || ' ' || p.description || ' ' || p.category || ' ' || b.name),'áéíóúü','aeiouu') LIKE {pattern} ESCAPE '\' THEN 1.0::float8
                        ELSE word_similarity({folded}, translate(lower(p.name || ' ' || p.description),'áéíóúü','aeiouu'))::float8 END AS "Relevance"
                FROM catalog_products p JOIN businesses b ON b.id=p.business_id
                JOIN user_accounts u ON u.id=b.owner_id JOIN business_categories c ON c.id=b.category_id
                WHERE p.status='published' AND NOT p.is_suspended AND p.available AND b.status='published' AND NOT u.is_suspended AND NOT b.is_suspended
                    AND EXISTS (SELECT 1 FROM business_subscriptions s WHERE s.business_id=b.id AND s.revoked_at IS NULL AND s.starts_at <= {now} AND s.ends_at > {now})
                    AND NOT EXISTS (SELECT 1 FROM inventory_balances i WHERE i.product_id=p.id AND i.quantity=0)
                    AND ({category}='' OR c.name={category})
                    AND (translate(lower(p.name || ' ' || p.description || ' ' || p.category || ' ' || b.name),'áéíóúü','aeiouu') LIKE {pattern} ESCAPE '\'
                        OR ({fuzzy} AND word_similarity({folded}, translate(lower(p.name || ' ' || p.description),'áéíóúü','aeiouu')) >= {SimilarityThreshold})
                        OR ({oneWord} AND EXISTS (
                            SELECT 1 FROM regexp_split_to_table(translate(lower(p.name || ' ' || p.description),'áéíóúü','aeiouu'), '[^a-z0-9ñ]+') w
                            WHERE length(w) BETWEEN 3 AND 40 AND left(w, 1) = left({folded}, 1)
                                AND levenshtein_less_equal(w, {folded}, {maxEdits}) <= {maxEdits})))
                    AND (NOT {located} OR ST_DWithin(b.location,ST_SetSRID(ST_MakePoint({lon},{lat}),4326)::geography,{radius}))
            ) m
            """);
    }

    private sealed class ProductRow
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public required string BusinessName { get; set; }
        public required string BusinessCategory { get; set; }
        public short CategoryId { get; set; }
        public DateTimeOffset? FirstPublishedAt { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string Category { get; set; }
        public decimal Price { get; set; }
        public required string Currency { get; set; }
        public int Version { get; set; }
        public bool HasPhoto { get; set; }
        public bool LowStock { get; set; }
        public double? DistanceKm { get; set; }
        public double Relevance { get; set; }
        public int? RankPosition { get; set; }
    }
}
