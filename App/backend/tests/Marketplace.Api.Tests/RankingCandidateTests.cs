using Marketplace.BLL;
using Marketplace.DAL;
using Marketplace.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.Api.Tests;

public sealed class RankingCandidateTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    private static readonly DiscoveryArea Area = new(-31.4167, -64.1833, 4);

    [Fact]
    public async Task Retrieves_all_45_candidates_with_history_and_rechecks_eligibility_on_next_read()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IRankingCandidateRepository>();
        var owner = Owner();
        var suspended = Owner();
        suspended.IsSuspended = true;
        db.UserAccounts.AddRange(owner, suspended);
        var eligible = Enumerable.Range(0, 45).Select(n => Business(owner.Id, 1, n)).ToArray();
        var draft = Business(owner.Id, 1); draft.Status = "draft";
        var outside = Business(owner.Id, 1); outside.Location = new(-31.6, -64.1833);
        db.Businesses.AddRange(eligible);
        db.Businesses.AddRange(draft, outside, Business(suspended.Id, 1), Business(owner.Id, 2));
        await db.SaveChangesAsync();

        var candidates = await repository.GetEligibleAsync(1, Area, default);
        Assert.Equal(eligible.Select(x => x.Id).Order(), candidates.Select(x => x.Id));
        Assert.Equal(45, candidates.Count);
        Assert.All(candidates, x =>
        {
            Assert.Equal(owner.Id, x.OwnerId);
            Assert.InRange(x.DistanceKm, 0, 4);
            Assert.False(x.PublicationHistoryUnknown);
            Assert.Equal(eligible.Single(b => b.Id == x.Id).FirstPublishedAt, x.FirstPublishedAt);
            Assert.NotNull(x.FirstPublishedAt);
        });
        var distant = candidates.Single(x => x.Id == eligible[44].Id);
        Assert.InRange(distant.DistanceKm, .48, .50);

        var input = await scope.ServiceProvider.GetRequiredService<MagazineRankingCandidates>()
            .PrepareAsync("centro", 1, 17, 0, default);
        var ranked = new BusinessRanker(new()).Rank(input.Candidates, input.Context,
            RankingVariant.EquityNewcomer, new Dictionary<Guid, ExposureBalance>());
        Assert.Equal(eligible.Select(x => x.Id).Order(), ranked.Select(x => x.Id).Order());
        Assert.All(input.Candidates, x => Assert.Equal(
            eligible.Single(b => b.Id == x.Id).FirstPublishedAt, x.PublishedAt));
        Assert.All(input.OwnerIds.Values, id => Assert.Equal(owner.Id, id));

        eligible[0].Status = "draft";
        eligible[1].CategoryId = 2;
        eligible[2].Location = outside.Location;
        await db.SaveChangesAsync();
        var updated = await repository.GetEligibleAsync(1, Area, default);
        Assert.Equal(eligible.Skip(3).Select(x => x.Id).Order(), updated.Select(x => x.Id));
        Assert.Equal(45, candidates.Count);
        owner.IsSuspended = true;
        await db.SaveChangesAsync();
        Assert.Empty(await repository.GetEligibleAsync(1, Area, default));
        Assert.Empty(await repository.GetEligibleAsync(32000, Area, default));
    }

    [Theory]
    [InlineData(0, -31, -64, 4)]
    [InlineData(-1, -31, -64, 4)]
    [InlineData(1, 91, -64, 4)]
    [InlineData(1, -31, 181, 4)]
    [InlineData(1, double.NaN, -64, 4)]
    [InlineData(1, -31, double.PositiveInfinity, 4)]
    [InlineData(1, -31, -64, 0)]
    [InlineData(1, -31, -64, 51)]
    [InlineData(1, -31, -64, double.NaN)]
    public async Task Invalid_scope_is_rejected(short category, double latitude, double longitude, double radius)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRankingCandidateRepository>();
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            repository.GetEligibleAsync(category, new(latitude, longitude, radius), default));
    }

    [Fact]
    public async Task Missing_area_and_cancellation_do_not_return_partial_results()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRankingCandidateRepository>();
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.GetEligibleAsync(1, null!, default));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repository.GetEligibleAsync(1, Area, cancellation.Token));
    }

    [Fact]
    public async Task Invalid_persisted_identity_aborts_instead_of_silently_removing_candidate()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        var owner = Owner();
        db.UserAccounts.Add(owner);
        var business = Business(owner.Id, 3);
        db.Businesses.Add(business);
        await db.SaveChangesAsync();
        // Corrupt only this fixture's temporary database, outside normal application writes.
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE businesses SET id = {Guid.Empty} WHERE id = {business.Id}");
        var repository = scope.ServiceProvider.GetRequiredService<IRankingCandidateRepository>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.GetEligibleAsync(3, Area, default));
    }

    private static UserAccount Owner() => new()
    {
        Email = $"candidates-{Guid.NewGuid():N}@example.test", DisplayName = "Responsable", PasswordHash = "unused"
    };

    private static BusinessProfile Business(Guid owner, short category, int offset = 0) => new()
    {
        OwnerId = owner, CategoryId = category, Name = "Negocio", Description = "", Address = "Córdoba",
        WhatsApp = "+5493515551234", Status = "published", Location = new(Area.Latitude - offset * .0001, Area.Longitude)
    };
}
