using Marketplace.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Marketplace.Api.Tests;

public sealed class RankingStorageSchemaTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    [Fact]
    public async Task Infrastructure_migration_roundtrips_without_changing_business_model()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        Assert.Equal(4, await TableCount(db));
        Assert.False(db.Database.HasPendingModelChanges());
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260922042650_AddBusinessPublicationHistory");
        Assert.Equal(0, await TableCount(db));
        Assert.Equal(3, await db.BusinessCategories.CountAsync());
        await migrator.MigrateAsync();
        Assert.Equal(4, await TableCount(db));
    }

    [Fact]
    public async Task Unique_request_and_contribution_constraints_and_cascade_are_enforced()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        var id = Guid.NewGuid();
        var request = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO ranking_contexts (context_key, sequence) VALUES ('schema-test', 1);
            INSERT INTO ranking_opportunities (id, context_key, sequence, occurred_at, payload)
            VALUES ({id}, 'schema-test', 1, now(), jsonb_build_object());
            INSERT INTO ranking_snapshots (id, request_id, context_key, expires_at, payload)
            VALUES ({id}, {request}, 'schema-test', now() + interval '30 minutes', jsonb_build_object());
            INSERT INTO ranking_contributions (opportunity_id, business_id, expected, received)
            VALUES ({id}, {Guid.NewGuid()}, 0.5, 0);
            """);
        var duplicate = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO ranking_snapshots (id, request_id, context_key, expires_at, payload)
            VALUES ({Guid.NewGuid()}, {request}, 'schema-test', now(), jsonb_build_object())
            """));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, duplicate.SqlState);
        var invalid = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO ranking_contributions (opportunity_id, business_id, expected, received)
            VALUES ({id}, {Guid.NewGuid()}, 1.01, 0)
            """));
        Assert.Equal(PostgresErrorCodes.CheckViolation, invalid.SqlState);
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM ranking_opportunities WHERE id = {id}");
        Assert.Equal(0, await db.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM ranking_contributions WHERE opportunity_id = {id}").SingleAsync());
        Assert.Equal(1, await db.Database.SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM ranking_snapshots WHERE id = {id}").SingleAsync());
    }

    private static Task<int> TableCount(MarketplaceDbContext db) => db.Database.SqlQuery<int>($"""
        SELECT count(*)::int AS "Value" FROM information_schema.tables
        WHERE table_schema = 'public' AND table_name IN
        ('ranking_contexts', 'ranking_opportunities', 'ranking_snapshots', 'ranking_contributions')
        """).SingleAsync();
}
