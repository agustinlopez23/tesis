using Marketplace.DTOs;

namespace Marketplace.BLL;

public interface ICommerceRepository
{
    Task<CommerceResponse?> Read(Guid business, Guid owner, int days, DateTimeOffset from, DateTimeOffset until, CancellationToken ct);
}
public sealed class CommerceService(ICommerceRepository repository, TimeProvider clock)
{
    public Task<CommerceResponse?> Read(Guid business, Guid owner, int days, CancellationToken ct)
    {
        if (days is not (7 or 30)) throw new ArgumentOutOfRangeException(nameof(days));
        var now = clock.GetUtcNow();
        var zone = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Cordoba");
        var first = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, zone).DateTime).AddDays(1 - days);
        var from = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(first.ToDateTime(TimeOnly.MinValue), zone));
        return repository.Read(business, owner, days, from, now, ct);
    }
}
