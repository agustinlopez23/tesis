using Marketplace.DTOs;
using Marketplace.Models;

namespace Marketplace.BLL;

public interface IActivityRepository
{
    Task RecordAsync(BusinessActivity activity, Guid? actorId, CancellationToken ct);
    Task<IReadOnlyList<DailyActivityResponse>> ReadDailyAsync(Guid id, DateTimeOffset from, DateTimeOffset until, CancellationToken ct);
    Task<int> PurgeAsync(DateTimeOffset before, CancellationToken ct);
}

public sealed class ActivityService(IActivityRepository repository, IBusinessProfileRepository profiles, TimeProvider clock)
{
    public Task RecordAsync(BusinessActivity activity, Guid? actorId, CancellationToken ct) =>
        repository.RecordAsync(activity, actorId, ct);

    public async Task<ActivityResponse?> ReadAsync(Guid id, Guid ownerId, int days, CancellationToken ct)
    {
        if (days is not (7 or 30)) throw new ArgumentOutOfRangeException(nameof(days));
        if (!await profiles.IsOwnedAsync(id, ownerId, ct)) return null;
        var now = clock.GetUtcNow();
        const string zoneId = "America/Argentina/Cordoba";
        var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, zone).DateTime);
        var first = today.AddDays(1 - days);
        var localStart = first.ToDateTime(TimeOnly.MinValue);
        var start = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localStart, zone));
        var rows = await repository.ReadDailyAsync(id, start, now, ct);
        var daily = Enumerable.Range(0, days).Select(offset => {
            var date = first.AddDays(offset);
            return rows.FirstOrDefault(row => row.Date == date) ?? new DailyActivityResponse(date, 0, 0, 0);
        }).ToArray();
        return new(days, now, zoneId, daily.Sum(x => x.Impressions), daily.Sum(x => x.ProfileViews), daily.Sum(x => x.Contacts), daily);
    }

    public Task<int> PurgeAsync(CancellationToken ct) => repository.PurgeAsync(clock.GetUtcNow().AddDays(-30), ct);
}
