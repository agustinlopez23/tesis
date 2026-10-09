using Marketplace.DAL;
using Marketplace.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Marketplace.Api.Tests;

public sealed class CatalogSchemaTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    [Fact]
    public async Task Downgrade_preserves_catalog_data_and_empty_schema_can_roundtrip()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        Assert.False(db.Database.HasPendingModelChanges());
        var account = new UserAccount { Email = $"schema-{Guid.NewGuid():N}@example.test", DisplayName = "Schema", PasswordHash = "unused" };
        var business = new BusinessProfile { OwnerId = account.Id, CategoryId = 1, Name = "Schema", Description = "", Address = "Córdoba",
            WhatsApp = "+5493515551234", Status = "draft", Location = new(-31.4, -64.18) };
        var product = new CatalogProduct { Id = Guid.NewGuid(), BusinessId = business.Id, Name = "Histórico", Description = "", Category = "General",
            Price = 15.25m, Currency = "ARS", Available = true, Status = "archived", Version = 1, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
        db.UserAccounts.Add(account); db.Businesses.Add(business); db.CatalogProducts.Add(product);
        await db.SaveChangesAsync();
        var migrator = db.GetService<IMigrator>();
        var blocked = await Assert.ThrowsAsync<PostgresException>(() => migrator.MigrateAsync("20261005020557_AllowRankingActivityVersions"));
        Assert.Equal(PostgresErrorCodes.CheckViolation, blocked.SqlState);
        Assert.Equal(15.25m, (await db.CatalogProducts.SingleAsync()).Price);
        // Remove only this generated fixture product to exercise an empty downgrade.
        await db.CatalogProducts.Where(p => p.Id == product.Id).ExecuteDeleteAsync();
        await migrator.MigrateAsync("20261005020557_AllowRankingActivityVersions");
        await migrator.MigrateAsync();
        Assert.Empty(await db.CatalogProducts.ToListAsync());
        Assert.Equal("administrator", (await db.Roles.SingleAsync(r => r.Id == 2)).Name);
    }
}
