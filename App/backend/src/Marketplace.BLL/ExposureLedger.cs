namespace Marketplace.BLL;

public sealed class ExposureLedger
{
    private sealed record Observation(string Context, DateTimeOffset At, Dictionary<Guid, ExposureBalance> Values);
    private readonly Queue<Observation> observations = new();
    private readonly Dictionary<string, Dictionary<Guid, ExposureBalance>> balances = new(StringComparer.Ordinal);
    private readonly Dictionary<(string Context, Guid Id), int> counts = new();
    private DateTimeOffset? latest;

    public static double Attention(int position) => position is >= 1 and <= 10 ? 1 / Math.Log2(position + 1) :
        throw new ArgumentException("Position must be in the top ten.");

    public static IReadOnlyDictionary<Guid, double> Targets(IReadOnlyDictionary<Guid, double> merits)
    {
        if (merits.Values.Any(x => !double.IsFinite(x) || x <= 0 || x > 1))
            throw new ArgumentException("Merit must be positive and at most one.");
        var pending = merits.OrderBy(x => x.Key).ToDictionary();
        var result = new Dictionary<Guid, double>();
        var budget = Enumerable.Range(1, Math.Min(10, merits.Count)).Sum(Attention);
        while (pending.Count > 0)
        {
            var total = pending.Values.Sum();
            var capped = pending.Where(x => budget * (x.Value / total) > 1).Select(x => x.Key).ToArray();
            if (capped.Length == 0)
            {
                foreach (var (id, merit) in pending) result[id] = budget * (merit / total);
                break;
            }
            foreach (var id in capped) { result[id] = 1; pending.Remove(id); budget -= 1; }
        }
        return result.OrderBy(x => x.Key).ToDictionary();
    }

    public IReadOnlyDictionary<Guid, ExposureBalance> Snapshot(string context, DateTimeOffset at)
    {
        Advance(context, at);
        return balances.TryGetValue(context, out var values) ? new Dictionary<Guid, ExposureBalance>(values) : [];
    }

    public void Observe(string context, DateTimeOffset at, IReadOnlyDictionary<Guid, double> expected,
        IReadOnlyList<Guid> displayed)
    {
        if (displayed.Count > 10 || displayed.Distinct().Count() != displayed.Count ||
            displayed.Any(x => !expected.ContainsKey(x)) ||
            expected.Values.Any(x => !double.IsFinite(x) || x < 0 || x > 1) ||
            Math.Abs(expected.Values.Sum() - Enumerable.Range(1, displayed.Count).Sum(Attention)) > 1e-8)
            throw new ArgumentException("Invalid exposure observation.");
        Advance(context, at);
        if (expected.Count == 0) return;
        var received = displayed.Select((id, i) => (id, value: Attention(i + 1))).ToDictionary(x => x.id, x => x.value);
        var values = expected.ToDictionary(x => x.Key, x => new ExposureBalance(x.Value, received.GetValueOrDefault(x.Key)));
        if (!balances.TryGetValue(context, out var totals)) balances[context] = totals = [];
        foreach (var (id, value) in values)
        {
            var old = totals.GetValueOrDefault(id, new(0, 0));
            totals[id] = new(old.Expected + value.Expected, old.Received + value.Received);
            counts[(context, id)] = counts.GetValueOrDefault((context, id)) + 1;
        }
        observations.Enqueue(new(context, at, values));
    }

    private void Advance(string context, DateTimeOffset at)
    {
        if (string.IsNullOrWhiteSpace(context) || latest is { } previous && at < previous)
            throw new ArgumentException("Contexts must be named and time must be monotonic.");
        latest = at;
        while (observations.TryPeek(out var first) && at - first.At >= TimeSpan.FromDays(30))
        {
            observations.Dequeue();
            var totals = balances[first.Context];
            foreach (var (id, value) in first.Values)
            {
                if (--counts[(first.Context, id)] == 0)
                { totals.Remove(id); counts.Remove((first.Context, id)); }
                else
                {
                    var old = totals[id];
                    totals[id] = new(Math.Max(0, old.Expected - value.Expected), Math.Max(0, old.Received - value.Received));
                }
            }
            if (totals.Count == 0) balances.Remove(first.Context);
        }
    }
}
