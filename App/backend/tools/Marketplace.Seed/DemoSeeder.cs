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
    public const string HostedOptIn = "--allow-hosted-demo";
    public const string HostedConfirmationVariable = "DEMO_SEED_CONFIRM_HOST";

    /// <summary>
    /// Guards the seed target. A local database is always allowed in Development/Testing; a hosted database
    /// additionally requires the explicit opt-in and a confirmation that names exactly the target host.
    /// </summary>
    public static void EnsureTargetAllowed(string environment, string? host, string? confirmedHostedHost)
    {
        if (environment is not ("Development" or "Testing")) throw new InvalidOperationException("Seed permitido solo en Development/Testing.");
        if (host is "localhost" or "127.0.0.1" or "::1") return;
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(confirmedHostedHost))
            throw new InvalidOperationException($"El seed requiere una base local. Para una base de demostración hosteada usá {HostedOptIn} y {HostedConfirmationVariable}=<host de la base>.");
        if (!string.Equals(host, confirmedHostedHost.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{HostedConfirmationVariable} no coincide con el host de la conexión; no se modificó la base.");
    }

    public static async Task ApplyAsync(MarketplaceDbContext db, string ownerPassword, string adminPassword, string environment, string? confirmedHostedHost = null)
    {
        var connection = new NpgsqlConnectionStringBuilder(db.Database.GetConnectionString());
        EnsureTargetAllowed(environment, connection.Host, confirmedHostedHost);
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
        foreach (var item in data.Campaigns ?? []) await Campaign(item);
        await transaction.CommitAsync();

        // Promotions are seeded as already approved and paid, mirroring the audit trail of the real flow.
        // Re-running renews the window of scheduled promotions; paused or rejected ones are left untouched.
        async Task Campaign(SeedCampaign item)
        {
            var business = data.Businesses.SingleOrDefault(b => b.Id == item.BusinessId);
            if (item.Id == Guid.Empty || ids.Contains(item.Id) || business is null || !business.Products.Any(p => p.Id == item.ProductId) || !Marketplace.BLL.CampaignService.Zone(item.ZoneId))
                throw new InvalidOperationException("Promoción de seed inválida.");
            var startsAt = now.AddDays(-1); var endsAt = now.AddDays(29);
            var existing = await db.Campaigns.FindAsync(item.Id);
            if (existing is not null && (existing.BusinessId != item.BusinessId || existing.ProductId != item.ProductId))
                throw new InvalidOperationException("ID de promoción ocupado por otra campaña.");
            if (existing is not null && (existing.Status != "scheduled" || existing.ApprovedAt is null || existing.PaidAt is null)) return;
            // Same zone lock and capacity rule as confirm_payment/resume in CampaignRepository.
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({item.ZoneId}))");
            if (await db.Campaigns.CountAsync(c => c.Id != item.Id && c.ZoneId == item.ZoneId && (c.Status == "scheduled" || c.Status == "paused") &&
                c.StartsAt < endsAt && c.EndsAt > startsAt) >= 3) throw new InvalidOperationException($"La zona {item.ZoneId} no tiene cupo para la promoción demo.");
            if (existing is null)
            {
                existing = new Campaign { Id=item.Id, BusinessId=item.BusinessId, ProductId=item.ProductId, Title=item.Title, Description=item.Description,
                    Conditions=item.Conditions, ZoneId=item.ZoneId, StartsAt=startsAt, EndsAt=endsAt, CreatedAt=now, Status="scheduled", Version=3,
                    SaveHash=Hash($"demo-v1:campaign:{item.Id}"), ApprovedAt=now, PaidAt=now, PaymentReference=item.PaymentReference,
                    Image=await db.ProductPhotos.Where(p => p.ProductId == item.ProductId).Select(p => p.Content).SingleOrDefaultAsync() };
                db.Campaigns.Add(existing);
                Change(existing, 1, OwnerId, "submit", "Contenido preparado por el negocio (seed demo)");
                Change(existing, 2, AdminId, "approve", "Aprobada para la demostración");
                Change(existing, 3, AdminId, "confirm_payment", "Pago ficticio del seed de demostración. Sin cobro.");
            }
            else
            {
                existing.StartsAt = startsAt; existing.EndsAt = endsAt; existing.Version++;
                Change(existing, existing.Version, AdminId, "resolve", "Ventana de demostración renovada por el seed");
            }
            await db.SaveChangesAsync();
        }

        void Change(Campaign campaign, int version, Guid actor, string action, string reason) => db.CampaignChanges.Add(new CampaignChange {
            OperationId=Guid.NewGuid(), CampaignId=campaign.Id, ActorId=actor, Version=version, Action=action, Reason=reason,
            RequestHash=Hash($"demo-v1:campaign:{campaign.Id}:{version}:{action}"), CreatedAt=now });

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

    private static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private sealed record SeedData(SeedBusiness[] Businesses, SeedCampaign[]? Campaigns);
    private sealed record SeedCampaign(Guid Id, Guid BusinessId, Guid ProductId, string ZoneId, string Title, string Description, string Conditions, string PaymentReference);
    private sealed record SeedBusiness(Guid Id, string Name, short CategoryId, string Description, string Address, double Latitude, double Longitude, SeedProduct[] Products);
    private sealed record SeedProduct(Guid Id, string Name, string Description, string Category, decimal Price, string Currency, int Stock, bool Available, string Status);
}
