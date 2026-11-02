using Marketplace.BLL;
using Marketplace.DTOs;
using Marketplace.Models;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.DAL;

// Every administrator mutation and its audit row commit in the same transaction.
public sealed class AdminRepository(MarketplaceDbContext db, TimeProvider clock) : IAdminRepository
{
    private const int PageSize = 20;

    public async Task<IReadOnlyList<AdminCategoryResponse>> CategoriesAsync(CancellationToken ct) =>
        await db.BusinessCategories.AsNoTracking().OrderBy(c => c.Name)
            .Select(c => new AdminCategoryResponse(c.Id, c.Name, c.Slug, c.IsActive, db.Businesses.Count(b => b.CategoryId == c.Id)))
            .ToListAsync(ct);

    public async Task<AdminCategoryResult> CreateCategoryAsync(Guid actor, string name, string slug, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Category ids are small and assigned here; the lock serializes concurrent creations.
        await db.Database.ExecuteSqlRawAsync("LOCK TABLE business_categories IN SHARE ROW EXCLUSIVE MODE", ct);
        if (await db.BusinessCategories.AnyAsync(c => c.Slug == slug || c.Name.ToLower() == name.ToLower(), ct))
            return new(AdminStatus.Conflict);
        var next = await db.BusinessCategories.MaxAsync(c => (int?)c.Id, ct) ?? 0;
        if (next >= short.MaxValue) return new(AdminStatus.Conflict);
        var category = new BusinessCategory { Id = (short)(next + 1), Name = name, Slug = slug, IsActive = true };
        db.BusinessCategories.Add(category);
        Audit(actor, "category", category.Id.ToString(), name, "create", null);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new(AdminStatus.Saved, new(category.Id, category.Name, category.Slug, category.IsActive, 0));
    }

    public async Task<AdminCategoryResult> UpdateCategoryAsync(Guid actor, short id, string name, bool isActive, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var category = await db.BusinessCategories.FromSqlInterpolated($"SELECT * FROM business_categories WHERE id = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
        if (category is null) return new(AdminStatus.NotFound);
        if (await db.BusinessCategories.AnyAsync(c => c.Id != id && c.Name.ToLower() == name.ToLower(), ct)) return new(AdminStatus.Conflict);
        var changes = new List<string>();
        if (category.Name != name) changes.Add($"Nombre anterior: {category.Name}");
        if (category.IsActive != isActive) changes.Add(isActive ? "Rubro habilitado" : "Rubro deshabilitado");
        category.Name = name;
        category.IsActive = isActive;
        if (changes.Count > 0) Audit(actor, "category", id.ToString(), name, "update", string.Join(". ", changes));
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException error) when (error.InnerException is Npgsql.PostgresException { SqlState: "23505" }) { return new(AdminStatus.Conflict); }
        await tx.CommitAsync(ct);
        var count = await db.Businesses.CountAsync(b => b.CategoryId == id, ct);
        return new(AdminStatus.Saved, new(category.Id, category.Name, category.Slug, category.IsActive, count));
    }

    // Businesses are the only rows referencing a category; the row lock blocks concurrent edits while counting,
    // and the foreign key still rejects a business assigned between the count and the delete.
    public async Task<AdminCategoryDeletion> DeleteCategoryAsync(Guid actor, short id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var category = await db.BusinessCategories.FromSqlInterpolated($"SELECT * FROM business_categories WHERE id = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
        if (category is null) return new(AdminStatus.NotFound);
        var usage = await db.Businesses.CountAsync(b => b.CategoryId == id, ct);
        if (usage > 0) return new(AdminStatus.Conflict, usage);
        db.BusinessCategories.Remove(category);
        Audit(actor, "category", id.ToString(), category.Name, "delete", null);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException error) when (error.InnerException is Npgsql.PostgresException { SqlState: "23503" })
        {
            await tx.RollbackAsync(ct);
            return new(AdminStatus.Conflict, Math.Max(1, await db.Businesses.CountAsync(b => b.CategoryId == id, ct)));
        }
        await tx.CommitAsync(ct);
        return new(AdminStatus.Saved);
    }

    public async Task<IReadOnlyList<ModeratedAccountResponse>> AccountsAsync(string query, int page, CancellationToken ct)
    {
        var term = query.ToLower();
        return await db.UserAccounts.AsNoTracking()
            .Where(u => !u.Roles.Any(r => r.Role.Name == "administrator") &&
                (u.Email.Contains(term) || u.DisplayName.ToLower().Contains(term)))
            .OrderBy(u => u.Email).ThenBy(u => u.Id).Skip((page - 1) * PageSize).Take(PageSize)
            .Select(u => new ModeratedAccountResponse(u.Id, u.Email, u.DisplayName, u.IsSuspended, db.Businesses.Count(b => b.OwnerId == u.Id)))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ModeratedBusinessResponse>> BusinessesAsync(string query, int page, CancellationToken ct)
    {
        var term = query.ToLower();
        return await (from b in db.Businesses.AsNoTracking()
                join u in db.UserAccounts on b.OwnerId equals u.Id
                join c in db.BusinessCategories on b.CategoryId equals c.Id
                where b.Name.ToLower().Contains(term) || u.Email.Contains(term)
                orderby b.Name, b.Id
                select new ModeratedBusinessResponse(b.Id, b.Name, u.Email, c.Name, b.Status, b.IsSuspended))
            .Skip((page - 1) * PageSize).Take(PageSize).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ModeratedProductResponse>> ProductsAsync(string query, int page, CancellationToken ct)
    {
        var term = query.ToLower();
        return await (from p in db.CatalogProducts.AsNoTracking()
                join b in db.Businesses on p.BusinessId equals b.Id
                where p.Name.ToLower().Contains(term) || b.Name.ToLower().Contains(term)
                orderby p.Name, p.Id
                select new ModeratedProductResponse(p.Id, b.Id, b.Name, p.Name, p.Status, p.IsSuspended))
            .Skip((page - 1) * PageSize).Take(PageSize).ToListAsync(ct);
    }

    public async Task<AdminStatus> ModerateAsync(Guid actor, ModerationTarget target, Guid id, bool suspend, string reason, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        string type, label;
        switch (target)
        {
            case ModerationTarget.Account:
                var account = await db.UserAccounts.FromSqlInterpolated($"SELECT * FROM user_accounts WHERE id = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
                if (account is null) return AdminStatus.NotFound;
                // Administrators are not moderated here, which also prevents self-lockout.
                if (account.IsSuspended == suspend || await db.UserAccountRoles.AnyAsync(r => r.UserId == id && r.Role.Name == "administrator", ct))
                    return AdminStatus.Conflict;
                account.IsSuspended = suspend;
                (type, label) = ("account", account.Email);
                break;
            case ModerationTarget.Business:
                var business = await db.Businesses.FromSqlInterpolated($"SELECT * FROM businesses WHERE id = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
                if (business is null) return AdminStatus.NotFound;
                if (business.IsSuspended == suspend) return AdminStatus.Conflict;
                business.IsSuspended = suspend;
                (type, label) = ("business", business.Name);
                break;
            default:
                var product = await db.CatalogProducts.FromSqlInterpolated($"SELECT * FROM catalog_products WHERE id = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
                if (product is null) return AdminStatus.NotFound;
                if (product.IsSuspended == suspend) return AdminStatus.Conflict;
                product.IsSuspended = suspend;
                (type, label) = ("product", product.Name);
                break;
        }
        Audit(actor, type, id.ToString(), label, suspend ? "suspend" : "reinstate", reason);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return AdminStatus.Saved;
    }

    public async Task<AdminAuditPage> AuditAsync(int page, CancellationToken ct)
    {
        var items = await (from a in db.AdminActions.AsNoTracking()
                join u in db.UserAccounts on a.ActorId equals u.Id
                orderby a.Id descending
                select new AdminActionResponse(a.Id, u.Email, a.TargetType, a.TargetId, a.TargetLabel, a.Action, a.Reason, a.CreatedAt))
            .Skip((page - 1) * 50).Take(51).ToListAsync(ct);
        return new(items.Take(50).ToArray(), page, items.Count > 50);
    }

    private void Audit(Guid actor, string type, string id, string label, string action, string? reason) =>
        db.AdminActions.Add(new AdminAction
        {
            ActorId = actor, TargetType = type, TargetId = id, TargetLabel = label.Length > 254 ? label[..254] : label,
            Action = action, Reason = reason, CreatedAt = clock.GetUtcNow()
        });
}
