using Marketplace.BLL;
using Marketplace.DTOs;
using Marketplace.Models;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.DAL;

public sealed class BusinessRepository(MarketplaceDbContext database) : IBusinessRepository
{
    public async Task<IReadOnlyList<PublicBusinessResponse>> GetPublishedAsync(int skip, int take, string? query, string? categoryFilter, CancellationToken cancellationToken)
    {
        var businesses = from business in database.Businesses.AsNoTracking()
               join owner in database.UserAccounts on business.OwnerId equals owner.Id
               join category in database.BusinessCategories on business.CategoryId equals category.Id
               where business.Status == "published" && !owner.IsSuspended
               select new { business.Id, business.Name, business.Description,
                   business.Address, Category = category.Name, business.WhatsApp };
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
        return await businesses.OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Skip(skip).Take(take)
            .Select(x => new PublicBusinessResponse(x.Id, x.Name, x.Description, x.Address, x.Category, x.WhatsApp))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BusinessCategory>> GetCategoriesAsync(CancellationToken cancellationToken) =>
        await database.BusinessCategories.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public Task<bool> CategoryExistsAsync(short categoryId, CancellationToken cancellationToken) =>
        database.BusinessCategories.AnyAsync(x => x.Id == categoryId, cancellationToken);

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
