using Marketplace.Models;

namespace Marketplace.BLL.Tests;

// HU-22: owner activity reaches the ranking as a bounded signal and never changes equity-v1.
public sealed class OwnerActivityRankingTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-02T12:00:00Z");

    [Theory]
    [InlineData(0, 0)]
    [InlineData(5, .5)]
    [InlineData(10, 1)]
    [InlineData(500, 1)]
    public void Activity_signal_is_capped_by_the_configured_value(int updates, double expected) =>
        Assert.Equal(expected, new OwnerActivitySettings(TimeSpan.FromHours(1), 10).Signal(updates), 12);

    [Fact]
    public void Settings_reject_a_non_positive_cap_or_window()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new OwnerActivitySettings(TimeSpan.FromHours(1), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new OwnerActivitySettings(TimeSpan.Zero, 10));
    }

    [Fact]
    public async Task Candidates_carry_the_capped_owner_activity()
    {
        var rows = new[] { Row(1, 0), Row(2, 3), Row(3, 1000) };
        var input = await new MagazineRankingCandidates(new Repository(rows), new Clock(), new OwnerActivitySettings(TimeSpan.FromHours(1), 6))
            .PrepareAsync("centro", 1, 17, 1, default);
        Assert.Equal([0, .5, 1], input.Candidates.Select(x => x.Activity));
    }

    [Fact]
    public async Task Very_frequent_activity_cannot_grow_the_advantage_without_limit_and_equity_v1_ignores_it()
    {
        var calm = await Prepare(Row(1, 0), Row(2, 10));
        var frantic = await Prepare(Row(1, 0), Row(2, 100000));
        var policy = RankingPolicies.EquityV1;
        var history = new Dictionary<Guid, ExposureBalance>();
        var ranker = new BusinessRanker(policy.Configuration);

        // equity-v1 (EquityOnly) is the evaluated policy: owner activity does not move it.
        var silent = (await Prepare(Row(1, 0), Row(2, 0))).Candidates;
        Assert.Equal(ranker.Rank(silent, calm.Context, policy.Variant, history).Select(x => (x.Id, x.Score)),
            ranker.Rank(calm.Candidates, calm.Context, policy.Variant, history).Select(x => (x.Id, x.Score)));

        // In the activity variant the contribution saturates at the cap.
        var calmScore = ranker.Rank(calm.Candidates, calm.Context, RankingVariant.EquityActivity, history).Single(x => x.Activity > 0);
        var franticScore = ranker.Rank(frantic.Candidates, frantic.Context, RankingVariant.EquityActivity, history).Single(x => x.Activity > 0);
        Assert.Equal(calmScore.Score, franticScore.Score, 12);
        Assert.True(franticScore.Activity <= 1);
    }

    private static Task<MagazineRankingInput> Prepare(params EligibleRankingBusiness[] rows) =>
        new MagazineRankingCandidates(new Repository(rows), new Clock(), new OwnerActivitySettings(TimeSpan.FromHours(1), 10))
            .PrepareAsync("centro", 1, 17, 1, default);
    private static EligibleRankingBusiness Row(int id, int updates) =>
        new(new Guid(id, 0, 0, new byte[8]), Guid.NewGuid(), 1, Now.AddDays(-60), false, updates);
    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => Now; }
    private sealed class Repository(IReadOnlyList<EligibleRankingBusiness> rows) : IRankingCandidateRepository
    {
        public Task<IReadOnlyList<EligibleRankingBusiness>> GetEligibleAsync(short categoryId, DiscoveryArea area, CancellationToken cancellationToken) =>
            Task.FromResult(rows);
    }
}
