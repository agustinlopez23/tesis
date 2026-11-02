using Marketplace.BLL;
using Marketplace.DTOs;
using Marketplace.Models;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.DAL;

public sealed class BusinessProfileRepository(MarketplaceDbContext database) : IBusinessProfileRepository
{
    public async Task<PublicBusinessProfileResponse?> GetPublicAsync(Guid id, CancellationToken ct)
    {
        var business = await (from b in database.Businesses.AsNoTracking()
            join u in database.UserAccounts on b.OwnerId equals u.Id
            join c in database.BusinessCategories on b.CategoryId equals c.Id
            where b.Id == id && b.Status == "published" && !b.IsSuspended && !u.IsSuspended
            select new { b.Id, b.Name, b.Description, b.Address, Category = c.Name, b.WhatsApp }).SingleOrDefaultAsync(ct);
        return business is null ? null : new(business.Id, business.Name, business.Description, business.Address,
            business.Category, business.WhatsApp, await GetPhotosAsync(id, ct));
    }
    public Task<bool> IsOwnedAsync(Guid id, Guid ownerId, CancellationToken ct) =>
        database.Businesses.AnyAsync(x => x.Id == id && x.OwnerId == ownerId, ct);

    public async Task<IReadOnlyList<BusinessPhotoResponse>> GetPhotosAsync(Guid id, CancellationToken ct) =>
        await database.BusinessPhotos.AsNoTracking().Where(x => x.BusinessId == id).OrderBy(x => x.Slot)
            .Select(x => new BusinessPhotoResponse(x.Slot, x.Caption, x.Width, x.Height)).ToListAsync(ct);

    public Task<byte[]?> GetPhotoAsync(Guid id, int slot, Guid? ownerId, CancellationToken ct) =>
        (from p in database.BusinessPhotos.AsNoTracking()
         join b in database.Businesses on p.BusinessId equals b.Id
         join u in database.UserAccounts on b.OwnerId equals u.Id
         where b.Id == id && p.Slot == slot && !u.IsSuspended &&
             (ownerId != null ? b.OwnerId == ownerId : b.Status == "published" && !b.IsSuspended)
         select p.Content).SingleOrDefaultAsync(ct);

    public async Task SavePhotoAsync(BusinessPhoto photo, CancellationToken ct) =>
        await database.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO business_photos (business_id, slot, caption, width, height, content)
            VALUES ({photo.BusinessId}, {photo.Slot}, {photo.Caption}, {photo.Width}, {photo.Height}, {photo.Content})
            ON CONFLICT (business_id, slot) DO UPDATE SET
                caption = EXCLUDED.caption, width = EXCLUDED.width, height = EXCLUDED.height, content = EXCLUDED.content
            """, ct);

    public async Task<bool> DeletePhotoAsync(Guid id, int slot, CancellationToken ct) =>
        await database.BusinessPhotos.Where(x => x.BusinessId == id && x.Slot == slot).ExecuteDeleteAsync(ct) > 0;
}
