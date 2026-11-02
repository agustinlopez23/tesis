using Marketplace.BLL;
using Marketplace.Models;

namespace Marketplace.Api;

public static class ProductDiscoveryEndpoints
{
    public static void MapProductDiscovery(this WebApplication app) => app.MapGet("/public/products", async (
        int? page, string? q, string? category, double? lat, double? lon, double? radiusKm, string? zone,
        ProductDiscoveryService products, IConfiguration config, CancellationToken ct) =>
    {
        if (page is < 1 or > 10000 || q?.Length > 150 || category?.Length > 100) return Results.BadRequest();
        DiscoveryArea? area = null;
        if (zone is not null)
        {
            // A zone search is centered on the public reference point; client coordinates are ignored.
            if (radiusKm is null || !TryZone(zone, out var center)) return Results.BadRequest();
            area = center with { RadiusKm = radiusKm.Value };
            if (!area.IsValid) return Results.BadRequest();
        }
        else if (lat.HasValue || lon.HasValue || radiusKm.HasValue)
        {
            if (lat == null || lon == null || radiusKm == null) return Results.BadRequest();
            area = new(lat.Value, lon.Value, radiusKm.Value);
            if (!area.IsValid) return Results.BadRequest();
        }
        return Results.Ok(await products.Read(page ?? 1, q, category, area, zone, config.GetValue<bool>("Ranking:Enabled"), ct));
    });

    private static bool TryZone(string zone, out DiscoveryArea center)
    {
        try { center = MagazineRankingCandidates.ResolveScope(zone, 1).Area; return true; }
        catch (ArgumentException) { center = null!; return false; }
    }
}
