using System.Text.Json;
using Marketplace.BLL;
using Marketplace.DTOs;

namespace Marketplace.Api;

public static class CampaignEndpoints
{
    public static void MapCampaigns(this WebApplication app)
    {
        var owner = app.MapGroup("/businesses/{business:guid}/campaigns").AddEndpointFilter(Authorize(false));
        var admin = app.MapGroup("/admin/campaigns").AddEndpointFilter(Authorize(true));
        owner.MapGet("", async (Guid business, int? page, HttpContext http, CampaignService campaigns, CancellationToken ct) =>
            page is < 1 or > 10000 ? Results.BadRequest() : Found(await campaigns.List(business, User(http).Id, false, page ?? 1, ct)));
        admin.MapGet("", async (int? page, HttpContext http, CampaignService campaigns, CancellationToken ct) =>
            page is < 1 or > 10000 ? Results.BadRequest() : Found(await campaigns.List(null, User(http).Id, true, page ?? 1, ct)));
        owner.MapPut("/{id:guid}", async (Guid business, Guid id, HttpContext http, CampaignService campaigns, CancellationToken ct) =>
            await Write<SaveCampaignRequest>(http, body => campaigns.Save(business, id, User(http).Id, body, ct), ct))
            .RecordsOwnerActivity(OwnerActivityKinds.PromotionUpdate, "business");
        owner.MapPut("/{id:guid}/pause", async (Guid business, Guid id, HttpContext http, CampaignService campaigns, CancellationToken ct) =>
            await Write<CampaignActionRequest>(http, body => campaigns.Act(business, id, User(http).Id, false, body, ct), ct));
        admin.MapPut("/{id:guid}/actions", async (Guid id, HttpContext http, CampaignService campaigns, CancellationToken ct) =>
            await Write<CampaignActionRequest>(http, body => campaigns.Act(null, id, User(http).Id, true, body, ct), ct));
        owner.MapGet("/{id:guid}/metrics", async (Guid business, Guid id, HttpContext http, CampaignService campaigns, CancellationToken ct) =>
            Found(await campaigns.Metrics(business, id, User(http).Id, ct)));
        app.MapGet("/public/campaigns", async (string? zone, CampaignService campaigns, CancellationToken ct) =>
            !CampaignService.Zone(zone) ? Results.BadRequest() : Results.Ok(new { items = await campaigns.Public(zone!, ct) }));
        app.MapGet("/public/campaigns/{id:guid}", async (Guid id, CampaignService campaigns, CancellationToken ct) => Found(await campaigns.Detail(id, ct)));
        app.MapGet("/public/campaigns/{id:guid}/image", async (Guid id, HttpContext http, CampaignService campaigns, AuthenticationService auth, CancellationToken ct) => {
            var user = await auth.GetSessionAsync(http.Request.Cookies["marketplace_session"], ct);
            var content = await campaigns.Image(id, user?.Id, user?.Roles.Contains("administrator") == true, ct);
            http.Response.Headers["X-Content-Type-Options"] = "nosniff";
            return content == null ? Results.NotFound() : Results.Bytes(content, "image/jpeg");
        });
        app.MapPost("/public/campaign-events", async (HttpContext http, ICampaignTokens tokens, CampaignService campaigns, AuthenticationService auth, IConfiguration config, CancellationToken ct) => {
            if (!Trusted(http, config)) return Results.StatusCode(403);
            var (body, status) = await Read<CampaignEventRequest>(http.Request, ct);
            if (status != 200) return Results.StatusCode(status);
            var context = tokens.Read(body!.Token);
            if (context == null || body.Kind is not ("impression" or "click")) return Results.BadRequest();
            var user = await auth.GetSessionAsync(http.Request.Cookies["marketplace_session"], ct);
            await campaigns.Record(context, body.Kind, user?.Id, ct);
            return Results.NoContent();
        }).RequireRateLimiting("activity");
    }
    private static bool Trusted(HttpContext http, IConfiguration config) => http.Request.Headers.Origin == config["FrontendOrigin"] && http.Request.Headers["X-Requested-With"] == "marketplace";
    private static Func<EndpointFilterInvocationContext, EndpointFilterDelegate, ValueTask<object?>> Authorize(bool admin) => async (context, next) => {
        var http = context.HttpContext;
        if (http.Request.Method != "GET" && !Trusted(http, http.RequestServices.GetRequiredService<IConfiguration>())) return Results.StatusCode(403);
        var user = await http.RequestServices.GetRequiredService<AuthenticationService>().GetSessionAsync(http.Request.Cookies["marketplace_session"], http.RequestAborted);
        if (user == null) return Results.Unauthorized();
        if (admin && !user.Roles.Contains("administrator")) return Results.StatusCode(403);
        http.Items["campaign-user"] = user; return await next(context);
    };
    private static OwnerIdentityResponse User(HttpContext http) => (OwnerIdentityResponse)http.Items["campaign-user"]!;
    private static IResult Found(object? value) => value == null ? Results.NotFound() : Results.Ok(value);
    private static async Task<(T? Body, int Status)> Read<T>(HttpRequest request, CancellationToken ct)
    {
        if (!request.HasJsonContentType()) return (default, 400);
        var bytes = new byte[16385]; var count = await request.Body.ReadAtLeastAsync(bytes, bytes.Length, false, ct);
        if (count > 16384) return (default, 413);
        try { var body = JsonSerializer.Deserialize<T>(bytes.AsSpan(0, count), JsonSerializerOptions.Web); return (body, body == null ? 400 : 200); }
        catch (JsonException) { return (default, 400); }
    }
    private static async Task<IResult> Write<T>(HttpContext http, Func<T, Task<SalesResult>> action, CancellationToken ct)
    {
        var (body, status) = await Read<T>(http.Request, ct); if (status != 200) return Results.StatusCode(status);
        var result = await action(body!);
        return Results.Json(result.Value ?? new { message = result.Status switch {
            400 => "Revisá contenido, imagen de promoción, zona, motivo y fechas (hasta 30 días).",
            403 => "Las campañas de productos requieren un plan vigente.",
            409 => "No se pudo guardar: cambió la versión, el estado o no hay cupo para esa zona y período. Actualizá antes de reintentar.",
            _ => "La campaña o el contenido no están disponibles." } }, statusCode: result.Status);
    }
}
