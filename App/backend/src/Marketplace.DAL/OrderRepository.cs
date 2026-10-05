using Marketplace.BLL;
using Marketplace.DTOs;
using Marketplace.Models;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.DAL;

public sealed class OrderRepository(MarketplaceDbContext db, TimeProvider clock) : IOrderRepository
{
    private Task<BusinessProfile?> Lock(Guid business, CancellationToken ct) => db.Businesses
        .FromSqlInterpolated($"SELECT * FROM businesses WHERE id = {business} FOR UPDATE").SingleOrDefaultAsync(ct);

    public async Task<OrderResult> CreateAsync(Guid business, CreateOrderRequest request, string keyHash, string requestHash, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var profile = await Lock(business, ct);
        if (profile is null) return new(OrderResultStatus.NotFound);
        var now = clock.GetUtcNow();
        var existing = await db.CustomerOrders.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == request.RequestId, ct);
        if (existing is not null)
        {
            if (existing.BusinessId != business || existing.AccessKeyHash != keyHash || existing.AccessExpiresAt <= now) return new(OrderResultStatus.NotFound);
            return existing.RequestHash == requestHash ? new(OrderResultStatus.Saved, await Response(existing, ct)) : new(OrderResultStatus.Conflict);
        }
        if (profile.Status != "published" || !await db.UserAccounts.AnyAsync(u => u.Id == profile.OwnerId && !u.IsSuspended, ct) ||
            !await db.BusinessSubscriptions.AnyAsync(s => s.BusinessId == business && s.RevokedAt == null && s.StartsAt <= now && s.EndsAt > now, ct))
            return new(OrderResultStatus.Unavailable);
        var requestedLines = request.Lines!;
        var ids = requestedLines.Select(x => x.ProductId).ToArray();
        var products = await db.CatalogProducts.Where(p => p.BusinessId == business && ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        if (requestedLines.Any(line => !products.TryGetValue(line.ProductId, out var product) || product.Status != "published" ||
            !product.Available || product.Version != line.ExpectedVersion)) return new(OrderResultStatus.Conflict);
        var order = new CustomerOrder {
            Id = request.RequestId, BusinessId = business, Reference = "CE-" + request.RequestId.ToString("N")[..12].ToUpperInvariant(),
            CustomerName = request.CustomerName!, BusinessName = profile.Name, WhatsApp = profile.WhatsApp,
            AccessKeyHash = keyHash, RequestHash = requestHash, CreatedAt = now, UpdatedAt = now, AccessExpiresAt = now.AddDays(30),
            Lines = requestedLines.Select(line => new OrderLine {
                OrderId = request.RequestId, ProductId = line.ProductId, Name = products[line.ProductId].Name,
                UnitPrice = products[line.ProductId].Price, Currency = products[line.ProductId].Currency,
                Quantity = line.Quantity, ProductVersion = products[line.ProductId].Version }).ToList()
        };
        db.CustomerOrders.Add(order);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException error) when (error.InnerException is Npgsql.PostgresException { SqlState: "23505" }) { return new(OrderResultStatus.Conflict); }
        await tx.CommitAsync(ct);
        return new(OrderResultStatus.Created, await Response(order, ct));
    }

    public async Task<OrderResponse?> GuestAsync(Guid id, string keyHash, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var order = await db.CustomerOrders.AsNoTracking().Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.Id == id && x.AccessKeyHash == keyHash && x.AccessExpiresAt > now, ct);
        return order is null ? null : await Response(order, ct);
    }

    public async Task<OrderResponse?> OwnedAsync(Guid business, Guid id, Guid owner, CancellationToken ct)
    {
        var order = await db.CustomerOrders.AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x =>
            x.BusinessId == business && x.Id == id && db.Businesses.Any(b => b.Id == business && b.OwnerId == owner), ct);
        return order is null ? null : await Response(order, ct);
    }

    public async Task<OrderPageResponse?> ListAsync(Guid business, Guid owner, int page, string reference, string? status, CancellationToken ct)
    {
        if (!await db.Businesses.AnyAsync(b => b.Id == business && b.OwnerId == owner, ct)) return null;
        var items = await db.CustomerOrders.AsNoTracking().Where(x => x.BusinessId == business &&
            (status == null || x.Status == status) && x.Reference.StartsWith(reference))
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Skip((page - 1) * 20).Take(21)
            .Select(x => new OrderSummaryResponse(x.Id, x.Reference, x.CustomerName, x.CreatedAt, x.Status, x.ContactReceivedAt)).ToListAsync(ct);
        return new(items.Take(20).ToArray(), page, items.Count > 20);
    }

    public async Task<OrderResult> UpdateAsync(Guid business, Guid id, Guid owner, UpdateOrderRequest request, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if ((await Lock(business, ct))?.OwnerId != owner) return new(OrderResultStatus.NotFound);
        var order = await db.CustomerOrders.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id && x.BusinessId == business, ct);
        if (order is null) return new(OrderResultStatus.NotFound);
        if (order.Version != request.Version || order.ContactReceivedAt != null && !request.ContactReceived ||
            order.Status == "cancelled" && (request.Status != "cancelled" || request.ContactReceived != (order.ContactReceivedAt != null)) ||
            order.Status == "in_attention" && request.Status == "pending") return new(OrderResultStatus.Conflict);
        if (order.Status == request.Status && request.ContactReceived == (order.ContactReceivedAt != null))
            return new(OrderResultStatus.Saved, await Response(order, ct));
        var now = clock.GetUtcNow();
        db.OrderChanges.Add(new() { OrderId = id, Version = order.Version + 1, ActorId = owner, OccurredAt = now,
            FromStatus = order.Status, ToStatus = request.Status!, ContactReceived = request.ContactReceived });
        order.Status = request.Status!;
        order.Version++;
        order.UpdatedAt = now;
        if (request.ContactReceived && order.ContactReceivedAt is null) order.ContactReceivedAt = now;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new(OrderResultStatus.Saved, await Response(order, ct));
    }

    private async Task<OrderResponse> Response(CustomerOrder order, CancellationToken ct)
    {
        var canContact = order.Status != "cancelled" && await db.Businesses.AnyAsync(b => b.Id == order.BusinessId && b.Status == "published" &&
            db.UserAccounts.Any(u => u.Id == b.OwnerId && !u.IsSuspended), ct);
        return new(order.Id, order.BusinessId, order.Reference, order.CustomerName, order.BusinessName, order.WhatsApp,
            order.CreatedAt, order.AccessExpiresAt, order.Status, order.Version, order.ContactReceivedAt, canContact,
            order.Lines.OrderBy(x => x.ProductId).Select(x => new OrderLineResponse(x.ProductId, x.Name, x.UnitPrice, x.Currency, x.Quantity)).ToArray(),
            order.Lines.GroupBy(x => x.Currency).ToDictionary(g => g.Key, g => g.Sum(x => x.UnitPrice * x.Quantity)));
    }
}
