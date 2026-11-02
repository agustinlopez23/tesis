using Marketplace.BLL;
using Marketplace.DTOs;
using Marketplace.Models;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.DAL;

public sealed class BusinessRepository(MarketplaceDbContext database) : IBusinessRepository
{
    public async Task<IReadOnlyList<PublicBusinessResponse>> GetPublishedAsync(int skip, int take, string? query, string? categoryFilter, DiscoveryArea? area, CancellationToken cancellationToken)
    {
        var businesses = area is not null ? Nearby(area) :
               from business in database.Businesses.AsNoTracking()
               join owner in database.UserAccounts on business.OwnerId equals owner.Id
               join category in database.BusinessCategories on business.CategoryId equals category.Id
               where business.Status == "published" && !business.IsSuspended && !owner.IsSuspended
               select new PublicBusinessRow { Id = business.Id, Name = business.Name, Description = business.Description,
                   Address = business.Address, Category = category.Name, WhatsApp = business.WhatsApp, DistanceKm = null };
        if (!string.IsNullOrWhiteSpace(categoryFilter))
            businesses = businesses.Where(x => x.Category == categoryFilter);
        if (!string.IsNullOrWhiteSpace(query))
        {
            // Escape LIKE metacharacters so a visitor searches for literal text.
            var normalized = query.ToLowerInvariant().Replace("á", "a").Replace("é", "e").Replace("í", "i")
                .Replace("ó", "o").Replace("ú", "u").Replace("ü", "u");
            var pattern = "%" + normalized.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            businesses = businesses.Where(x =>
                EF.Functions.ILike(x.Name.ToLower().Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u").Replace("ü", "u"), pattern) ||
                EF.Functions.ILike(x.Description.ToLower().Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u").Replace("ü", "u"), pattern) ||
                EF.Functions.ILike(x.Address.ToLower().Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u").Replace("ü", "u"), pattern));
        }
        var ordered = area is null ? businesses.OrderBy(x => x.Name).ThenBy(x => x.Id) :
            businesses.OrderBy(x => x.DistanceKm).ThenBy(x => x.Name).ThenBy(x => x.Id);
        return await ordered
            .Skip(skip).Take(take)
            .Select(x => new PublicBusinessResponse(x.Id, x.Name, x.Description, x.Address, x.Category, x.WhatsApp, x.DistanceKm,
                database.BusinessPhotos.Where(p => p.BusinessId == x.Id).Select(p => (int?)p.Slot).Min()))
            .ToListAsync(cancellationToken);
    }

    // Interpolated values become database parameters; geography distances are in meters.
    private IQueryable<PublicBusinessRow> Nearby(DiscoveryArea area) => database.Database.SqlQuery<PublicBusinessRow>($"""
        SELECT b.id AS "Id", b.name AS "Name", b.description AS "Description",
               b.address AS "Address", c.name AS "Category", b.whatsapp AS "WhatsApp",
               ST_Distance(b.location, ST_SetSRID(ST_MakePoint({area.Longitude}, {area.Latitude}), 4326)::geography) / 1000 AS "DistanceKm"
        FROM businesses b
        JOIN user_accounts u ON u.id = b.owner_id
        JOIN business_categories c ON c.id = b.category_id
        WHERE b.status = 'published' AND NOT u.is_suspended AND NOT b.is_suspended
          AND ST_DWithin(b.location, ST_SetSRID(ST_MakePoint({area.Longitude}, {area.Latitude}), 4326)::geography, {area.RadiusKm * 1000})
        """);

    private sealed class PublicBusinessRow
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string Address { get; set; }
        public required string Category { get; set; }
        public required string WhatsApp { get; set; }
        public double? DistanceKm { get; set; }
    }

    public async Task<IReadOnlyList<BusinessCategory>> GetCategoriesAsync(CancellationToken cancellationToken) =>
        await database.BusinessCategories.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public Task<BusinessCategory?> FindCategoryAsync(short categoryId, CancellationToken cancellationToken) =>
        database.BusinessCategories.AsNoTracking().SingleOrDefaultAsync(x => x.Id == categoryId, cancellationToken);

    public async Task<IReadOnlyList<BusinessProfile>> GetOwnedAsync(Guid ownerId, CancellationToken cancellationToken) =>
        await database.Businesses.AsNoTracking().Where(x => x.OwnerId == ownerId)
            .OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public Task<BusinessProfile?> GetOwnedAsync(Guid ownerId, Guid id, CancellationToken cancellationToken) =>
        database.Businesses.SingleOrDefaultAsync(x => x.Id == id && x.OwnerId == ownerId, cancellationToken);

    public async Task AddAsync(BusinessProfile business, CancellationToken cancellationToken)
    {
        database.Businesses.Add(business);
        await database.SaveChangesAsync(cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken) => database.SaveChangesAsync(cancellationToken);
}
