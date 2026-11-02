using Marketplace.BLL;
using Marketplace.DTOs;

namespace Marketplace.Api;

public static class AdminEndpoints
{
    public static void MapAdministration(this WebApplication app)
    {
        var admin = app.MapGroup("/admin").AddEndpointFilter(Authorize);
        admin.MapGet("/categories", async (AdminService service, CancellationToken ct) => Results.Ok(await service.CategoriesAsync(ct)));
        admin.MapPost("/categories", async (SaveCategoryRequest body, HttpContext http, AdminService service, CancellationToken ct) =>
            Category(await service.CreateCategoryAsync(User(http).Id, body, ct), StatusCodes.Status201Created));
        admin.MapPut("/categories/{id:int}", async (int id, SaveCategoryRequest body, HttpContext http, AdminService service, CancellationToken ct) =>
            id is < 1 or > short.MaxValue ? Results.NotFound() : Category(await service.UpdateCategoryAsync(User(http).Id, (short)id, body, ct), StatusCodes.Status200OK));
        admin.MapDelete("/categories/{id:int}", async (int id, HttpContext http, AdminService service, CancellationToken ct) =>
        {
            if (id is < 1 or > short.MaxValue) return Results.NotFound(new { message = "No encontramos el rubro." });
            var result = await service.DeleteCategoryAsync(User(http).Id, (short)id, ct);
            return result.Status switch
            {
                AdminStatus.Saved => Results.NoContent(),
                AdminStatus.NotFound => Results.NotFound(new { message = "No encontramos el rubro." }),
                _ => Results.Conflict(new { code = "category_in_use", usageCount = result.UsageCount, message = AdminService.CategoryInUseMessage(result.UsageCount) })
            };
        });
        admin.MapGet("/moderation/{target}", async (string target, string? q, int? page, AdminService service, CancellationToken ct) =>
        {
            if (q?.Length > 150 || page is < 1 or > 10000) return Results.BadRequest();
            var query = q?.Trim() ?? "";
            return AdminService.Target(target) switch
            {
                ModerationTarget.Account => Results.Ok(await service.AccountsAsync(query, page ?? 1, ct)),
                ModerationTarget.Business => Results.Ok(await service.BusinessesAsync(query, page ?? 1, ct)),
                ModerationTarget.Product => Results.Ok(await service.ProductsAsync(query, page ?? 1, ct)),
                _ => Results.NotFound()
            };
        });
        admin.MapPost("/moderation/{target}/{id:guid}/{action}", async (string target, Guid id, string action, AdminReasonRequest body,
            HttpContext http, AdminService service, CancellationToken ct) =>
        {
            var kind = AdminService.Target(target);
            if (kind is null || action is not ("suspend" or "reinstate")) return Results.NotFound();
            return await service.ModerateAsync(User(http).Id, kind.Value, id, action == "suspend", body, ct) switch
            {
                AdminStatus.Saved => Results.Ok(new { saved = true }),
                AdminStatus.Invalid => Results.BadRequest(new { message = "Indicá un motivo de entre 3 y 500 caracteres." }),
                AdminStatus.NotFound => Results.NotFound(new { message = "No encontramos el elemento a moderar." }),
                _ => Results.Conflict(new { message = "El estado ya cambió o la cuenta es administradora. Actualizá la lista." })
            };
        });
        admin.MapGet("/audit", async (int? page, AdminService service, CancellationToken ct) =>
            page is < 1 or > 10000 ? Results.BadRequest() : Results.Ok(await service.AuditAsync(page ?? 1, ct)));
    }

    // Same contract as the other admin groups: trusted origin for mutations, live session and live role.
    private static async ValueTask<object?> Authorize(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        http.Response.Headers.CacheControl = "no-store";
        var config = http.RequestServices.GetRequiredService<IConfiguration>();
        if (http.Request.Method != "GET" && (http.Request.Headers.Origin != config["FrontendOrigin"] ||
            http.Request.Headers["X-Requested-With"] != "marketplace")) return Results.StatusCode(StatusCodes.Status403Forbidden);
        var user = await http.RequestServices.GetRequiredService<AuthenticationService>()
            .GetSessionAsync(http.Request.Cookies["marketplace_session"], http.RequestAborted);
        if (user is null) return Results.Unauthorized();
        if (!user.Roles.Contains("administrator")) return Results.StatusCode(StatusCodes.Status403Forbidden);
        http.Items["admin-user"] = user;
        return await next(context);
    }

    private static OwnerIdentityResponse User(HttpContext http) => (OwnerIdentityResponse)http.Items["admin-user"]!;

    private static IResult Category(AdminCategoryResult result, int successStatus) => result.Status switch
    {
        AdminStatus.Saved => Results.Json(result.Category, statusCode: successStatus),
        AdminStatus.Invalid => Results.BadRequest(new { message = "Escribí un nombre de rubro de 2 a 80 caracteres." }),
        AdminStatus.NotFound => Results.NotFound(new { message = "No encontramos el rubro." }),
        _ => Results.Conflict(new { message = "Ya existe un rubro con ese nombre." })
    };
}
