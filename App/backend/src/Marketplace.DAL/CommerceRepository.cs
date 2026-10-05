using System.Data;
using Marketplace.BLL;
using Marketplace.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.DAL;

public sealed class CommerceRepository(MarketplaceDbContext db) : ICommerceRepository
{
    public async Task<CommerceResponse?> Read(Guid business, Guid owner, int days, DateTimeOffset from, DateTimeOffset until, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        if (!await db.Businesses.AnyAsync(b => b.Id == business && b.OwnerId == owner, ct)) return null;
        var cohort = db.CustomerOrders.AsNoTracking().Where(o => o.BusinessId == business && o.CreatedAt >= from && o.CreatedAt <= until);
        var orders = await cohort.CountAsync(ct);
        var contacts = await cohort.CountAsync(o => o.ContactReceivedAt != null && o.ContactReceivedAt <= until, ct);
        var converted = await cohort.CountAsync(o => db.Sales.Any(s => s.OrderId == o.Id && s.CreatedAt <= until && s.VoidedAt == null), ct);
        var period = db.Sales.AsNoTracking().Where(s => s.BusinessId == business && s.CreatedAt >= from && s.CreatedAt <= until);
        var active = await period.CountAsync(s => s.VoidedAt == null, ct);
        var voided = await period.CountAsync(s => s.VoidedAt != null, ct);
        var amounts = await (from line in db.SaleLines.AsNoTracking() join sale in period on line.SaleId equals sale.Id
            where sale.VoidedAt == null group line by new { line.Origin, line.Currency } into g
            select new { g.Key.Origin, g.Key.Currency, Amount = g.Sum(l => l.UnitPrice * l.Quantity) }).ToListAsync(ct);
        await tx.CommitAsync(ct);
        return new(days, from, until, "America/Argentina/Cordoba", orders, contacts, converted,
            orders == 0 ? null : (decimal)converted / orders, active, voided,
            amounts.GroupBy(a => a.Origin).ToDictionary(g => g.Key, g => g.ToDictionary(a => a.Currency, a => a.Amount)));
    }
}
