using Marketplace.DAL;
using Marketplace.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Marketplace.Seed;

public static class DemoSeeder
{
    public static readonly Guid OwnerId = Guid.Parse("de000000-0000-4000-8000-000000000001");
    public static readonly Guid AdminId = Guid.Parse("de000000-0000-4000-8000-000000000002");
    public static async Task ApplyAsync(MarketplaceDbContext db, string ownerPassword, string adminPassword, string environment)
    {
        if (environment is not ("Development" or "Testing")) throw new InvalidOperationException("Seed permitido solo en Development/Testing.");
        var connection = new NpgsqlConnectionStringBuilder(db.Database.GetConnectionString());
        if (connection.Host is not ("localhost" or "127.0.0.1" or "::1")) throw new InvalidOperationException("El seed requiere una base local.");
        if (ownerPassword.Length is < 12 or > 128 || adminPassword.Length is < 12 or > 128 || ownerPassword == adminPassword)
            throw new InvalidOperationException("Se requieren dos contraseñas distintas de 12 a 128 caracteres.");
        var data = JsonSerializer.Deserialize<SeedData>(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "demo-seed.json")), new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Seed vacío.");
        var ids = data.Businesses.Select(x => x.Id).Concat(data.Businesses.SelectMany(x => x.Products).Select(x => x.Id)).ToArray();
        if (ids.Length == 0 || ids.Contains(Guid.Empty) || ids.Distinct().Count() != ids.Length) throw new InvalidOperationException("Identificadores de seed inválidos o repetidos.");
        if ((await db.Database.GetPendingMigrationsAsync()).Any()) throw new InvalidOperationException("Aplicar las migraciones antes de cargar el seed.");
        await using var transaction = await db.Database.BeginTransactionAsync();
        // Serialize concurrent runs; existing records are never overwritten.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20261005, 1)");
        await Account(OwnerId, "comerciante.seed@example.test", "Comerciante demo", ownerPassword, false);
        await Account(AdminId, "admin.seed@example.test", "Administración demo", adminPassword, true);
        var now = DateTimeOffset.UtcNow;
        foreach (var item in data.Businesses)
        {
            var business = await db.Businesses.FindAsync(item.Id);
            if (business is not null && business.OwnerId != OwnerId) throw new InvalidOperationException("ID de negocio ocupado por otro titular.");
            if (business is null)
            {
                db.Businesses.Add(new BusinessProfile { Id=item.Id, OwnerId=OwnerId, CategoryId=item.CategoryId, Name=item.Name,
                    Description=item.Description, Address=item.Address, WhatsApp="+5493515550101", Location=new(item.Latitude, item.Longitude), Status="published", CreatedAt=now, UpdatedAt=now });
                db.BusinessSubscriptions.Add(new BusinessSubscription { Id=item.Id, BusinessId=item.Id, StartsAt=now.AddMinutes(-1), EndsAt=now.AddDays(365),
                    GrantedAt=now, GrantedBy=AdminId, Reason="Plan ficticio del seed de demostración. Sin cobro." });
                await db.SaveChangesAsync();
            }
            foreach (var product in item.Products)
            {
                var existing = await db.CatalogProducts.FindAsync(product.Id);
                if (existing is not null)
                {
                    if (existing.BusinessId != item.Id) throw new InvalidOperationException("ID de producto ocupado por otro negocio.");
                    continue;
                }
                db.CatalogProducts.Add(new CatalogProduct { Id=product.Id, BusinessId=item.Id, Name=product.Name, Description=product.Description,
                    Category=product.Category, Price=product.Price, Currency=product.Currency, Available=product.Available, Status=product.Status,
                    Version=1, CreatedAt=now, UpdatedAt=now });
                db.InventoryBalances.Add(new InventoryBalance { ProductId=product.Id, Quantity=product.Stock, Version=1 });
                db.InventoryMovements.Add(new InventoryMovement { OperationId=product.Id, ProductId=product.Id, BusinessId=item.Id, ActorId=OwnerId,
                    Delta=product.Stock, Balance=product.Stock, Kind="adjustment", Reason="Saldo inicial ficticio del seed demo",
                    RequestHash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"demo-v1:{product.Id}:{product.Stock}"))), CreatedAt=now });
            }
            await db.SaveChangesAsync();
        }
        await transaction.CommitAsync();

        async Task Account(Guid id, string email, string name, string password, bool admin)
        {
            var byEmail = await db.UserAccounts.SingleOrDefaultAsync(x => x.Email == email);
            var byId = await db.UserAccounts.FindAsync(id);
            if (byEmail is not null && byEmail.Id != id || byId is not null && byId.Email != email)
                throw new InvalidOperationException("Identidad de seed ocupada; no se modificó la cuenta existente.");
            var hasher = new PasswordHasher<UserAccount>();
            if (byId is not null)
            {
                if (hasher.VerifyHashedPassword(byId, byId.PasswordHash, password) == PasswordVerificationResult.Failed)
                    throw new InvalidOperationException("Usá las credenciales originales del seed; no se restablecen contraseñas.");
                return;
            }
            var user = new UserAccount { Id=id, Email=email, DisplayName=name, PasswordHash="" };
            user.PasswordHash = hasher.HashPassword(user, password);
            user.Roles.Add(new UserAccountRole { UserId=id, RoleId=1 });
            if (admin) user.Roles.Add(new UserAccountRole { UserId=id, RoleId=2 });
            db.UserAccounts.Add(user); await db.SaveChangesAsync();
        }
    }

    private sealed record SeedData(SeedBusiness[] Businesses);
    private sealed record SeedBusiness(Guid Id, string Name, short CategoryId, string Description, string Address, double Latitude, double Longitude, SeedProduct[] Products);
    private sealed record SeedProduct(Guid Id, string Name, string Description, string Category, decimal Price, string Currency, int Stock, bool Available, string Status);
}
