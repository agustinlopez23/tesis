using Marketplace.BLL;

namespace Marketplace.BLL.Tests;

public sealed class ExposureLedgerTests
{
    private static readonly Guid A = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid B = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-01T00:00:00Z");

    [Fact]
    public void Target_conserves_attention_and_caps_impossible_individual_allocations()
    {
        var target = ExposureLedger.Targets(new Dictionary<Guid, double> { [A] = 1, [B] = .01 });
        Assert.Equal(1, target[A], 10);
        Assert.Equal(1 / Math.Log2(3), target[B], 10);
        var equal = ExposureLedger.Targets(new Dictionary<Guid, double> { [A] = 1, [B] = 1 });
        Assert.Equal((1 + 1 / Math.Log2(3)) / 2, equal[A], 10);
        Assert.Empty(ExposureLedger.Targets(new Dictionary<Guid, double>()));
        Assert.Throws<ArgumentException>(() => ExposureLedger.Targets(new Dictionary<Guid, double> { [A] = 0 }));
    }

    [Fact]
    public void Top_ten_budget_is_shared_with_all_twenty_eligible_candidates()
    {
        var target = ExposureLedger.Targets(Enumerable.Range(1, 20)
            .ToDictionary(i => new Guid(i, 0, 0, new byte[8]), _ => 1.0));
        var budget = Enumerable.Range(1, 10).Sum(i => 1 / Math.Log2(i + 1));
        Assert.Equal(20, target.Count);
        Assert.Equal(budget, target.Values.Sum(), 10);
        Assert.All(target.Values, value => Assert.Equal(budget / 20, value, 10));
    }

    [Fact]
    public void Expiration_removes_only_old_contributions_and_snapshot_cannot_mutate_history()
    {
        var ledger = new ExposureLedger();
        var target = new Dictionary<Guid, double> { [A] = .5, [B] = .5 };
        ledger.Observe("x", Now, target, [A]);
        ledger.Observe("x", Now.AddDays(1), target, [B]);
        var copy = Assert.IsType<Dictionary<Guid, ExposureBalance>>(ledger.Snapshot("x", Now.AddDays(1)));
        copy.Clear();
        var remaining = ledger.Snapshot("x", Now.AddDays(30));
        Assert.Equal(new ExposureBalance(.5, 0), remaining[A]);
        Assert.Equal(new ExposureBalance(.5, 1), remaining[B]);
    }

    [Fact]
    public void Ledger_preserves_zeros_isolates_context_and_expires_exactly_at_30_days()
    {
        var ledger = new ExposureLedger();
        ledger.Observe("centro", Now, new Dictionary<Guid, double> { [A] = .5, [B] = .5 }, [A]);
        var state = ledger.Snapshot("centro", Now.AddDays(30).AddTicks(-1));
        Assert.Equal(new ExposureBalance(.5, 1), state[A]);
        Assert.Equal(new ExposureBalance(.5, 0), state[B]);
        Assert.Empty(ledger.Snapshot("alberdi", Now.AddDays(30).AddTicks(-1)));
        Assert.Empty(ledger.Snapshot("centro", Now.AddDays(30)));
    }

    [Fact]
    public void Observations_reject_time_travel_duplicates_unknown_ids_and_invalid_budget()
    {
        var ledger = new ExposureLedger();
        var target = new Dictionary<Guid, double> { [A] = 1 };
        Assert.Throws<ArgumentException>(() => ledger.Observe("x", Now, target, [B]));
        Assert.Throws<ArgumentException>(() => ledger.Observe("x", Now, target, [A, A]));
        Assert.Throws<ArgumentException>(() => ledger.Observe("x", Now, new Dictionary<Guid, double> { [A] = .1 }, [A]));
        ledger.Observe("x", Now, target, [A]);
        Assert.Throws<ArgumentException>(() => ledger.Snapshot("x", Now.AddTicks(-1)));
        Assert.Equal(new ExposureBalance(1, 1), ledger.Snapshot("x", Now)[A]);
    }
}
