using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Marketplace.DAL;
using Marketplace.Models;
using Marketplace.Seed;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Api.Tests;

public sealed class DemoSeedTests
{
    private const string OwnerPassword = "Test-owner-seed-12345", AdminPassword = "Test-admin-seed-12345";
    private static MarketplaceDbContext Context(string connection) => new(new DbContextOptionsBuilder<MarketplaceDbContext>()
        .UseNpgsql(connection, o => o.UseNetTopologySuite()).Options);

    [Fact]
    public async Task Loads_authenticated_demo_and_repetition_preserves_edits_and_other_data()
    {
        var fixture = new OnboardingFixture(); await fixture.InitializeAsync();
        try
        {
            await using var db = Context(fixture.ConnectionString);
            using var client = fixture.Client();
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/auth/register", new { email="existing@example.test", password=OwnerPassword, displayName="Existing" })).StatusCode);
            await DemoSeeder.ApplyAsync(db, OwnerPassword, AdminPassword, "Testing");
            Assert.Equal(3, await db.Businesses.CountAsync()); Assert.Equal(12, await db.CatalogProducts.CountAsync());
            Assert.Equal(12, await db.InventoryMovements.CountAsync()); Assert.Equal(3, await db.BusinessSubscriptions.CountAsync());
            foreach (var (email, password, admin) in new[] { ("comerciante.seed@example.test", OwnerPassword, false), ("admin.seed@example.test", AdminPassword, true) })
            {
                var login = await client.PostAsJsonAsync("/auth/login", new { email, password }); Assert.Equal(HttpStatusCode.OK, login.StatusCode);
                client.DefaultRequestHeaders.Remove("Cookie"); client.DefaultRequestHeaders.Add("Cookie", login.Headers.GetValues("Set-Cookie").First().Split(';')[0]);
                Assert.Equal(admin ? HttpStatusCode.OK : HttpStatusCode.Forbidden, (await client.GetAsync("/admin/businesses")).StatusCode);
            }
            var published = await client.GetFromJsonAsync<JsonElement>("/public/products?page=1");
            Assert.Equal(8, published.GetProperty("items").GetArrayLength());
            var ownerLogin = await client.PostAsJsonAsync("/auth/login", new { email="comerciante.seed@example.test", password=OwnerPassword });
            client.DefaultRequestHeaders.Remove("Cookie"); client.DefaultRequestHeaders.Add("Cookie", ownerLogin.Headers.GetValues("Set-Cookie").First().Split(';')[0]);
            var businessId = Guid.Parse("de100000-0000-4000-8000-000000000001");
            var productId = Guid.Parse("de200000-0000-4000-8000-000000000001");
            var orderId = Guid.NewGuid();
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync($"/public/businesses/{businessId}/orders", new {
                requestId=orderId, accessKey=new string('a',64), customerName="Prueba seed", lines=new[] { new { productId, quantity=1, expectedVersion=1 } }
            })).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await client.PutAsJsonAsync($"/businesses/{businessId}/sales/{Guid.NewGuid()}", new {
                orderId, orderVersion=1, lines=new[] { new { productId, quantity=1, unitPrice=7500, currency="ARS", origin="requested" } }
            })).StatusCode);
            db.ChangeTracker.Clear();
            var product = await db.CatalogProducts.FirstAsync(); product.Price = 123;
            var balance = await db.InventoryBalances.FirstAsync(); balance.Quantity = 2;
            var subscription = await db.BusinessSubscriptions.FirstAsync(); subscription.EndsAt = subscription.StartsAt.AddHours(1);
            var role = await db.UserAccountRoles.SingleAsync(x => x.UserId == DemoSeeder.AdminId && x.RoleId == 2); db.Remove(role);
            await db.SaveChangesAsync();
            await db.Entry(subscription).ReloadAsync(); // Compare persisted PostgreSQL timestamp precision.
            await Task.WhenAll(Repeat(), Repeat());
            db.ChangeTracker.Clear();
            Assert.Equal(123, (await db.CatalogProducts.FindAsync(product.Id))!.Price);
            Assert.Equal(2, (await db.InventoryBalances.FindAsync(balance.ProductId))!.Quantity);
            Assert.Equal(subscription.EndsAt, (await db.BusinessSubscriptions.FindAsync(subscription.Id))!.EndsAt);
            Assert.False(await db.UserAccountRoles.AnyAsync(x => x.UserId == DemoSeeder.AdminId && x.RoleId == 2));
            Assert.Equal(3, await db.UserAccounts.CountAsync()); Assert.Equal(13, await db.InventoryMovements.CountAsync());
            Assert.Equal(1, await db.Sales.CountAsync()); Assert.Equal(1, await db.CustomerOrders.CountAsync());
            Assert.Equal(3, await db.Businesses.CountAsync()); Assert.Equal(12, await db.CatalogProducts.CountAsync());
            async Task Repeat() { await using var again = Context(fixture.ConnectionString); await DemoSeeder.ApplyAsync(again, OwnerPassword, AdminPassword, "Testing"); }
        }
        finally { await fixture.DisposeAsync(); }
    }

    [Fact]
    public async Task Email_collision_rolls_back_without_changing_existing_identity()
    {
        var fixture = new OnboardingFixture(); await fixture.InitializeAsync();
        try
        {
            using var client = fixture.Client();
            await client.PostAsJsonAsync("/auth/register", new { email="admin.seed@example.test", password=OwnerPassword, displayName="Existing" });
            await using var db = Context(fixture.ConnectionString);
            await Assert.ThrowsAsync<InvalidOperationException>(() => DemoSeeder.ApplyAsync(db, OwnerPassword, AdminPassword, "Testing"));
            db.ChangeTracker.Clear();
            Assert.Equal(1, await db.UserAccounts.CountAsync()); Assert.Empty(await db.Businesses.ToListAsync());
            Assert.False(await db.UserAccountRoles.AnyAsync(x => x.RoleId == 2));
        }
        finally { await fixture.DisposeAsync(); }
    }

    [Fact]
    public async Task Production_is_rejected_before_connecting()
    {
        await using var db = Context("Host=localhost;Database=unused;Username=unused");
        await Assert.ThrowsAsync<InvalidOperationException>(() => DemoSeeder.ApplyAsync(db, OwnerPassword, AdminPassword, "Production"));
    }
}
