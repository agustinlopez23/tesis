using Marketplace.BLL;
using Marketplace.DAL;
using Marketplace.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Marketplace.Api.Tests;

public sealed class RankingSnapshotPageTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    [Fact]
    public async Task Pages_preserve_positions_revalidate_all_eligibility_and_never_assign_again()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        var owners = Enumerable.Range(0, 45).Select(i => new UserAccount
            { Email = $"page-{Guid.NewGuid():N}@example.test", DisplayName = "Dueño", PasswordHash = "unused" }).ToArray();
        db.UserAccounts.AddRange(owners);
        var businesses = owners.Select(owner => new BusinessProfile
        {
            OwnerId = owner.Id, CategoryId = 1, Name = "Original", Description = "", Address = "Córdoba",
            WhatsApp = "+5493515551234", Status = "published", Location = new(-31.416281, -64.184022)
        }).ToArray();
        db.Businesses.AddRange(businesses);
        await db.SaveChangesAsync();
        var snapshot = (await scope.ServiceProvider.GetRequiredService<RankingSnapshotService>().CreateAsync(
            Guid.NewGuid(), "centro", 1, RankingVariant.EquityOnly, new(), 17, null, default)).Snapshot!;
        var pages = scope.ServiceProvider.GetRequiredService<RankingSnapshotPages>();
        var first = (await pages.ReadAsync(snapshot.Id, 0, default))!;
        var second = (await pages.ReadAsync(snapshot.Id, 20, default))!;
        var third = (await pages.ReadAsync(snapshot.Id, 40, default))!;
        Assert.Equal(new[] { 20, 20, 5 }, new[] { first.Items.Count, second.Items.Count, third.Items.Count });
        Assert.Equal(20, first.NextOffset);
        Assert.Equal(40, second.NextOffset);
        Assert.Null(third.NextOffset);
        Assert.Equal(snapshot.BusinessIds, first.Items.Concat(second.Items).Concat(third.Items).Select(x => x.Business.Id));
        Assert.Equal(second.Items, (await pages.ReadAsync(snapshot.Id, 20, default))!.Items);
        BusinessProfile At(int index) => businesses.Single(x => x.Id == snapshot.BusinessIds[index]);
        At(20).Status = "draft";
        owners.Single(x => x.Id == At(21).OwnerId).IsSuspended = true;
        At(22).CategoryId = 2;
        At(23).Location = new(-32, -65);
        At(24).Name = "Actualizado";
        At(24).WhatsApp = "+5493515559999";
        db.Businesses.Add(new BusinessProfile
        {
            OwnerId = owners[0].Id, CategoryId = 1, Name = "Nuevo", Description = "", Address = "Córdoba",
            WhatsApp = "+5493515551234", Status = "published", Location = new(-31.416281, -64.184022)
        });
        await db.SaveChangesAsync();
        var changed = (await pages.ReadAsync(snapshot.Id, 20, default))!;
        Assert.Equal(16, changed.Items.Count);
        Assert.Equal(Enumerable.Range(25, 16), changed.Items.Select(x => x.Position));
        Assert.Equal(snapshot.BusinessIds.Skip(24).Take(16), changed.Items.Select(x => x.Business.Id));
        Assert.Equal("Actualizado", changed.Items[0].Business.Name);
        Assert.Equal("+5493515559999", changed.Items[0].Business.WhatsApp);
        Assert.Equal(40, changed.NextOffset);
        foreach (var business in snapshot.BusinessIds.Skip(20).Take(20).Select(id => businesses.Single(x => x.Id == id)))
            business.Status = "draft";
        await db.SaveChangesAsync();
        var empty = (await pages.ReadAsync(snapshot.Id, 20, default))!;
        Assert.Empty(empty.Items);
        Assert.Equal(40, empty.NextOffset);
        Assert.Equal(third.Items, (await pages.ReadAsync(snapshot.Id, 40, default))!.Items);
        Assert.Equal(1, await db.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM ranking_opportunities WHERE context_key = {snapshot.ContextKey}").SingleAsync());
        Assert.Null(await pages.ReadAsync(Guid.NewGuid(), 0, default));
        foreach (var offset in new[] { -20, 1, 60, int.MaxValue })
            await Assert.ThrowsAnyAsync<ArgumentException>(() => pages.ReadAsync(snapshot.Id, offset, default));
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE ranking_snapshots SET expires_at = now() WHERE id = {snapshot.Id}");
        Assert.Null(await pages.ReadAsync(snapshot.Id, 0, default));
    }
}
