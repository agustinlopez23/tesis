using Marketplace.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Marketplace.Api.Tests;

public sealed class CampaignSchemaTests(OnboardingFixture fixture) : IClassFixture<OnboardingFixture>
{
    [Fact]
    public async Task Campaign_history_blocks_downgrade_and_empty_schema_roundtrips()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        Assert.False(db.Database.HasPendingModelChanges());
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO user_accounts (id,email,display_name,password_hash,is_suspended,created_at)
            VALUES ('00000000-0000-0000-0000-000000000311','campaign-schema@example.test','Test','unused',false,now());
            INSERT INTO businesses (id,owner_id,category_id,name,description,address,whatsapp,location,status,created_at,updated_at)
            VALUES ('00000000-0000-0000-0000-000000000312','00000000-0000-0000-0000-000000000311',1,'Test','','','+5493515551234',ST_SetSRID(ST_MakePoint(-64.18,-31.4),4326),'draft',now(),now());
            INSERT INTO campaigns (id,business_id,photo_slot,title,description,conditions,zone_id,starts_at,ends_at,created_at,status,version,save_hash)
            VALUES ('00000000-0000-0000-0000-000000000313','00000000-0000-0000-0000-000000000312',1,'Promoción','Descripción','Condiciones','centro',now(),now()+interval '1 day',now(),'draft',1,repeat('a',64));
            """);
        var migrator = db.GetService<IMigrator>();
        var error = await Assert.ThrowsAsync<PostgresException>(() => migrator.MigrateAsync("20261005114423_AddSalesInventory"));
        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState); Assert.Single(await db.Campaigns.ToListAsync());
        await db.Campaigns.ExecuteDeleteAsync(); // Only this isolated fixture draft.
        await migrator.MigrateAsync("20261005114423_AddSalesInventory"); await migrator.MigrateAsync();
        Assert.Empty(await db.Campaigns.ToListAsync());
    }
}
