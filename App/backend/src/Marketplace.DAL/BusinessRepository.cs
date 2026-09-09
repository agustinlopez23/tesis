using Marketplace.BLL;
using Marketplace.Models;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.DAL;

public sealed class BusinessRepository(MarketplaceDbContext database) : IBusinessRepository
{
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
