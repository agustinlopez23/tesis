using Marketplace.BLL;
using Marketplace.Models;

namespace Marketplace.Api;

public static class ProductDiscoveryEndpoints
{
    public static void MapProductDiscovery(this WebApplication app) => app.MapGet("/public/products", async (
        int? page, string? q, string? category, double? lat, double? lon, double? radiusKm,
        ProductDiscoveryService products, CancellationToken ct) =>
    {
        if (page is < 1 or > 10000 || q?.Length > 150 || category?.Length > 100) return Results.BadRequest();
        DiscoveryArea? area = null;
        if (lat.HasValue || lon.HasValue || radiusKm.HasValue)
        {
            if (lat == null || lon == null || radiusKm == null) return Results.BadRequest();
            area = new(lat.Value, lon.Value, radiusKm.Value);
            if (!area.IsValid) return Results.BadRequest();
        }
        return Results.Ok(await products.Read(page ?? 1, q, category, area, ct));
    });
}
