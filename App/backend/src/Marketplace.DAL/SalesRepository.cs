using Marketplace.BLL;
using Marketplace.DTOs;
using Marketplace.Models;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.DAL;

public sealed class SalesRepository(MarketplaceDbContext db, TimeProvider clock) : ISalesRepository
{
    private Task<bool> Owns(Guid business, Guid owner, CancellationToken ct) => db.Businesses.AnyAsync(b => b.Id == business && b.OwnerId == owner, ct);
    private async Task<bool> Lock(Guid business, Guid owner, CancellationToken ct) =>
        (await db.Businesses.FromSqlInterpolated($"SELECT * FROM businesses WHERE id={business} FOR UPDATE").SingleOrDefaultAsync(ct))?.OwnerId == owner;
    public async Task<object?> Inventory(Guid business, Guid owner, CancellationToken ct)
    {
        if (!await Owns(business, owner, ct)) return null;
        return await (from p in db.CatalogProducts.AsNoTracking() where p.BusinessId == business
            join b in db.InventoryBalances on p.Id equals b.ProductId into balances from b in balances.DefaultIfEmpty()
            orderby p.Name, p.Id select new InventoryItem(p.Id, p.Name, b == null ? null : b.Quantity, b == null ? 0 : b.Version)).ToListAsync(ct);
    }
    public async Task<object?> Movements(Guid business, Guid owner, Guid? product, int page, CancellationToken ct)
    {
        if (!await Owns(business, owner, ct)) return null;
        var rows = await (from m in db.InventoryMovements.AsNoTracking() join p in db.CatalogProducts on m.ProductId equals p.Id
            where m.BusinessId == business && (product == null || m.ProductId == product)
            orderby m.CreatedAt descending, m.OperationId descending, m.ProductId
            select new MovementResponse(m.OperationId, m.ProductId, p.Name, m.Delta, m.Balance, m.Kind, m.Reason, m.ActorId, m.SaleId, m.CreatedAt))
            .Skip((page - 1) * 20).Take(21).ToListAsync(ct);
        return new SalesPage<MovementResponse>(rows.Take(20).ToArray(), page, rows.Count > 20);
    }
    public async Task<object?> List(Guid business, Guid owner, Guid? order, int page, CancellationToken ct)
    {
        if (!await Owns(business, owner, ct)) return null;
        var rows = await db.Sales.AsNoTracking().Include(s => s.Lines).Where(s => s.BusinessId == business && (order == null || s.OrderId == order))
            .OrderByDescending(s => s.CreatedAt).ThenByDescending(s => s.Id).Skip((page - 1) * 20).Take(21).ToListAsync(ct);
        var items = new List<SaleResponse>();
        foreach (var row in rows.Take(20)) items.Add(await Response(row, ct));
        return new SalesPage<SaleResponse>(items, page, rows.Count > 20);
    }
    public async Task<SaleResponse?> Read(Guid business, Guid owner, Guid id, CancellationToken ct)
    {
        if (!await Owns(business, owner, ct)) return null;
        var sale = await db.Sales.AsNoTracking().Include(s => s.Lines).SingleOrDefaultAsync(s => s.Id == id && s.BusinessId == business, ct);
        return sale == null ? null : await Response(sale, ct);
    }
    public async Task<SalesResult> Adjust(Guid business, Guid owner, Guid id, AdjustStockRequest body, string hash, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (!await Lock(business, owner, ct)) return new(404);
        var previous = await db.InventoryMovements.FirstOrDefaultAsync(m => m.OperationId == id, ct);
        if (previous != null) return previous.BusinessId == business && previous.RequestHash == hash
            ? new(200, new { quantity = previous.Balance }) : new(409);
        if (!await db.CatalogProducts.AnyAsync(p => p.Id == body.ProductId && p.BusinessId == business, ct)) return new(404);
        var balance = await db.InventoryBalances.SingleOrDefaultAsync(b => b.ProductId == body.ProductId, ct);
        if ((balance?.Version ?? 0) != body.Version || (balance?.Quantity ?? 0) + (long)body.Delta is < 0 or > 1000000) return new(409);
        if (balance == null) { balance = new() { ProductId = body.ProductId }; db.InventoryBalances.Add(balance); }
        Change(balance, body.Delta, business, owner, id, null, "adjustment", body.Reason!, hash);
        if (!await Save(ct)) return new(409);
        await tx.CommitAsync(ct);
        return new(200, new { quantity = balance.Quantity });
    }
    public async Task<SalesResult> Create(Guid business, Guid owner, Guid id, CreateSaleRequest body, string hash, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (!await Lock(business, owner, ct)) return new(404);
        var previous = await db.Sales.Include(s => s.Lines).SingleOrDefaultAsync(s => s.Id == id, ct);
        if (previous != null) return previous.BusinessId == business && previous.RequestHash == hash ? new(200, await Response(previous, ct)) : new(409);
        var now = clock.GetUtcNow();
        CustomerOrder? order = null;
        if (body.OrderId != null)
        {
            order = await db.CustomerOrders.Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == body.OrderId && o.BusinessId == business, ct);
            if (order == null) return new(404);
            if (order.Status is not ("pending" or "in_attention") || order.Version != body.OrderVersion || await db.Sales.AnyAsync(s => s.OrderId == order.Id, ct)) return new(409);
        }
        else if (!await db.BusinessSubscriptions.AnyAsync(s => s.BusinessId == business && s.RevokedAt == null && s.StartsAt <= now && now < s.EndsAt, ct)) return new(403);
        var lines = body.Lines!;
        var ids = lines.Select(l => l.ProductId).ToArray();
        var products = await db.CatalogProducts.Where(p => p.BusinessId == business && ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        var balances = await db.InventoryBalances.Where(b => ids.Contains(b.ProductId)).ToDictionaryAsync(b => b.ProductId, ct);
        // Validate every line before mutating any balance or order.
        foreach (var line in lines)
        {
            if (!products.ContainsKey(line.ProductId) || !balances.TryGetValue(line.ProductId, out var balance) || balance.Quantity < line.Quantity) return new(409);
            if (order != null && line.Origin == "requested" && !order.Lines.Any(l => l.ProductId == line.ProductId)) return new(400);
        }
        var sale = new Sale { Id = id, BusinessId = business, OrderId = order?.Id, ActorId = owner, CreatedAt = now, RequestHash = hash,
            Lines = lines.Select(l => new SaleLine { SaleId = id, ProductId = l.ProductId, Name = products[l.ProductId].Name,
                Quantity = l.Quantity, UnitPrice = l.UnitPrice, Currency = l.Currency!, Origin = l.Origin! }).ToList() };
        db.Sales.Add(sale);
        foreach (var line in lines) Change(balances[line.ProductId], -line.Quantity, business, owner, id, id, "sale", "Venta registrada", hash);
        if (order != null) Transition(order, "converted", owner, now);
        if (!await Save(ct)) return new(409);
        await tx.CommitAsync(ct);
        return new(201, await Response(sale, ct));
    }
    public async Task<SalesResult> Void(Guid business, Guid owner, Guid id, VoidSaleRequest body, string hash, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (!await Lock(business, owner, ct)) return new(404);
        var sale = await db.Sales.Include(s => s.Lines).SingleOrDefaultAsync(s => s.Id == id && s.BusinessId == business, ct);
        if (sale == null) return new(404);
        if (sale.VoidedAt != null) return sale.VoidHash == hash ? new(200, await Response(sale, ct)) : new(409);
        if (await db.InventoryMovements.AnyAsync(m => m.OperationId == body.OperationId, ct) || await db.Sales.AnyAsync(s => s.VoidOperationId == body.OperationId, ct)) return new(409);
        var returns = body.Returns!;
        var ids = returns.Select(l => l.ProductId).ToArray();
        var balances = await db.InventoryBalances.Where(b => ids.Contains(b.ProductId)).ToDictionaryAsync(b => b.ProductId, ct);
        foreach (var line in returns)
            if (!sale.Lines.Any(l => l.ProductId == line.ProductId && l.Quantity >= line.Quantity) || !balances.TryGetValue(line.ProductId, out var balance) || balance.Quantity + (long)line.Quantity > 1000000) return new(409);
        var now = clock.GetUtcNow();
        sale.VoidedAt = now; sale.VoidedBy = owner; sale.VoidHash = hash; sale.VoidReason = body.Reason; sale.VoidOperationId = body.OperationId;
        foreach (var line in returns)
        {
            sale.Lines.Single(l => l.ProductId == line.ProductId).ReturnedQuantity = line.Quantity;
            Change(balances[line.ProductId], line.Quantity, business, owner, body.OperationId, id, "return", body.Reason!, hash);
        }
        if (sale.OrderId != null)
        {
            var order = await db.CustomerOrders.SingleAsync(o => o.Id == sale.OrderId, ct);
            Transition(order, "cancelled", owner, now);
        }
        if (!await Save(ct)) return new(409);
        await tx.CommitAsync(ct);
        return new(200, await Response(sale, ct));
    }
    private void Change(InventoryBalance balance, int delta, Guid business, Guid owner, Guid operation, Guid? sale, string kind, string reason, string hash)
    {
        balance.Quantity += delta; balance.Version++;
        db.InventoryMovements.Add(new() { OperationId = operation, ProductId = balance.ProductId, BusinessId = business,
            ActorId = owner, SaleId = sale, Delta = delta, Balance = balance.Quantity, Kind = kind, Reason = reason, RequestHash = hash, CreatedAt = clock.GetUtcNow() });
    }
    private void Transition(CustomerOrder order, string status, Guid owner, DateTimeOffset now)
    {
        db.OrderChanges.Add(new() { OrderId = order.Id, Version = order.Version + 1, ActorId = owner, OccurredAt = now,
            FromStatus = order.Status, ToStatus = status, ContactReceived = order.ContactReceivedAt != null });
        order.Status = status; order.Version++; order.UpdatedAt = now;
    }
    private async Task<bool> Save(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); return true; }
        catch (DbUpdateException error) when (error.InnerException is Npgsql.PostgresException { SqlState: "23505" }) { return false; }
    }
    private async Task<SaleResponse> Response(Sale sale, CancellationToken ct) => new(sale.Id, sale.OrderId,
        sale.OrderId == null ? null : await db.CustomerOrders.Where(o => o.Id == sale.OrderId).Select(o => o.Reference).SingleAsync(ct),
        sale.ActorId, sale.CreatedAt, sale.VoidedAt, sale.VoidReason, sale.VoidedBy,
        sale.Lines.OrderBy(l => l.ProductId).Select(l => new SaleLineResponse(l.ProductId, l.Name, l.Quantity, l.UnitPrice, l.Currency, l.Origin, l.ReturnedQuantity)).ToArray(),
        sale.Lines.GroupBy(l => l.Origin).ToDictionary(g => g.Key, g => g.GroupBy(l => l.Currency).ToDictionary(c => c.Key, c => c.Sum(l => l.UnitPrice * l.Quantity))));
}
