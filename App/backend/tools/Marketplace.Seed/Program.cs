using Marketplace.DAL;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Seed;

internal static class SeedProgram
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var hosted = args.Length == 2 && args[1] == DemoSeeder.HostedOptIn;
            if (args.Length is < 1 or > 2 || args[0] != "--apply" || args.Length == 2 && !hosted)
                throw new InvalidOperationException($"Uso: Marketplace.Seed --apply [{DemoSeeder.HostedOptIn}]. Ver App/seed/README.md.");
            var connection = Required("ConnectionStrings__Marketplace");
            await using var db = new MarketplaceDbContext(new DbContextOptionsBuilder<MarketplaceDbContext>().UseNpgsql(connection, o => o.UseNetTopologySuite()).Options);
            await DemoSeeder.ApplyAsync(db, Required("SEED_OWNER_PASSWORD"), Required("SEED_ADMIN_PASSWORD"), Required("ASPNETCORE_ENVIRONMENT"),
                hosted ? Required(DemoSeeder.HostedConfirmationVariable) : null);
            Console.WriteLine("Seed completo. Datos existentes conservados; promociones demo vigentes por 30 días. Cuentas: comerciante.seed@example.test / admin.seed@example.test.");
            return 0;
        }
        catch (InvalidOperationException e) { Console.Error.WriteLine(e.Message); return 1; }
        catch { Console.Error.WriteLine("No se pudo cargar el seed. Revisá conexión, migraciones y datos; la transacción no se confirmó."); return 1; }
    }
    private static string Required(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : throw new InvalidOperationException($"Falta {name}.");
}
