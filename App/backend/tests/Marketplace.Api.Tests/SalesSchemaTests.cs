using Marketplace.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Marketplace.Api.Tests;

public sealed class SalesSchemaTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    [Fact]
    public async Task Inventory_history_blocks_downgrade_and_empty_schema_roundtrips()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        Assert.False(db.Database.HasPendingModelChanges());
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO user_accounts (id,email,display_name,password_hash,is_suspended,created_at)
            VALUES ('00000000-0000-0000-0000-000000000211','sales-schema@example.test','Test','unused',false,now());
            INSERT INTO businesses (id,owner_id,category_id,name,description,address,whatsapp,location,status,created_at,updated_at)
            VALUES ('00000000-0000-0000-0000-000000000212','00000000-0000-0000-0000-000000000211',1,'Test','','','+5493515551234',ST_SetSRID(ST_MakePoint(-64.18,-31.4),4326),'draft',now(),now());
            INSERT INTO catalog_products (id,business_id,name,description,category,price,currency,available,status,version,created_at,updated_at)
            VALUES ('00000000-0000-0000-0000-000000000213','00000000-0000-0000-0000-000000000212','Product','','Test',1,'ARS',true,'draft',1,now(),now());
            INSERT INTO inventory_balances (product_id,quantity,version) VALUES ('00000000-0000-0000-0000-000000000213',7,1);
            """);
        var migrator = db.GetService<IMigrator>();
        var error = await Assert.ThrowsAsync<PostgresException>(() => migrator.MigrateAsync("20261005111735_AddProductOrders"));
        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
        Assert.Equal(7, (await db.InventoryBalances.SingleAsync()).Quantity);
        await db.InventoryBalances.ExecuteDeleteAsync(); // Only the fixture balance above.
        await migrator.MigrateAsync("20261005111735_AddProductOrders");
        await migrator.MigrateAsync();
        Assert.Empty(await db.InventoryBalances.ToListAsync());
    }
}
