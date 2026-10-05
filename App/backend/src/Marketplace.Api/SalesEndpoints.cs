using Marketplace.BLL;
using Marketplace.DTOs;
using System.Text.Json;

namespace Marketplace.Api;

public static class SalesEndpoints
{
    public static void MapSales(this WebApplication app)
    {
        var group = app.MapGroup("/businesses/{business:guid}");
        group.AddEndpointFilter(async (context, next) => {
            var http = context.HttpContext;
            if (http.Request.Method != "GET" && (http.Request.Headers.Origin != http.RequestServices.GetRequiredService<IConfiguration>()["FrontendOrigin"] || http.Request.Headers["X-Requested-With"] != "marketplace")) return Results.StatusCode(403);
            var user = await http.RequestServices.GetRequiredService<AuthenticationService>().GetSessionAsync(http.Request.Cookies["marketplace_session"], http.RequestAborted);
            if (user == null) return Results.Unauthorized();
            http.Items["sales-owner"] = user.Id;
            return await next(context);
        });
        group.MapGet("/inventory", async (Guid business, HttpContext http, SalesService service, CancellationToken ct) => Found(await service.Inventory(business, Owner(http), ct)));
        group.MapGet("/inventory/movements", async (Guid business, Guid? productId, int? page, HttpContext http, SalesService service, CancellationToken ct) =>
            page is < 1 or > 10000 ? Results.BadRequest() : Found(await service.Movements(business, Owner(http), productId, page ?? 1, ct)));
        group.MapGet("/sales", async (Guid business, Guid? orderId, int? page, HttpContext http, SalesService service, CancellationToken ct) =>
            page is < 1 or > 10000 ? Results.BadRequest() : Found(await service.List(business, Owner(http), orderId, page ?? 1, ct)));
        group.MapGet("/sales/{id:guid}", async (Guid business, Guid id, HttpContext http, SalesService service, CancellationToken ct) => Found(await service.Read(business, Owner(http), id, ct)));
        group.MapPut("/inventory/adjustments/{id:guid}", async (Guid business, Guid id, HttpContext http, SalesService service, CancellationToken ct) =>
            await Write<AdjustStockRequest>(http, body => service.Adjust(business, Owner(http), id, body, ct), ct));
        group.MapPut("/sales/{id:guid}", async (Guid business, Guid id, HttpContext http, SalesService service, CancellationToken ct) =>
            await Write<CreateSaleRequest>(http, body => service.Create(business, Owner(http), id, body, ct), ct));
        group.MapPut("/sales/{id:guid}/void", async (Guid business, Guid id, HttpContext http, SalesService service, CancellationToken ct) =>
            await Write<VoidSaleRequest>(http, body => service.Void(business, Owner(http), id, body, ct), ct));
    }
    private static Guid Owner(HttpContext http) => (Guid)http.Items["sales-owner"]!;
    private static IResult Found(object? value) => value == null ? Results.NotFound() : Results.Ok(value);
    private static async Task<IResult> Write<T>(HttpContext http, Func<T, Task<SalesResult>> action, CancellationToken ct)
    {
        if (!http.Request.HasJsonContentType()) return Results.BadRequest();
        await using var buffer = new MemoryStream(); var chunk = new byte[4096]; int count;
        while ((count = await http.Request.Body.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + count > 16384) return Results.StatusCode(413);
            await buffer.WriteAsync(chunk.AsMemory(0, count), ct);
        }
        T? body;
        try { body = JsonSerializer.Deserialize<T>(buffer.ToArray(), new JsonSerializerOptions(JsonSerializerDefaults.Web)); }
        catch (JsonException) { return Results.BadRequest(); }
        if (body == null) return Results.BadRequest();
        var result = await action(body);
        return Results.Json(result.Value ?? new { message = result.Status switch {
            400 => "Revisá cantidades, precios, origen y motivo de la operación.",
            403 => "Se necesita un plan vigente para registrar una venta presencial nueva.",
            409 => "La operación no pudo confirmarse. Actualizá y revisá el stock, la versión o si ya existe una venta.",
            _ => "No se encontró la operación o el producto." } }, statusCode: result.Status);
    }
}
