using Marketplace.BLL;
using Marketplace.DTOs;
using Microsoft.AspNetCore.Http.Features;

namespace Marketplace.Api;

public static class CatalogEndpoints
{
    public static void MapCatalog(this WebApplication app)
    {
        // A shared filter validates sessions, live roles and CSRF before any private handler.
        var owner = app.MapGroup("/businesses/{id:guid}").AddEndpointFilter(Authorize(false));
        var admin = app.MapGroup("/admin").AddEndpointFilter(Authorize(true));
        owner.MapGet("/plan", async (Guid id, HttpContext http, CatalogService catalog, CancellationToken ct) =>
            Found(await catalog.PlanAsync(id, User(http).Id, ct)));
        owner.MapGet("/products", async (Guid id, HttpContext http, CatalogService catalog, CancellationToken ct) =>
            Found(await catalog.OwnedAsync(id, User(http).Id, ct)));
        owner.MapPut("/products/{product:guid}", async (Guid id, Guid product, SaveProductRequest body, HttpContext http, CatalogService catalog, CancellationToken ct) =>
            Result(await catalog.SaveAsync(id, product, User(http).Id, body, ct)));
        owner.MapGet("/products/{product:guid}/photo", async (Guid id, Guid product, HttpContext http, CatalogService catalog, CancellationToken ct) =>
            Photo(await catalog.ReadPhotoAsync(id, product, User(http).Id, ct), http));
        owner.MapPut("/products/{product:guid}/photo", Upload);
        owner.MapDelete("/products/{product:guid}/photo", async (Guid id, Guid product, int version, HttpContext http, CatalogService catalog, CancellationToken ct) =>
            Result(await catalog.PhotoAsync(id, product, User(http).Id, version, null, ct)));

        admin.MapGet("/businesses", async (string? q, int? page, CatalogService catalog, CancellationToken ct) =>
            q?.Length > 150 || page is < 1 or > 10000 ? Results.BadRequest() : Results.Ok(await catalog.AdminBusinessesAsync(q?.Trim() ?? "", page ?? 1, ct)));
        admin.MapGet("/businesses/{id:guid}/plan", async (Guid id, CatalogService catalog, CancellationToken ct) => Found(await catalog.PlanAsync(id, null, ct)));
        admin.MapPut("/businesses/{id:guid}/subscriptions/{subscription:guid}", async (
            Guid id, Guid subscription, GrantSubscriptionRequest body, HttpContext http, CatalogService catalog, CancellationToken ct) =>
            Result(new(await catalog.GrantAsync(id, subscription, User(http).Id, body, ct))));
        admin.MapPost("/businesses/{id:guid}/subscriptions/{subscription:guid}/revoke", async (
            Guid id, Guid subscription, RevokeSubscriptionRequest body, HttpContext http, CatalogService catalog, CancellationToken ct) =>
            Result(new(await catalog.RevokeAsync(id, subscription, User(http).Id, body.Reason, ct))));

        var publicCatalog = app.MapGroup("/public/businesses/{id:guid}/products");
        publicCatalog.AddEndpointFilter(async (context, next) => {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            return await next(context);
        });
        publicCatalog.MapGet("", async (Guid id, int? page, string? q, CatalogService catalog, CancellationToken ct) =>
            page is < 1 or > 10000 || q?.Length > 150 ? Results.BadRequest() : Found(await catalog.PublicAsync(id, page ?? 1, q?.Trim() ?? "", ct)));
        publicCatalog.MapGet("/{product:guid}/photo", async (Guid id, Guid product, HttpContext http, CatalogService catalog, CancellationToken ct) =>
            Photo(await catalog.ReadPhotoAsync(id, product, null, ct), http));
    }

    private static Func<EndpointFilterInvocationContext, EndpointFilterDelegate, ValueTask<object?>> Authorize(bool admin) => async (context, next) =>
    {
        var http = context.HttpContext;
        http.Response.Headers.CacheControl = "no-store";
        var config = http.RequestServices.GetRequiredService<IConfiguration>();
        if (http.Request.Method != "GET" && (http.Request.Headers.Origin != config["FrontendOrigin"] ||
            http.Request.Headers["X-Requested-With"] != "marketplace")) return Results.StatusCode(403);
        var user = await http.RequestServices.GetRequiredService<AuthenticationService>().GetSessionAsync(http.Request.Cookies["marketplace_session"], http.RequestAborted);
        if (user is null) return Results.Unauthorized();
        if (admin && !user.Roles.Contains("administrator")) return Results.StatusCode(403);
        http.Items["catalog-user"] = user;
        return await next(context);
    };
    private static OwnerIdentityResponse User(HttpContext http) => (OwnerIdentityResponse)http.Items["catalog-user"]!;
    private static IResult Found(object? value) => value is null ? Results.NotFound() : Results.Ok(value);
    private static IResult Photo(byte[]? bytes, HttpContext http)
    {
        http.Response.Headers["X-Content-Type-Options"] = "nosniff";
        return bytes is null ? Results.NotFound() : Results.Bytes(bytes, "image/jpeg");
    }
    private static IResult Result(CatalogResult result) => result.Status switch
    {
        CatalogStatus.Saved => Results.Ok((object?)result.Product ?? new { saved = true }),
        CatalogStatus.NotFound => Results.NotFound(),
        CatalogStatus.PlanRequired => Results.Json(new { message = "El catálogo requiere un plan Impulso vigente. Tus productos se conservan." }, statusCode: 403),
        CatalogStatus.Conflict => Results.Conflict(new { message = "Los datos cambiaron o la vigencia se superpone. Actualizá antes de volver a guardar." }),
        CatalogStatus.LimitReached => Results.Conflict(new { message = "Este prototipo admite hasta 200 productos por negocio, incluidos los archivados." }),
        _ => Results.BadRequest(new { message = "Revisá los datos: nombre, categoría, precio con hasta dos decimales, moneda y estado válidos." })
    };
    private static async Task<IResult> Upload(Guid id, Guid product, HttpContext http, CatalogService catalog, CancellationToken ct)
    {
        var request = http.Request;
        if (request.ContentLength > BusinessProfileService.MaxBytes + 65536) return Results.StatusCode(413);
        if (!request.HasFormContentType) return Results.BadRequest();
        // Ownership and plan checked before decoding, and again transactionally before storing.
        var plan = await catalog.PlanAsync(id, User(http).Id, ct);
        if (plan is null) return Results.NotFound();
        if (!plan.CatalogEnabled) return Result(new(CatalogStatus.PlanRequired));
        try
        {
            var form = await request.ReadFormAsync(new FormOptions { MultipartBodyLengthLimit = BusinessProfileService.MaxBytes + 1, ValueLengthLimit = 128, ValueCountLimit = 2 }, ct);
            var file = form.Files.GetFile("file");
            if (file is null || form.Files.Count != 1 || !int.TryParse(form["version"], out var version)) return Results.BadRequest();
            if (file.Length > BusinessProfileService.MaxBytes) return Results.StatusCode(413);
            await using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, ct);
            var result = await catalog.PhotoAsync(id, product, User(http).Id, version, buffer.ToArray(), ct);
            return result.Status == CatalogStatus.Invalid ? Results.BadRequest(new { message = "Elegí una foto JPEG, PNG o WebP válida, de hasta 5 MB y 20 megapíxeles." }) : Result(result);
        }
        catch (InvalidDataException) { return Results.BadRequest(new { message = "La foto no es válida o supera 5 MB." }); }
        catch (BadHttpRequestException error) { return Results.StatusCode(error.StatusCode); }
    }
}
