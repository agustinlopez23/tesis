using Marketplace.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Marketplace.Api.Tests;

public sealed class OrderSchemaTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    [Fact]
    public async Task Populated_orders_block_downgrade_but_empty_schema_roundtrips()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        Assert.False(db.Database.HasPendingModelChanges());
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO user_accounts (id,email,display_name,password_hash,is_suspended,created_at)
            VALUES ('00000000-0000-0000-0000-000000000111','order-schema@example.test','Test','unused',false,now());
            INSERT INTO businesses (id,owner_id,category_id,name,description,address,whatsapp,location,status,created_at,updated_at)
            VALUES ('00000000-0000-0000-0000-000000000112','00000000-0000-0000-0000-000000000111',1,'Test','','','+5493515551234',ST_SetSRID(ST_MakePoint(-64.18,-31.4),4326),'draft',now(),now());
            INSERT INTO customer_orders (id,business_id,reference,customer_name,business_name,whatsapp,access_key_hash,request_hash,access_expires_at,created_at,updated_at,status,source,version)
            VALUES ('00000000-0000-0000-0000-000000000113','00000000-0000-0000-0000-000000000112','CE-SCHEMA','Ana','Test','+5493515551234',repeat('a',64),repeat('b',64),now()+interval '30 days',now(),now(),'pending','catalog',1);
            """);
        var migrator = db.GetService<IMigrator>();
        var error = await Assert.ThrowsAsync<PostgresException>(() => migrator.MigrateAsync("20261005025720_AddBusinessCatalog"));
        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
        Assert.Single(await db.CustomerOrders.ToListAsync());
        // This fixture order only: exercise the supported empty downgrade.
        await db.CustomerOrders.ExecuteDeleteAsync();
        await migrator.MigrateAsync("20261005025720_AddBusinessCatalog");
        await migrator.MigrateAsync();
        Assert.Empty(await db.CustomerOrders.ToListAsync());
    }
}
