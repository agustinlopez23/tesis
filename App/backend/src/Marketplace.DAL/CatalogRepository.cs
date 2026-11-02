using Marketplace.BLL;
using Marketplace.DTOs;
using Marketplace.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Marketplace.DAL;

public sealed class CatalogRepository(MarketplaceDbContext db, TimeProvider clock) : ICatalogRepository
{
    private static readonly Expression<Func<CatalogProduct, ProductResponse>> Projection = p =>
        new(p.Id, p.Name, p.Description, p.Category, p.Price, p.Currency, p.Available, p.Status, p.Version, p.Photo != null);

    private IQueryable<BusinessSubscription> Active(Guid business, DateTimeOffset now) => db.BusinessSubscriptions
        .Where(s => s.BusinessId == business && s.RevokedAt == null && s.StartsAt <= now && s.EndsAt > now);
    private IQueryable<BusinessProfile> Eligible(Guid business, DateTimeOffset now) => db.Businesses.Where(b =>
        b.Id == business && b.Status == "published" && !b.IsSuspended && db.UserAccounts.Any(u => u.Id == b.OwnerId && !u.IsSuspended) &&
        Active(business, now).Any());

    // All plan and catalog writes lock the business first, including concurrent grants.
    // Check time after acquiring the lock so queued writes cannot reuse an expired plan.
    private async Task<BusinessProfile?> Lock(Guid business, CancellationToken ct) =>
        await db.Businesses.FromSqlInterpolated($"SELECT * FROM businesses WHERE id = {business} FOR UPDATE").SingleOrDefaultAsync(ct);

    public async Task<BusinessPlanResponse?> PlanAsync(Guid businessId, Guid? ownerId, CancellationToken ct)
    {
        if (!await db.Businesses.AnyAsync(b => b.Id == businessId && (ownerId == null || b.OwnerId == ownerId), ct)) return null;
        var periods = await db.BusinessSubscriptions.AsNoTracking().Where(s => s.BusinessId == businessId)
            .OrderByDescending(s => s.StartsAt).Select(s => new SubscriptionResponse(s.Id, s.StartsAt, s.EndsAt,
                s.GrantedAt, s.Reason, s.RevokedAt, s.RevocationReason)).ToListAsync(ct);
        var now = clock.GetUtcNow();
        var enabled = periods.Any(s => s.RevokedAt == null && s.StartsAt <= now && now < s.EndsAt);
        return new(enabled, enabled ? "Impulso" : "Presencia", periods);
    }

    public async Task<CatalogStatus> GrantAsync(Guid businessId, Guid id, Guid admin, GrantSubscriptionRequest request, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (await Lock(businessId, ct) is null) return CatalogStatus.NotFound;
        var previous = await db.BusinessSubscriptions.SingleOrDefaultAsync(s => s.Id == id, ct);
        if (previous is not null) return previous.BusinessId == businessId && previous.StartsAt == request.StartsAt &&
            previous.EndsAt == request.EndsAt && previous.Reason == request.Reason && previous.GrantedBy == admin
            ? CatalogStatus.Saved : CatalogStatus.Conflict;
        if (await db.BusinessSubscriptions.AnyAsync(s => s.BusinessId == businessId && s.RevokedAt == null &&
            s.StartsAt < request.EndsAt && request.StartsAt < s.EndsAt, ct)) return CatalogStatus.Conflict;
        db.BusinessSubscriptions.Add(new() { Id = id, BusinessId = businessId, StartsAt = request.StartsAt, EndsAt = request.EndsAt,
            Reason = request.Reason!, GrantedBy = admin, GrantedAt = clock.GetUtcNow() });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException error) when (error.InnerException is Npgsql.PostgresException { SqlState: "23505" }) { return CatalogStatus.Conflict; }
        await tx.CommitAsync(ct);
        return CatalogStatus.Saved;
    }

    public async Task<CatalogStatus> RevokeAsync(Guid businessId, Guid id, Guid admin, string reason, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (await Lock(businessId, ct) is null) return CatalogStatus.NotFound;
        var subscription = await db.BusinessSubscriptions.SingleOrDefaultAsync(s => s.Id == id && s.BusinessId == businessId, ct);
        if (subscription is null) return CatalogStatus.NotFound;
        if (subscription.RevokedAt is not null) return subscription.RevocationReason == reason && subscription.RevokedBy == admin
            ? CatalogStatus.Saved : CatalogStatus.Conflict;
        subscription.RevokedAt = clock.GetUtcNow();
        subscription.RevokedBy = admin;
        subscription.RevocationReason = reason;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return CatalogStatus.Saved;
    }

    public async Task<IReadOnlyList<AdminBusinessResponse>> AdminBusinessesAsync(string query, int page, CancellationToken ct) =>
        await (from b in db.Businesses.AsNoTracking() join u in db.UserAccounts on b.OwnerId equals u.Id
            where b.Name.ToLower().Contains(query.ToLower()) || u.Email.Contains(query.ToLower())
            orderby b.Name, b.Id select new AdminBusinessResponse(b.Id, b.Name, u.Email))
            .Skip((page - 1) * 20).Take(20).ToListAsync(ct);

    public async Task<IReadOnlyList<ProductResponse>?> OwnedAsync(Guid businessId, Guid ownerId, CancellationToken ct)
    {
        if (!await db.Businesses.AnyAsync(b => b.Id == businessId && b.OwnerId == ownerId, ct)) return null;
        return await db.CatalogProducts.AsNoTracking().Where(p => p.BusinessId == businessId)
            .OrderBy(p => p.Name).ThenBy(p => p.Id).Select(Projection).ToListAsync(ct);
    }

    public async Task<PublicCatalogResponse?> PublicAsync(Guid businessId, int page, string query, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        if (!await Eligible(businessId, now).AnyAsync(ct)) return null;
        var items = await db.CatalogProducts.AsNoTracking().Where(p => p.BusinessId == businessId && p.Status == "published" && !p.IsSuspended &&
            Eligible(businessId, now).Any() && (p.Name.ToLower().Contains(query.ToLower()) ||
            p.Description.ToLower().Contains(query.ToLower()) || p.Category.ToLower().Contains(query.ToLower())))
            .OrderBy(p => p.Name).ThenBy(p => p.Id).Skip((page - 1) * 20).Take(21)
            .Select(p => new ProductResponse(p.Id, p.Name, p.Description, p.Category, p.Price, p.Currency,
                p.Available && !db.InventoryBalances.Any(b => b.ProductId == p.Id && b.Quantity == 0), p.Status, p.Version, p.Photo != null)).ToListAsync(ct);
        return new(items.Take(20).ToArray(), page, items.Count > 20);
    }

    public async Task<CatalogResult> SaveAsync(Guid businessId, Guid productId, Guid ownerId, SaveProductRequest request, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var business = await Lock(businessId, ct);
        if (business?.OwnerId != ownerId) return new(CatalogStatus.NotFound);
        var now = clock.GetUtcNow();
        if (!await Active(businessId, now).AnyAsync(ct)) return new(CatalogStatus.PlanRequired);
        var product = await db.CatalogProducts.SingleOrDefaultAsync(p => p.Id == productId, ct);
        if (product is not null && product.BusinessId != businessId) return new(CatalogStatus.NotFound);
        if ((product?.Version ?? 0) != request.Version) return new(CatalogStatus.Conflict);
        if (product is null)
        {
            if (await db.CatalogProducts.CountAsync(p => p.BusinessId == businessId, ct) >= 200) return new(CatalogStatus.LimitReached);
            product = new() { Id = productId, BusinessId = businessId, CreatedAt = now, Name = "", Description = "", Category = "", Currency = "ARS", Status = "draft" };
            db.CatalogProducts.Add(product);
        }
        product.Name = request.Name!;
        product.Description = request.Description!;
        product.Category = request.Category!;
        product.Price = request.Price;
        product.Currency = request.Currency!;
        product.Available = request.Available;
        product.Status = request.Status!;
        product.Version++;
        product.UpdatedAt = now;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException error) when (error.InnerException is Npgsql.PostgresException { SqlState: "23505" }) { return new(CatalogStatus.Conflict); }
        var response = await db.CatalogProducts.Where(p => p.Id == productId).Select(Projection).SingleAsync(ct);
        await tx.CommitAsync(ct);
        return new(CatalogStatus.Saved, response);
    }

    public async Task<CatalogResult> PhotoAsync(Guid businessId, Guid productId, Guid ownerId, int version, byte[]? content, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if ((await Lock(businessId, ct))?.OwnerId != ownerId) return new(CatalogStatus.NotFound);
        if (!await Active(businessId, clock.GetUtcNow()).AnyAsync(ct)) return new(CatalogStatus.PlanRequired);
        var product = await db.CatalogProducts.Include(p => p.Photo).SingleOrDefaultAsync(p => p.Id == productId && p.BusinessId == businessId, ct);
        if (product is null) return new(CatalogStatus.NotFound);
        if (product.Version != version) return new(CatalogStatus.Conflict);
        if (content is null) { if (product.Photo is not null) db.ProductPhotos.Remove(product.Photo); }
        else if (product.Photo is null) product.Photo = new() { ProductId = productId, Content = content };
        else product.Photo.Content = content;
        product.Version++;
        product.UpdatedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
        var response = await db.CatalogProducts.Where(p => p.Id == productId).Select(Projection).SingleAsync(ct);
        await tx.CommitAsync(ct);
        return new(CatalogStatus.Saved, response);
    }

    public Task<byte[]?> ReadPhotoAsync(Guid businessId, Guid productId, Guid? ownerId, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        return db.CatalogProducts.AsNoTracking().Where(p => p.Id == productId && p.BusinessId == businessId && p.Photo != null &&
            (ownerId != null ? db.Businesses.Any(b => b.Id == businessId && b.OwnerId == ownerId) :
                p.Status == "published" && !p.IsSuspended && Eligible(businessId, now).Any())).Select(p => p.Photo!.Content).SingleOrDefaultAsync(ct);
    }
}
