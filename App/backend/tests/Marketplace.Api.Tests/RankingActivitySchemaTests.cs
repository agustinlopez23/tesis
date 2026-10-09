using Marketplace.DAL;
using Marketplace.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Marketplace.Api.Tests;

public sealed class RankingActivitySchemaTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    [Fact]
    public async Task Policy_hashes_require_directory_context_and_downgrade_never_loses_attribution()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        Assert.False(db.Database.HasPendingModelChanges());
        var owner = new UserAccount { Email = $"schema-{Guid.NewGuid():N}@example.test", DisplayName = "Schema", PasswordHash = "unused" };
        var business = new BusinessProfile { OwnerId = owner.Id, CategoryId = 1, Name = "Schema", Description = "",
            Address = "Córdoba", WhatsApp = "+5493515551234", Status = "published", Location = new(-31.416281, -64.184022) };
        db.UserAccounts.Add(owner);
        db.Businesses.Add(business);
        await db.SaveChangesAsync();
        var hash = new string('a', 64);
        Task<int> Insert(string source, int? position, string? version) => db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO business_activity (visit_id, business_id, kind, surface, source, position, ranking_version, occurred_at)
            VALUES ({Guid.NewGuid()}, {business.Id}, 'impression', 'directory', {source}, {position}, {version}, now())
            """);
        await Insert("directory", 1, "name-v1");
        await Insert("directory", 22, hash);
        foreach (var (source, position, version) in new (string, int?, string?)[] {
            ("search", 1, hash), ("directory", 1, "forged"), ("directory", null, hash), ("directory", 1, null), ("directory", 0, hash) })
        {
            var error = await Assert.ThrowsAsync<PostgresException>(() => Insert(source, position, version));
            Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
        }
        var migrator = db.GetService<IMigrator>();
        var blocked = await Assert.ThrowsAsync<PostgresException>(() => migrator.MigrateAsync("20261004100000_AddRankingSnapshots"));
        Assert.Equal(PostgresErrorCodes.CheckViolation, blocked.SqlState);
        Assert.Equal(hash, (await db.BusinessActivity.SingleAsync(x => x.Position == 22)).RankingVersion);
        Assert.Equal(2, await db.BusinessActivity.CountAsync());
        // Only this generated fixture row is removed to exercise the post-retention downgrade.
        await db.BusinessActivity.Where(x => x.RankingVersion == hash).ExecuteDeleteAsync();
        await migrator.MigrateAsync("20261004100000_AddRankingSnapshots");
        Assert.Equal("name-v1", (await db.BusinessActivity.SingleAsync()).RankingVersion);
        await migrator.MigrateAsync();
        await Insert("directory", 22, hash);
        Assert.Equal(2, await db.BusinessActivity.CountAsync());
    }
}
