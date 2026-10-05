using Marketplace.BLL;
using Marketplace.DTOs;
using Marketplace.Models;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.DAL;

public sealed class CampaignRepository(MarketplaceDbContext db, TimeProvider clock) : ICampaignRepository
{
    internal static IQueryable<Campaign> Eligible(MarketplaceDbContext db, DateTimeOffset now) => db.Campaigns.Where(c =>
        c.Status == "scheduled" && c.ApprovedAt != null && c.PaidAt != null && c.StartsAt <= now && c.EndsAt > now &&
        db.Businesses.Any(b => b.Id == c.BusinessId && b.Status == "published" && db.UserAccounts.Any(u => u.Id == b.OwnerId && !u.IsSuspended)) &&
        (c.ProductId == null || db.CatalogProducts.Any(p => p.Id == c.ProductId && p.BusinessId == c.BusinessId && p.Status == "published" && p.Available &&
            !db.InventoryBalances.Any(i => i.ProductId == p.Id && i.Quantity == 0) &&
            db.BusinessSubscriptions.Any(s => s.BusinessId == c.BusinessId && s.RevokedAt == null && s.StartsAt <= now && s.EndsAt > now))));
    private Task<BusinessProfile?> Lock(Guid business, CancellationToken ct) => db.Businesses.FromSqlInterpolated($"SELECT * FROM businesses WHERE id={business} FOR UPDATE").SingleOrDefaultAsync(ct);
    private IQueryable<CampaignResponse> Responses(IQueryable<Campaign> query)
    {
        var now = clock.GetUtcNow();
        return query.Select(c => new CampaignResponse(c.Id, c.BusinessId, db.Businesses.Where(b => b.Id == c.BusinessId).Select(b => b.Name).Single(),
            c.ProductId, c.PhotoSlot, c.Image != null, c.Title, c.Description, c.Conditions, c.ZoneId, c.StartsAt, c.EndsAt, c.Status,
            c.Status == "scheduled" ? (c.EndsAt <= now ? "finished" : c.StartsAt > now ? "scheduled" : Eligible(db, now).Any(x => x.Id == c.Id) ? "active" : "blocked") : c.Status,
            c.Version, c.PaymentReference));
    }
    public async Task<object?> List(Guid? business, Guid actor, bool admin, int page, CancellationToken ct)
    {
        if (!admin && !await db.Businesses.AnyAsync(b => b.Id == business && b.OwnerId == actor, ct)) return null;
        var items = await Responses(db.Campaigns.AsNoTracking().Where(c => business == null || c.BusinessId == business)
            .OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id).Skip((page - 1) * 20).Take(21)).ToListAsync(ct);
        return new SalesPage<CampaignResponse>(items.Take(20).ToArray(), page, items.Count > 20);
    }
    public async Task<SalesResult> Save(Guid business, Guid id, Guid owner, SaveCampaignRequest body, string hash, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if ((await Lock(business, ct))?.OwnerId != owner) return new(404);
        var campaign = await db.Campaigns.SingleOrDefaultAsync(c => c.Id == id, ct);
        if (campaign != null && campaign.BusinessId != business) return new(404);
        if (campaign?.SaveHash == hash) return new(200, await Responses(db.Campaigns.Where(c => c.Id == id)).SingleAsync(ct));
        if ((campaign?.Version ?? 0) != body.Version || campaign != null && campaign.Status is not ("draft" or "rejected")) return new(409);
        var now = clock.GetUtcNow();
        if (body.EndsAt <= now || body.StartsAt > now.AddDays(180)) return new(400);
        byte[]? image;
        if (body.ProductId != null)
        {
            if (!await db.CatalogProducts.AnyAsync(p => p.Id == body.ProductId && p.BusinessId == business, ct)) return new(404);
            if (!await db.BusinessSubscriptions.AnyAsync(s => s.BusinessId == business && s.RevokedAt == null && s.StartsAt <= now && s.EndsAt > now, ct)) return new(403);
            image = await db.ProductPhotos.Where(p => p.ProductId == body.ProductId).Select(p => p.Content).SingleOrDefaultAsync(ct);
        }
        else
        {
            image = await db.BusinessPhotos.Where(p => p.BusinessId == business && p.Slot == body.PhotoSlot).Select(p => p.Content).SingleOrDefaultAsync(ct);
            if (image == null) return new(400);
        }
        if (campaign == null)
        {
            if (await db.Campaigns.CountAsync(c => c.BusinessId == business, ct) >= 100) return new(409);
            campaign = new() { Id = id, BusinessId = business, CreatedAt = now, Title = "", Description = "", Conditions = "", ZoneId = "", SaveHash = "" }; db.Campaigns.Add(campaign);
        }
        campaign.ProductId = body.ProductId; campaign.PhotoSlot = body.PhotoSlot; campaign.Image = image;
        campaign.Title = body.Title!; campaign.Description = body.Description!; campaign.Conditions = body.Conditions!; campaign.ZoneId = body.ZoneId!;
        campaign.StartsAt = body.StartsAt; campaign.EndsAt = body.EndsAt; campaign.Status = body.Status!;
        campaign.Version++; campaign.SaveHash = hash; campaign.ApprovedAt = null; campaign.PaidAt = null; campaign.PaymentReference = null;
        db.CampaignChanges.Add(new() { OperationId = Guid.NewGuid(), CampaignId = id, ActorId = owner, Version = campaign.Version,
            Action = body.Status == "draft" ? "save_draft" : "submit", Reason = "Contenido preparado por el negocio", RequestHash = hash, CreatedAt = now });
        if (!await SaveChanges(ct)) return new(409);
        await tx.CommitAsync(ct);
        return new(200, await Responses(db.Campaigns.Where(c => c.Id == id)).SingleAsync(ct));
    }
    public async Task<SalesResult> Act(Guid? business, Guid id, Guid actor, bool admin, CampaignActionRequest body, string hash, CancellationToken ct)
    {
        var target = await db.Campaigns.AsNoTracking().Where(c => c.Id == id && (business == null || c.BusinessId == business)).Select(c => (Guid?)c.BusinessId).SingleOrDefaultAsync(ct);
        if (target == null) return new(404);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var profile = await Lock(target.Value, ct);
        if (profile == null || !admin && profile.OwnerId != actor) return new(404);
        var previous = await db.CampaignChanges.SingleOrDefaultAsync(c => c.OperationId == body.OperationId, ct);
        if (previous != null) return previous.RequestHash == hash ? new(200, await Responses(db.Campaigns.Where(c => c.Id == id)).SingleAsync(ct)) : new(409);
        var campaign = await db.Campaigns.SingleAsync(c => c.Id == id, ct);
        if (campaign.Version != body.Version) return new(409);
        var next = body.Action switch {
            "approve" when admin && campaign.Status == "pending_review" => "awaiting_payment",
            "reject" when admin && campaign.Status is "pending_review" or "awaiting_payment" => "rejected",
            "confirm_payment" when admin && campaign.Status == "awaiting_payment" => "scheduled",
            "pause" when campaign.Status == "scheduled" => "paused",
            "resume" when admin && campaign.Status == "paused" => "scheduled",
            "resolve" when admin => campaign.Status,
            _ => null };
        if (next == null) return new(409);
        if (body.Action is "confirm_payment" or "resume")
        {
            // All reservations of a zone share this lock, including different businesses.
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({campaign.ZoneId}))", ct);
            if (campaign.EndsAt <= clock.GetUtcNow()) return new(409);
            if (await db.Campaigns.CountAsync(c => c.Id != id && c.ZoneId == campaign.ZoneId && (c.Status == "scheduled" || c.Status == "paused") &&
                c.StartsAt < campaign.EndsAt && c.EndsAt > campaign.StartsAt, ct) >= 3) return new(409);
        }
        var now = clock.GetUtcNow();
        campaign.Status = next; campaign.Version++;
        if (body.Action == "approve") campaign.ApprovedAt = now;
        if (body.Action == "confirm_payment") { campaign.PaidAt = now; campaign.PaymentReference = body.PaymentReference; }
        db.CampaignChanges.Add(new() { OperationId = body.OperationId, CampaignId = id, ActorId = actor, Version = campaign.Version,
            Action = body.Action!, Reason = body.Reason!, RequestHash = hash, CreatedAt = now });
        if (!await SaveChanges(ct)) return new(409);
        await tx.CommitAsync(ct);
        return new(200, await Responses(db.Campaigns.Where(c => c.Id == id)).SingleAsync(ct));
    }
    private IQueryable<PublicCampaign> PublicQuery(IQueryable<Campaign> source) => source.Select(c => new PublicCampaign(c.Id, c.BusinessId,
        db.Businesses.Where(b => b.Id == c.BusinessId).Select(b => b.Name).Single(), c.ProductId, c.Image != null, c.Title, c.Description,
        c.Conditions, c.ZoneId, c.EndsAt, db.Businesses.Where(b => b.Id == c.BusinessId).Select(b => b.WhatsApp).Single(), null));
    public async Task<IReadOnlyList<PublicCampaign>> Public(string zone, CancellationToken ct)
    {
        var items = await PublicQuery(Eligible(db, clock.GetUtcNow()).Where(c => c.ZoneId == zone).OrderBy(c => c.Id).Take(3)).ToListAsync(ct);
        if (items.Count == 0) return items;
        var offset = (int)(clock.GetUtcNow().ToUnixTimeSeconds() / 60 % items.Count);
        return items.Skip(offset).Concat(items.Take(offset)).ToArray();
    }
    public Task<PublicCampaign?> Detail(Guid id, CancellationToken ct) => PublicQuery(Eligible(db, clock.GetUtcNow()).Where(c => c.Id == id)).SingleOrDefaultAsync(ct);
    public Task<byte[]?> Image(Guid id, Guid? actor, bool admin, CancellationToken ct) => db.Campaigns.Where(c => c.Id == id &&
        (admin || actor != null && db.Businesses.Any(b => b.Id == c.BusinessId && b.OwnerId == actor) || Eligible(db, clock.GetUtcNow()).Any(x => x.Id == c.Id)))
        .Select(c => c.Image).SingleOrDefaultAsync(ct);
    public async Task<bool> Record(CampaignContext context, string kind, Guid? actor, CancellationToken ct)
    {
        var business = await db.Campaigns.Where(c => c.Id == context.CampaignId).Select(c => (Guid?)c.BusinessId).SingleOrDefaultAsync(ct);
        if (business == null) return false;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if ((await Lock(business.Value, ct))?.OwnerId == actor) return false;
        var now = clock.GetUtcNow();
        if (!await Eligible(db, now).AnyAsync(c => c.Id == context.CampaignId, ct)) return false;
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO campaign_events (campaign_id,context_id,kind,created_at) VALUES ({context.CampaignId},{context.ContextId},{kind},{now}) ON CONFLICT DO NOTHING", ct);
        await tx.CommitAsync(ct); return true;
    }
    public async Task<CampaignMetrics?> Metrics(Guid business, Guid id, Guid owner, CancellationToken ct)
    {
        if (!await db.Campaigns.AnyAsync(c => c.Id == id && c.BusinessId == business && db.Businesses.Any(b => b.Id == business && b.OwnerId == owner), ct)) return null;
        var from = clock.GetUtcNow().AddDays(-30);
        var events = db.CampaignEvents.Where(e => e.CampaignId == id && e.CreatedAt >= from);
        var orders = db.CustomerOrders.Where(o => o.CampaignId == id);
        var sales = db.Sales.Where(s => s.VoidedAt == null && orders.Any(o => o.Id == s.OrderId));
        var amounts = await (from l in db.SaleLines join s in sales on l.SaleId equals s.Id group l by new { l.Origin, l.Currency } into g
            select new { g.Key.Origin, g.Key.Currency, Amount = g.Sum(l => l.Quantity * l.UnitPrice) }).ToListAsync(ct);
        var changes = await db.CampaignChanges.Where(c => c.CampaignId == id).OrderByDescending(c => c.Version).Take(100)
            .Select(c => new CampaignAuditResponse(c.Version, c.ActorId, c.Action, c.Reason, c.CreatedAt)).ToListAsync(ct);
        return new(30, await events.CountAsync(e => e.Kind == "impression", ct), await events.CountAsync(e => e.Kind == "click", ct),
            await orders.CountAsync(ct), await sales.CountAsync(ct), amounts.GroupBy(a => a.Origin).ToDictionary(g => g.Key, g => g.ToDictionary(a => a.Currency, a => a.Amount)), changes);
    }
    public Task<int> Purge(CancellationToken ct) => db.CampaignEvents.Where(e => e.CreatedAt < clock.GetUtcNow().AddDays(-30)).ExecuteDeleteAsync(ct);
    private async Task<bool> SaveChanges(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); return true; }
        catch (DbUpdateException error) when (error.InnerException is Npgsql.PostgresException { SqlState: "23505" }) { return false; }
    }
}
