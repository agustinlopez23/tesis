using Marketplace.BLL;
using Marketplace.DTOs;
using System.Text.Json;

namespace Marketplace.Api;

public static class OrderEndpoints
{
    public static void MapOrders(this WebApplication app)
    {
        app.MapPost("/public/businesses/{id:guid}/orders", async (Guid id, HttpContext http, IConfiguration config, OrderService orders, CancellationToken ct) =>
        {
            if (!Trusted(http.Request, config)) return Results.StatusCode(403);
            var (body, tooLarge) = await Read<CreateOrderRequest>(http.Request, ct);
            if (tooLarge) return Results.StatusCode(413);
            return body is null ? Results.BadRequest() : Result(await orders.CreateAsync(id, body, ct));
        }).RequireRateLimiting("orders-create");
        app.MapGet("/public/orders/{id:guid}", async (Guid id, HttpRequest request, OrderService orders, CancellationToken ct) =>
            Found(await orders.GuestAsync(id, request.Headers["X-Order-Key"].ToString(), ct))).RequireRateLimiting("orders-read");
        var owner = app.MapGroup("/businesses/{business:guid}/orders");
        owner.AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            if (http.Request.Method != "GET" && !Trusted(http.Request, http.RequestServices.GetRequiredService<IConfiguration>())) return Results.StatusCode(403);
            var user = await http.RequestServices.GetRequiredService<AuthenticationService>().GetSessionAsync(http.Request.Cookies["marketplace_session"], http.RequestAborted);
            if (user is null) return Results.Unauthorized();
            http.Items["order-owner"] = user.Id;
            return await next(context);
        });
        owner.MapGet("", async (Guid business, int? page, string? reference, string? status, HttpContext http, OrderService orders, CancellationToken ct) =>
            page is < 1 or > 10000 || reference?.Length > 40 || status is not (null or "pending" or "in_attention" or "cancelled")
                ? Results.BadRequest() : Found(await orders.ListAsync(business, Owner(http), page ?? 1, reference ?? "", status, ct)));
        owner.MapGet("/{id:guid}", async (Guid business, Guid id, HttpContext http, OrderService orders, CancellationToken ct) =>
            Found(await orders.OwnedAsync(business, id, Owner(http), ct)));
        owner.MapPut("/{id:guid}", async (Guid business, Guid id, HttpContext http, OrderService orders, CancellationToken ct) =>
        {
            var (body, tooLarge) = await Read<UpdateOrderRequest>(http.Request, ct);
            if (tooLarge) return Results.StatusCode(413);
            return body is null ? Results.BadRequest() : Result(await orders.UpdateAsync(business, id, Owner(http), body, ct));
        });
    }
    private static Guid Owner(HttpContext http) => (Guid)http.Items["order-owner"]!;
    private static bool Trusted(HttpRequest request, IConfiguration config) =>
        request.Headers.Origin == config["FrontendOrigin"] && request.Headers["X-Requested-With"] == "marketplace";
    private static IResult Found(object? value) => value is null ? Results.NotFound() : Results.Ok(value);
    private static IResult Result(OrderResult result) => result.Status switch
    {
        OrderResultStatus.Created => Results.Json(result.Order, statusCode: 201),
        OrderResultStatus.Saved => Results.Ok(result.Order),
        OrderResultStatus.NotFound => Results.NotFound(),
        OrderResultStatus.Unavailable => Results.Json(new { message = "Este negocio no está recibiendo nuevas solicitudes de productos." }, statusCode: 403),
        OrderResultStatus.Conflict => Results.Conflict(new { message = "Los datos cambiaron. Actualizá y revisá la selección o el estado antes de continuar." }),
        _ => Results.BadRequest(new { message = "Indicá tu nombre y entre 1 y 20 productos distintos, con cantidades de 1 a 99." })
    };
    private static async Task<(T? Body, bool TooLarge)> Read<T>(HttpRequest request, CancellationToken ct)
    {
        if (!request.HasJsonContentType()) return (default, false);
        await using var buffer = new MemoryStream();
        var chunk = new byte[4096];
        int read;
        while ((read = await request.Body.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > 16384) return (default, true);
            await buffer.WriteAsync(chunk.AsMemory(0, read), ct);
        }
        try { return (JsonSerializer.Deserialize<T>(buffer.ToArray(), new JsonSerializerOptions(JsonSerializerDefaults.Web)), false); }
        catch (JsonException) { return (default, false); }
    }
}
